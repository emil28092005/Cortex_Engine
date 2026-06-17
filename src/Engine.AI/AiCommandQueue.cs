using System.Collections.Concurrent;
using System.Text.Json;
using Engine.AI.Commands;
using Engine.Core;

namespace Engine.AI;

/// <summary>
/// Thread-safe queue that marshals AI commands from the MCP server thread
/// to the main engine thread where the Flecs world is touched.
/// </summary>
public sealed class AiCommandQueue
{
    private readonly ConcurrentQueue<(string commandJson, TaskCompletionSource<AiCommandResult> tcs)> _queue = new();
    private readonly AiCommandProcessor _processor;
    private readonly IScreenshotProvider _screenshot;
    private readonly JsonSerializerOptions _jsonOptions;
    private (TaskCompletionSource<AiCommandResult> tcs, Task<byte[]> screenshotTask, string path)? _pendingScreenshot;

    public AiCommandQueue(AiCommandProcessor processor, IScreenshotProvider screenshot)
    {
        _processor = processor;
        _screenshot = screenshot;
        _jsonOptions = processor.JsonOptions;
    }

    /// <summary>
    /// Enqueue a command object. The returned task completes on the main thread
    /// when the command has been processed.
    /// </summary>
    public Task<AiCommandResult> EnqueueAsync(AiCommand command)
    {
        var json = JsonSerializer.Serialize<AiCommand>(command, _jsonOptions);
        return EnqueueJsonAsync(json);
    }

    /// <summary>
    /// Enqueue a raw JSON command string.
    /// </summary>
    public Task<AiCommandResult> EnqueueJsonAsync(string commandJson)
    {
        var tcs = new TaskCompletionSource<AiCommandResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        _queue.Enqueue((commandJson, tcs));
        return tcs.Task;
    }

    /// <summary>
    /// Process all pending commands. Must be called on the main engine thread.
    /// Returns the number of processed commands.
    /// </summary>
    public int ProcessPending()
    {
        int processed = 0;
        while (_queue.TryDequeue(out var item))
        {
            var command = JsonSerializer.Deserialize<AiCommand>(item.commandJson, _jsonOptions);
            if (command is CaptureScreenshotCommand screenshotCommand)
            {
                // Screenshot commands are handled asynchronously because the frame must be rendered
                // before the PNG bytes are available. CompletePendingScreenshots must be called after
                // the renderer has presented the frame.
                if (_pendingScreenshot.HasValue)
                {
                    item.tcs.TrySetResult(AiCommandResult.Error("Another screenshot request is already pending."));
                    continue;
                }

                var path = screenshotCommand.OutputPath ?? $"screenshot_{DateTime.UtcNow:yyyyMMdd_HHmmss_fff}.png";
                var screenshotTask = _screenshot.CaptureAsync(path);
                _pendingScreenshot = (item.tcs, screenshotTask, path);
            }
            else
            {
                var result = _processor.Process(item.commandJson);
                item.tcs.TrySetResult(result);
            }
            processed++;
        }
        return processed;
    }

    /// <summary>
    /// Completes any pending screenshot requests that have finished rendering.
    /// Must be called on the main engine thread after the frame has been presented.
    /// </summary>
    public void CompletePendingScreenshots()
    {
        if (_pendingScreenshot == null || !_pendingScreenshot.Value.screenshotTask.IsCompleted)
            return;

        var (tcs, screenshotTask, path) = _pendingScreenshot.Value;
        _pendingScreenshot = null;

        try
        {
            var bytes = screenshotTask.Result;
            var base64 = Convert.ToBase64String(bytes);
            var json = $"{{\"path\":{JsonSerializer.Serialize(path)},\"base64\":{JsonSerializer.Serialize(base64)}}}";
            tcs.TrySetResult(AiCommandResult.Ok(json));
        }
        catch (Exception ex)
        {
            tcs.TrySetResult(AiCommandResult.Error($"Screenshot capture failed: {ex.Message}"));
        }
    }

    /// <summary>
    /// Number of commands waiting to be processed.
    /// </summary>
    public int PendingCount => _queue.Count;
}

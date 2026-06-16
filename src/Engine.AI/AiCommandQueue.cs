using System.Collections.Concurrent;
using System.Text.Json;
using Engine.AI.Commands;

namespace Engine.AI;

/// <summary>
/// Thread-safe queue that marshals AI commands from the MCP server thread
/// to the main engine thread where the Flecs world is touched.
/// </summary>
public sealed class AiCommandQueue
{
    private readonly ConcurrentQueue<(string commandJson, TaskCompletionSource<AiCommandResult> tcs)> _queue = new();
    private readonly AiCommandProcessor _processor;
    private readonly JsonSerializerOptions _jsonOptions;

    public AiCommandQueue(AiCommandProcessor processor)
    {
        _processor = processor;
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
            var result = _processor.Process(item.commandJson);
            item.tcs.TrySetResult(result);
            processed++;
        }
        return processed;
    }

    /// <summary>
    /// Number of commands waiting to be processed.
    /// </summary>
    public int PendingCount => _queue.Count;
}

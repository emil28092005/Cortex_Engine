using System.ComponentModel;
using System.Numerics;
using Engine.AI.Commands;
using ModelContextProtocol.Server;

namespace Engine.AI.Mcp;

/// <summary>
/// MCP tools that expose engine commands to AI agents.
/// </summary>
[McpServerToolType]
public sealed class EngineMcpTools
{
    private readonly AiCommandQueue _queue;

    public EngineMcpTools(AiCommandQueue queue)
    {
        _queue = queue;
    }

    [McpServerTool, Description("Spawn a 3D model entity in the engine world.")]
    public Task<string> SpawnModel(
        string name,
        string modelPath,
        [Description("Optional position as [x, y, z] (default: 0,0,0)")] IReadOnlyList<double>? position = null,
        [Description("Optional rotation as [x, y, z, w] quaternion (default: identity)")] IReadOnlyList<double>? rotation = null,
        [Description("Optional scale as [x, y, z] (default: 1,1,1)")] IReadOnlyList<double>? scale = null,
        [Description("Optional: enable physics (default: false)")] bool physics = false)
    {
        var cmd = new SpawnModelCommand
        {
            Name = name,
            ModelPath = modelPath,
            Position = ToVector3(position, Vector3.Zero),
            Rotation = ToQuaternion(rotation, Quaternion.Identity),
            Scale = ToVector3(scale, Vector3.One),
            Physics = physics
        };

        return EnqueueAndReturnMessage(cmd);
    }

    [McpServerTool, Description("Update the transform of an existing entity by name.")]
    public Task<string> SetTransform(
        string name,
        [Description("Optional position as [x, y, z]")] IReadOnlyList<double>? position = null,
        [Description("Optional rotation as [x, y, z, w] quaternion")] IReadOnlyList<double>? rotation = null,
        [Description("Optional scale as [x, y, z]")] IReadOnlyList<double>? scale = null)
    {
        var cmd = new SetTransformCommand
        {
            Name = name,
            Position = ToVector3(position),
            Rotation = ToQuaternion(rotation),
            Scale = ToVector3(scale)
        };

        return EnqueueAndReturnMessage(cmd);
    }

    [McpServerTool, Description("Delete an entity by name.")]
    public Task<string> DeleteEntity(string name)
    {
        var cmd = new DeleteEntityCommand { Name = name };
        return EnqueueAndReturnMessage(cmd);
    }

    [McpServerTool, Description("List all named entities in the ECS world.")]
    public Task<string> ListEntities()
    {
        var cmd = new ListEntitiesCommand();
        return EnqueueAndReturnMessage(cmd);
    }

    [McpServerTool, Description("Capture a screenshot of the current rendered frame and return it as a base64-encoded PNG. The image is also saved to disk.")]
    public Task<string> CaptureScreenshot([Description("Optional output file path (default: screenshot_<timestamp>.png)")] string? outputPath = null)
    {
        var cmd = new CaptureScreenshotCommand { OutputPath = outputPath };
        return EnqueueAndReturnMessage(cmd);
    }

    [McpServerTool, Description("Dump the current ECS world state as JSON, including Transform, Camera, Material, Light, and Mesh component summaries.")]
    public Task<string> GetWorldState()
    {
        var cmd = new GetWorldStateCommand();
        return EnqueueAndReturnMessage(cmd);
    }

    [McpServerTool, Description("Update the material of an existing entity by name.")]
    public Task<string> SetMaterial(
        string name,
        [Description("Optional albedo color as [r, g, b] (0-1)")] IReadOnlyList<double>? albedo = null,
        [Description("Optional roughness value (0-1)")] float? roughness = null,
        [Description("Optional metallic value (0-1)")] float? metallic = null,
        [Description("Optional path to a PNG texture file")] string? texturePath = null)
    {
        var cmd = new SetMaterialCommand
        {
            Name = name,
            Albedo = ToVector3(albedo),
            Roughness = roughness,
            Metallic = metallic,
            TexturePath = texturePath
        };

        return EnqueueAndReturnMessage(cmd);
    }

    private async Task<string> EnqueueAndReturnMessage(AiCommand command)
    {
        var result = await _queue.EnqueueAsync(command).ConfigureAwait(false);
        return result.Success
            ? result.Message
            : throw new InvalidOperationException(result.Message);
    }

    private static Vector3 ToVector3(IReadOnlyList<double>? values, Vector3 defaultValue)
    {
        if (values == null || values.Count == 0)
            return defaultValue;

        if (values.Count != 3)
            throw new ArgumentException("Expected 3 values for Vector3.", nameof(values));

        return new Vector3((float)values[0], (float)values[1], (float)values[2]);
    }

    private static Vector3? ToVector3(IReadOnlyList<double>? values)
    {
        if (values == null || values.Count == 0)
            return null;

        if (values.Count != 3)
            throw new ArgumentException("Expected 3 values for Vector3.", nameof(values));

        return new Vector3((float)values[0], (float)values[1], (float)values[2]);
    }

    private static Quaternion ToQuaternion(IReadOnlyList<double>? values, Quaternion defaultValue)
    {
        if (values == null || values.Count == 0)
            return defaultValue;

        if (values.Count != 4)
            throw new ArgumentException("Expected 4 values for Quaternion.", nameof(values));

        return new Quaternion((float)values[0], (float)values[1], (float)values[2], (float)values[3]);
    }

    private static Quaternion? ToQuaternion(IReadOnlyList<double>? values)
    {
        if (values == null || values.Count == 0)
            return null;

        if (values.Count != 4)
            throw new ArgumentException("Expected 4 values for Quaternion.", nameof(values));

        return new Quaternion((float)values[0], (float)values[1], (float)values[2], (float)values[3]);
    }
}

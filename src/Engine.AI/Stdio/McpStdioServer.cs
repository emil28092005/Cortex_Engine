using System.Text;
using System.Text.Json;
using Engine.AI.Commands;

namespace Engine.AI.Stdio;

/// <summary>
/// A minimal stdio MCP server that routes JSON-RPC requests to an <see cref="AiCommandProcessor"/>.
/// This is suitable for clients like Claude Desktop that speak MCP over stdio.
/// </summary>
public sealed class McpStdioServer
{
    private readonly AiCommandProcessor _processor;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly Dictionary<string, ToolDefinition> _tools;

    public McpStdioServer(AiCommandProcessor processor)
    {
        _processor = processor;
        _jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        _tools = new Dictionary<string, ToolDefinition>
        {
            ["SpawnModel"] = new(
                "Spawn a 3D model entity in the engine world.",
                new JsonSchemaBuilder()
                    .AddRequiredString("name")
                    .AddRequiredString("modelPath")
                    .AddOptionalArray("position", "number", 3)
                    .AddOptionalArray("rotation", "number", 4)
                    .AddOptionalArray("scale", "number", 3)
                    .Build()),
            ["SetTransform"] = new(
                "Update the transform of an existing entity by name.",
                new JsonSchemaBuilder()
                    .AddRequiredString("name")
                    .AddOptionalArray("position", "number", 3)
                    .AddOptionalArray("rotation", "number", 4)
                    .AddOptionalArray("scale", "number", 3)
                    .Build()),
            ["SetMaterial"] = new(
                "Update the material of an existing entity by name.",
                new JsonSchemaBuilder()
                    .AddRequiredString("name")
                    .AddOptionalArray("albedo", "number", 3)
                    .AddOptionalNumber("roughness")
                    .AddOptionalNumber("metallic")
                    .AddOptionalString("texturePath")
                    .Build()),
            ["DeleteEntity"] = new(
                "Delete an entity by name.",
                new JsonSchemaBuilder()
                    .AddRequiredString("name")
                    .Build()),
            ["ListEntities"] = new(
                "List all named entities in the ECS world.",
                new JsonSchemaBuilder().Build()),
            ["CaptureScreenshot"] = new(
                "Capture a screenshot of the current rendered frame and save it to disk. (No graphics context in stdio mode; returns requested path.)",
                new JsonSchemaBuilder()
                    .AddOptionalString("outputPath")
                    .Build()),
            ["GetWorldState"] = new(
                "Dump the current ECS world state as JSON.",
                new JsonSchemaBuilder().Build())
        };
    }

    public void Run()
    {
        var input = Console.OpenStandardInput();
        var output = Console.OpenStandardOutput();
        using var reader = new StreamReader(input, Encoding.UTF8);
        using var writer = new StreamWriter(output, Encoding.UTF8) { AutoFlush = true };

        while (true)
        {
            var line = reader.ReadLine();
            if (line == null)
                break;

            if (string.IsNullOrWhiteSpace(line))
                continue;

            var response = HandleMessage(line);
            if (response != null)
            {
                writer.WriteLine(JsonSerializer.Serialize(response, _jsonOptions));
            }
        }
    }

    private JsonElement? HandleMessage(string line)
    {
        using var document = JsonDocument.Parse(line);
        var root = document.RootElement;

        var method = root.GetProperty("method").GetString();
        var id = root.TryGetProperty("id", out var idProp) ? (JsonElement?)idProp : null;

        switch (method)
        {
            case "initialize":
                return MakeResponse(id, new
                {
                    protocolVersion = "2024-11-05",
                    capabilities = new { tools = new { } },
                    serverInfo = new { name = "CortexEngine", version = "0.1.0" }
                });

            case "notifications/initialized":
                return null;

            case "tools/list":
                return MakeResponse(id, new { tools = _tools.Select(t => new { type = "function", function = new { name = t.Key, description = t.Value.Description, parameters = t.Value.Parameters } }).ToList() });

            case "tools/call":
                return HandleToolCall(id, root.GetProperty("params"));

            case "ping":
                return MakeResponse(id, new { });

            default:
                return MakeError(id, -32601, $"Method not found: {method}");
        }
    }

    private JsonElement? HandleToolCall(JsonElement? id, JsonElement paramsElement)
    {
        var name = paramsElement.GetProperty("name").GetString();
        var arguments = paramsElement.GetProperty("arguments");

        if (!_tools.TryGetValue(name ?? string.Empty, out _))
            return MakeError(id, -32601, $"Tool not found: {name}");

        var command = BuildCommand(name!, arguments);
        var result = _processor.Process(command);

        return MakeResponse(id, new { content = new[] { new { type = "text", text = result.Message } }, isError = !result.Success });
    }

    private string BuildCommand(string toolName, JsonElement arguments)
    {
        var type = toolName switch
        {
            "SpawnModel" => "spawn_model",
            "SetTransform" => "set_transform",
            "SetMaterial" => "set_material",
            "DeleteEntity" => "delete_entity",
            "ListEntities" => "list_entities",
            "CaptureScreenshot" => "capture_screenshot",
            "GetWorldState" => "get_world_state",
            _ => toolName.ToLowerInvariant()
        };

        var dict = new Dictionary<string, object> { ["type"] = type };
        foreach (var property in arguments.EnumerateObject())
            dict[property.Name] = ConvertArgument(property.Value);

        return JsonSerializer.Serialize(dict, _jsonOptions);
    }

    private static object ConvertArgument(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString()!,
            JsonValueKind.Number => element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Array => element.EnumerateArray().Select(ConvertArgument).ToList(),
            JsonValueKind.Object => element.EnumerateObject().ToDictionary(p => p.Name, p => ConvertArgument(p.Value)),
            _ => element.GetRawText()
        };
    }

    private JsonElement? MakeResponse(JsonElement? id, object result)
    {
        if (id == null)
            return null;

        var json = JsonSerializer.Serialize(new { jsonrpc = "2.0", result, id = id.Value }, _jsonOptions);
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.Clone();
    }

    private JsonElement? MakeError(JsonElement? id, int code, string message)
    {
        if (id == null)
            return null;

        var json = JsonSerializer.Serialize(new { jsonrpc = "2.0", error = new { code, message }, id = id.Value }, _jsonOptions);
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.Clone();
    }

    private sealed record ToolDefinition(string Description, JsonElement Parameters);

    private sealed class JsonSchemaBuilder
    {
        private readonly Dictionary<string, object> _properties = new();
        private readonly List<string> _required = new();
        private readonly string _type = "object";

        public JsonSchemaBuilder AddRequiredString(string name)
        {
            _properties[name] = new { type = "string" };
            _required.Add(name);
            return this;
        }

        public JsonSchemaBuilder AddOptionalString(string name)
        {
            _properties[name] = new { type = "string" };
            return this;
        }

        public JsonSchemaBuilder AddOptionalNumber(string name)
        {
            _properties[name] = new { type = "number" };
            return this;
        }

        public JsonSchemaBuilder AddOptionalArray(string name, string itemType, int? minItems = null)
        {
            _properties[name] = new { type = "array", items = new { type = itemType }, minItems };
            return this;
        }

        public JsonElement Build()
        {
            var dict = new Dictionary<string, object>
            {
                ["type"] = _type,
                ["properties"] = _properties,
                ["required"] = _required
            };
            var json = JsonSerializer.Serialize(dict);
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.Clone();
        }
    }

}

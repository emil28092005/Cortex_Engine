using System.Text.Json.Serialization;

namespace Engine.AI.Commands;

/// <summary>
/// Base class for AI commands sent to the engine as JSON.
/// The discriminator is the <c>type</c> property.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(SpawnModelCommand), "spawn_model")]
[JsonDerivedType(typeof(SetTransformCommand), "set_transform")]
[JsonDerivedType(typeof(DeleteEntityCommand), "delete_entity")]
[JsonDerivedType(typeof(ListEntitiesCommand), "list_entities")]
public abstract record AiCommand
{
    public string Type => GetType().Name.Replace("Command", "").ToLowerInvariant();
}

namespace YouTubeBroadcastCreator.Util.Serialization.Schema;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Class | AttributeTargets.Struct)]
public class SchemaTypeAttribute(string types, bool removeProperties = true) : Attribute
{
    /// <summary>
    /// Comma separated type list
    /// </summary>
    public string Types { get; } = types;
    public bool RemoveProperties { get; } = removeProperties;
}
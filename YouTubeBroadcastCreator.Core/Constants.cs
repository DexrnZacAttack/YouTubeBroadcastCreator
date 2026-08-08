using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Schema;
using YouTubeBroadcastCreator.Core.Util.Serialization.Schema;

namespace YouTubeBroadcastCreator.Core;

public class Constants
{
    public const string ProgramIdentifier = "me.dexrn.YouTubeBroadcastCreator";

    public static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        WriteIndented = true
    };
    
    //derived from https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/extract-schema
    public static readonly JsonSchemaExporterOptions JsonSchemaExporterOptions = new()
    {
        TransformSchemaNode = (context, schema) =>
        {
            // Determine if a type or property and extract the relevant attribute provider.
            ICustomAttributeProvider? attributeProvider = context.PropertyInfo is not null
                                                              ? context.PropertyInfo.AttributeProvider
                                                              : context.TypeInfo.Type;

            // Look up any description attributes.
            DescriptionAttribute? descriptionAttr = attributeProvider?
                                                   .GetCustomAttributes(inherit: true)
                                                   .Select(attr => attr as DescriptionAttribute)
                                                   .FirstOrDefault(attr => attr is not null);
            
            //our attr
            SchemaTypeAttribute? schemaAttr = attributeProvider?
                                                   .GetCustomAttributes(inherit: true)
                                                   .Select(attr => attr as SchemaTypeAttribute)
                                                   .FirstOrDefault(attr => attr is not null);

            if (schemaAttr != null && schema is JsonObject o)
            {
                //hack!!!
                string[] types = schemaAttr.Types.Split(',');

                if (types.Length > 1)
                {
                    o["type"] = new JsonArray(types.Select(s => (JsonNode?)s.Trim()).ToArray());
                }
                else if (types.Length == 1)
                {
                    o["type"] = types[0];
                }

                if (schemaAttr.RemoveProperties)
                {
                    o.Remove("properties");
                    o.Remove("required");
                    o.Remove("additionalProperties");
                }
            }
            
            // Apply description attribute to the generated schema.
            if (descriptionAttr != null)
            {
                if (schema is not JsonObject jObj)
                {
                    // Handle the case where the schema is a Boolean.
                    JsonValueKind valueKind = schema.GetValueKind();
                    Debug.Assert(valueKind is JsonValueKind.True or JsonValueKind.False);
                    schema = jObj = new JsonObject();
                    if (valueKind is JsonValueKind.False)
                    {
                        jObj.Add("not", true);
                    }
                }

                jObj.Insert(0, "description", descriptionAttr.Description);
            }

            return schema;
        }
    };
}
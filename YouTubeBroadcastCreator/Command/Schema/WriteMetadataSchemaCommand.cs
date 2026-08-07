using DotMake.CommandLine;
using YouTubeBroadcastCreator.Core;

namespace YouTubeBroadcastCreator.Command.Schema;

[CliCommand(Name = "metadata", Parent = typeof(WriteSchemaCommand), Description = "Writes the metadata JSON schema file and exits.", ShortFormAutoGenerate = CliNameAutoGenerate.Arguments | CliNameAutoGenerate.Directives | CliNameAutoGenerate.Options)]
public class WriteMetadataSchemaCommand : BaseWriteSchemaCommand<BroadcastMetadata>
{
    [CliOption(Name = "output", Alias = "-o", Description = "The schema output path")]
    public override FileInfo OutputPath { get; set; } = new("metadata.schema.json");
}
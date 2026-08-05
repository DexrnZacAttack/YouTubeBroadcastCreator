using DotMake.CommandLine;

namespace YouTubeBroadcastCreator.Command.Schema;

[CliCommand(Name = "output-metadata", Parent = typeof(WriteSchemaCommand), Description = "Writes the outputted metadata JSON schema file and exits.", ShortFormAutoGenerate = CliNameAutoGenerate.Arguments | CliNameAutoGenerate.Directives | CliNameAutoGenerate.Options)]
public class WriteOutputMetadataSchemaCommand : BaseWriteSchemaCommand<BroadcastInfo>
{
    [CliOption(Name = "output", Alias = "-o", Description = "The schema output path")]
    public override FileInfo OutputPath { get; set; } = new("output_metadata.schema.json");
}
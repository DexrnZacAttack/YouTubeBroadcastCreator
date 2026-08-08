using DotMake.CommandLine;
using YouTubeBroadcastCreator.Core;
using YouTubeBroadcastCreator.Core.Types.Broadcast;
using YouTubeBroadcastCreator.Types;

namespace YouTubeBroadcastCreator.Command.Schema;

[CliCommand(Name = "profile", Parent = typeof(WriteSchemaCommand), Description = "Writes the profile JSON schema file and exits.", ShortFormAutoGenerate = CliNameAutoGenerate.Arguments | CliNameAutoGenerate.Directives | CliNameAutoGenerate.Options)]
public class WriteProfileSchemaCommand : BaseWriteSchemaCommand<LiveStreamBroadcastProfile>
{
    [CliOption(Name = "output", Alias = "-o", Description = "The schema output path")]
    public override FileInfo OutputPath { get; set; } = new("profile.schema.json");
}
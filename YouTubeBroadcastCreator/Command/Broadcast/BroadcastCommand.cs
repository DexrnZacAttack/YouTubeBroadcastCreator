using DotMake.CommandLine;

namespace YouTubeBroadcastCreator.Command.Broadcast;

[CliCommand(Alias = "bc", Name = "broadcast", Description = "Manage broadcasts", Parent = typeof(YouTubeBroadcastCreatorCommand), ShortFormAutoGenerate = CliNameAutoGenerate.Arguments | CliNameAutoGenerate.Directives | CliNameAutoGenerate.Options)]
public class BroadcastCommand;
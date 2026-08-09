using DotMake.CommandLine;
using YouTubeBroadcastCreator.Core;

namespace YouTubeBroadcastCreator.Command.Schema;

[CliCommand(Description = $"Write a JSON schema of various data types used by {Constants.ProgramName}", Parent = typeof(YouTubeBroadcastCreatorCommand), ShortFormAutoGenerate = CliNameAutoGenerate.Arguments | CliNameAutoGenerate.Directives | CliNameAutoGenerate.Options)]
public class WriteSchemaCommand;
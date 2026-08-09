using DotMake.CommandLine;

namespace YouTubeBroadcastCreator.Command.Auth;

[CliCommand(Name = "auth", Description = "Google Account authentication", Parent = typeof(YouTubeBroadcastCreatorCommand),
            ShortFormAutoGenerate = CliNameAutoGenerate.Arguments | CliNameAutoGenerate.Directives |
                                    CliNameAutoGenerate.Options)]
public class BaseAuthenticationCommand;
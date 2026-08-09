using DotMake.CommandLine;
using YouTubeBroadcastCreator.Core;

namespace YouTubeBroadcastCreator.Command;

[CliCommand(Description = $"""
            This project is licensed under the MIT license, see https://github.com/DexrnZacAttack/YouTubeBroadcastCreator/tree/master/LICENSE for more info.
            NOTE: The developers of {Constants.ProgramName} are NOT responsible for any problems that this program may cause, even if your account gets banned.
            """, ShortFormAutoGenerate = CliNameAutoGenerate.Arguments | CliNameAutoGenerate.Directives | CliNameAutoGenerate.Options)]
public class YouTubeBroadcastCreatorCommand
{
    
}
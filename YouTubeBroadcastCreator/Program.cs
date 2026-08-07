using DotMake.CommandLine;
using YouTubeBroadcastCreator.Command;

namespace YouTubeBroadcastCreator;

internal class Program
{
    private static async Task<int> Main(string[] args)
    {
        CliSettings settings = new()
        {
            EnableDefaultExceptionHandler = true,
        };

        int res = await Cli.RunAsync<YouTubeBroadcastCreatorCommand>(args, settings);
        if (res != 0)
        {
            await Cli.RunAsync<YouTubeBroadcastCreatorCommand>([ "--help" ], settings);
        }
        
        return res;
    }
}
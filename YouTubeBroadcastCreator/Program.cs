using DotMake.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;
using YouTubeBroadcastCreator.Command;

namespace YouTubeBroadcastCreator;

internal class Program
{
    private static async Task<int> Main(string[] args)
    {
        Log.Logger = new LoggerConfiguration()
                    .MinimumLevel.Verbose()
                    .WriteTo.Console(
                                     standardErrorFromLevel: LogEventLevel.Verbose
                                     )
                    .CreateLogger();

        Cli.Ext.ConfigureServices(services =>
        {
            services.AddLogging(b =>
            {
                b.ClearProviders();
                b.AddSerilog(Log.Logger);
            });
        });
        
        return await Cli.RunAsync<YouTubeBroadcastCreatorCommand>(args, new CliSettings
        {
            EnableDefaultExceptionHandler = true,
        });
    }
}
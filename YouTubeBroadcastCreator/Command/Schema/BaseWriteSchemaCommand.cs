using System.Text.Json;
using System.Text.Json.Schema;
using DotMake.CommandLine;
using Serilog;
using YouTubeBroadcastCreator.Core;

namespace YouTubeBroadcastCreator.Command.Schema;

public abstract class BaseWriteSchemaCommand<T> : ICliRunAsyncWithReturn
{
    public abstract FileInfo OutputPath { get; set; }

    public async Task<int> RunAsync()
    {
        await using FileStream fs = OutputPath.Open(FileMode.Create, FileAccess.Write);
        await using StreamWriter sw = new(fs);
        
        await sw.WriteAsync(JsonSerializerOptions.Default.GetJsonSchemaAsNode(typeof(T), Constants.JsonSchemaExporterOptions).ToString());

        Log.Information("{Type} schema has been written to '{Path}'", typeof(T).Name, OutputPath.FullName);
        
        return 0;
    }
}
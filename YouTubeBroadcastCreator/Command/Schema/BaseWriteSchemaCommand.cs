using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Schema;
using DotMake.CommandLine;
using YouTubeBroadcastCreator.Core;
using YouTubeBroadcastCreator.Core.Util.Serialization.Schema;

namespace YouTubeBroadcastCreator.Command.Schema;

public abstract class BaseWriteSchemaCommand<T> : ICliRunAsyncWithReturn
{
    public abstract FileInfo OutputPath { get; set; }

    public async Task<int> RunAsync()
    {
        await using FileStream fs = OutputPath.Open(FileMode.Create, FileAccess.Write);
        await using StreamWriter sw = new(fs);
        
        await sw.WriteAsync(JsonSerializerOptions.Default.GetJsonSchemaAsNode(typeof(T), Constants.JsonSchemaExporterOptions).ToString());

        return 0;
    }
}
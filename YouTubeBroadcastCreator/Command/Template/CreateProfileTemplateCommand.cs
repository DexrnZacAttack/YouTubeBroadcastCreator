using System.Text.Json;
using DotMake.CommandLine;
using YouTubeBroadcastCreator.Core;
using YouTubeBroadcastCreator.Types;

namespace YouTubeBroadcastCreator.Command.Template;

[CliCommand(Name = "create-template", Description = "Creates a template profile.template.json.\nIt is recommended to run 'write-schema profile' after running this command for IDE/text editor suggestions to work.", Parent = typeof(YouTubeBroadcastCreatorCommand), ShortFormAutoGenerate = CliNameAutoGenerate.Arguments | CliNameAutoGenerate.Directives | CliNameAutoGenerate.Options)]
public class CreateProfileTemplateCommand : ICliRunAsyncWithReturn
{
    [CliOption(Name = "profile-file", Alias = "-p", Description = "Choose an exact path to place the template profile meta file")]
    public FileInfo ProfileFile { get; set; } = new("profile.template.json");

    public async Task<int> RunAsync()
    {
        LiveStreamBroadcastProfile profile = new();
        await using FileStream fs = ProfileFile.Open(FileMode.Create, FileAccess.Write);

        await JsonSerializer.SerializeAsync(fs, profile, Constants.JsonSerializerOptions);

        return 0;
    }
}
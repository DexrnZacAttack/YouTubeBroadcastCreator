using System.Text.RegularExpressions;
using CliWrap.Buffered;
using SmartFormat;

namespace YouTubeBroadcastCreator.Util;

public static partial class BroadcastTextFormatter
{
    private const string CmdEscape = "~";//using \$ gives Unhandled exception: System.ArgumentException: Unrecognized escape sequence "\$" in literal.
    private const string CmdStart = "$cmd[";
    private const string CmdEnd = "]";
    
    private static string FormatDefault(string s)
        => Smart.Format(s, new
        {
            date = DateTime.Now //idk what else to add
        });

    private static string FormatEval(string s)
        => CmdFormatAttribute().Replace(s, match =>
        {
            string command = FormatEval(match.Groups["Command"].Value);

            List<string> cmdline = SplitCmd(command);
            if (cmdline.Count == 0)
                return string.Empty;

            try
            {
                string exec = cmdline[0];
                IEnumerable<string> args = cmdline.Skip(1);

                var task = CliWrap.Cli.Wrap(exec).WithArguments(args).ExecuteBufferedAsync();
                BufferedCommandResult res = task.GetAwaiter().GetResult();

                return res.StandardOutput.Trim();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Failed to format command '{string.Join(' ', cmdline)}'! {ex.Message}");
                return "YBCC_CMD_EVAL_FAIL";
            }
        }).Replace($"{CmdEscape}{CmdStart}", CmdStart);
    

    private static List<string> SplitCmd(string cmd) => [..CmdSplitRegex().Matches(cmd).Select(m => m.Groups["Arg"].Value)];
    
    public static string Format(string s, bool eval)
    {
        string formatted = FormatDefault(s);
        
        return !eval ? formatted : FormatEval(formatted);
    }
    
    //https://learn.microsoft.com/en-us/dotnet/standard/base-types/grouping-constructs-in-regular-expressions#balancing-group-definitions
    //based off https://blog.stevenlevithan.com/archives/balancing-groups
    [GeneratedRegex($"""
                    (?x)
                    (?<!{CmdEscape})\$cmd\[
                        (?<Command>
                            (?>
                                (?! (?<!{CmdEscape})\$cmd\[ | \] ) .
                            |
                                (?<!{CmdEscape})\$cmd\[ (?<Depth>)
                            |
                                \] (?<-Depth>)
                            )*
                        )
                    	(?(Depth)(?!))
                    \]
                    """, RegexOptions.Singleline)]
    private static partial Regex CmdFormatAttribute();
    
    [GeneratedRegex("""(?:"(?<Arg>[^"]*)"|'(?<Arg>[^']*)'|(?<Arg>[^\s]+))""")]
    private static partial Regex CmdSplitRegex();
}
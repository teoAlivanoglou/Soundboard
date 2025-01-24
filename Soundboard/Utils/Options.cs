using CommandLine;
using Soundboard.AudioEngine;

namespace Soundboard.Utils;

public class Options
{
    [Option('d', "debug", Required = false, HelpText = "Set debug mode.")]
    public bool Debug { get; set; }

    [Option('o', "output", Required = false, HelpText = "Sets output device type.")]
    public DriverType? OutputType { get; set; }


    public static Parser Parser = new(s =>
    {
        s.CaseInsensitiveEnumValues = true;
        s.CaseSensitive = false;
    });


    public static Options? Parse(string[] args)
    {
        var options = Parser.ParseArguments<Options>(args);

        return options.Tag == ParserResultType.Parsed ? options.Value : null;
    }
}
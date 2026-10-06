using System;
using Soundboard.Avalonia.AudioEngine;

namespace Soundboard.Avalonia.Utils;

public class Options
{
    public bool Debug { get; set; } = false;
    public DriverType? OutputType { get; set; }

    public static Options Parse(string[] args)
    {
        var result = new Options();
        for (int i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            if (arg.Equals("-d", StringComparison.OrdinalIgnoreCase) ||
                arg.Equals("--debug", StringComparison.OrdinalIgnoreCase))
            {
                result.Debug = true;
            }
            else if ((arg.Equals("-o", StringComparison.OrdinalIgnoreCase) ||
                      arg.Equals("--output", StringComparison.OrdinalIgnoreCase)) &&
                     i + 1 < args.Length)
            {
                var val = args[++i];
                if (Enum.TryParse<DriverType>(val, ignoreCase: true, out var dt))
                {
                    result.OutputType = dt;
                }
            }
        }
        return result;
    }
}
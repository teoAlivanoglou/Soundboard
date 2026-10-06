using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;

namespace Soundboard.Avalonia.Utils;

public static class EnumHelper
{
    public static string? Description(this Enum value)
    {
        var attributes = value.GetType().GetField(value.ToString())
            ?.GetCustomAttributes(typeof(DescriptionAttribute), false);

        Debug.Assert(attributes != null, nameof(attributes) + " != null");
        return attributes.Any() ? (attributes.First() as DescriptionAttribute)?.Description : value.ToString();
    }

    public static IEnumerable<ValueDescription> GetAllValuesAndDescriptions(Type t)
    {
        if (!t.IsEnum)
            throw new ArgumentException($"{nameof(t)} must be an enum type");

        return
        [
            .. Enum.GetValues(t).Cast<Enum>()
                .Select((e) => new ValueDescription()
                {
                    Value = e,
                    Description = e.Description()
                })
        ];
    }
}
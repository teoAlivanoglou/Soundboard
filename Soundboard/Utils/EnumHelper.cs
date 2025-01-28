using System.ComponentModel;
using System.Globalization;

namespace Soundboard.Utils;

public static class EnumHelper
{
    public static string? Description(this Enum value)
    {
        var attributes = value.GetType().GetField(value.ToString())
            .GetCustomAttributes(typeof(DescriptionAttribute), false);

        if (attributes.Any())
            return (attributes.First() as DescriptionAttribute)?.Description;

        return value.ToString();
    }

    public static IEnumerable<ValueDescription> GetAllValuesAndDescriptions(Type t)
    {
        if (!t.IsEnum)
            throw new ArgumentException($"{nameof(t)} must be an enum type");

        return Enum.GetValues(t).Cast<Enum>()
            .Select((e) => new ValueDescription() { Value = e, Description = e.Description() }).ToList();
    }
}
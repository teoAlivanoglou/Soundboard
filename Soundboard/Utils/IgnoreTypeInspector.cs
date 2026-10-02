using YamlDotNet.Serialization;
using YamlDotNet.Serialization.TypeInspectors;

namespace Soundboard.Utils;

public class IgnoreNamedPropertyInspector(
    ITypeInspector innerTypeDescriptor,
    IEnumerable<string> propertyNames,
    StringComparison comparisonType)
    : TypeInspectorSkeleton
{
    private readonly ITypeInspector _innerTypeDescriptor =
        innerTypeDescriptor ?? throw new ArgumentNullException(nameof(innerTypeDescriptor));

    public override string GetEnumName(Type enumType, string name) => _innerTypeDescriptor.GetEnumName(enumType, name);

    public override string GetEnumValue(object enumValue) => _innerTypeDescriptor.GetEnumValue(enumValue);

    public override IEnumerable<IPropertyDescriptor> GetProperties(Type type, object? container)
    {
        var properties = _innerTypeDescriptor.GetProperties(type, container)
            .Where(p => !propertyNames.Contains(p.Name, new StringComparer(comparisonType)));
        return properties;
    }

    public override bool HasParseMethod(Type type)
    {
        throw new NotImplementedException();
    }

    public override object? Parse(string value, Type expectedType)
    {
        throw new NotImplementedException();
    }
}

internal class StringComparer(StringComparison comparisonType) : IEqualityComparer<string>
{
    public bool Equals(string? x, string? y)
    {
        return x?.Equals(y, comparisonType) ?? false;
    }

    public int GetHashCode(string obj)
    {
        throw new NotImplementedException();
    }
}
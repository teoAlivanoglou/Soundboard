using System.CodeDom;
using System.Text;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;
using System.Linq;
using System.Windows;
using System.Windows.Documents;

namespace Soundboard.Settings;

public enum SerializedToken
{
    Key,
    Value
}

// Not refactoring yet, I don't remember what the purpose is.
public class EnumConverter<T>(SerializedToken serializedToken, bool comment) : IYamlTypeConverter
    where T : struct, Enum
{
    private SerializedToken _serializedToken = serializedToken;
    private bool _comment = comment;

    public bool Accepts(Type type)
    {
        return type == typeof(T);
    }

    public object? ReadYaml(IParser parser, Type type, ObjectDeserializer rootDeserializer)
    {
        var enumValue = Enum.Parse(type, ((Scalar)parser.Current!).Value);
        parser.MoveNext();

        return enumValue;
    }

    public void WriteYaml(IEmitter emitter, object? value, Type type, ObjectSerializer serializer)
    {
        switch (_serializedToken)
        {
            case SerializedToken.Key:
                emitter.Emit(new Scalar(value!.ToString()!));
                emitter.Emit(new Comment($"{string.Join(", ", Enum.GetNames(typeof(T)))}", true));
                break;
            case SerializedToken.Value:
                emitter.Emit(new Scalar(((int)value!).ToString()));
                emitter.Emit(new Comment(
                    $"{string.Join(", ", (Enum.GetValuesAsUnderlyingType(typeof(T)) as int[]).Select(i => $"{i}").ToArray())}",
                    true));
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
}
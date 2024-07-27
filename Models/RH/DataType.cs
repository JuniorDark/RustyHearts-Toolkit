namespace RHToolkit.Models.RH;

public class DataType
{
    public static string GetColumnType(int type)
    {
        return type switch
        {
            0 => "int32",
            1 => "float",
            2 => "string2",
            3 => "string",
            4 => "int64",
            _ => throw new ArgumentOutOfRangeException(nameof(type), $"Unexpected type: {type}"),
        };
    }

    public static Type GetColumnDataType(int type)
    {
        return type switch
        {
            0 => typeof(int),
            1 => typeof(float),
            2 => typeof(string),
            3 => typeof(string),
            4 => typeof(long),
            _ => throw new ArgumentOutOfRangeException(nameof(type), $"Unexpected type: {type}"),
        };
    }

    public static void WriteValueByType(BinaryWriter writer, object value, int type)
    {
        switch (type)
        {
            case 0:
                writer.Write(Convert.ToInt32(value));
                break;
            case 1:
                writer.Write(Convert.ToSingle(value));
                break;
            case 2:
            case 3:
                {
                    string strValue = Convert.ToString(value) ?? string.Empty;
                    byte[] strBytes = Encoding.Unicode.GetBytes(strValue);
                    short numStrLen = (short)(strBytes.Length / 2);
                    writer.Write(numStrLen);
                    writer.Write(strBytes);
                    break;
                }
            case 4:
                writer.Write(Convert.ToInt64(value));
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(type), $"Unexpected type: {type}");
        }
    }

    public static object GetValueByType(int type, BinaryReader reader)
    {
        switch (type)
        {
            case 0:
                return reader.ReadInt32();
            case 1:
                return reader.ReadSingle();
            case 2:
            case 3:
                {
                    int numStrLen = reader.ReadInt16();
                    return Encoding.Unicode.GetString(reader.ReadBytes(numStrLen * 2));
                }
            case 4:
                return reader.ReadInt64();
            default:
                throw new ArgumentOutOfRangeException(nameof(type), $"Unexpected type: {type}");
        }
    }

    public static bool ValidateAttributes(string type)
    {
        string[] allowedTypes = ["int32", "float", "string", "string2", "int64"];
        return allowedTypes.Contains(type);
    }

    public static string XMLEncodeAttribute(string str)
    {
        str = str.Replace("(", "");
        str = str.Replace(")", "");

        return str;
    }

    public static string XMLEncode(string str)
    {
        str = str.Replace("<", "&lt;");
        str = str.Replace(">", "&gt;");
        str = str.Replace("&", "&amp;");
        str = str.Replace("'", "&apos;");
        str = str.Replace("\"", "&quot;");

        return str;
    }

    public static string XMLDecode(string str)
    {
        str = str.Replace("&lt;", "<");
        str = str.Replace("&gt;", ">");
        str = str.Replace("&amp;", "&");
        str = str.Replace("&apos;", "'");
        str = str.Replace("&quot;", "\"");

        return str;
    }
}

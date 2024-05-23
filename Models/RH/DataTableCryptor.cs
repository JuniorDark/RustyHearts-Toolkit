using System.Data;

namespace RHToolkit.Models.RH
{
    public class DataTableCryptor
    {
        public static DataTable RhToDataTable(byte[] encryptedData)
        {
            try
            {
                // Decrypt RH
                byte[] decryptedData = RHCryptor.Decrypt(encryptedData);

                using MemoryStream stream = new(decryptedData);
                using BinaryReader reader = new(stream);
                DataTable dataTable = new();

                int numRow = reader.ReadInt32();
                int numCol = reader.ReadInt32();

                if (numCol <= 0)
                {
                    throw new Exception("The rh file data format is incorrect");
                }

                List<string> listTitles = new(numCol);
                List<Type> listTypes = new(numCol);

                // Get the title
                for (int i = 0; i < numCol; i++)
                {
                    int numStrLen = reader.ReadInt16();
                    string value = Encoding.Unicode.GetString(reader.ReadBytes(numStrLen * 2));
                    listTitles.Add(value);
                }

                // Get the type
                int[] intTypes = new int[numCol];
                for (int i = 0; i < numCol; i++)
                {
                    int t = reader.ReadInt32();
                    Type columnType = GetColumnType(t);
                    intTypes[i] = t;
                    listTypes.Add(columnType);
                    dataTable.Columns.Add(listTitles[i], columnType);
                }

                // Populate the DataTable
                for (int i = 0; i < numRow; i++)
                {
                    DataRow row = dataTable.NewRow();
                    for (int j = 0; j < numCol; j++)
                    {
                        row[j] = GetValueByType(intTypes[j], reader);
                    }
                    dataTable.Rows.Add(row);
                }

                return dataTable;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public static byte[] DataTableToRh(DataTable dataTable)
        {
            try
            {
                using MemoryStream stream = new();
                using BinaryWriter writer = new(stream);

                int numRow = dataTable.Rows.Count;
                int numCol = dataTable.Columns.Count;

                writer.Write(numRow);
                writer.Write(numCol);

                // Write column names
                foreach (DataColumn column in dataTable.Columns)
                {
                    byte[] strByte = Encoding.Unicode.GetBytes(column.ColumnName);
                    short numStrLen = (short)(strByte.Length / 2);
                    writer.Write(numStrLen);
                    writer.Write(strByte);
                }

                // Write column types
                foreach (DataColumn column in dataTable.Columns)
                {
                    int columnType = GetColumnType(column.DataType);
                    writer.Write(columnType);
                }

                // Write rows
                foreach (DataRow row in dataTable.Rows)
                {
                    for (int j = 0; j < numCol; j++)
                    {
                        DataColumn column = dataTable.Columns[j];
                        int columnType = GetColumnType(column.DataType);
                        WriteValueByType(writer, row[j], columnType);
                    }
                }

                writer.Flush();
                stream.Flush();

                byte[] buffer = stream.ToArray();
                return RHCryptor.Encrypt(buffer);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        private static int GetColumnType(Type type)
        {
            if (type == typeof(int))
                return 0;
            if (type == typeof(float))
                return 1;
            if (type == typeof(string))
                return 3;
            if (type == typeof(long))
                return 4;
            throw new ArgumentOutOfRangeException(nameof(type), $"Unexpected type: {type}");
        }

        private static void WriteValueByType(BinaryWriter writer, object value, int type)
        {
            switch (type)
            {
                case 0:
                    writer.Write(Convert.ToInt32(value));
                    break;
                case 1:
                    writer.Write(Convert.ToSingle(value));
                    break;
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

        private static Type GetColumnType(int value)
        {
            return value switch
            {
                0 => typeof(int),
                1 => typeof(float),
                2 => typeof(string),
                3 => typeof(string),
                4 => typeof(long),
                _ => throw new ArgumentOutOfRangeException(nameof(value), $"Unexpected type: {value}"),
            };
        }

        private static object GetValueByType(int type, BinaryReader reader)
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
    }
}

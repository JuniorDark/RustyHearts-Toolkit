using OfficeOpenXml;
using System.Data;

namespace RHToolkit.Models.RH
{
    public class DataTableCryptor
    {
        private readonly RHCryptor _rhCryptor = new();

        /// <summary>
        /// Creates a DataTable with specified columns.
        /// </summary>
        /// <param name="columns">List of column names and their data types.</param>
        /// <returns>A DataTable with the specified columns.</returns>
        /// <exception cref="ArgumentNullException">Thrown when columns list is null.</exception>
        public static DataTable CreateDataTable(List<KeyValuePair<string, int>> columns)
        {
            if (columns == null)
            {
                throw new ArgumentNullException(nameof(columns), "The columns list cannot be null.");
            }

            DataTable dataTable = new();

            for (int i = 0; i < columns.Count; i++)
            {
                var column = columns[i];
                string columnName = column.Key;
                int columnDataType = column.Value;
                Type columnType = DataType.GetColumnDataType(columnDataType);

                // Add the column to the DataTable
                dataTable.Columns.Add(columnName, columnType);

                // Store the ColumnType as an extended property
                dataTable.Columns[i].ExtendedProperties["ColumnType"] = columnDataType;
            }

            return dataTable;
        }

        /// <summary>
        /// Converts encrypted RH data to a DataTable.
        /// </summary>
        /// <param name="encryptedData">The encrypted RH data.</param>
        /// <returns>A DataTable containing the decrypted data.</returns>
        /// <exception cref="Exception">Thrown when the RH file is invalid or an error occurs during decryption.</exception>
        public DataTable RhToDataTable(byte[] encryptedData)
        {
            try
            {
                // Decrypt RH
                byte[] decryptedData = _rhCryptor.Decrypt(encryptedData);

                using MemoryStream stream = new(decryptedData);
                using BinaryReader reader = new(stream);
                DataTable dataTable = new();

                int numRow = reader.ReadInt32();
                int numCol = reader.ReadInt32();

                if (numCol <= 0)
                {
                    throw new Exception(Resources.CryptorInvalidRHFileMessage);
                }

                List<string> listTitles = new(numCol);
                List<Type> listTypes = new(numCol);
                Dictionary<string, int> columnCounter = [];

                // Get the title
                for (int i = 0; i < numCol; i++)
                {
                    int numStrLen = reader.ReadInt16();
                    string value = Encoding.Unicode.GetString(reader.ReadBytes(numStrLen * 2));

                    // Ensure unique column name
                    if (columnCounter.TryGetValue(value, out int counter))
                    {
                        columnCounter[value] = ++counter;
                        value = $"{value}-{columnCounter}";
                    }
                    else
                    {
                        columnCounter[value] = 1;
                    }

                    listTitles.Add(value);
                }

                // Get the type
                int[] intTypes = new int[numCol];
                for (int i = 0; i < numCol; i++)
                {
                    int t = reader.ReadInt32();
                    Type columnType = DataType.GetColumnDataType(t);
                    intTypes[i] = t;
                    dataTable.Columns.Add(listTitles[i], columnType);
                    // Store columntype as extended property
                    dataTable.Columns[i].ExtendedProperties["ColumnType"] = t;
                }

                // Populate the DataTable
                for (int i = 0; i < numRow; i++)
                {
                    DataRow row = dataTable.NewRow();
                    for (int j = 0; j < numCol; j++)
                    {
                        row[j] = DataType.GetValueByType(intTypes[j], reader);
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

        /// <summary>
        /// Converts a DataTable to encrypted RH data.
        /// </summary>
        /// <param name="dataTable">The DataTable to convert.</param>
        /// <returns>The encrypted RH data.</returns>
        /// <exception cref="InvalidOperationException">Thrown when a column type is missing.</exception>
        /// <exception cref="Exception">Thrown when an error occurs during encryption.</exception>
        public byte[] DataTableToRh(DataTable dataTable)
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
                    string columnName = column.ColumnName;
                    if (columnName.Contains('-'))
                    {
                        string[] parts = columnName.Split('-');
                        columnName = parts[0]; // Use the original attribute name
                    }
                    byte[] strByte = Encoding.Unicode.GetBytes(columnName);
                    short numStrLen = (short)(strByte.Length / 2);
                    writer.Write(numStrLen);
                    writer.Write(strByte);
                }

                // Write column types
                foreach (DataColumn column in dataTable.Columns)
                {
                    // Get column type from extended property
                    int? columnType = (int?)column.ExtendedProperties?["ColumnType"];
                    if (columnType.HasValue)
                    {
                        writer.Write(columnType.Value);
                    }
                    else
                    {
                        throw new InvalidOperationException("Column type is missing.");
                    }
                }

                // Write rows
                foreach (DataRow row in dataTable.Rows)
                {
                    for (int j = 0; j < numCol; j++)
                    {
                        DataColumn column = dataTable.Columns[j];
                        // Get column type from extended property
                        int? columnType = (int?)column.ExtendedProperties?["ColumnType"];
                        if (columnType.HasValue)
                        {
                            DataType.WriteValueByType(writer, row[j], columnType.Value);
                        }
                        else
                        {
                            throw new InvalidOperationException("Column type is missing.");
                        }
                    }
                }

                writer.Flush();
                stream.Flush();

                byte[] buffer = stream.ToArray();
                return _rhCryptor.Encrypt(buffer);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        /// <summary>
        /// Converts a DataTable to an XML byte array.
        /// </summary>
        /// <param name="dataTable">The DataTable to convert.</param>
        /// <returns>The XML byte array representing the DataTable.</returns>
        /// <exception cref="Exception">Thrown when a column does not have a valid 'ColumnType' property or an error occurs during conversion.</exception>
        public static byte[] DataTableToXML(DataTable dataTable)
        {
            try
            {
                StringBuilder xmlBuilder = new();
                xmlBuilder.Append("<?xml version=\"1.0\" encoding=\"utf-8\" ?>\n");

                xmlBuilder.Append("<Root>\n");
                xmlBuilder.Append("<Attributes>\n");

                List<string> listTitles = new(dataTable.Columns.Count);
                List<string> listTypes = new(dataTable.Columns.Count);
                Dictionary<string, int> attributeCounter = [];

                // Get the title and type
                foreach (DataColumn column in dataTable.Columns)
                {
                    string title = column.ColumnName;
                    listTitles.Add(title);

                    if (column.ExtendedProperties.ContainsKey("ColumnType") && column.ExtendedProperties["ColumnType"] is int typeValue)
                    {
                        string type = DataType.GetColumnType(typeValue);
                        listTypes.Add(type);
                    }
                    else
                    {
                        throw new Exception($"Column '{title}' does not have a valid 'ColumnType' property.");
                    }
                }

                // Write the Attributes
                for (int i = 0; i < dataTable.Columns.Count; i++)
                {
                    string title = listTitles[i];
                    string type = listTypes[i];

                    if (attributeCounter.TryGetValue(title, out int counter))
                    {
                        attributeCounter[title] = ++counter;
                        title = $"{title}-{counter}";
                    }
                    else
                    {
                        attributeCounter[title] = 1;
                    }

                    xmlBuilder.AppendFormat("<Attribute name=\"{0}\" type=\"{1}\" />\n", DataType.XMLEncodeAttribute(title), type);
                }

                xmlBuilder.Append("</Attributes>\n");
                xmlBuilder.Append("<Data>\n");

                // Get all rows
                foreach (DataRow row in dataTable.Rows)
                {
                    xmlBuilder.Append("<Row ");

                    // Reset the counter for each row
                    Dictionary<string, int> attributeCounterForRow = [];

                    for (int j = 0; j < dataTable.Columns.Count; j++)
                    {
                        string title = listTitles[j];
                        string value = row[j]?.ToString() ?? string.Empty;

                        if (attributeCounterForRow.TryGetValue(title, out int counter))
                        {
                            attributeCounterForRow[title] = ++counter;
                            title = $"{title}-{counter}";
                        }
                        else
                        {
                            attributeCounterForRow[title] = 1;
                        }

                        xmlBuilder.AppendFormat("{0}=\"{1}\" ", DataType.XMLEncodeAttribute(title), DataType.XMLEncode(value));
                    }
                    xmlBuilder.Append("/>\n");
                }

                xmlBuilder.Append("</Data>\n");
                xmlBuilder.Append("</Root>");
                return Encoding.UTF8.GetBytes(xmlBuilder.ToString());
            }
            catch (Exception ex)
            {
                throw new Exception($"{string.Format(Resources.CryptorDataTableFileErrorMessage, "XML")}: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Converts a DataTable to an XLSX byte array.
        /// </summary>
        /// <param name="dataTable">The DataTable to convert.</param>
        /// <returns>The XLSX byte array representing the DataTable.</returns>
        /// <exception cref="Exception">Thrown when a column does not have a valid 'ColumnType' property or an error occurs during conversion.</exception>
        public static byte[] DataTableToXLSX(DataTable dataTable)
        {
            try
            {
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

                using ExcelPackage package = new();
                ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("sheet");

                int numRow = dataTable.Rows.Count;
                int numCol = dataTable.Columns.Count;

                if (numCol <= 0)
                {
                    throw new Exception("The DataTable has no columns.");
                }

                List<string> listTitles = new(numCol);
                List<string> listTypes = new(numCol);

                // Get the titles and types
                foreach (DataColumn column in dataTable.Columns)
                {
                    string title = column.ColumnName;
                    listTitles.Add(title);

                    int typeValue;
                    if (column.ExtendedProperties.ContainsKey("ColumnType") && column.ExtendedProperties["ColumnType"] is int value)
                    {
                        typeValue = value;
                    }
                    else
                    {
                        throw new Exception($"Column '{title}' does not have a valid 'ColumnType' property.");
                    }
                    string type = DataType.GetColumnType(typeValue);
                    listTypes.Add(type);
                }

                // Write titles to the worksheet
                if (worksheet.Cells["A1"].LoadFromArrays(new List<string[]> { listTitles.ToArray() }) is ExcelRange rowTitle)
                {
                    rowTitle.Style.Font.Bold = true;
                }

                // Write types to the worksheet
                ExcelRange? rowType = worksheet.Cells["A2"].LoadFromArrays(new List<string[]> { listTypes.ToArray() }) as ExcelRange;

                // Freeze the first two rows
                worksheet.View.FreezePanes(2, 1);

                // Write rows to the worksheet
                for (int i = 0; i < numRow; i++)
                {
                    List<object> rowValues = new(numCol);
                    for (int j = 0; j < numCol; j++)
                    {
                        rowValues.Add(dataTable.Rows[i][j]);
                    }
                    ExcelRange? row = worksheet.Cells[i + 3, 1].LoadFromArrays([[.. rowValues]]) as ExcelRange;
                }

                // Automatically adjust the column widths
                worksheet.Cells.AutoFitColumns();

                using MemoryStream streamBook = new();
                package.SaveAs(streamBook);
                return streamBook.ToArray();
            }
            catch (Exception ex)
            {
                throw new Exception($"{string.Format(Resources.CryptorDataTableFileErrorMessage, "XLSX")}: {ex.Message}", ex);
            }
        }

    }
}
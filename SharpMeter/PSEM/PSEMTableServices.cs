using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SharpMeter.PSEM.Models;
using SharpMeter.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using static SharpMeter.LibraryConstants;

namespace SharpMeter.PSEM
{
    /// <summary>
    /// PSEM table services class. This class is used to interpret tables from bytes to fields.
    /// </summary>
    public class PSEMTableServices
    {
        private static string TableMapString = File.ReadAllText(@"PSEM\data\TableMap.json");
        private static JObject tableMapObject = JObject.Parse(TableMapString);
        private static string EnumsString = File.ReadAllText(@"PSEM\data\Enums.json");
        private static JObject enumObject = JObject.Parse(EnumsString);
        private static string BitsString = File.ReadAllText(@"PSEM\data\Bits.json");
        private static JObject bitsObject = JObject.Parse(BitsString);

        /// <summary>Deserializes the table.</summary>
        /// <param name="tableID">The table identifier.</param>
        /// <param name="tableData">The table data.</param>
        /// <param name="createMeter">if set to <c>true</c> [create meter].</param>
        /// <returns>Table object</returns>
        /// <exception cref="System.Collections.Generic.KeyNotFoundException">ERR - Key [{tablePrefix + tableID}] is not found in TableMap.json
        /// or</exception>
        public Table DeserializeTable(int tableID, byte[] tableData, bool createMeter = false)
        {
            if (tableData == null)
            {
                throw new ArgumentNullException(nameof(tableData));
            }

            if (tableID == 2165)
            {
                Console.WriteLine("Here");
            }
            var tablePrefix = "";

            // Variable to count the bytes in a table
            // It's used to as an iterator to keep track of the bytes while deserializing
            var byteCounter = 0;

            // Set the table prefix
            // IF greater or equal than 2048 then it's manufacturing table
            // ELSE it's standard table
            if (tableID >= 2048)
            {
                tablePrefix = "MT";
            }
            else
            {
                tablePrefix = "ST";
            }

            Log.Debug($"Table {tableID} - {BitConverter.ToString(tableData)}");
            if (!tableMapObject.ContainsKey(tablePrefix + (tablePrefix == "ST" ? tableID : tableID - 2048)))
            {
                Log.Trace($"ERR - Key [{tablePrefix + tableID}] is not found in TableMap.json");
                throw new KeyNotFoundException($"ERR - Key [{tablePrefix + tableID}] is not found in TableMap.json");
            }
            // Find the node for specifc  table by its key
            // For example: tableMapObject[ST52]
            var fieldsStr = tableMapObject[tablePrefix + (tablePrefix == "ST" ? tableID : tableID - 2048)]["Fields"].ToString();

            // Deserialize it into a C# dictonary with string keys and dynamic values
            var fields = JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(fieldsStr);

            // Initializes table and declare it's information
            var table = new Table();
            table.Bytes = tableData;
            table.Id = tableID;
            table.HexData = BitConverter.ToString(tableData);
            table.Name = tablePrefix + (tablePrefix == "ST" ? tableID : tableID - 2048);
            table.Description = tableMapObject[tablePrefix + (tablePrefix == "ST" ? tableID : tableID - 2048)]["Description"].ToString();
            table.Size = (int)tableMapObject[tablePrefix + (tablePrefix == "ST" ? tableID : tableID - 2048)]["Size"];
            table.Fields = new List<TableField>();

            // Loop used to go over all the fields and try to decipher them based on their size and/or type
            foreach (var f in fields)
            {
                var fieldValues = new Dictionary<string, dynamic>();
                var field = new TableField();
                var fieldSize = 0;

                field.Name = f.Key;

                var isHeaderContainingBits = false;

                // Handle header type of fields
                // Headers usually have subfields or children
                if (field.Name.Contains("_[HEADER]"))
                {
                    var fieldChildrenDict = new Dictionary<string, dynamic>();
                    var childrenFields = JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(f.Value.ToString());

                    // Nested foreach loop to decipher this field children based on their size and/or type
                    foreach (var child in childrenFields)
                    {
                        byte[] fieldBytes = null;
                        var tempfieldDefArray = child.Value.Split(',');
                        Int32.TryParse(tempfieldDefArray[1] == "BITS" ? tempfieldDefArray[2] : tempfieldDefArray[0], out fieldSize);
                        field.Type = tempfieldDefArray[1];
                        fieldSize = tempfieldDefArray[1] == "BITS" ? (fieldSize / 8) : fieldSize;
                        field.Offset = byteCounter;
                        field.Size = fieldSize;

                        fieldBytes = tableData.SubArray(field.Offset, field.Size).ToArray();
                        field.Bytes = fieldBytes;
                        field.HexString = BitConverter.ToString(fieldBytes);
                        Log.Trace($"[{child.Key}]: {BitConverter.ToString(fieldBytes)}");
                        // Handle String and Char types
                        if (field.Type == "STRING" || field.Type == "CHAR")
                        {
                            field.Value = Encoding.Default.GetString(fieldBytes, 0, fieldBytes.Length);
                        }

                        // Handle ASCII types
                        else if (field.Type == "ASCII")
                        {
                            field.Value = Encoding.ASCII.GetString(fieldBytes, 0, fieldBytes.Length);

                            // Handle ASCII Null value and turn it to a space
                            if (((string)field.Value).Contains("\u0000"))
                            {
                                field.Value = field.Value.Replace("\u0000", " ");
                            }
                            byteCounter = byteCounter + fieldSize;
                        }
                        else if (field.Type == "HEX")
                        {
                            field.Value = BitConverter.ToString(fieldBytes);

                            byteCounter = byteCounter + fieldSize;
                        }
                        // Handle unisnged integer
                        else if (field.Type == "UINT")
                        {
                            field.Value = (uint)fieldBytes[0];
                        }
                        // Handle enumration type
                        // In this case, enumeration maps the true value to a human-readable value using Enums.json
                        else if (field.Type == "ENUM")
                        {
                            var enumField = enumObject[tempfieldDefArray[2]].ToArray();
                            field.Value = enumField[(int)fieldBytes[0]];
                        }

                        // Handle bits type
                        // Uses BitArray to parse the values and decipher them
                        else if (field.Type == "BITS")
                        {
                            fieldBytes = tableData.SubArray(field.Offset, field.Size).ToArray();
                            BitArray bits = null;
                            if (tempfieldDefArray[2] == "32")
                            {
                                bits = new BitArray(new byte[] { fieldBytes[0], tableData[field.Offset + 1], tableData[field.Offset + 2], tableData[field.Offset + 3] });
                                byteCounter += 4;
                            }
                            else if (tempfieldDefArray[2] == "16")
                            {
                                bits = new BitArray(new byte[] { fieldBytes[0], tableData[field.Offset + 1] });
                                byteCounter += 2;
                            }
                            else
                            {
                                bits = new BitArray(new byte[] { fieldBytes[0] });
                                byteCounter++;
                            }

                            var bitField = bitsObject[field.Name.Replace("_[HEADER]", "")] as JObject;
                            if (bitField == null)
                            {
                                bitField = tableMapObject[tablePrefix + (tablePrefix == "ST" ? tableID : tableID - 2048)]["Fields"][field.Name] as JObject;
                                isHeaderContainingBits = true;
                            }
                            var elementCounter = 0;
                            var endingBitIteration = 0;
                            foreach (var element in bitField)
                            {
                                var name = element.Key;
                                var value = element.Value.ToArray();
                                if (name == "FILLER")
                                {
                                    Console.WriteLine("Hey");
                                }
                                var startingBit = 0;
                                var endingBit = 0;
                                if (isHeaderContainingBits)
                                {
                                    tempfieldDefArray = element.Value.ToString().Split(',');
                                    startingBit = endingBitIteration;
                                    endingBit = Convert.ToInt16(tempfieldDefArray[0]) + endingBitIteration;
                                    var bitArrayLength = (endingBit) - startingBit;
                                    if (endingBitIteration <= Convert.ToInt16(tempfieldDefArray[2]))
                                    {
                                        var subBits = bits.Between(startingBit, bitArrayLength - 1);

                                        var numOnes = subBits.ToInt();
                                        if (tempfieldDefArray.Length > 3)
                                        {
                                            if (tempfieldDefArray[3] == "ENUM")
                                            {
                                                if (enumObject.ContainsKey(element.Key.ToUpper()))
                                                {
                                                    var enumField = enumObject[element.Key.ToUpper()];
                                                    fieldValues.Add(name, enumField[numOnes]);
                                                }
                                                else
                                                {
                                                    throw new KeyNotFoundException(element.Key.ToUpper());
                                                }
                                            }
                                        }
                                        else
                                        {
                                            fieldValues.Add(name, numOnes);
                                        }

                                        endingBitIteration += bitArrayLength;
                                    }
                                }
                                else
                                {
                                    startingBit = (int)value[0];
                                    endingBit = (int)value[1];
                                    var bitArrayLength = endingBit - startingBit;
                                    var subBits = bits.Between(startingBit, bitArrayLength - 1);

                                    var numOnes = subBits.ToInt();

                                    fieldValues.Add(name, (string)enumObject[name][numOnes]);
                                }

                                elementCounter++;
                            }

                            field.Value = fieldValues;

                            break;
                        }

                        // Handles array type
                        // This type is usually a list of unsigned integer that needs to perform bitwise operation in order to make them more
                        // human readable.
                        else if (field.Type == "ARRAY")
                        {
                            var intList = new List<int>();

                            for (var i = 0; i < fieldBytes.Length; i++)
                            {
                                for (var j = 0; j < 8; j++)
                                {
                                    if ((fieldBytes[i] & 1 << j) != 0)
                                        intList.Add(j + i * 8);
                                }
                            }
                            field.Value = String.Join(", ", intList.ToArray());
                        }
                        // Unknown type
                        // Try to cast it to an integer value
                        // TOIMPROVE
                        else
                        {
                            if (tempfieldDefArray.Length > 2)
                            {
                                if (tempfieldDefArray[2] == "TIME")
                                {
                                    field.Value = (int)fieldBytes[0];
                                    byteCounter = byteCounter + fieldSize;
                                }
                            }
                            else
                            {
                                if (fieldBytes.Length > 4)
                                {
                                    fieldBytes = fieldBytes.Reverse().ToArray();

                                    field.Value = Convert.ToUInt64(BitConverter.ToString(fieldBytes).Replace("-", ""), 16);
                                }
                                else
                                {
                                    field.Value = (uint)fieldBytes[0];
                                }

                                byteCounter = byteCounter + fieldSize;
                            }
                        }
                        if (field.Name.Contains("_[HEADER]") && field.Name.Contains("_[FLAGS]"))
                        {
                            field.Value = fieldValues;
                        }
                        fieldChildrenDict.Add(child.Key, field.Value);
                        field.Value = fieldChildrenDict;
                    }
                }
                // Handle regular fields with no children
                else
                {
                    var tempfieldDefArray = f.Value.Split(',');

                    Int32.TryParse(tempfieldDefArray[0], out fieldSize);
                    field.Type = tempfieldDefArray[1];
                    fieldSize = tempfieldDefArray[1] == "BITS" ? (fieldSize / Convert.ToInt32(tempfieldDefArray[0])) : fieldSize;
                    field.Offset += byteCounter;
                    field.Size = fieldSize;
                    if ((field.Offset + field.Size) > tableData.Length)
                    {
                        break;
                    }
                    var fieldBytes = tableData.SubArray(field.Offset, field.Size).ToArray();
                    Log.Trace($"[{f.Key}]: {BitConverter.ToString(fieldBytes)}");

                    // Handle String and Char types
                    if (field.Type == "STRING" || field.Type == "CHAR")
                    {
                        field.Value = Encoding.Default.GetString(fieldBytes, 0, fieldBytes.Length);
                    }

                    // Handle ASCII types
                    else if (field.Type == "ASCII")
                    {
                        field.Value = Encoding.ASCII.GetString(fieldBytes, 0, fieldBytes.Length);

                        // Handle ASCII Null value and turn it to a space
                        if (((string)field.Value).Contains("\u0000"))
                        {
                            field.Value = field.Value.Replace("\u0000", " ");
                        }
                    }

                    // Handle unisnged integer
                    else if (field.Type == "UINT")
                    {
                        field.Value = (uint)fieldBytes[0];
                    }

                    // Handle enumration type
                    // In this case, enumeration maps the true value to a human-readable value using Enums.json
                    else if (field.Type == "ENUM")
                    {
                        var enumField = enumObject[tempfieldDefArray[2]];
                        field.Value = enumField[(int)fieldBytes[0]];
                    }

                    // Handle bits type
                    // Uses BitArray to parse the values and decipher them
                    else if (field.Type == "BITS")
                    {
                        BitArray bits = null;
                        if (tempfieldDefArray[0] == "32")
                        {
                            bits = new BitArray(new byte[] { fieldBytes[0], tableData[field.Offset + 1], tableData[field.Offset + 2] });
                            tableData = tableData.Where((source, index) => index != field.Offset).ToArray();
                            tableData = tableData.Where((source, index) => index != field.Offset + 1).ToArray();
                        }
                        else if (tempfieldDefArray[0] == "16")
                        {
                            bits = new BitArray(new byte[] { fieldBytes[0], tableData[field.Offset + 1] });
                            tableData = tableData.Where((source, index) => index != field.Offset).ToArray();
                        }
                        else
                        {
                            bits = new BitArray(new byte[] { fieldBytes[0] });
                        }

                        var bitField = bitsObject[field.Name].Value<JObject>();
                        var elementCounter = 0;
                        foreach (var element in bitField)
                        {
                            var name = element.Key;
                            var value = element.Value.ToArray();
                            var startingBit = (int)value[0];
                            var endingBit = (int)value[1];
                            var bitArrayLength = endingBit - startingBit;
                            var subBits = bits.Between(startingBit, bitArrayLength);

                            var numOnes = subBits.ToInt();

                            fieldValues.Add(name, (string)enumObject[name][numOnes]);

                            elementCounter++;
                        }

                        field.Value = fieldValues;
                    }

                    // Handles array type
                    // This type is usually a list of unsigned integer that needs to perform bitwise operation in order to make them more
                    // human readable.
                    else if (field.Type == "ARRAY")
                    {
                        var intList = new List<int>();

                        for (var i = 0; i < fieldBytes.Length; i++)
                        {
                            for (var j = 0; j < 8; j++)
                            {
                                if ((fieldBytes[i] & 1 << j) != 0)
                                    intList.Add(j + i * 8);
                            }
                        }
                        field.Value = String.Join(", ", intList.ToArray());
                    }

                    // Unknown type
                    // Try to cast it to an integer value
                    // TOIMPROVE
                    else
                    {
                        if (fieldBytes.Length > 1)
                        {
                            field.Value = BitConverter.ToInt16(fieldBytes, 0);
                        }
                        else
                        {
                            field.Value = (uint)fieldBytes[0];
                        }
                    }
                    byteCounter += Convert.ToInt32(tempfieldDefArray[0]);

                    field.HexString = BitConverter.ToString(fieldBytes);
                    field.Bytes = fieldBytes;
                }

                table.Fields.Add(field);
            }
            if (!createMeter)
            {
                var dumpJson = JsonConvert.SerializeObject(table, Formatting.Indented, settings: new JsonSerializerSettings()
                {
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                });
                Log.Debug($"\n============================\n{dumpJson}\n============================\n");
            }

            return table;
        }

        /// <summary>Finds the field.</summary>
        /// <param name="tableID">The table identifier.</param>
        /// <param name="tableData">The table data.</param>
        /// <param name="fieldName">Name of the field.</param>
        /// <returns>Table field</returns>
        public TableField FindField(int tableID, byte[] tableData, string fieldName)
        {
            var tablePrefix = "";
            var json = File.ReadAllText(@"PSEM\data\TableMap.json");
            var obj = JObject.Parse(json);

            if (tableID >= 2048)
            {
                tablePrefix = "MT";
            }
            else
            {
                tablePrefix = "ST";
            }

            var fieldsStr = obj[tablePrefix + (tablePrefix == "ST" ? tableID : tableID - 2048)]["Fields"].ToString();
            var fields = JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(fieldsStr);

            var field = new TableField();

            var size = 0;
            var fieldDefArray = fields[fieldName].Split(',');
            Int32.TryParse(fieldDefArray[0], out size);

            field.Size = size;
            field.Name = fieldName;
            field.Offset = 0;

            foreach (var f in fields)
            {
                if (f.Key == fieldName)
                {
                    break;
                }
                else
                {
                    var fieldSize = 0;
                    var tempfieldDefArray = f.Value.ToString().Split(',');
                    Int32.TryParse(tempfieldDefArray[0], out fieldSize);
                    field.Offset += fieldSize;
                }
            }

            var fieldBytes = tableData.SubArray(field.Offset, field.Size).ToArray();
            if (fieldDefArray[1] == "STRING" || fieldDefArray[1] == "CHAR")
            {
                field.Value = Encoding.Default.GetString(fieldBytes, 0, fieldBytes.Length);
            }
            else
            {
                field.Value = (int)fieldBytes[0];
            }
            return field;
        }
    }
}
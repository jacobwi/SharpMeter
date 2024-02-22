using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace SharpMeter.Utils
{
    /// <summary>
    /// The extensions.
    /// </summary>
    public static class Extensions
    {
        /// <summary>
        /// Subs the array.
        /// </summary>
        /// <param name="array">The array.</param>
        /// <param name="offset">The offset.</param>
        /// <param name="length">The length.</param>
        /// <returns>An array of TS.</returns>
        public static T[] SubArray<T>(this T[] array, int offset, int length)
        {
            if (length < 0)
            {
                throw new ArgumentOutOfRangeException("length");
            }
            var result = new T[length];
            Array.Copy(array, offset, result, 0, length);
            return result;
        }

        /// <summary>
        /// Offsets to sub bits array.
        /// </summary>
        /// <param name="bits">The bits.</param>
        /// <param name="starting">The starting.</param>
        /// <param name="length">The length.</param>
        /// <returns>A BitArray.</returns>
        public static BitArray Between(this BitArray bits, int starting, int length)
        {
            var bitArray = new BitArray(length + 1);

            for (var i = 0; i <= length; i++)
            {
                bitArray[i] = bits[starting];
                starting++;
            }

            return bitArray;
        }

        /// <summary>
        /// Converts Bits array to bytes.
        /// </summary>
        /// <param name="bits">The bits.</param>
        /// <returns>An array of byte.</returns>
        public static byte[] ConvertToBytes(this BitArray bits)
        {
            var bytes = new byte[(bits.Length - 1) / 8 + 1];
            bits.CopyTo(bytes, 0);
            return bytes;
        }
        public static byte ConvertToByte(this BitArray bits)
        {
            if (bits.Count != 8)
            {
                throw new ArgumentException("bits");
            }
            var bytes = new byte[1];
            bits.CopyTo(bytes, 0);
            return bytes[0];
        }
        /// <summary>
        /// String to byte array.
        /// </summary>
        /// <param name="str">The str.</param>
        /// <returns>An array of byte.</returns>
        internal static byte[] ToByteArray(this string str)
        {
            byte[] tmp = null;

            if (null != str)
            {
                if (0 < str.Length)
                {
                    tmp = new ASCIIEncoding().GetBytes(str);
                }
            }

            return tmp;
        }

        /// <summary>
        /// Hex string to byte array.
        /// </summary>
        /// <param name="hex">The hex.</param>
        /// <returns>An array of byte.</returns>
        public static byte[] HexToByteArray(this string hex)
        {
            return Enumerable.Range(0, hex.Length)
                             .Where(x => x % 2 == 0)
                             .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                             .ToArray();
        }

        /// <summary>
        /// Hex string to binary.
        /// </summary>
        /// <param name="hexValue">The hex value.</param>
        /// <returns>A string.</returns>
        public static string HexToBinary(string hexValue)
        {
            var number = UInt64.Parse(hexValue, System.Globalization.NumberStyles.HexNumber);

            var bytes = BitConverter.GetBytes(number);

            var binaryString = string.Empty;
            foreach (var singleByte in bytes)
            {
                binaryString += Convert.ToString(singleByte, 2);
            }

            return binaryString;
        }

        /// <summary>
        /// Bits array to an int.
        /// </summary>
        /// <param name="bitArray">The bit array.</param>
        /// <returns>An int.</returns>
        public static int ToInt(this BitArray bitArray)
        {
            if (bitArray.Length > 32)
                throw new ArgumentException("Argument length shall be at most 32 bits.");

            var array = new int[1];
            bitArray.CopyTo(array, 0);
            return array[0];
        }

        /// <summary>
        /// Takes the last.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="N">The n.</param>
        /// <returns>A list of TS.</returns>
        public static IEnumerable<T> TakeLast<T>(this IEnumerable<T> source, int N)
        {
            return source.Skip(Math.Max(0, source.Count() - N));
        }

        /// <summary>
        /// Skips the last.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="n">The n.</param>
        /// <returns>A list of TS.</returns>
        public static IEnumerable<T> SkipLast<T>(this IEnumerable<T> source, int n)
        {
            var it = source.GetEnumerator();
            var hasRemainingItems = false;
            var cache = new Queue<T>(n + 1);

            do
            {
                if (hasRemainingItems = it.MoveNext())
                {
                    cache.Enqueue(it.Current);
                    if (cache.Count > n)
                        yield return cache.Dequeue();
                }
            } while (hasRemainingItems);
        }

        /// <summary>
        /// Swaps the bits.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>A byte.</returns>
        public static byte SwapBits(this byte value)
        {
            byte ret = 0;
            for (var pos = 0; pos != 8; ++pos)
            {
                ret = (byte)((ret << 1) | (value & 0x01));
                value = (byte)(value >> 1);
            }
            return ret;
        }

        /// <summary>
        /// Splits the camel case.
        /// </summary>
        /// <param name="str">The str.</param>
        /// <returns>A string.</returns>
        public static string SplitCamelCase(this string str)
        {
            return Regex.Replace(
                Regex.Replace(
                    str,
                    @"(\P{Ll})(\P{Ll}\p{Ll})",
                    "$1 $2"
                ),
                @"(\p{Ll})(\P{Ll})",
                "$1 $2"
            );
        }

        /// <summary>
        /// Serializes the.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="fileName">The file name.</param>
        /// <returns>A string.</returns>
        public static string Serialize<T>(this T value, string filePath, string fileName, bool isJson = true)
        {
            if (value == null)
            {
                return "ERR - Object is null";
            }
            try
            {
                if (filePath.Last() == '\\')
                {
                    filePath = filePath.Remove(filePath.Length - 1, 1);
                }
                if (!Directory.Exists($@"{filePath}"))
                {
                    throw new FileNotFoundException(filePath);
                }

                if (string.IsNullOrEmpty(fileName)) throw new ArgumentException("File name is empty");
                var jsonString = Newtonsoft.Json.JsonConvert.SerializeObject(value, Newtonsoft.Json.Formatting.Indented);
                var wrappedDocument = string.Format("{{ Meter: {0} }}", jsonString);
                var xDocument = JsonConvert.DeserializeXmlNode(wrappedDocument, "SharpMeter");
                xDocument.Save($@"{filePath}\{fileName}.xml");

                if (isJson)
                {
                    File.WriteAllText($@"{filePath}\{fileName}.json", jsonString);
                };

                return "OK";
            }
            catch (Exception ex)
            {
                return $"ERR - Object is null {ex.Message}";
            }
        }

        public static bool IsEqual(this byte[] value, byte[] other)
        {
            var Equal = false;

            if (value == null && other == null)
            {
                Equal = true;
            }
            else if (value != null && other != null && value.Length == other.Length)
            {
                var DataMatches = true;

                for (var iIndex = 0; iIndex < value.Length; iIndex++)
                {
                    if (value[iIndex] != other[iIndex])
                    {
                        DataMatches = false;
                        break;
                    }
                }

                Equal = DataMatches;
            }

            return Equal;
        }

        public static string ToHexString(this byte[] value)
        {
            var HexString = new StringBuilder();

            if (value != null)
            {
                for (var iIndex = 0; iIndex < value.Length; iIndex++)
                {
                    HexString.Append(value[iIndex].ToString("X2", CultureInfo.InvariantCulture));

                    // Add a space after all but the last byte
                    if (iIndex != value.Length - 1)
                    {
                        HexString.Append(" ");
                    }
                }
            }

            return HexString.ToString();
        }

        public static string ToLogicalName(this byte[] value)
        {
            if (value is byte[])
            {
                var buff = (byte[])value;
                if (buff.Length == 0)
                {
                    buff = new byte[6];
                }
                if (buff.Length == 6)
                {
                    return (buff[0] & 0xFF) + "." + (buff[1] & 0xFF) + "." + (buff[2] & 0xFF) + "." +
                           (buff[3] & 0xFF) + "." + (buff[4] & 0xFF) + "." + (buff[5] & 0xFF);
                }
            }
            return Convert.ToString(value);
        }

        public static byte[] ConvertLogicalAddressToBytes(this string value)
        {
            var items = value.Split('.');
            var buff = new byte[6];
            byte pos = 0;
            try
            {
                foreach (var it in items)
                {
                    buff[pos] = Convert.ToByte(it);
                    ++pos;
                }
            }
            catch { }

            Console.Write("HEX Address: " + BitConverter.ToString(buff));
            return buff;
        }
    }
}
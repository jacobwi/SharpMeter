using System;
using System.Xml.Serialization;

namespace SharpMeter.Utils
{
    /// <summary>
    /// Extension methods for enums
    /// </summary>
    public static class EnumExtensions
    {
        #region Public Methods

 
        public static string ToDescription(this Enum value)
        {
            return EnumDescriptionRetriever.RetrieveDescription(value);
        }

        public static T ParseToEnum<T>(this string strValue)
        {
            return EnumDescriptionRetriever.ParseToEnum<T>(strValue);
        }


        public static string ToSerializedValue(this Enum value)
        {
            string serializedValue = null;
            var enumType = value.GetType();
            var enumField = enumType.GetField(value.ToString());

            var xmlEnumAttributes = (XmlEnumAttribute[])enumField.GetCustomAttributes(typeof(XmlEnumAttribute), false);

            if (xmlEnumAttributes.Length > 0)
            {
                serializedValue = xmlEnumAttributes[0].Name;
            }
            else
            {
                throw new InvalidOperationException("The enum is not marked with the XmlEnum attribute");
            }

            return serializedValue;
        }

        public static T ParseToEnumFromSerializedValue<T>(this string value)
        {
            var returnValue = default(T);

            foreach (T enumValue in Enum.GetValues(typeof(T)))
            {
                var enumInfo = typeof(T).GetField(enumValue.ToString());
                var serializedValues = (XmlEnumAttribute[])enumInfo.GetCustomAttributes(typeof(XmlEnumAttribute), false);

                if (serializedValues.Length > 0)
                {
                    if (serializedValues[0].Name != value) continue;
                    returnValue = enumValue;
                    break;
                }
                else
                {
                    throw new InvalidOperationException("The enum is not marked with the XmlEnum attribute");
                }
            }

            if (returnValue != null)
            {
                return returnValue;
            }
            else
            {
                throw new FormatException("No matching serialized value.");
            }
        }

        #endregion Public Methods
    }
}
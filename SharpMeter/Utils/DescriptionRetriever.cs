using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Text;

namespace SharpMeter.Utils
{
    /// <summary>
    /// Attribute used for Descriptions of Enumeration values
    /// </summary>
    [global::System.AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
    public sealed class EnumDescriptionAttribute : Attribute
    {
        #region Public Methods

      
        public EnumDescriptionAttribute(string description)
        {
            m_Description = description;
            m_ResourceBaseName = null;
            m_ResourceType = null;
            m_StringName = null;
        }

 
        public EnumDescriptionAttribute(string resourceBaseName, Type resourceType, string stringName)
        {
            m_ResourceBaseName = resourceBaseName;
            m_ResourceType = resourceType;
            m_StringName = stringName;
            m_Description = null;
        }

        #endregion Public Methods

        #region Public Properties

 
        public string Description
        {
            get
            {
                var enumDescription = "";

                if (m_ResourceBaseName != null && m_StringName != null)
                {
                    enumDescription = GetStringFromResourceFile(m_ResourceBaseName, m_ResourceType.Assembly, m_StringName);
                }
                else
                {
                    enumDescription = m_Description;
                }

                return enumDescription;
            }
        }

        #endregion Public Properties

        #region Private Methods

 
        private static string GetStringFromResourceFile(string resourceBaseName, Assembly resourceAssembly, string stringName)
        {
            var resourceString = "";
            var resources = new ResourceManager(resourceBaseName, resourceAssembly);

            try
            {
                resourceString = resources.GetString(stringName, CultureInfo.CurrentCulture);
            }
            catch
            {
                // Just return an empty string if it can't be retrieved. Unless we are in debug mode
#if DEBUG
                throw;
#endif
            }

            return resourceString;
        }

        #endregion Private Methods

        #region Member Variables

        private string m_Description;
        private string m_ResourceBaseName;
        private Type m_ResourceType;
        private string m_StringName;

        #endregion Member Variables
    }

    /// <summary>
    /// Attribute used to determine which Event Table the event is injected
    /// </summary>
    [global::System.AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
    public sealed class EnumEventInfoAttribute : Attribute
    {
        #region Public Methods


        public EnumEventInfoAttribute(string tableId)
        {
            m_TableId = tableId;
        }

        #endregion Public Methods

        #region Public Properties

 
        public string TableId => m_TableId;

        #endregion Public Properties

        #region Member Variables

        private string m_TableId;

        #endregion Member Variables
    }

    /// <summary>
    /// Methods to be used for retrieving Descriptions
    /// </summary>
    public static class EnumDescriptionRetriever
    {
        #region Public Methods


        public static string RetrieveDescription(Enum value, string separator = null)
        {
            if (value == null)
            {
                throw new ArgumentNullException("value");
            }

            var strDescription = value.ToString();
            var enumType = value.GetType();

            var lstEnumFields = new List<FieldInfo>();
            var lstDescriptions = new List<string>();

            var enumField = enumType.GetField(strDescription);

            if (enumField != null)
            {
                // Got the enum
                lstEnumFields.Add(enumField);

                // IsDefined is much faster than GetCustomAttributes so lets call that first
                if (enumField.IsDefined(typeof(DescriptionAttribute), false))
                {
                    // Check for the Description attribute. The EnumDescription attribute
                    // will overwrite this below if it is set.
                    var attributes = (DescriptionAttribute[])enumField.GetCustomAttributes(typeof(DescriptionAttribute), false);

                    if (attributes != null && attributes.Length > 0)
                    {
                        strDescription = attributes[0].Description;
                    }
                }

                lstDescriptions.Add(strDescription);
            }
            else if (strDescription.Contains(","))
            {
                // An enum with the [Flags] attribute set with more than one flag set
                var flagsArray = strDescription.Split(new char[] { ',' });

                for (var index = 0; flagsArray != null && index < flagsArray.Length; index++)
                {
                    lstDescriptions.Add(flagsArray[index].Trim());
                    lstEnumFields.Add(enumType.GetField(flagsArray[index].Trim()));
                }
            }

            try
            {
                // Update the list of descriptions from the EnumDescription attribute
                for (var ndx = 0; ndx < lstEnumFields.Count; ndx++)
                {
                    var currentField = lstEnumFields[ndx];

                    if (!currentField.IsDefined(typeof(EnumDescriptionAttribute), false)) continue;
                    var descriptionAttributes = (EnumDescriptionAttribute[])currentField.GetCustomAttributes(typeof(EnumDescriptionAttribute), false);

                    if (descriptionAttributes.Length > 0)
                    {
                        lstDescriptions[ndx] = descriptionAttributes[0].Description;
                    }
                }

                // Build the string to return if no exception was thrown.
                var str = new StringBuilder();
                for (var ndx = 0; ndx < lstDescriptions.Count; ndx++)
                {
                    str.Append(lstDescriptions[ndx]);

                    if (string.IsNullOrEmpty(separator) == false && ndx < lstDescriptions.Count - 1)
                    {
                        str.Append(separator);
                    }
                }

                strDescription = str.ToString();
            }
            catch (Exception)
            {
                // This probably means that the enum is not marked with the attribute so we should just return the default
            }

            return strDescription;
        }


        public static string RetrieveEventTableInfo(Enum value)
        {
            var strTableId = value.ToString();
            var enumType = value.GetType();
            var enumField = enumType.GetField(value.ToString());

            try
            {
                var eventTableId = (EnumEventInfoAttribute[])enumField.GetCustomAttributes(typeof(EnumEventInfoAttribute), false);

                if (eventTableId.Length > 0)
                {
                    strTableId = eventTableId[0].TableId;
                }
            }
            catch (Exception)
            {
            }

            return strTableId;
        }

#if(!WindowsCE)

        public static T ParseToEnum<T>(string strValue)
        {
            var returnValue = default(T);

            foreach (T enumValue in Enum.GetValues(typeof(T)))
            {
                var enumInfo = typeof(T).GetField(enumValue.ToString());
                var descriptions = (EnumDescriptionAttribute[])enumInfo.GetCustomAttributes(typeof(EnumDescriptionAttribute), false);

                if (descriptions.Length <= 0) continue;
                if (descriptions[0].Description != strValue) continue;
                returnValue = enumValue;
                break;
            }

            if (returnValue != null)
            {
                return returnValue;
            }
            else
            {
                throw new FormatException("Could not parse to Enum: No matching description.");
            }
        }

#endif


        private static IEnumerable<T> GetValues<T>()
        {
            var enumObject = (T)Activator.CreateInstance(typeof(T));

            // The Compact Framework does not support Enum.GetValues() so this will give us the equivalent results.

            return typeof(T).GetFields(BindingFlags.Public | BindingFlags.Static).Select(currentFieldInfo => (T)currentFieldInfo.GetValue(enumObject)).ToList();
        }

        public static IEnumerable<string> GetValueDescriptions<T>()
        {
            var values = GetValues<T>();

            return values.Select(currentValue => (Enum)(object)currentValue).Select(enumValue => EnumDescriptionRetriever.RetrieveDescription(enumValue)).ToList();
        }

        #endregion Public Methods
    }
}
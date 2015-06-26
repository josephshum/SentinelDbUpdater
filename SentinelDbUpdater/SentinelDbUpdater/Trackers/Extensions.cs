
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.XPath;

namespace SentinelDbUpdater.Trackers
{
    public static class Extensions
    {
        /// <summary>Initial value for ToJavascriptMilliseconds</summary>
        private static readonly long DatetimeMinTimeTicks = (new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).Ticks;

        #region Private methods
        /// <summary>Gets an attribute value from the XML back as the specified data type</summary>
        /// <typeparam name="T">The type to return as</typeparam>
        /// <param name="navigableItem">The node to scan for the attribute value</param>
        /// <param name="attributeName">The attribute to pull the value from</param>
        /// <param name="fallbackValue">If the fallback attribute and the attribute are empty then use the fallback value</param>
        /// <param name="outValue">A form of the specified attribute value in the specified type</param>
        /// <returns>True if value did not fall back to the fallbackValue, otherwise false</returns>
        private static bool GetAttribute<T>(this IXPathNavigable navigableItem, string attributeName, T fallbackValue, out T outValue)
        {
            bool returnValue;
            outValue = fallbackValue;
            if(null == navigableItem)
            {
                return false;
            }

            var currentType = typeof(T);

            if(currentType.IsNullable())
            {
                currentType = Nullable.GetUnderlyingType(currentType);
            }

            object tempObject = null;
            switch(currentType.Name)
            {
                case "Boolean":
                    tempObject = navigableItem.GetAttributeBoolean(attributeName, fallbackValue as bool?);
                    returnValue = true;
                    break;
                case "String":
                    var tempFallbackValue = (null == fallbackValue) ? string.Empty : fallbackValue.ToString();
                    tempObject = navigableItem.GetAttributeString(attributeName, tempFallbackValue);
                    returnValue = true;
                    break;
                case "Int32":
                    var intValue = navigableItem.GetAttributeString(attributeName, string.Empty);
                    tempObject = 0;
                    returnValue = ProcessInt(intValue, fallbackValue.Cast(0), ref tempObject);
                    break;
                case "Double":
                    string doubleValue = navigableItem.GetAttributeString(attributeName, string.Empty);
                    tempObject = 0.0;
                    returnValue = ProcessDouble(doubleValue, fallbackValue.Cast(0.0), ref tempObject);
                    break;
                case "DateTime":
                    string dateTimeValue = navigableItem.GetAttributeString(attributeName, string.Empty);
                    tempObject = DateTime.MinValue;
                    returnValue = ProcessDataTime(dateTimeValue, null, ref tempObject);
                    break;
                case "Uri":
                    tempObject = navigableItem.GetAttributeUri(attributeName);
                    returnValue = true;
                    break;
                default:
                    if(currentType.IsEnum)
                    {
                        outValue = navigableItem.GetAttributeEnum(attributeName, fallbackValue);
                        returnValue = true;
                    }
                    else
                    {
                        //Unsupported type at the moment
                        throw new Exception("Value specified is not a suported type for reading from XML.");
                    }
                    break;
            }

            if(null != tempObject)
            {
                outValue = (T)tempObject;
            }

            return returnValue;
        }

        /// <summary>Gets an attribute value from the XML back as a bool data type</summary>
        /// <param name="navigableItem">The node to scan for the attribute value</param>
        /// <param name="attributeName">The attribute to pull the value from</param>
        /// <param name="fallbackValue">If the attribute is null or does not exist then use the fallback value</param>
        /// <returns>a bool data type form of the specified attribute value</returns>
        private static bool? GetAttributeBoolean(this IXPathNavigable navigableItem, string attributeName, bool? fallbackValue)
        {
            var returnValue = fallbackValue;
            var tempValue = navigableItem.GetAttribute<string>(attributeName);

            if(!string.IsNullOrWhiteSpace(tempValue))
            {
                returnValue = Regex.IsMatch(tempValue, "^(true|1|yes|on)$");
            }

            return returnValue;
        }

        /// <summary>Gets an attribute value from the XML back as a string data type</summary>
        /// <param name="navigableItem">The node to scan for the attribute value</param>
        /// <param name="attributeName">The attribute to pull the value from</param>
        /// <param name="fallbackValue">If the attribute is empty or does not exist then use the fallback value</param>
        /// <returns>a string data type form of the specified attribute value</returns>
        private static string GetAttributeString(this IXPathNavigable navigableItem, string attributeName, string fallbackValue)
        {
            var returnValue = fallbackValue;
            if(null == navigableItem)
            {
                return returnValue;
            }
            if(string.IsNullOrWhiteSpace(attributeName))
            {
                return returnValue;
            }
            var xac = ((XmlNode)navigableItem).Attributes;
            if(null == xac)
            {
                return returnValue;
            }
            var xa = xac[attributeName];
            if(null == xa)
            {
                return returnValue;
            }
            if(!string.IsNullOrEmpty(xa.Value))
            {
                returnValue = xa.Value;
            }
            return returnValue;
        }

        /// <summary>Gets an attribute value from the XML back as the specified enum value</summary>
        /// <typeparam name="T">The enum type to return as</typeparam>
        /// <param name="navigableItem">The node to scan for the attribute value</param>
        /// <param name="attributeName">The attribute to pull the value from</param>
        /// <param name="fallbackValue">If the attribute value is empty or fails to convert properly then use the fallback value</param>
        /// <returns>a enum data type form of the specified attribute value</returns>
        private static T GetAttributeEnum<T>(this IXPathNavigable navigableItem, string attributeName, T fallbackValue)
        {
            return navigableItem.GetAttribute<string>(attributeName).ToEnum<T>(fallbackValue);
        }

        /// <summary>Gets an attribute value from the XML back as the specified URI value</summary>
        /// <param name="navigableItem">The node to scan for the attribute value</param>
        /// <param name="attributeName">The attribute to pull the value from</param>
        /// <returns>a URI data type form of the specified attribute value</returns>
        private static Uri GetAttributeUri(this IXPathNavigable navigableItem, string attributeName)
        {
            Uri returnValue = null;
            if(null == navigableItem)
            {
                return null;
            }
            if(string.IsNullOrWhiteSpace(attributeName))
            {
                return null;
            }
            var xac = ((XmlNode)navigableItem).Attributes;
            if(null == xac)
            {
                return null;
            }
            var xa = xac[attributeName];
            if(null == xa)
            {
                return null;
            }
            if(!string.IsNullOrWhiteSpace(xa.Value))
            {
                returnValue = new Uri(xa.Value);
            }
            return returnValue;
        }

        /// <summary>Processes a value from a string and converts it to a int.</summary>
        /// <param name="inputValue">The value to process.</param>
        /// <param name="fallbackValue">The fallback value in case the string cannot be processed.</param>
        /// <param name="returnObject">A reference to the returnObject that is used for later processing.</param>
        /// <returns>true if the value converted correctly; otherwise false when the fallback value is used.</returns>
        private static bool ProcessInt(string inputValue, int fallbackValue, ref object returnObject)
        {
            var returnValue = false;

            if(string.IsNullOrWhiteSpace(inputValue))
            {
                return false;
            }
            switch(inputValue)
            {
                case "MaxValue":
                    returnObject = int.MaxValue;
                    returnValue = true;
                    break;
                case "MinValue":
                    returnObject = int.MinValue;
                    returnValue = true;
                    break;
                default:
                    int outValue;
                    if(int.TryParse(inputValue, out outValue))
                    {
                        returnObject = outValue;
                        returnValue = true;
                    }
                    else
                    {
                        returnObject = fallbackValue;
                    }
                    break;
            }

            return returnValue;
        }

        /// <summary>Processes a value from a string and converts it to a DateTime.</summary>
        /// <param name="inputValue">The value to process.</param>
        /// <param name="fallbackValue">The fallback value in case the string cannot be processed.</param>
        /// <param name="returnObject">A reference to the returnObject that is used for later processing.</param>
        /// <returns>true if the value converted correctly; otherwise false when the fallback value is used.</returns>
        private static bool ProcessDataTime(string inputValue, DateTime? fallbackValue, ref object returnObject)
        {
            var returnValue = false;

            if(string.IsNullOrWhiteSpace(inputValue))
            {
                return false;
            }
            switch(inputValue)
            {
                case "MaxValue":
                    returnObject = DateTime.MaxValue;
                    returnValue = true;
                    break;
                case "MinValue":
                    returnObject = DateTime.MinValue;
                    returnValue = true;
                    break;
                case "Now":
                    returnObject = DateTime.Now;
                    returnValue = true;
                    break;
                case "Today":
                    returnObject = DateTime.Today;
                    returnValue = true;
                    break;
                case "UtcNow":
                    returnObject = DateTime.UtcNow;
                    returnValue = true;
                    break;
                default:
                    DateTime outValue;
                    if(DateTime.TryParse(inputValue, out outValue))
                    {
                        returnObject = outValue;
                        returnValue = true;
                    }
                    else
                    {
                        returnObject = fallbackValue;
                    }
                    break;
            }

            return returnValue;
        }

        /// <summary>Processes a value from a string and converts it to a double.</summary>
        /// <param name="inputValue">The value to process.</param>
        /// <param name="fallbackValue">The fallback value in case the string cannot be processed.</param>
        /// <param name="returnObject">A reference to the returnObject that is used for later processing.</param>
        /// <returns>true if the value converted correctly; otherwise false when the fallback value is used.</returns>
        private static bool ProcessDouble(string inputValue, double fallbackValue, ref object returnObject)
        {
            var returnValue = false;

            if(string.IsNullOrWhiteSpace(inputValue))
            {
                return false;
            }
            switch(inputValue)
            {
                case "MaxValue":
                    returnObject = double.MaxValue;
                    returnValue = true;
                    break;
                case "MinValue":
                    returnObject = double.MinValue;
                    returnValue = true;
                    break;
                case "Now":
                    returnObject = DateTime.Now.Ticks;
                    returnValue = true;
                    break;
                default:
                    double outValue;
                    if(double.TryParse(inputValue, out outValue))
                    {
                        returnObject = outValue;
                        returnValue = true;
                    }
                    else
                    {
                        returnObject = fallbackValue;
                    }
                    break;
            }

            return returnValue;
        }
        #endregion

        /// <summary>Determines if a type is a Nullable type</summary>
        /// <param name="source">The type to examine</param>
        /// <returns>true if the type is a Nullable type; otherwise false</returns>
        /// <exception cref="InvalidOperationException"></exception>
        public static bool IsNullable(this Type source)
        {
            return (null != source) && (source.IsGenericType && (source.GetGenericTypeDefinition() == typeof (Nullable<>)));
        }

        /// <summary>Converts a string to a specified enum type that matches the string input value</summary>
        /// <typeparam name="T">The type of enum to convert to</typeparam>
        /// <param name="value">The string representation that needs to match an enum value</param>
        /// <param name="fallbackValue">The default enum value to fall back to if processing fails</param>
        /// <returns>An enum value</returns>
        /// <exception cref="ArgumentException"></exception>
        public static T ToEnum<T>(this string value, T fallbackValue)
        {
            T returnValue = fallbackValue;

            if(string.IsNullOrWhiteSpace(value))
            {
                return returnValue;
            }

            // Correct XML value if it has an invalid character
            value = Regex.Replace(value, @"[^\d\w]", string.Empty);

            var currentType = typeof(T);

            if(!currentType.IsEnum)
            {
                currentType = Nullable.GetUnderlyingType(currentType);
            }

            if(currentType.IsEnum)
            {
                var enumList = Enum.GetNames(currentType);
                if(enumList.Contains(value, StringComparer.OrdinalIgnoreCase))
                {
                    returnValue = (T)Enum.Parse(currentType, value, true);
                }
                else
                {
                    throw new ArgumentException("Value specified is not an Enum value.");
                }
            }
            else
            {
                throw new ArgumentException("Type specified is not an Enum type.");
            }

            return returnValue;
        }

        /// <summary>Convert DateTimeOffset to Javascript milliseconds</summary>
        /// <param name="dt">A DataTimeOffset value</param>
        /// <returns>The offset time in milliseconds</returns>
        public static long ToJavaScriptMilliseconds(this DateTimeOffset? dt)
        {
            Debug.Assert(dt != null, "dt != null");
            return (dt.Value.ToUniversalTime().Ticks - DatetimeMinTimeTicks) / 10000;
        }

        /// <summary>Gets a SHA1 hash of the specified string</summary>
        /// <param name="value">The string to hash</param>
        /// <returns>A SHA1 hash</returns>
        public static string GetHashSha1(this string value)
        {
            var returnValue = string.Empty;

            var bytes = Encoding.Unicode.GetBytes(value);
            using(SHA1 sha = new SHA1Managed())
            {
                var hash = sha.ComputeHash(bytes);
                returnValue = hash.Aggregate(returnValue, (current, x) => current + string.Format("{0:x2}", x));
            }

            return returnValue;
        }

        /// <summary>Gets an attribute value from the XML back as the specified data type</summary>
        /// <typeparam name="T">The type to return as</typeparam>
        /// <param name="navigableItem">The node to scan for the attribute value</param>
        /// <param name="attributeName">The attribute to pull the value from</param>
        /// <returns>A form of the specified attribute value in the specified type</returns>
        public static T GetAttribute<T>(this IXPathNavigable navigableItem, string attributeName)
        {
            return navigableItem.GetAttribute(attributeName, default(T));
        }

        /// <summary>Gets an attribute value from the XML back as the specified data type</summary>
        /// <typeparam name="T">The type to return as</typeparam>
        /// <param name="navigableItem">The node to scan for the attribute value</param>
        /// <param name="attributeName">The attribute to pull the value from</param>
        /// <param name="fallbackValue">If the fallback attribute and the attribute are empty then use the fallback value</param>
        /// <returns>A form of the specified attribute value in the specified type</returns>
        public static T GetAttribute<T>(this IXPathNavigable navigableItem, string attributeName, T fallbackValue)
        {
            T outValue;
            if(!GetAttribute(navigableItem, attributeName, fallbackValue, out outValue))
            {
                outValue = fallbackValue;
            }
            return outValue;
        }

        /// <summary>Converts an object from one type to another. If the conversion fails return a default value.</summary>
        /// <typeparam name="T">The type to covert to</typeparam>
        /// <param name="source">The source object to convert</param>
        /// <param name="defaultValue">A default value to use if the conversion fails</param>
        /// <returns>The converted object</returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="InvalidCastException"></exception>
        public static T Cast<T>(this object source, T defaultValue)
        {
            return (null == source) ? defaultValue : (T) Convert.ChangeType(source, typeof (T), CultureInfo.InvariantCulture);
        }

        /// <summary>Performs an exact clone of all members of the specified item</summary>
        /// <typeparam name="T">The type of the item to clone</typeparam>
        /// <param name="item">The item to copy/clone</param>
        /// <returns>An exact deep clone of the original object</returns>
        public static T DeepClone<T>(this T item)
        {
            using(var stream = new MemoryStream())
            {
                var formatter = new BinaryFormatter();
                formatter.Serialize(stream, item);
                stream.Position = 0;
                return (T)formatter.Deserialize(stream);
            }
        }

        /// <summary>Determines if null, empty of whitespace only</summary>
        /// <param name="input">The input string</param>
        /// <param name="fallback">The fallback string</param>
        /// <returns>if null, empty or whitespace only then use fallback, otherwise original string</returns>
        public static string IsNullOrEmptyOrWhiteSpace(this string input, string fallback)
        {
            return !string.IsNullOrWhiteSpace(input) ? input.Trim() : fallback;
        }
    }
}
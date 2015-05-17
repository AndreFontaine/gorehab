// -----------------------------------------------------------------------
// <copyright file="JsonSerializationExtensions.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Microsoft.Samples.Kinect.Webserver
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Reflection;
    using System.Runtime.Serialization;
    using System.Text;
    using System.Threading.Tasks;
    using System.Web.Script.Serialization;

    using Microsoft.Samples.Kinect.Webserver.Properties;

    /// <summary>
    /// Static class that defines extensions used to serialize/deserialize objects to/from
    /// JSON strings.
    /// </summary>
    public static class JsonSerializationExtensions
    {
        /// <summary>
        /// Serialization default buffer size.
        /// </summary>
        private const int BufferSize = 512;

        /// <summary>
        /// Serialize specified object to a stream as UTF8-encoded JSON.
        /// </summary>
        /// <typeparam name="T">
        /// Type of object to serialize.
        /// </typeparam>
        /// <param name="obj">
        /// Object to serialize. 
        /// </param>
        /// <param name="outputStream">
        /// Stream where UTF8-encoded JSON representing object will be output.
        /// </param>
        public static void ToJson<T>(this T obj, Stream outputStream)
        {
            using (var writer = new StreamWriter(outputStream, new UTF8Encoding(false), BufferSize, true))
            {
                writer.Write(obj.ToJson());
            }
        }

        /// <summary>
        /// Serialize specified object to a JSON string.
        /// </summary>
        /// <typeparam name="T">
        /// Type of object to serialize.
        /// </typeparam>
        /// <param name="obj">
        /// Object to serialize. 
        /// </param>
        /// <returns>
        /// JSON string representing serialized object.
        /// </returns>
        public static string ToJson<T>(this T obj)
        {
            return (new JavaScriptSerializer()).Serialize(obj);
        }

        /// <summary>
        /// Serialize specified dictionary to a stream as UTF8-encoded JSON.
        /// </summary>
        /// <param name="dictionary">
        /// Dictionary to serialize. 
        /// </param>
        /// <param name="outputStream">
        /// Stream where UTF8-encoded JSON representing dictionary will be output.
        /// </param>
        /// <remarks>
        /// <para>
        /// Only IDictionary&lt;string,object&gt;  objects and default types will be
        /// recognized by the serializer.
        /// </para>
        /// <para>
        /// Dictionaries mapping string keys to object values will be treated as direct
        /// representations of JSON objects, so that a dictionary that contains:
        /// <list type="bullet">
        /// <item>
        /// <description>a key "foo" that maps to a numeric value of 23</description>
        /// </item>
        /// <item>
        /// <description>a key "bar" that maps to a string value of "tar"</description>
        /// </item>
        /// </list>
        /// will be serialized as {"foo":23,"bar":"tar"}, rather than as
        /// [{"Key":"foo","Value":23},{"Key":"bar","Value":"tar"}], which is the default
        /// way to serialize dictionary objects as JSON.
        /// </para>
        /// <para>
        /// This method does not look for circular references and therefore does not
        /// support them.
        /// </para>
        /// </remarks>
        public static void DictionaryToJson(this IDictionary<string, object> dictionary, Stream outputStream)
        {
            // Leave output stream open after we're done writing
            using (var writer = new StreamWriter(outputStream, new UTF8Encoding(false), BufferSize, true))
            {
                writer.Write(dictionary.DictionaryToJson());
            }
        }

        /// <summary>
        /// Asynchronously serialize specified dictionary to a stream as UTF8-encoded JSON.
        /// </summary>
        /// <param name="dictionary">
        /// Dictionary to serialize. 
        /// </param>
        /// <param name="outputStream">
        /// Stream where UTF8-encoded JSON representing dictionary will be output.
        /// </param>
        /// <returns>
        /// Await-able task.
        /// </returns>
        /// <remarks>
        /// <para>
        /// Only IDictionary&lt;string,object&gt; objects and default types will be
        /// recognized by the serializer.
        /// </para>
        /// <para>
        /// Dictionaries mapping string keys to object values will be treated as direct
        /// representations of JSON objects, so that a dictionary that contains:
        /// <list type="bullet">
        /// <item>
        /// <description>a key "foo" that maps to a numeric value of 23</description>
        /// </item>
        /// <item>
        /// <description>a key "bar" that maps to a string value of "tar"</description>
        /// </item>
        /// </list>
        /// will be serialized as {"foo":23,"bar":"tar"}, rather than as
        /// [{"Key":"foo","Value":23},{"Key":"bar","Value":"tar"}], which is the default
        /// way to serialize dictionary objects as JSON.
        /// </para>
        /// <para>
        /// This method does not look for circular references and therefore does not
        /// support them.
        /// </para>
        /// </remarks>
        public static async Task DictionaryToJsonAsync(this IDictionary<string, object> dictionary, Stream outputStream)
        {
            // Leave output stream open after we're done writing
            using (var writer = new StreamWriter(outputStream, new UTF8Encoding(false), BufferSize, true))
            {
                await writer.WriteAsync(dictionary.DictionaryToJson());
            }
        }

        /// <summary>
        /// Serialize specified dictionary to a JSON string.
        /// </summary>
        /// <param name="dictionary">
        /// Dictionary to serialize. 
        /// </param>
        /// <returns>
        /// JSON string representing serialized object.
        /// </returns>
        /// <remarks>
        /// <para>
        /// Only IDictionary&lt;string,object&gt; objects and default types will be
        /// recognized by the serializer.
        /// </para>
        /// <para>
        /// Dictionaries mapping string keys to object values will be treated as direct
        /// representations of JSON objects, so that a dictionary that contains:
        /// <list type="bullet">
        /// <item>
        /// <description>a key "foo" that maps to a numeric value of 23</description>
        /// </item>
        /// <item>
        /// <description>a key "bar" that maps to a string value of "tar"</description>
        /// </item>
        /// </list>
        /// will be serialized as {"foo":23,"bar":"tar"}, rather than as
        /// [{"Key":"foo","Value":23},{"Key":"bar","Value":"tar"}], which is the default
        /// way to serialize dictionary objects as JSON.
        /// </para>
        /// <para>
        /// This method does not look for circular references and therefore does not
        /// support them.
        /// </para>
        /// </remarks>
        public static string DictionaryToJson(this IDictionary<string, object> dictionary)
        {
            return (new JavaScriptSerializer()).Serialize(dictionary);
        }
        
        /// <summary>
        /// Deserialize specified object from UTF8-encoded JSON read from a stream.
        /// </summary>
        /// <typeparam name="T">
        /// Type of object to deserialize.
        /// </typeparam>
        /// <param name="inputStream">
        /// Stream from which to read UTF8-encoded JSON representing serialized object.
        /// </param>
        /// <returns>
        /// Deserialized object corresponding to input JSON.
        /// </returns>
        public static T FromJson<T>(this Stream inputStream)
        {
            using (var reader = new StreamReader(inputStream, Encoding.UTF8))
            {
                return reader.ReadToEnd().FromJson<T>();
            }
        }

        /// <summary>
        /// Deserialize specified object from a JSON string.
        /// </summary>
        /// <typeparam name="T">
        /// Type of object to deserialize.
        /// </typeparam>
        /// <param name="input">
        /// JSON string representing serialized object.
        /// </param>
        /// <returns>
        /// Deserialized object corresponding to JSON string.
        /// </returns>
        /// <remarks>
        /// Errors encountered during serialization might throw SerializationException.
        /// </remarks>
        public static T FromJson<T>(this string input)
        {
            try
            {
                return (new JavaScriptSerializer()).Deserialize<T>(input);

                // Convert exceptions to Serialization exception to provide a single exception to
                // catch for callers.
            }
            catch (ArgumentException e)
            {
                throw new SerializationException(@"Exception encountered deserializing JSON string", e);
            }
            catch (InvalidOperationException e)
            {
                throw new SerializationException(@"Exception encountered deserializing JSON string", e);
            }
        }

        /// <summary>
        /// Deserialize specified dictionary from a stream as UTF8-encoded JSON.
        /// </summary>
        /// <param name="inputStream">
        /// Stream containing UTF8-encoded JSON representation of dictionary.
        /// </param>
        /// <returns>
        /// Deserialized dictionary.
        /// </returns>
        /// <remarks>
        /// <para>
        /// Dictionaries mapping string keys to object values will be treated as direct
        /// representations of JSON objects, so that a JSON object such as
        /// {"foo":23,"bar":"tar"} will be deserialized as a dictionary that contains:
        /// <list type="bullet">
        /// <item>
        /// <description>a key "foo" that maps to a numeric value of 23</description>
        /// </item>
        /// <item>
        /// <description>a key "bar" that maps to a string value of "tar"</description>
        /// </item>
        /// </list>
        /// </para>
        /// <para>
        /// This method does not look for circular references and therefore does not
        /// support them.
        /// </para>
        /// </remarks>
        public static Dictionary<string, object> DictionaryFromJson(this Stream inputStream)
        {
            using (var reader = new StreamReader(inputStream, Encoding.UTF8))
            {
                return reader.ReadToEnd().DictionaryFromJson();
            }
        }

        /// <summary>
        /// Asynchronously deserialize specified dictionary from a stream as UTF8-encoded JSON.
        /// </summary>
        /// <param name="inputStream">
        /// Stream containing UTF8-encoded JSON representation of dictionary.
        /// </param>
        /// <returns>
        /// Await-able task Deserialized dictionary.
        /// </returns>
        /// <remarks>
        /// <para>
        /// Dictionaries mapping string keys to object values will be treated as direct
        /// representations of JSON objects, so that a JSON object such as
        /// {"foo":23,"bar":"tar"} will be deserialized as a dictionary that contains:
        /// <list type="bullet">
        /// <item>
        /// <description>a key "foo" that maps to a numeric value of 23</description>
        /// </item>
        /// <item>
        /// <description>a key "bar" that maps to a string value of "tar"</description>
        /// </item>
        /// </list>
        /// </para>
        /// <para>
        /// This method does not look for circular references and therefore does not
        /// support them.
        /// </para>
        /// </remarks>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures", Justification = "Clients won't have to create nested structure themselves. They will just await on task to get dictionary object.")]
        public static async Task<Dictionary<string, object>> DictionaryFromJsonAsync(this Stream inputStream)
        {
            using (var reader = new StreamReader(inputStream, Encoding.UTF8))
            {
                var input = await reader.ReadToEndAsync();
                return input.DictionaryFromJson();
            }
        }

        /// <summary>
        /// Deserialize specified dictionary from a JSON string.
        /// </summary>
        /// <param name="input">
        /// JSON string containing JSON representation of dictionary.
        /// </param>
        /// <returns>
        /// Deserialized dictionary.
        /// </returns>
        /// <remarks>
        /// <para>
        /// Dictionaries mapping string keys to object values will be treated as direct
        /// representations of JSON objects, so that a JSON object such as
        /// {"foo":23,"bar":"tar"} will be deserialized as a dictionary that contains:
        /// <list type="bullet">
        /// <item>
        /// <description>a key "foo" that maps to a numeric value of 23</description>
        /// </item>
        /// <item>
        /// <description>a key "bar" that maps to a string value of "tar"</description>
        /// </item>
        /// </list>
        /// </para>
        /// <para>
        /// This method does not look for circular references and therefore does not
        /// support them.
        /// </para>
        /// <para>
        /// Errors encountered during serialization might throw SerializationException.
        /// </para>
        /// </remarks>
        public static Dictionary<string, object> DictionaryFromJson(this string input)
        {
            try
            {
                return (new JavaScriptSerializer()).Deserialize<Dictionary<string, object>>(input);

                // Convert exceptions to Serialization exception to provide a single exception to
                // catch for callers.
            }
            catch (ArgumentException e)
            {
                throw new SerializationException(@"Exception encountered deserializing JSON string", e);
            }
            catch (InvalidOperationException e)
            {
                throw new SerializationException(@"Exception encountered deserializing JSON string", e);
            }
        }

        /// <summary>
        /// Extract serializable JSON data from specified value, ensuring that property names
        /// match JSON naming conventions (camel case).
        /// </summary>
        /// <param name="value">The object to be converted.</param>
        /// <returns>The converted object.</returns>
        internal static object ExtractSerializableJsonData(object value)
        {
            if (value == null || IsSerializationPrimitive(value))
            {
                return value;
            }

            if (IsDictionary(value))
            {
                // The key type for the given dictionary must be string type.
                if (!IsSerializableGenericDictionary(value) && !IsSerializableDictionary(value))
                {
                    throw new NotSupportedException(Resources.UnsupportedKeyType);
                }

                var result = new Dictionary<string, object>();
                var dict = (IDictionary)value;

                foreach (var key in dict.Keys)
                {
                    result.Add((string)key, ExtractSerializableJsonData(dict[key]));
                }

                return result;
            }

            if (IsEnumerable(value))
            {
                // For the object with IEnumerable interface, serialize each items in it.
                var result = new List<object>();

                foreach (var v in (IEnumerable)value)
                {
                    result.Add(ExtractSerializableJsonData(v));
                }

                return result;
            }
            else
            {
                var result = new Dictionary<string, object>();

                foreach (var p in value.GetType().GetProperties())
                {
                    if (IsSerializableProperty(p))
                    {
                        result.Add(ToCamelCase(p.Name), ExtractSerializableJsonData(p.GetValue(value)));
                    }
                }

                return result;
            }
        }

        /// <summary>
        /// Filters the classes represented in an array of Type objects.
        /// </summary>
        /// <param name="type">The Type object to which the filter is applied.</param>
        /// <param name="criteria">An arbitrary object used to filter the list.</param>
        /// <returns>True to include the Type in the filtered list; otherwise false.</returns>
        private static bool InterfaceTypeFilter(Type type, object criteria)
        {
            var targetType = criteria as Type;

            if (type.IsGenericType)
            {
                return type.GetGenericTypeDefinition() == targetType;
            }

            return targetType != null && type == targetType;
        }

        /// <summary>
        /// Find the target interface from a given object.
        /// </summary>
        /// <param name="value">The given object.</param>
        /// <param name="targetInterface">The target interface to be found.</param>
        /// <returns>The interface which has been found.</returns>
        private static Type FindTargetInterface(object value, Type targetInterface)
        {
            var types = value.GetType().FindInterfaces(InterfaceTypeFilter, targetInterface);
            return (types.Length > 0) ? types[0] : null;
        }

        /// <summary>
        /// Determine if the given object has a serializable generic dictionary interface.
        /// </summary>
        /// <param name="value">The given object.</param>
        /// <returns>Returns true if it has the interface; otherwise false.</returns>
        private static bool IsSerializableGenericDictionary(object value)
        {
            var genericDictType = typeof(IDictionary<object, object>).GetGenericTypeDefinition();
            var type = FindTargetInterface(value, genericDictType);

            if (type != null)
            {
                // Only the dictionaries with string type keys are serializable.
                var argumentTypes = type.GetGenericArguments();
                return argumentTypes.Length > 0 && argumentTypes[0] == typeof(string);
            }

            return false;
        }

        /// <summary>
        /// Determine if the given object has a serializable dictionary interface.
        /// </summary>
        /// <param name="value">The given object.</param>
        /// <returns>Returns true if it has the interface; otherwise false.</returns>
        private static bool IsSerializableDictionary(object value)
        {
            var type = FindTargetInterface(value, typeof(IDictionary));

            if (type == null)
            {
                return false;
            }

            foreach (var key in ((IDictionary)value).Keys)
            {
                if (!(key is string))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Determine if the given object has a dictionary or generic dictionary interface.
        /// </summary>
        /// <param name="value">The given object.</param>
        /// <returns>Returns true if it has the interface; otherwise false.</returns>
        private static bool IsDictionary(object value)
        {
            var dictType = FindTargetInterface(value, typeof(IDictionary));
            if (dictType == null)
            {
                dictType = FindTargetInterface(value, typeof(IDictionary<object, object>).GetGenericTypeDefinition());
            }

            return dictType != null;
        }

        /// <summary>
        /// Determine if the given object has an IEnumerable interface.
        /// </summary>
        /// <param name="value">The given object.</param>
        /// <returns>Returns true if it has the interface; otherwise false.</returns>
        private static bool IsEnumerable(object value)
        {
            var type = FindTargetInterface(value, typeof(IEnumerable));
            return type != null;
        }

        /// <summary>
        /// Convert the given string to camelCase.
        /// </summary>
        /// <param name="text">The given string.</param>
        /// <returns>The returned string.</returns>
        private static string ToCamelCase(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return text;
            }

            return string.Format(CultureInfo.CurrentCulture, "{0}{1}", char.ToLower(text[0], CultureInfo.CurrentCulture), text.Substring(1));
        }

        /// <summary>
        /// Determine if the given object is a primitive to be serialized.
        /// </summary>
        /// <param name="o">The input object.</param>
        /// <returns>Return true if it is a serialization primitive, otherwise return false.</returns>
        private static bool IsSerializationPrimitive(object o)
        {
            Type t = o.GetType();
            return t.IsPrimitive || t.IsEnum || t == typeof(string) || t == typeof(DateTime);
        }

        /// <summary>
        /// Determine if the given property is serializable.
        /// </summary>
        /// <param name="propertyInfo">The input property.</param>
        /// <returns>Return true if it is serializable, otherwise return false.</returns>
        private static bool IsSerializableProperty(PropertyInfo propertyInfo)
        {
            foreach (var accessor in propertyInfo.GetAccessors())
            {
                if (accessor.IsPublic && !accessor.IsStatic)
                {
                    return propertyInfo.GetGetMethod(false) != null && propertyInfo.CanWrite;
                }
            }

            return false;
        }
    }
}

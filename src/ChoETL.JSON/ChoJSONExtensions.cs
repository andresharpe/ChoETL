﻿using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    public static class ChoJSONExtensions
    {
        static ChoJSONExtensions()
        {
        }

        public static JsonWriter CreateJSONWriter(this StringBuilder sb)
        {
            ChoGuard.ArgumentNotNull(sb, nameof(sb));
            return CreateJSONWriter(new StringWriter(sb));
        }

        public static JsonWriter CreateJSONWriter(this string filePath)
        {
            ChoGuard.ArgumentNotNull(filePath, nameof(filePath));
            return CreateJSONWriter(new StreamWriter(filePath));
        }

        public static JsonWriter CreateJSONWriter(this TextWriter writer)
        {
            ChoGuard.ArgumentNotNull(writer, nameof(writer));

            JsonWriter jwriter = new JsonTextWriter(writer);
            jwriter.Formatting = Newtonsoft.Json.Formatting.None;
            return jwriter;
        }
        public static void WriteFormattedRawValue(this JsonWriter writer, string json, Action<JsonReader> setup = null)
        {
            if (json == null)
                writer.WriteRawValue(json);
            else
            {
                if (setup == null)
                {
                    setup = (rd) =>
                    {
                        rd.DateParseHandling = DateParseHandling.None;
                        rd.FloatParseHandling = default;
                    };
                }
                using (var reader = new JsonTextReader(new StringReader(json)))
                {
                    setup(reader);
                    writer.WriteToken(reader);
                }
            }
        }

        public static void WriteReader(this JsonWriter writer, JsonReader reader, ChoJObjectLoadOptions? options = null)
        {
            ChoGuard.ArgumentNotNull(reader, nameof(reader));
            ChoGuard.ArgumentNotNull(writer, nameof(writer));

            if (options == null)
                options = ChoJObjectLoadOptions.All;

            writer.WriteStartObject();
            writer.WriteToReader(reader, options);
        }

        private static void WriteToReader(this JsonWriter writer, JsonReader reader, ChoJObjectLoadOptions? options = null)
        {
            var path = reader.Path;

            while (reader.Read())
            {
                if (reader.TokenType == JsonToken.StartObject)
                {
                    if ((options & ChoJObjectLoadOptions.ExcludeNestedObjects) == ChoJObjectLoadOptions.ExcludeNestedObjects)
                    {
                        reader.Skip();
                    }
                    else
                    {
                        writer.WriteStartObject();
                        writer.WriteToReader(reader, options);
                    }
                }
                else if (reader.TokenType == JsonToken.EndObject)
                {
                    try

                    {
                        if ((options & ChoJObjectLoadOptions.ExcludeNestedObjects) == ChoJObjectLoadOptions.ExcludeNestedObjects)
                        {

                        }
                        else
                            writer.WriteEndObject();
                    }
                    catch { }
                }
                else if (reader.TokenType == JsonToken.StartArray)
                {
                    if ((options & ChoJObjectLoadOptions.ExcludeArrays) == ChoJObjectLoadOptions.ExcludeArrays)
                    {
                        reader.Skip();
                    }
                    else
                    {
                        writer.WriteStartArray();
                        //InvokeJArrayLoader(reader);
                        return;
                    }
                }
                else if (reader.TokenType == JsonToken.EndArray)
                {
                    if ((options & ChoJObjectLoadOptions.ExcludeArrays) == ChoJObjectLoadOptions.ExcludeArrays)
                    {
                    }
                    else
                    {
                        writer.WriteEndArray();
                    }
                }
                else if (reader.TokenType == JsonToken.PropertyName)
                {
                    var propName = reader.Value.ToNString();
                    writer.WritePropertyName(propName);

                    writer.WriteToReader(reader, options);
                }
                else if (reader.TokenType == JsonToken.Integer
                    || reader.TokenType == JsonToken.Float
                    || reader.TokenType == JsonToken.String
                    || reader.TokenType == JsonToken.Boolean
                    || reader.TokenType == JsonToken.Date
                    || reader.TokenType == JsonToken.Bytes
                    || reader.TokenType == JsonToken.Raw
                    || reader.TokenType == JsonToken.String
                    )
                {
                    writer.WriteValue(reader.Value);
                }
                else
                    writer.WriteValue(JValue.CreateNull());

                if (reader.Path == path)
                    break;
            }
        }

        public static JsonSerializer DeepCopy(this JsonSerializer serializer)
        {
            var copiedSerializer = new JsonSerializer
            {
                Context = serializer.Context,
                Culture = serializer.Culture,
                ContractResolver = serializer.ContractResolver,
                ConstructorHandling = serializer.ConstructorHandling,
                CheckAdditionalContent = serializer.CheckAdditionalContent,
                DateFormatHandling = serializer.DateFormatHandling,
                DateFormatString = serializer.DateFormatString,
                DateParseHandling = serializer.DateParseHandling,
                DateTimeZoneHandling = serializer.DateTimeZoneHandling,
                DefaultValueHandling = serializer.DefaultValueHandling,
                EqualityComparer = serializer.EqualityComparer,
                FloatFormatHandling = serializer.FloatFormatHandling,
                Formatting = serializer.Formatting,
                FloatParseHandling = serializer.FloatParseHandling,
                MaxDepth = serializer.MaxDepth,
                MetadataPropertyHandling = serializer.MetadataPropertyHandling,
                MissingMemberHandling = serializer.MissingMemberHandling,
                NullValueHandling = serializer.NullValueHandling,
                ObjectCreationHandling = serializer.ObjectCreationHandling,
                PreserveReferencesHandling = serializer.PreserveReferencesHandling,
                ReferenceResolver = serializer.ReferenceResolver,
                ReferenceLoopHandling = serializer.ReferenceLoopHandling,
                StringEscapeHandling = serializer.StringEscapeHandling,
                TraceWriter = serializer.TraceWriter,
                TypeNameHandling = serializer.TypeNameHandling,
                SerializationBinder = serializer.SerializationBinder,
                TypeNameAssemblyFormatHandling = serializer.TypeNameAssemblyFormatHandling
            };
            foreach (var converter in serializer.Converters)
            {
                copiedSerializer.Converters.Add(converter);
            }
            return copiedSerializer;
        }
        public static JsonReader CopyReaderForObject(this JsonReader reader, JToken jToken)
        {
            // create reader and copy over settings
            JsonReader jTokenReader = jToken.CreateReader();
            jTokenReader.Culture = reader.Culture;
            jTokenReader.DateFormatString = reader.DateFormatString;
            jTokenReader.DateParseHandling = reader.DateParseHandling;
            jTokenReader.DateTimeZoneHandling = reader.DateTimeZoneHandling;
            jTokenReader.FloatParseHandling = reader.FloatParseHandling;
            jTokenReader.MaxDepth = reader.MaxDepth;
            jTokenReader.SupportMultipleContent = reader.SupportMultipleContent;
            return jTokenReader;
        }

        public static JToken Flatten(this string json)
        {
            JToken input = JToken.Parse(json);
            return Flatten(input);
        }

        public static JToken Flatten(this JToken input)
        {
            var res = new JArray();
            foreach (var obj in GetFlattenedObjects(input, null))
                res.Add(obj);
            return res;
        }

        private static IEnumerable<JToken> GetFlattenedObjects(JToken token, IEnumerable<JProperty> otherProperties = null)
        {
            if (token is JObject obj)
            {
                var children = obj.Children<JProperty>().GroupBy(prop => prop.Value?.Type == JTokenType.Array).ToDictionary(gr => gr.Key);
                if (children.TryGetValue(false, out var directProps))
                    otherProperties = otherProperties?.Concat(directProps) ?? directProps;

                if (children.TryGetValue(true, out var ChildCollections))
                {
                    foreach (var childObj in ChildCollections.SelectMany(childColl => childColl.Values()).SelectMany(childColl => GetFlattenedObjects(childColl, otherProperties)))
                        yield return childObj;
                }
                else
                {
                    var res = new JObject();
                    if (otherProperties != null)
                        foreach (var prop in otherProperties)
                            res.Add(prop);
                    yield return res;
                }
            }
            else if (token is JArray arr)
            {
                foreach (var co in token.Children().SelectMany(c => GetFlattenedObjects(c, otherProperties)))
                    yield return co;
            }
            else
                throw new NotImplementedException(token.GetType().Name);
        }

        private static string GetTypeConverterName(Type type)
        {
            if (type == null) return String.Empty;

            type = type.GetUnderlyingType();
            if (typeof(Array).IsAssignableFrom(type))
                return $"{type.GetItemType().GetUnderlyingType().Name}ArrayConverter";
            else if (typeof(IList).IsAssignableFrom(type))
                return $"{type.GetItemType().GetUnderlyingType().Name}ListConverter";
            else
                return $"{type.Name}Converter";
        }

        public static string JTokenToString(this JToken jt)
        {
            if (jt != null && jt.Type == JTokenType.String)
                return $"\"{jt.ToNString()}\"";
            else
                return jt.ToNString();
        }

        public static JToken SerializeToJToken(this JsonSerializer serializer, object value, Formatting? formatting = null, JsonSerializerSettings settings = null,
            bool dontUseConverter = false, bool enableXmlAttributePrefix = false)
        {
            JsonConverter conv = null;
            if (!dontUseConverter)
            {
                Type vt = value != null ? value.GetType() : typeof(object);
                var convName = GetTypeConverterName(vt);
                conv = serializer.Converters.Where(c => c.GetType().Name == convName || (c.GetType().IsGenericType && c.GetType().GetGenericArguments()[0] == vt)).FirstOrDefault();
                if (conv == null)
                {
                    if (ChoJSONConvertersCache.Contains(convName))
                        conv = ChoJSONConvertersCache.Get(convName);
                    else if (ChoJSONConvertersCache.Contains(vt))
                        conv = ChoJSONConvertersCache.Get(vt);
                }
            }

            if (value != null)
            {
                if (!value.GetType().IsSimple())
                {
                    bool disableImplcityOp = false;
                    if (ChoTypeDescriptor.GetTypeAttribute<ChoTurnOffImplicitOpsAttribute>(value.GetType()) != null)
                        disableImplcityOp = ChoTypeDescriptor.GetTypeAttribute<ChoTurnOffImplicitOpsAttribute>(value.GetType()).Flag;

                    if (!disableImplcityOp)
                    {
                        Type to = null;
                        if (value.GetType().CanCastToPrimitiveType(out to))
                            value = ChoConvert.ConvertTo(value, to);
                        else if (value.GetType().GetImplicitTypeCastBackOps().Any())
                        {
                            var castTypes = value.GetType().GetImplicitTypeCastBackOps();

                            foreach (var ct in castTypes)
                            {
                                try
                                {
                                    value = ChoConvert.ConvertTo(value, ct);
                                    break;
                                }
                                catch { }
                            }
                        }
                    }
                }
            }

            JToken t = null;
            if (settings != null)
            {
                if (conv != null)
                    settings.Converters.Add(conv);
            }
            if (formatting == null)
                formatting = serializer.Formatting;

            if (settings != null && settings.Context.Context == null && enableXmlAttributePrefix)
            {
                settings.Context = new System.Runtime.Serialization.StreamingContext(System.Runtime.Serialization.StreamingContextStates.All, new ChoDynamicObject());
                dynamic ctx = settings.Context.Context;
                ctx.EnableXmlAttributePrefix = enableXmlAttributePrefix;
            }

            if (conv != null)
            {
                serializer.Converters.Add(conv);
                t = JToken.FromObject(value, serializer);
            }
            //else if (settings != null)
            //    t = JToken.Parse(JsonConvert.SerializeObject(value, formatting.Value, settings));
            else
                t = JToken.FromObject(value, serializer);
            return t;
        }

        public static object DeserializeObject(this JsonSerializer serializer, JsonReader reader, Type objType)
        {
            var convName = GetTypeConverterName(objType);
            var conv = serializer.Converters.Where(c => c.GetType().Name == convName || (c.GetType().IsGenericType && c.GetType().GetGenericArguments()[0] == objType)).FirstOrDefault();
            if (conv == null)
            {
                if (ChoJSONConvertersCache.Contains(convName))
                    conv = ChoJSONConvertersCache.Get(convName);
            }

            if (conv == null)
            {
                return serializer.Deserialize(reader, objType);
            }
            else
            {
                return JsonConvert.DeserializeObject(JObject.ReadFrom(reader).ToString(), objType, conv);
            }
        }

        public static string DumpAsJson(this DataTable table, Formatting formatting = Formatting.Indented)
        {
            if (table == null)
                return String.Empty;

            return JsonConvert.SerializeObject(table, formatting);
        }

        public static object GetNameAt(this JObject @this, int index)
        {
            if (@this == null || index < 0)
                return null;

            return @this.Properties().Skip(index).Select(p => p.Name).FirstOrDefault();
        }

        public static object GetValueAt(this JObject @this, int index)
        {
            if (@this == null || index < 0)
                return null;

            return @this.Properties().Skip(index).Select(p => p.Value).FirstOrDefault();
        }

        public static object ToJSONObject(this IDictionary<string, object> dict, Type type)
        {
            object target = ChoActivator.CreateInstance(type);
            string key = null;
            foreach (var p in ChoType.GetProperties(type))
            {
                if (p.GetCustomAttribute<JsonIgnoreAttribute>() != null)
                    continue;

                key = p.Name;
                var attr = p.GetCustomAttribute<JsonPropertyAttribute>();
                if (attr != null && !attr.PropertyName.IsNullOrWhiteSpace())
                    key = attr.PropertyName.NTrim();

                if (!dict.ContainsKey(key))
                    continue;

                p.SetValue(target, dict[key].CastObjectTo(p.PropertyType));
            }

            return target;
        }

        public static T ToJSONObject<T>(this IDictionary<string, object> dict)
            where T : class, new()
        {
            return (T)ToJSONObject(dict, typeof(T));
        }
    }
}

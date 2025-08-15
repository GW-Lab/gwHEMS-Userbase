// Program..: GeneratedEntities.cs
// Author...: G. Wassink
// Design...:
// Date.....: 25/02/2024 Last revised: 28/10/2024
// Notice...: Copyright 2025, All Rights Reserved
// Notes....: C#13 .Net 9.0.10
// Files....: None
// Programs.:
// Reserved.: Type Class

using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace gwTibber.Classes;

#region base classes
public struct GraphQlFieldMetadata
{
	public string Name;            // { get; set; }
	public string DefaultAlias;    // { get; set; }
	public bool IsComplex;         // { get; set; }
	public Type QueryBuilderType; // { get; set; }
}

public enum Formatting
{
	None,
	Indented
}

public class GraphQlObjectTypeAttribute(string typeName) : Attribute
{
	public string TypeName { get; } = typeName;
}

#if !GRAPHQL_GENERATOR_DISABLE_NEWTONSOFT_JSON
public class QueryBuilderParameterConverter<T> : JsonConverter
{
	public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
	{
		switch (reader.TokenType)
		{
			case JsonToken.Null:
				return null;

			default:
				return (QueryBuilderParameter<T>)(T)serializer.Deserialize(reader, typeof(T));
		}
	}

	public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
	{
		if (value == null)
			writer.WriteNull();
		else
			serializer.Serialize(writer, ((QueryBuilderParameter<T>)value).Value, typeof(T));
	}

	public override bool CanConvert(Type objectType) => objectType.IsSubclassOf(typeof(QueryBuilderParameter));
}

public class GraphQlInterfaceJsonConverter : JsonConverter
{
	private const string FieldNameType = "__typename";

	private static readonly Dictionary<string, Type> InterfaceTypeMapping =
		  typeof(GraphQlInterfaceJsonConverter).Assembly.GetTypes()
				 .Select(t => new { Type = t, Attribute = t.GetCustomAttribute<GraphQlObjectTypeAttribute>() })
				 .Where(x => x.Attribute != null && x.Type.Namespace == typeof(GraphQlInterfaceJsonConverter).Namespace)
				 .ToDictionary(x => x.Attribute.TypeName, x => x.Type);

	public override bool CanConvert(Type objectType) => objectType.IsInterface || objectType.IsArray;

	public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
	{
		while (reader.TokenType == JsonToken.Comment)
			reader.Read();

		switch (reader.TokenType)
		{
			case JsonToken.Null:
				return null;

			case JsonToken.StartObject:
				var jObject = JObject.Load(reader);

				if (!jObject.TryGetValue(FieldNameType, out var token) || token.Type != JTokenType.String)
					throw CreateJsonReaderException(reader, $"\"{GetType().FullName}\" requires JSON object to contain \"{FieldNameType}\" field with type name");

				var typeName = token.Value<string>();

				if (!InterfaceTypeMapping.TryGetValue(typeName, out var type))
					throw CreateJsonReaderException(reader, $"type \"{typeName}\" not found");

				using (reader = CloneReader(jObject, reader))
					return serializer.Deserialize(reader, type);

			case JsonToken.StartArray:
				var elementType = GetElementType(objectType);

				if (elementType == null)
					throw CreateJsonReaderException(reader, $"array element type could not be resolved for type \"{objectType.FullName}\"");

				return ReadArray(reader, objectType, elementType, serializer);

			default:
				throw CreateJsonReaderException(reader, $"unrecognized token: {reader.TokenType}");
		}
	}

	public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) => serializer.Serialize(writer, value);

	private static JsonReader CloneReader(JToken jToken, JsonReader reader)
	{
		var jObjectReader = jToken.CreateReader();
		jObjectReader.Culture = reader.Culture;
		jObjectReader.CloseInput = reader.CloseInput;
		jObjectReader.SupportMultipleContent = reader.SupportMultipleContent;
		jObjectReader.DateTimeZoneHandling = reader.DateTimeZoneHandling;
		jObjectReader.FloatParseHandling = reader.FloatParseHandling;
		jObjectReader.DateFormatString = reader.DateFormatString;
		jObjectReader.DateParseHandling = reader.DateParseHandling;

		return jObjectReader;
	}

	private static JsonReaderException CreateJsonReaderException(JsonReader reader, string message)
	{
		if (reader is IJsonLineInfo lineInfo && lineInfo.HasLineInfo())
			return new JsonReaderException(message, reader.Path, lineInfo.LineNumber, lineInfo.LinePosition, null);

		return new JsonReaderException(message);
	}

	private static Type GetElementType(Type arrayOrGenericContainer) =>
		  arrayOrGenericContainer.IsArray ? arrayOrGenericContainer.GetElementType() : arrayOrGenericContainer.GenericTypeArguments.FirstOrDefault();

	private IList ReadArray(JsonReader reader, Type targetType, Type elementType, JsonSerializer serializer)
	{
		var list = CreateCompatibleList(targetType, elementType);

		while (reader.Read() && reader.TokenType != JsonToken.EndArray)
			list.Add(ReadJson(reader, elementType, null, serializer));

		if (!targetType.IsArray)
			return list;

		var array = Array.CreateInstance(elementType, list.Count);
		list.CopyTo(array, 0);

		return array;
	}

	private static IList CreateCompatibleList(Type targetContainerType, Type elementType) =>
		  (IList)Activator.CreateInstance(targetContainerType.IsArray || targetContainerType.IsAbstract ? typeof(List<>).MakeGenericType(elementType) : targetContainerType);
}
#endif

internal static class GraphQlQueryHelper
{
	private static readonly Regex RegexWhiteSpace = new Regex(@"\s", RegexOptions.Compiled);
	private static readonly Regex RegexGraphQlIdentifier = new Regex(@"^[_A-Za-z][_0-9A-Za-z]*$", RegexOptions.Compiled);

	public static string GetIndentation(int level, byte indentationSize) => new string(' ', level * indentationSize);

	public static string BuildArgumentValue(object value, string formatMask, Formatting formatting, int level, byte indentationSize)
	{
		if (value is null)
			return "null";

#if !GRAPHQL_GENERATOR_DISABLE_NEWTONSOFT_JSON
		if (value is JValue jValue)
		{
			//switch (jValue.Type)
			//{
			//	case JTokenType.Null: return "null";
			//	case JTokenType.Integer:
			//	case JTokenType.Float:
			//	case JTokenType.Boolean:
			//		return BuildArgumentValue(jValue.Value, null, formatting, level, indentationSize);
			//	case JTokenType.String:
			//		return "\"" + ((string)jValue.Value).Replace("\"", "\\\"") + "\"";
			//	default:
			//		return "\"" + jValue.Value + "\"";
			//}
			return jValue.Type switch
			{
				JTokenType.Null => "null",
				JTokenType.Integer or JTokenType.Float or JTokenType.Boolean => BuildArgumentValue(jValue.Value, null, formatting, level, indentationSize),
				JTokenType.String => "\"" + ((string)jValue.Value).Replace("\"", "\\\"") + "\"",
				_ => "\"" + jValue.Value + "\"",
			};
		}

		if (value is JProperty jProperty)
		{
			if (RegexWhiteSpace.IsMatch(jProperty.Name))
				throw new ArgumentException($"JSON object keys used as GraphQL arguments must not contain whitespace; key: {jProperty.Name}");

			return $"{jProperty.Name}:{(formatting == Formatting.Indented ? " " : null)}{BuildArgumentValue(jProperty.Value, null, formatting, level, indentationSize)}";
		}

		if (value is JObject jObject)
			return BuildEnumerableArgument(jObject, null, formatting, level + 1, indentationSize, '{', '}');
#endif

		var enumerable = value as IEnumerable;
		if (!string.IsNullOrEmpty(formatMask) && enumerable == null)
			return
				  value is IFormattable formattable
						 ? "\"" + formattable.ToString(formatMask, CultureInfo.InvariantCulture) + "\""
						 : throw new ArgumentException($"Value must implement {nameof(IFormattable)} interface to use a format mask. ", nameof(value));

		if (value is Enum @enum)
			return ConvertEnumToString(@enum);

		if (value is bool @bool)
			return @bool ? "true" : "false";

		if (value is DateTime dateTime)
			return "\"" + dateTime.ToString("O") + "\"";

		if (value is DateTimeOffset dateTimeOffset)
			return "\"" + dateTimeOffset.ToString("O") + "\"";

		if (value is IGraphQlInputObject inputObject)
			return BuildInputObject(inputObject, formatting, level + 2, indentationSize);

		if (value is Guid)
			return "\"" + value + "\"";

		if (value is string @string)
			return "\"" + @string.Replace("\"", "\\\"") + "\"";

		if (enumerable != null)
			return BuildEnumerableArgument(enumerable, formatMask, formatting, level, indentationSize, '[', ']');

		if (value is short || value is ushort || value is byte || value is int || value is uint || value is long || value is ulong || value is float || value is double || value is decimal)
			return Convert.ToString(value, CultureInfo.InvariantCulture);

		var argumentValue = Convert.ToString(value, CultureInfo.InvariantCulture);

		return "\"" + argumentValue + "\"";
	}

	private static string BuildEnumerableArgument(IEnumerable enumerable, string formatMask, Formatting formatting, int level, byte indentationSize, char openingSymbol, char closingSymbol)
	{
		var builder = new StringBuilder();
		builder.Append(openingSymbol);
		var delimiter = string.Empty;

		foreach (var item in enumerable)
		{
			builder.Append(delimiter);

			if (formatting == Formatting.Indented)
			{
				builder.AppendLine();
				builder.Append(GetIndentation(level + 1, indentationSize));
			}

			builder.Append(BuildArgumentValue(item, formatMask, formatting, level, indentationSize));
			delimiter = ",";
		}

		builder.Append(closingSymbol);

		return builder.ToString();
	}

	public static string BuildInputObject(IGraphQlInputObject inputObject, Formatting formatting, int level, byte indentationSize)
	{
		var builder = new StringBuilder();
		builder.Append("{");

		var isIndentedFormatting = formatting == Formatting.Indented;
		string valueSeparator;

		if (isIndentedFormatting)
		{
			builder.AppendLine();
			valueSeparator = ": ";
		}
		else
			valueSeparator = ":";

		var separator = string.Empty;

		foreach (var propertyValue in inputObject.GetPropertyValues())
		{
			var queryBuilderParameter = propertyValue.Value as QueryBuilderParameter;
			var value =
				  queryBuilderParameter?.Name != null
						 ? "$" + queryBuilderParameter.Name
						 : BuildArgumentValue(queryBuilderParameter == null ? propertyValue.Value : queryBuilderParameter.Value, propertyValue.FormatMask, formatting, level, indentationSize);

			builder.Append(isIndentedFormatting ? GetIndentation(level, indentationSize) : separator);
			builder.Append(propertyValue.Name);
			builder.Append(valueSeparator);
			builder.Append(value);

			separator = ",";

			if (isIndentedFormatting)
				builder.AppendLine();
		}

		if (isIndentedFormatting)
			builder.Append(GetIndentation(level - 1, indentationSize));

		builder.Append("}");

		return builder.ToString();
	}

	public static string BuildDirective(GraphQlDirective directive, Formatting formatting, int level, byte indentationSize)
	{
		if (directive == null)
			return string.Empty;

		var isIndentedFormatting = formatting == Formatting.Indented;
		var indentationSpace = isIndentedFormatting ? " " : string.Empty;
		var builder = new StringBuilder();
		builder.Append(indentationSpace);
		builder.Append("@");
		builder.Append(directive.Name);
		builder.Append("(");

		string separator = null;

		foreach (var kvp in directive.Arguments)
		{
			var argumentName = kvp.Key;
			var argument = kvp.Value;

			builder.Append(separator);
			builder.Append(argumentName);
			builder.Append(":");
			builder.Append(indentationSpace);

			if (argument.Name == null)
				builder.Append(BuildArgumentValue(argument.Value, null, formatting, level, indentationSize));
			else
			{
				builder.Append("$");
				builder.Append(argument.Name);
			}

			separator = isIndentedFormatting ? ", " : ",";
		}

		builder.Append(")");

		return builder.ToString();
	}

	public static void ValidateGraphQlIdentifier(string name, string identifier)
	{
		if (identifier != null && !RegexGraphQlIdentifier.IsMatch(identifier))
			throw new ArgumentException("value must match " + RegexGraphQlIdentifier, name);
	}

	private static string ConvertEnumToString(Enum @enum)
	{
		var enumMember = @enum.GetType().GetField(@enum.ToString()) ?? throw new InvalidOperationException("enum member resolution failed");
		var enumMemberAttribute = (EnumMemberAttribute)enumMember.GetCustomAttribute(typeof(EnumMemberAttribute));

		return enumMemberAttribute == null ? @enum.ToString() : enumMemberAttribute.Value;
	}
}

internal struct InputPropertyInfo
{
	public string Name; // { get; set; }
	public object Value; // { get; set; }
	public string FormatMask; // { get; set; }
}

internal interface IGraphQlInputObject
{
	IEnumerable<InputPropertyInfo> GetPropertyValues();
}

public interface IGraphQlQueryBuilder
{
	void Clear();
	void IncludeAllFields();
	string Build(Formatting formatting = Formatting.None, byte indentationSize = 2);
}

public struct QueryBuilderArgumentInfo
{
	public string ArgumentName;                     // { get; set; }
	public QueryBuilderParameter ArgumentValue; // { get; set; }
	public string FormatMask;                           // { get; set; }
}

public abstract class QueryBuilderParameter
{
	private string _name;

	internal string GraphQlTypeName { get; }
	internal object Value { get; set; }

	public string Name
	{
		get => _name;
		set
		{
			GraphQlQueryHelper.ValidateGraphQlIdentifier(nameof(Name), value);
			_name = value;
		}
	}

	protected QueryBuilderParameter(string name, string graphQlTypeName, object value)
	{
		Name = name.Trim();
		GraphQlTypeName = graphQlTypeName?.Replace(" ", null).Replace("\t", null).Replace("\n", null).Replace("\r", null);
		Value = value;
	}

	protected QueryBuilderParameter(object value) => Value = value;
}

public class QueryBuilderParameter<T> : QueryBuilderParameter
{
	public new T Value
	{
		get => (T)base.Value;
		set => base.Value = value;
	}

	protected QueryBuilderParameter(string name, string graphQlTypeName, T value) : base(name, graphQlTypeName, value)
	{
		if (string.IsNullOrWhiteSpace(graphQlTypeName))
			throw new ArgumentException("value required", nameof(graphQlTypeName));
	}

	private QueryBuilderParameter(T value) : base(value)
	{
	}

	public static implicit operator QueryBuilderParameter<T>(T value) => new QueryBuilderParameter<T>(value);

	public static implicit operator T(QueryBuilderParameter<T> parameter) => parameter.Value;
}

public class GraphQlQueryParameter<T> : QueryBuilderParameter<T>
{
	private string _formatMask;

	public string FormatMask
	{
		get => _formatMask;
		set => _formatMask = typeof(IFormattable).IsAssignableFrom(typeof(T))
									? value
									: throw new InvalidOperationException($"Value must be of {nameof(IFormattable)} type. ");
	}

	public GraphQlQueryParameter(string name, string graphQlTypeName, T value) : base(name, graphQlTypeName, value)
	{ }

	public GraphQlQueryParameter(string name, T value, bool isNullable = true) : base(name, GetGraphQlTypeName(value, isNullable), value)
	{ }

	private static string GetGraphQlTypeName(T value, bool isNullable)
	{
		var graphQlTypeName = GetGraphQlTypeName(typeof(T));

		if (!isNullable)
			graphQlTypeName += "!";

		return graphQlTypeName;
	}

	private static string GetGraphQlTypeName(Type valueType)
	{
		var nullableUnderlyingType = Nullable.GetUnderlyingType(valueType);
		valueType = nullableUnderlyingType ?? valueType;

		if (valueType.IsArray)
		{
			var arrayItemType = GetGraphQlTypeName(valueType.GetElementType());

			return arrayItemType == null ? null : "[" + arrayItemType + "]";
		}

		if (typeof(IEnumerable).IsAssignableFrom(valueType))
		{
			var genericArguments = valueType.GetGenericArguments();

			if (genericArguments.Length == 1)
			{
				var listItemType = GetGraphQlTypeName(valueType.GetGenericArguments()[0]);

				return listItemType == null ? null : "[" + listItemType + "]";
			}
		}

		if (GraphQlTypes.ReverseMapping.TryGetValue(valueType, out var graphQlTypeName))
			return graphQlTypeName;

		if (valueType == typeof(string))
			return "String";

		var nullableSuffix = nullableUnderlyingType == null ? null : "?";
		graphQlTypeName = GetValueTypeGraphQlTypeName(valueType);
		return graphQlTypeName == null ? null : graphQlTypeName + nullableSuffix;
	}

	private static string GetValueTypeGraphQlTypeName(Type valueType)
	{
		if (valueType == typeof(bool))
			return "Boolean";

		if (valueType == typeof(float) || valueType == typeof(double) || valueType == typeof(decimal))
			return "Float";

		if (valueType == typeof(Guid))
			return "ID";

		if (valueType == typeof(sbyte) || valueType == typeof(byte) || valueType == typeof(short) || valueType == typeof(ushort) || valueType == typeof(int) || valueType == typeof(uint) ||
			  valueType == typeof(long) || valueType == typeof(ulong))
			return "Int";

		return null;
	}
}

public abstract class GraphQlDirective
{
	private readonly Dictionary<string, QueryBuilderParameter> _arguments = new Dictionary<string, QueryBuilderParameter>();

	internal IEnumerable<KeyValuePair<string, QueryBuilderParameter>> Arguments => _arguments;

	public string Name { get; }

	protected GraphQlDirective(string name)
	{
		GraphQlQueryHelper.ValidateGraphQlIdentifier(nameof(name), name);
		Name = name;
	}

	protected void AddArgument(string name, QueryBuilderParameter value)
	{
		if (value != null)
			_arguments[name] = value;
	}
}

public abstract class GraphQlQueryBuilder : IGraphQlQueryBuilder
{
	private readonly Dictionary<string, GraphQlFieldCriteria> _fieldCriteria = new Dictionary<string, GraphQlFieldCriteria>();

	private readonly string _operationType;
	private readonly string _operationName;
	private Dictionary<string, GraphQlFragmentCriteria> _fragments;
	private List<QueryBuilderArgumentInfo> _queryParameters;

	protected abstract string TypeName { get; }

	public abstract IReadOnlyList<GraphQlFieldMetadata> AllFields { get; }

	protected GraphQlQueryBuilder(string operationType, string operationName)
	{
		GraphQlQueryHelper.ValidateGraphQlIdentifier(nameof(operationName), operationName);
		_operationType = operationType;
		_operationName = operationName;
	}

	public virtual void Clear()
	{
		_fieldCriteria.Clear();
		_fragments.Clear();
		_queryParameters.Clear();
	}

	void IGraphQlQueryBuilder.IncludeAllFields() => IncludeAllFields();

	public string Build(Formatting formatting = Formatting.None, byte indentationSize = 2) => Build(formatting, 1, indentationSize);

	protected void IncludeAllFields() => IncludeFields(AllFields);

	protected virtual string Build(Formatting formatting, int level, byte indentationSize)
	{
		var isIndentedFormatting = formatting == Formatting.Indented;
		var separator = string.Empty;
		var indentationSpace = isIndentedFormatting ? " " : string.Empty;
		var builder = new StringBuilder();

		if (!string.IsNullOrEmpty(_operationType))
		{
			builder.Append(_operationType);

			if (!string.IsNullOrEmpty(_operationName))
			{
				builder.Append(" ");
				builder.Append(_operationName);
			}

			if (_queryParameters?.Count > 0)
			{
				builder.Append(indentationSpace);
				builder.Append("(");

				foreach (var queryParameterInfo in _queryParameters)
				{
					if (isIndentedFormatting)
					{
						builder.AppendLine(separator);
						builder.Append(GraphQlQueryHelper.GetIndentation(level, indentationSize));
					}
					else
						builder.Append(separator);

					builder.Append("$");
					builder.Append(queryParameterInfo.ArgumentValue.Name);
					builder.Append(":");
					builder.Append(indentationSpace);

					builder.Append(queryParameterInfo.ArgumentValue.GraphQlTypeName);

					if (!queryParameterInfo.ArgumentValue.GraphQlTypeName.EndsWith("!"))
					{
						builder.Append(indentationSpace);
						builder.Append("=");
						builder.Append(indentationSpace);
						builder.Append(GraphQlQueryHelper.BuildArgumentValue(queryParameterInfo.ArgumentValue.Value, queryParameterInfo.FormatMask, formatting, 0, indentationSize));
					}

					separator = ",";
				}

				builder.Append(")");
			}
		}

		builder.Append(indentationSpace);
		builder.Append("{");

		if (isIndentedFormatting)
			builder.AppendLine();

		separator = string.Empty;

		foreach (var criteria in _fieldCriteria.Values.Concat(_fragments?.Values ?? Enumerable.Empty<GraphQlFragmentCriteria>()))
		{
			var fieldCriteria = criteria.Build(formatting, level, indentationSize);

			if (isIndentedFormatting)
				builder.AppendLine(fieldCriteria);
			else if (!string.IsNullOrEmpty(fieldCriteria))
			{
				builder.Append(separator);
				builder.Append(fieldCriteria);
			}

			separator = ",";
		}

		if (isIndentedFormatting)
			builder.Append(GraphQlQueryHelper.GetIndentation(level - 1, indentationSize));

		builder.Append("}");

		return builder.ToString();
	}

	protected void IncludeScalarField(string fieldName, string alias, IList<QueryBuilderArgumentInfo> args, GraphQlDirective[] directives) => _fieldCriteria[alias ?? fieldName] = new GraphQlScalarFieldCriteria(fieldName, alias, args, directives);

	protected void IncludeObjectField(string fieldName, string alias, GraphQlQueryBuilder objectFieldQueryBuilder, IList<QueryBuilderArgumentInfo> args, GraphQlDirective[] directives) => _fieldCriteria[alias ?? fieldName] = new GraphQlObjectFieldCriteria(fieldName, alias, objectFieldQueryBuilder, args, directives);

	protected void IncludeFragment(GraphQlQueryBuilder objectFieldQueryBuilder, GraphQlDirective[] directives)
	{
		_fragments = _fragments ?? new Dictionary<string, GraphQlFragmentCriteria>();
		_fragments[objectFieldQueryBuilder.TypeName] = new GraphQlFragmentCriteria(objectFieldQueryBuilder, directives);
	}

	protected void ExcludeField(string fieldName)
	{
		if (fieldName == null)
			throw new ArgumentNullException(nameof(fieldName));

		_fieldCriteria.Remove(fieldName);
	}

	protected void IncludeFields(IEnumerable<GraphQlFieldMetadata> fields) => IncludeFields(fields, null);

	private void IncludeFields(IEnumerable<GraphQlFieldMetadata> fields, List<Type> parentTypes)
	{
		foreach (var field in fields)
		{
			if (field.QueryBuilderType == null)
				IncludeScalarField(field.Name, field.DefaultAlias, null, null);
			else
			{
				var builderType = GetType();

				if (parentTypes != null && parentTypes.Any(t => t.IsAssignableFrom(field.QueryBuilderType)))
					continue;

				parentTypes?.Add(builderType);

				var queryBuilder = InitializeChildBuilder(builderType, field.QueryBuilderType, parentTypes);
				var includeFragmentMethods = field.QueryBuilderType.GetMethods().Where(IsIncludeFragmentMethod);

				foreach (var includeFragmentMethod in includeFragmentMethods)
					includeFragmentMethod.Invoke(queryBuilder, [InitializeChildBuilder(builderType, includeFragmentMethod.GetParameters()[0].ParameterType, parentTypes)]);

				IncludeObjectField(field.Name, field.DefaultAlias, queryBuilder, null, null);
			}
		}
	}

	private static GraphQlQueryBuilder InitializeChildBuilder(Type parentQueryBuilderType, Type queryBuilderType, List<Type> parentTypes)
	{
		var queryBuilder = (GraphQlQueryBuilder)Activator.CreateInstance(queryBuilderType);
		queryBuilder.IncludeFields(queryBuilder.AllFields, parentTypes ?? new List<Type> { parentQueryBuilderType });

		return queryBuilder;
	}

	private static bool IsIncludeFragmentMethod(MethodInfo methodInfo)
	{
		if (!methodInfo.Name.StartsWith("With") || !methodInfo.Name.EndsWith("Fragment"))
			return false;

		var parameters = methodInfo.GetParameters();

		return parameters.Length == 1 && parameters[0].ParameterType.IsSubclassOf(typeof(GraphQlQueryBuilder));
	}

	protected void AddParameter<T>(GraphQlQueryParameter<T> parameter)
	{
		_queryParameters ??= [];

		_queryParameters.Add(new QueryBuilderArgumentInfo { ArgumentValue = parameter, FormatMask = parameter.FormatMask });
	}

	private abstract class GraphQlFieldCriteria
	{
		private readonly IList<QueryBuilderArgumentInfo> _args;
		private readonly GraphQlDirective[] _directives;

		protected readonly string FieldName;
		protected readonly string Alias;

		protected static string GetIndentation(Formatting formatting, int level, byte indentationSize) => formatting == Formatting.Indented ? GraphQlQueryHelper.GetIndentation(level, indentationSize) : null;

		protected GraphQlFieldCriteria(string fieldName, string alias, IList<QueryBuilderArgumentInfo> args, GraphQlDirective[] directives)
		{
			GraphQlQueryHelper.ValidateGraphQlIdentifier(nameof(alias), alias);
			FieldName = fieldName;
			Alias = alias;
			_args = args;
			_directives = directives;
		}

		public abstract string Build(Formatting formatting, int level, byte indentationSize);

		protected string BuildArgumentClause(Formatting formatting, int level, byte indentationSize)
		{
			var separator = formatting == Formatting.Indented ? " " : null;
			var argumentCount = _args?.Count ?? 0;

			if (argumentCount == 0)
				return string.Empty;

			var arguments = _args.Select(
						 a => $"{a.ArgumentName}:{separator}{(a.ArgumentValue.Name == null ? GraphQlQueryHelper.BuildArgumentValue(a.ArgumentValue.Value, a.FormatMask, formatting, level, indentationSize) : "$" + a.ArgumentValue.Name)}");

			return $"({string.Join($",{separator}", arguments)})";
		}

		protected string BuildDirectiveClause(Formatting formatting, int level, byte indentationSize) =>
			  _directives == null ? null : string.Concat(_directives.Select(d => d == null ? null : GraphQlQueryHelper.BuildDirective(d, formatting, level, indentationSize)));

		protected static string BuildAliasPrefix(string alias, Formatting formatting)
		{
			var separator = formatting == Formatting.Indented ? " " : string.Empty;

			return string.IsNullOrWhiteSpace(alias) ? null : alias + ':' + separator;
		}
	}

	private class GraphQlScalarFieldCriteria(string fieldName, string alias, IList<QueryBuilderArgumentInfo> args, GraphQlDirective[] directives) : GraphQlFieldCriteria(fieldName, alias, args, directives)
	{
		public override string Build(Formatting formatting, int level, byte indentationSize) =>
											  GetIndentation(formatting, level, indentationSize) +
											  BuildAliasPrefix(Alias, formatting) +
											  FieldName +
											  BuildArgumentClause(formatting, level, indentationSize) +
											  BuildDirectiveClause(formatting, level, indentationSize);
	}

	private class GraphQlObjectFieldCriteria(string fieldName, string alias, GraphQlQueryBuilder objectQueryBuilder, IList<QueryBuilderArgumentInfo> args, GraphQlDirective[] directives) : GraphQlFieldCriteria(fieldName, alias, args, directives)
	{
		private readonly GraphQlQueryBuilder _objectQueryBuilder = objectQueryBuilder;

		public override string Build(Formatting formatting, int level, byte indentationSize) =>
			  _objectQueryBuilder._fieldCriteria.Count > 0 || _objectQueryBuilder._fragments?.Count > 0
					 ? GetIndentation(formatting, level, indentationSize) + BuildAliasPrefix(Alias, formatting) + FieldName +
						 BuildArgumentClause(formatting, level, indentationSize) + BuildDirectiveClause(formatting, level, indentationSize) + _objectQueryBuilder.Build(formatting, level + 1, indentationSize)
					 : null;
	}

	private class GraphQlFragmentCriteria : GraphQlFieldCriteria
	{
		private readonly GraphQlQueryBuilder _objectQueryBuilder;

		public GraphQlFragmentCriteria(GraphQlQueryBuilder objectQueryBuilder, GraphQlDirective[] directives) : base(objectQueryBuilder.TypeName, null, null, directives)
		{
			_objectQueryBuilder = objectQueryBuilder;
		}

		public override string Build(Formatting formatting, int level, byte indentationSize) =>
			  _objectQueryBuilder._fieldCriteria.Count == 0
					 ? null
					 : GetIndentation(formatting, level, indentationSize) + "..." + (formatting == Formatting.Indented ? " " : null) + "on " +
						 FieldName + BuildArgumentClause(formatting, level, indentationSize) + BuildDirectiveClause(formatting, level, indentationSize) + _objectQueryBuilder.Build(formatting, level + 1, indentationSize);
	}
}

public abstract class GraphQlQueryBuilder<TQueryBuilder>(string operationType = null, string operationName = null) : GraphQlQueryBuilder(operationType, operationName) where TQueryBuilder : GraphQlQueryBuilder<TQueryBuilder>
{
	public TQueryBuilder WithAllFields()
	{
		IncludeAllFields();

		return (TQueryBuilder)this;
	}

	public TQueryBuilder WithAllScalarFields()
	{
		IncludeFields(AllFields.Where(f => !f.IsComplex));

		return (TQueryBuilder)this;
	}

	public TQueryBuilder ExceptField(string fieldName)
	{
		ExcludeField(fieldName);

		return (TQueryBuilder)this;
	}

	public TQueryBuilder WithTypeName(string alias = null, params GraphQlDirective[] directives)
	{
		IncludeScalarField("__typename", alias, null, directives);

		return (TQueryBuilder)this;
	}

	protected TQueryBuilder WithScalarField(string fieldName, string alias, GraphQlDirective[] directives, IList<QueryBuilderArgumentInfo> args = null)
	{
		IncludeScalarField(fieldName, alias, args, directives);

		return (TQueryBuilder)this;
	}

	protected TQueryBuilder WithObjectField(string fieldName, string alias, GraphQlQueryBuilder queryBuilder, GraphQlDirective[] directives, IList<QueryBuilderArgumentInfo> args = null)
	{
		IncludeObjectField(fieldName, alias, queryBuilder, args, directives);

		return (TQueryBuilder)this;
	}

	protected TQueryBuilder WithFragment(GraphQlQueryBuilder queryBuilder, GraphQlDirective[] directives)
	{
		IncludeFragment(queryBuilder, directives);

		return (TQueryBuilder)this;
	}

	protected TQueryBuilder WithParameterInternal<T>(GraphQlQueryParameter<T> parameter)
	{
		AddParameter(parameter);

		return (TQueryBuilder)this;
	}
}

public abstract class GraphQlResponse<TDataContract>
{
	public TDataContract Data; // { get; set; }
	public ICollection<GraphQlQueryError> Errors; // { get; set; }
}

public class GraphQlQueryError
{
	public string Message; // { get; set; }
	public ICollection<GraphQlErrorLocation> Locations; // { get; set; }
}

public class GraphQlErrorLocation
{
	public int Line; //{ get; set; }
	public int Column; // { get; set; }
}
#endregion

#region GraphQL type helpers
public static class GraphQlTypes
{
	public const string Boolean = "Boolean";
	public const string Float = "Float";
	public const string Id = "ID";
	public const string Int = "Int";
	public const string String = "String";

	public const string AppScreen = "AppScreen";
	public const string EnergyResolution = "EnergyResolution";
	public const string HeatingSource = "HeatingSource";
	public const string HomeAvatar = "HomeAvatar";
	public const string HomeType = "HomeType";
	public const string PriceLevel = "PriceLevel";
	public const string PriceRatingLevel = "PriceRatingLevel";
	public const string PriceResolution = "PriceResolution";

	public const string Address = "Address";
	public const string Consumption = "Consumption";
	public const string ContactInfo = "ContactInfo";
	public const string Home = "Home";
	public const string HomeConsumptionConnection = "HomeConsumptionConnection";
	public const string HomeConsumptionEdge = "HomeConsumptionEdge";
	public const string HomeConsumptionPageInfo = "HomeConsumptionPageInfo";
	public const string HomeFeatures = "HomeFeatures";
	public const string HomeProductionConnection = "HomeProductionConnection";
	public const string HomeProductionEdge = "HomeProductionEdge";
	public const string HomeProductionPageInfo = "HomeProductionPageInfo";
	public const string LegalEntity = "LegalEntity";
	public const string LiveMeasurement = "LiveMeasurement";
	public const string MeteringPointData = "MeteringPointData";
	public const string MeterReadingResponse = "MeterReadingResponse";
	public const string Price = "Price";
	public const string PriceInfo = "PriceInfo";
	public const string PriceRating = "PriceRating";
	public const string PriceRatingEntry = "PriceRatingEntry";
	public const string PriceRatingThresholdPercentages = "PriceRatingThresholdPercentages";
	public const string PriceRatingType = "PriceRatingType";
	public const string Production = "Production";
	public const string PushNotificationResponse = "PushNotificationResponse";
	public const string Query = "Query";
	public const string RootMutation = "RootMutation";
	public const string RootSubscription = "RootSubscription";
	public const string Subscription = "Subscription";
	public const string SubscriptionPriceConnection = "SubscriptionPriceConnection";
	public const string SubscriptionPriceConnectionPageInfo = "SubscriptionPriceConnectionPageInfo";
	public const string SubscriptionPriceEdge = "SubscriptionPriceEdge";
	public const string Viewer = "Viewer";

	public const string MeterReadingInput = "MeterReadingInput";
	public const string PushNotificationInput = "PushNotificationInput";
	public const string UpdateHomeInput = "UpdateHomeInput";

	public const string PageInfo = "PageInfo";

	public static readonly IReadOnlyDictionary<Type, string> ReverseMapping =
		  new Dictionary<Type, string>
		  {
				 { typeof(string), "String" },
				 { typeof(DateTimeOffset), "String" },
				 { typeof(decimal), "Float" },
				 { typeof(Guid), "ID" },
				 { typeof(int), "Int" },
				 { typeof(bool), "Boolean" },
				 { typeof(MeterReadingInput), "MeterReadingInput" },
				 { typeof(PushNotificationInput), "PushNotificationInput" },
				 { typeof(UpdateHomeInput), "UpdateHomeInput" }
		  };
}
#endregion

#region enums
public enum PriceLevel
{
	[EnumMember(Value = "NORMAL")] Normal,
	[EnumMember(Value = "CHEAP")] Cheap,
	[EnumMember(Value = "VERY_CHEAP")] VeryCheap,
	[EnumMember(Value = "EXPENSIVE")] Expensive,
	[EnumMember(Value = "VERY_EXPENSIVE")] VeryExpensive
}

public enum PriceResolution
{
	[EnumMember(Value = "HOURLY")] Hourly,
	[EnumMember(Value = "DAILY")] Daily
}

public enum PriceRatingLevel
{
	[EnumMember(Value = "NORMAL")] Normal,
	[EnumMember(Value = "LOW")] Low,
	[EnumMember(Value = "HIGH")] High
}

public enum EnergyResolution
{
	[EnumMember(Value = "HOURLY")] Hourly,
	[EnumMember(Value = "DAILY")] Daily,
	[EnumMember(Value = "WEEKLY")] Weekly,
	[EnumMember(Value = "MONTHLY")] Monthly,
	[EnumMember(Value = "ANNUAL")] Annual
}

public enum HomeType
{
	[EnumMember(Value = "APARTMENT")] Apartment,
	[EnumMember(Value = "ROWHOUSE")] Rowhouse,
	[EnumMember(Value = "HOUSE")] House,
	[EnumMember(Value = "COTTAGE")] Cottage
}

public enum HeatingSource
{
	[EnumMember(Value = "AIR2AIR_HEATPUMP")] Air2AirHeatpump,
	[EnumMember(Value = "ELECTRICITY")] Electricity,
	[EnumMember(Value = "GROUND")] Ground,
	[EnumMember(Value = "DISTRICT_HEATING")] DistrictHeating,
	[EnumMember(Value = "ELECTRIC_BOILER")] ElectricBoiler,
	[EnumMember(Value = "AIR2WATER_HEATPUMP")] Air2WaterHeatpump,
	[EnumMember(Value = "OTHER")] Other
}

public enum HomeAvatar
{
	[EnumMember(Value = "APARTMENT")] Apartment,
	[EnumMember(Value = "ROWHOUSE")] Rowhouse,
	[EnumMember(Value = "FLOORHOUSE1")] Floorhouse1,
	[EnumMember(Value = "FLOORHOUSE2")] Floorhouse2,
	[EnumMember(Value = "FLOORHOUSE3")] Floorhouse3,
	[EnumMember(Value = "COTTAGE")] Cottage,
	[EnumMember(Value = "CASTLE")] Castle
}

public enum AppScreen
{
	[EnumMember(Value = "HOME")] Home,
	[EnumMember(Value = "REPORTS")] Reports,
	[EnumMember(Value = "CONSUMPTION")] Consumption,
	[EnumMember(Value = "COMPARISON")] Comparison,
	[EnumMember(Value = "DISAGGREGATION")] Disaggregation,
	[EnumMember(Value = "HOME_PROFILE")] HomeProfile,
	[EnumMember(Value = "CUSTOMER_PROFILE")] CustomerProfile,
	[EnumMember(Value = "METER_READING")] MeterReading,
	[EnumMember(Value = "NOTIFICATIONS")] Notifications,
	[EnumMember(Value = "INVOICES")] Invoices
}
#endregion

#region directives
public class IncludeDirective : GraphQlDirective
{
	public IncludeDirective(QueryBuilderParameter<bool> @if) : base("include")
	{
		AddArgument("if", @if);
	}
}

public class SkipDirective : GraphQlDirective
{
	public SkipDirective(QueryBuilderParameter<bool> @if) : base("skip")
	{
		AddArgument("if", @if);
	}
}
#endregion

#region builder classes
public partial class AddressQueryBuilder : GraphQlQueryBuilder<AddressQueryBuilder>
{
	private static readonly GraphQlFieldMetadata[] AllFieldMetadata =
		  [
			  new GraphQlFieldMetadata {Name = "address1"},
				 new GraphQlFieldMetadata {Name = "address2"},
				 new GraphQlFieldMetadata {Name = "address3"},
				 new GraphQlFieldMetadata {Name = "city"},
				 new GraphQlFieldMetadata {Name = "postalCode"},
				 new GraphQlFieldMetadata {Name = "country"},
				 new GraphQlFieldMetadata {Name = "latitude"},
				 new GraphQlFieldMetadata {Name = "longitude"}
		  ];

	protected override string TypeName { get { return "Address"; } }
	public override IReadOnlyList<GraphQlFieldMetadata> AllFields { get { return AllFieldMetadata; } }
	public AddressQueryBuilder WithAddress1(string alias = null, IncludeDirective include = null, SkipDirective skip = null) => WithScalarField("address1", alias, [include, skip]);
	public AddressQueryBuilder ExceptAddress1() => ExceptField("address1");
	public AddressQueryBuilder WithAddress2(string alias = null, IncludeDirective include = null, SkipDirective skip = null) => WithScalarField("address2", alias, [include, skip]);
	public AddressQueryBuilder ExceptAddress2() => ExceptField("address2");
	public AddressQueryBuilder WithAddress3(string alias = null, IncludeDirective include = null, SkipDirective skip = null) => WithScalarField("address3", alias, [include, skip]);
	public AddressQueryBuilder ExceptAddress3() => ExceptField("address3");
	public AddressQueryBuilder WithCity(string alias = null, IncludeDirective include = null, SkipDirective skip = null) => WithScalarField("city", alias, [include, skip]);
	public AddressQueryBuilder ExceptCity() => ExceptField("city");
	public AddressQueryBuilder WithPostalCode(string alias = null, IncludeDirective include = null, SkipDirective skip = null) => WithScalarField("postalCode", alias, [include, skip]);
	public AddressQueryBuilder ExceptPostalCode() => ExceptField("postalCode");
	public AddressQueryBuilder WithCountry(string alias = null, IncludeDirective include = null, SkipDirective skip = null) => WithScalarField("country", alias, [include, skip]);
	public AddressQueryBuilder ExceptCountry() => ExceptField("country");
	public AddressQueryBuilder WithLatitude(string alias = null, IncludeDirective include = null, SkipDirective skip = null) => WithScalarField("latitude", alias, [include, skip]);
	public AddressQueryBuilder ExceptLatitude() => ExceptField("latitude");
	public AddressQueryBuilder WithLongitude(string alias = null, IncludeDirective include = null, SkipDirective skip = null) => WithScalarField("longitude", alias, [include, skip]);
	public AddressQueryBuilder ExceptLongitude() => ExceptField("longitude");
}

public partial class ContactInfoQueryBuilder : GraphQlQueryBuilder<ContactInfoQueryBuilder>
{
	private static readonly GraphQlFieldMetadata[] AllFieldMetadata =
		  [
			  new GraphQlFieldMetadata {Name = "email"},
				 new GraphQlFieldMetadata {Name = "mobile"}
		  ];

	protected override string TypeName { get { return "ContactInfo"; } }
	public override IReadOnlyList<GraphQlFieldMetadata> AllFields { get { return AllFieldMetadata; } }
	public ContactInfoQueryBuilder WithEmail(string alias = null, IncludeDirective include = null, SkipDirective skip = null) => WithScalarField("email", alias, [include, skip]);
	public ContactInfoQueryBuilder ExceptEmail() => ExceptField("email");
	public ContactInfoQueryBuilder WithMobile(string alias = null, IncludeDirective include = null, SkipDirective skip = null) => WithScalarField("mobile", alias, [include, skip]);
	public ContactInfoQueryBuilder ExceptMobile() => ExceptField("mobile");
}

public partial class LegalEntityQueryBuilder : GraphQlQueryBuilder<LegalEntityQueryBuilder>
{
	private static readonly GraphQlFieldMetadata[] AllFieldMetadata =
		  [
			  new GraphQlFieldMetadata {Name = "id"},
				 new GraphQlFieldMetadata {Name = "firstName"},
				 new GraphQlFieldMetadata {Name = "isCompany"},
				 new GraphQlFieldMetadata {Name = "name"},
				 new GraphQlFieldMetadata {Name = "middleName"},
				 new GraphQlFieldMetadata {Name = "lastName"},
				 new GraphQlFieldMetadata {Name = "organizationNo"},
				 new GraphQlFieldMetadata {Name = "language"},
				 new GraphQlFieldMetadata {Name = "contactInfo", IsComplex = true, QueryBuilderType = typeof(ContactInfoQueryBuilder) },
				 new GraphQlFieldMetadata {Name = "address", IsComplex = true, QueryBuilderType = typeof(AddressQueryBuilder) }
		  ];

	protected override string TypeName { get { return "LegalEntity"; } }
	public override IReadOnlyList<GraphQlFieldMetadata> AllFields { get { return AllFieldMetadata; } }
	public LegalEntityQueryBuilder WithId(string alias = null, IncludeDirective include = null, SkipDirective skip = null) => WithScalarField("id", alias, [include, skip]);
	public LegalEntityQueryBuilder ExceptId() => ExceptField("id");
	public LegalEntityQueryBuilder WithFirstName(string alias = null, IncludeDirective include = null, SkipDirective skip = null) => WithScalarField("firstName", alias, [include, skip]);
	public LegalEntityQueryBuilder ExceptFirstName() => ExceptField("firstName");
	public LegalEntityQueryBuilder WithIsCompany(string alias = null, IncludeDirective include = null, SkipDirective skip = null) => WithScalarField("isCompany", alias, [include, skip]);
	public LegalEntityQueryBuilder ExceptIsCompany() => ExceptField("isCompany");
	public LegalEntityQueryBuilder WithName(string alias = null, IncludeDirective include = null, SkipDirective skip = null) => WithScalarField("name", alias, [include, skip]);
	public LegalEntityQueryBuilder ExceptName() => ExceptField("name");
	public LegalEntityQueryBuilder WithMiddleName(string alias = null, IncludeDirective include = null, SkipDirective skip = null) => WithScalarField("middleName", alias, [include, skip]);
	public LegalEntityQueryBuilder ExceptMiddleName() => ExceptField("middleName");
	public LegalEntityQueryBuilder WithLastName(string alias = null, IncludeDirective include = null, SkipDirective skip = null) => WithScalarField("lastName", alias, [include, skip]);
	public LegalEntityQueryBuilder ExceptLastName() => ExceptField("lastName");
	public LegalEntityQueryBuilder WithOrganizationNo(string alias = null, IncludeDirective include = null, SkipDirective skip = null) => WithScalarField("organizationNo", alias, [include, skip]);
	public LegalEntityQueryBuilder ExceptOrganizationNo() => ExceptField("organizationNo");
	public LegalEntityQueryBuilder WithLanguage(string alias = null, IncludeDirective include = null, SkipDirective skip = null) => WithScalarField("language", alias, [include, skip]);
	public LegalEntityQueryBuilder ExceptLanguage() => ExceptField("language");
	public LegalEntityQueryBuilder WithContactInfo(ContactInfoQueryBuilder contactInfoQueryBuilder, string alias = null, IncludeDirective include = null, SkipDirective skip = null) => WithObjectField("contactInfo", alias, contactInfoQueryBuilder, [include, skip]);
	public LegalEntityQueryBuilder ExceptContactInfo() => ExceptField("contactInfo");
	public LegalEntityQueryBuilder WithAddress(AddressQueryBuilder addressQueryBuilder, string alias = null, IncludeDirective include = null, SkipDirective skip = null) => WithObjectField("address", alias, addressQueryBuilder, [include, skip]);
	public LegalEntityQueryBuilder ExceptAddress() => ExceptField("address");
}

public partial class HomeConsumptionPageInfoQueryBuilder : GraphQlQueryBuilder<HomeConsumptionPageInfoQueryBuilder>
{
	private static readonly GraphQlFieldMetadata[] AllFieldMetadata =
		  [
			  new GraphQlFieldMetadata {Name = "endCursor"},
				 new GraphQlFieldMetadata {Name = "hasNextPage"},
				 new GraphQlFieldMetadata {Name = "hasPreviousPage"},
				 new GraphQlFieldMetadata {Name = "startCursor"},
				 new GraphQlFieldMetadata {Name = "count"},
				 new GraphQlFieldMetadata {Name = "currency"},
				 new GraphQlFieldMetadata {Name = "totalCost"},
				 new GraphQlFieldMetadata {Name = "totalConsumption"},
				 new GraphQlFieldMetadata {Name = "filtered"}
		  ];

	protected override string TypeName { get { return "HomeConsumptionPageInfo"; } }
	public override IReadOnlyList<GraphQlFieldMetadata> AllFields { get { return AllFieldMetadata; } }
	public HomeConsumptionPageInfoQueryBuilder WithEndCursor(string alias = null, IncludeDirective include = null, SkipDirective skip = null) => WithScalarField("endCursor", alias, [include, skip]);
	public HomeConsumptionPageInfoQueryBuilder ExceptEndCursor() => ExceptField("endCursor");
	public HomeConsumptionPageInfoQueryBuilder WithHasNextPage(string alias = null, IncludeDirective include = null, SkipDirective skip = null) => WithScalarField("hasNextPage", alias, [include, skip]);
	public HomeConsumptionPageInfoQueryBuilder ExceptHasNextPage() => ExceptField("hasNextPage");
	public HomeConsumptionPageInfoQueryBuilder WithHasPreviousPage(string alias = null, IncludeDirective include = null, SkipDirective skip = null) => WithScalarField("hasPreviousPage", alias, [include, skip]);
	public HomeConsumptionPageInfoQueryBuilder ExceptHasPreviousPage() => ExceptField("hasPreviousPage");
	public HomeConsumptionPageInfoQueryBuilder WithStartCursor(string alias = null, IncludeDirective include = null, SkipDirective skip = null) => WithScalarField("startCursor", alias, [include, skip]);
	public HomeConsumptionPageInfoQueryBuilder ExceptStartCursor() => ExceptField("startCursor");
	public HomeConsumptionPageInfoQueryBuilder WithCount(string alias = null, IncludeDirective include = null, SkipDirective skip = null) => WithScalarField("count", alias, [include, skip]);
	public HomeConsumptionPageInfoQueryBuilder ExceptCount() => ExceptField("count");
	public HomeConsumptionPageInfoQueryBuilder WithCurrency(string alias = null, IncludeDirective include = null, SkipDirective skip = null) => WithScalarField("currency", alias, [include, skip]);
	public HomeConsumptionPageInfoQueryBuilder ExceptCurrency() => ExceptField("currency");
	public HomeConsumptionPageInfoQueryBuilder WithTotalCost(string alias = null, IncludeDirective include = null, SkipDirective skip = null) => WithScalarField("totalCost", alias, [include, skip]);
	public HomeConsumptionPageInfoQueryBuilder ExceptTotalCost() => ExceptField("totalCost");
	public HomeConsumptionPageInfoQueryBuilder WithTotalConsumption(string alias = null, IncludeDirective include = null, SkipDirective skip = null) => WithScalarField("totalConsumption", alias, [include, skip]);
	public HomeConsumptionPageInfoQueryBuilder ExceptTotalConsumption() => ExceptField("totalConsumption");
	public HomeConsumptionPageInfoQueryBuilder WithFiltered(string alias = null, IncludeDirective include = null, SkipDirective skip = null) => WithScalarField("filtered", alias, [include, skip]);
	public HomeConsumptionPageInfoQueryBuilder ExceptFiltered() => ExceptField("filtered");
}

public partial class HomeProductionPageInfoQueryBuilder : GraphQlQueryBuilder<HomeProductionPageInfoQueryBuilder>
{
	private static readonly GraphQlFieldMetadata[] AllFieldMetadata =
		  [
			  new GraphQlFieldMetadata {Name = "endCursor"},
				 new GraphQlFieldMetadata {Name = "hasNextPage"},
				 new GraphQlFieldMetadata {Name = "hasPreviousPage"},
				 new GraphQlFieldMetadata {Name = "startCursor"},
				 new GraphQlFieldMetadata {Name = "count"},
				 new GraphQlFieldMetadata {Name = "currency"},
				 new GraphQlFieldMetadata {Name = "totalProfit"},
				 new GraphQlFieldMetadata {Name = "totalProduction"},
				 new GraphQlFieldMetadata {Name = "filtered"}
		  ];

	protected override string TypeName { get { return "HomeProductionPageInfo"; } }
	public override IReadOnlyList<GraphQlFieldMetadata> AllFields { get { return AllFieldMetadata; } }
	public HomeProductionPageInfoQueryBuilder WithEndCursor(string alias = null, IncludeDirective include = null, SkipDirective skip = null) => WithScalarField("endCursor", alias, [include, skip]);
	public HomeProductionPageInfoQueryBuilder ExceptEndCursor() => ExceptField("endCursor");
	public HomeProductionPageInfoQueryBuilder WithHasNextPage(string alias = null, IncludeDirective include = null, SkipDirective skip = null) => WithScalarField("hasNextPage", alias, [include, skip]);
	public HomeProductionPageInfoQueryBuilder ExceptHasNextPage() => ExceptField("hasNextPage");
	public HomeProductionPageInfoQueryBuilder WithHasPreviousPage(string alias = null, IncludeDirective include = null, SkipDirective skip = null) => WithScalarField("hasPreviousPage", alias, [include, skip]);
	public HomeProductionPageInfoQueryBuilder ExceptHasPreviousPage() => ExceptField("hasPreviousPage");
	public HomeProductionPageInfoQueryBuilder WithStartCursor(string alias = null, IncludeDirective include = null, SkipDirective skip = null) => WithScalarField("startCursor", alias, [include, skip]);
	public HomeProductionPageInfoQueryBuilder ExceptStartCursor() => ExceptField("startCursor");
	public HomeProductionPageInfoQueryBuilder WithCount(string alias = null, IncludeDirective include = null, SkipDirective skip = null) => WithScalarField("count", alias, [include, skip]);
	public HomeProductionPageInfoQueryBuilder ExceptCount() => ExceptField("count");
	public HomeProductionPageInfoQueryBuilder WithCurrency(string alias = null, IncludeDirective include = null, SkipDirective skip = null) => WithScalarField("currency", alias, [include, skip]);
	public HomeProductionPageInfoQueryBuilder ExceptCurrency() => ExceptField("currency");
	public HomeProductionPageInfoQueryBuilder WithTotalProfit(string alias = null, IncludeDirective include = null, SkipDirective skip = null) => WithScalarField("totalProfit", alias, [include, skip]);
	public HomeProductionPageInfoQueryBuilder ExceptTotalProfit() => ExceptField("totalProfit");
	public HomeProductionPageInfoQueryBuilder WithTotalProduction(string alias = null, IncludeDirective include = null, SkipDirective skip = null) => WithScalarField("totalProduction", alias, [include, skip]);
	public HomeProductionPageInfoQueryBuilder ExceptTotalProduction() => ExceptField("totalProduction");
	public HomeProductionPageInfoQueryBuilder WithFiltered(string alias = null, IncludeDirective include = null, SkipDirective skip = null) => WithScalarField("filtered", alias, [include, skip]);
	public HomeProductionPageInfoQueryBuilder ExceptFiltered() => ExceptField("filtered");
}

public partial class PriceQueryBuilder : GraphQlQueryBuilder<PriceQueryBuilder>
{
	private static readonly GraphQlFieldMetadata[] AllFieldMetadata =
		  [
			  new GraphQlFieldMetadata {Name = "total"},
				 new GraphQlFieldMetadata {Name = "energy"},
				 new GraphQlFieldMetadata {Name = "tax"},
				 new GraphQlFieldMetadata {Name = "startsAt"},
				 new GraphQlFieldMetadata {Name = "currency"},
				 new GraphQlFieldMetadata {Name = "level"}
		  ];

	protected override string TypeName { get { return "Price"; } }
	public override IReadOnlyList<GraphQlFieldMetadata> AllFields { get { return AllFieldMetadata; } }
	public PriceQueryBuilder WithTotal(string alias = null, IncludeDirective include = null, SkipDirective skip = null) => WithScalarField("total", alias, [include, skip]);
	public PriceQueryBuilder ExceptTotal() => ExceptField("total");
	public PriceQueryBuilder WithEnergy(string alias = null, IncludeDirective include = null, SkipDirective skip = null) => WithScalarField("energy", alias, [include, skip]);
	public PriceQueryBuilder ExceptEnergy() => ExceptField("energy");
	public PriceQueryBuilder WithTax(string alias = null, IncludeDirective include = null, SkipDirective skip = null) => WithScalarField("tax", alias, [include, skip]);
	public PriceQueryBuilder ExceptTax() => ExceptField("tax");
	public PriceQueryBuilder WithStartsAt(string alias = null, IncludeDirective include = null, SkipDirective skip = null) => WithScalarField("startsAt", alias, [include, skip]);
	public PriceQueryBuilder ExceptStartsAt() => ExceptField("startsAt");
	public PriceQueryBuilder WithCurrency(string alias = null, IncludeDirective include = null, SkipDirective skip = null) => WithScalarField("currency", alias, [include, skip]);
	public PriceQueryBuilder ExceptCurrency() => ExceptField("currency");
	public PriceQueryBuilder WithLevel(string alias = null, IncludeDirective include = null, SkipDirective skip = null) => WithScalarField("level", alias, [include, skip]);
	public PriceQueryBuilder ExceptLevel() => ExceptField("level");
}

public partial class SubscriptionPriceEdgeQueryBuilder : GraphQlQueryBuilder<SubscriptionPriceEdgeQueryBuilder>
{
	private static readonly GraphQlFieldMetadata[] AllFieldMetadata =
		  [
			  new GraphQlFieldMetadata { Name = "cursor" },
				 new GraphQlFieldMetadata { Name = "node", IsComplex = true, QueryBuilderType = typeof(PriceQueryBuilder) }
		  ];

	protected override string TypeName { get { return "SubscriptionPriceEdge"; } }
	public override IReadOnlyList<GraphQlFieldMetadata> AllFields { get { return AllFieldMetadata; } }
	public SubscriptionPriceEdgeQueryBuilder WithCursor(string alias = null, IncludeDirective include = null, SkipDirective skip = null) => WithScalarField("cursor", alias, [include, skip]);
	public SubscriptionPriceEdgeQueryBuilder ExceptCursor() => ExceptField("cursor");
	public SubscriptionPriceEdgeQueryBuilder WithNode(PriceQueryBuilder priceQueryBuilder, string alias = null, IncludeDirective include = null, SkipDirective skip = null) => WithObjectField("node", alias, priceQueryBuilder, [include, skip]);
	public SubscriptionPriceEdgeQueryBuilder ExceptNode() => ExceptField("node");
}

public partial class PageInfoQueryBuilder : GraphQlQueryBuilder<PageInfoQueryBuilder>
{
	private static readonly GraphQlFieldMetadata[] AllFieldMetadata =
		  [
			  new GraphQlFieldMetadata {Name = "endCursor"},
				 new GraphQlFieldMetadata {Name = "hasNextPage"},
				 new GraphQlFieldMetadata {Name = "hasPreviousPage"},
				 new GraphQlFieldMetadata {Name = "startCursor"}
		  ];

	protected override string TypeName { get { return "PageInfo"; } }
	public override IReadOnlyList<GraphQlFieldMetadata> AllFields { get { return AllFieldMetadata; } }
	public PageInfoQueryBuilder WithEndCursor(string alias = null, IncludeDirective include = null, SkipDirective skip = null) => WithScalarField("endCursor", alias, [include, skip]);
	public PageInfoQueryBuilder ExceptEndCursor() => ExceptField("endCursor");
	public PageInfoQueryBuilder WithHasNextPage(string alias = null, IncludeDirective include = null, SkipDirective skip = null) => WithScalarField("hasNextPage", alias, [include, skip]);
	public PageInfoQueryBuilder ExceptHasNextPage() => ExceptField("hasNextPage");
	public PageInfoQueryBuilder WithHasPreviousPage(string alias = null, IncludeDirective include = null, SkipDirective skip = null) => WithScalarField("hasPreviousPage", alias, [include, skip]);
	public PageInfoQueryBuilder ExceptHasPreviousPage() => ExceptField("hasPreviousPage");
	public PageInfoQueryBuilder WithStartCursor(string alias = null, IncludeDirective include = null, SkipDirective skip = null) => WithScalarField("startCursor", alias, [include, skip]);
	public PageInfoQueryBuilder ExceptStartCursor() => ExceptField("startCursor");
	public PageInfoQueryBuilder WithHomeConsumptionPageInfoFragment(HomeConsumptionPageInfoQueryBuilder homeConsumptionPageInfoQueryBuilder, IncludeDirective include = null, SkipDirective skip = null) => WithFragment(homeConsumptionPageInfoQueryBuilder, [include, skip]);
	public PageInfoQueryBuilder WithHomeProductionPageInfoFragment(HomeProductionPageInfoQueryBuilder homeProductionPageInfoQueryBuilder, IncludeDirective include = null, SkipDirective skip = null) => WithFragment(homeProductionPageInfoQueryBuilder, [include, skip]);
	public PageInfoQueryBuilder WithSubscriptionPriceConnectionPageInfoFragment(SubscriptionPriceConnectionPageInfoQueryBuilder subscriptionPriceConnectionPageInfoQueryBuilder, IncludeDirective include = null, SkipDirective skip = null) => WithFragment(subscriptionPriceConnectionPageInfoQueryBuilder, [include, skip]);
}

public partial class SubscriptionPriceConnectionPageInfoQueryBuilder : GraphQlQueryBuilder<SubscriptionPriceConnectionPageInfoQueryBuilder>
{
	private static readonly GraphQlFieldMetadata[] AllFieldMetadata =
		  [
			  new GraphQlFieldMetadata {Name = "endCursor"},
				 new GraphQlFieldMetadata {Name = "hasNextPage"},
				 new GraphQlFieldMetadata {Name = "hasPreviousPage"},
				 new GraphQlFieldMetadata {Name = "startCursor"},
				 new GraphQlFieldMetadata {Name = "resolution"},
				 new GraphQlFieldMetadata {Name = "currency"},
				 new GraphQlFieldMetadata {Name = "count"},
				 new GraphQlFieldMetadata {Name = "precision"},
				 new GraphQlFieldMetadata {Name = "minEnergy"},
				 new GraphQlFieldMetadata {Name = "minTotal"},
				 new GraphQlFieldMetadata {Name = "maxEnergy"},
				 new GraphQlFieldMetadata {Name = "maxTotal"}
		  ];

	protected override string TypeName { get { return "SubscriptionPriceConnectionPageInfo"; } }
	public override IReadOnlyList<GraphQlFieldMetadata> AllFields { get { return AllFieldMetadata; } }
	public SubscriptionPriceConnectionPageInfoQueryBuilder WithEndCursor(string alias = null, IncludeDirective include = null, SkipDirective skip = null) => WithScalarField("endCursor", alias, [include, skip]);
	public SubscriptionPriceConnectionPageInfoQueryBuilder ExceptEndCursor() => ExceptField("endCursor");
	public SubscriptionPriceConnectionPageInfoQueryBuilder WithHasNextPage(string alias = null, IncludeDirective include = null, SkipDirective skip = null) => WithScalarField("hasNextPage", alias, [include, skip]);
	public SubscriptionPriceConnectionPageInfoQueryBuilder ExceptHasNextPage() => ExceptField("hasNextPage");
	public SubscriptionPriceConnectionPageInfoQueryBuilder WithHasPreviousPage(string alias = null, IncludeDirective include = null, SkipDirective skip = null) => WithScalarField("hasPreviousPage", alias, [include, skip]);
	public SubscriptionPriceConnectionPageInfoQueryBuilder ExceptHasPreviousPage() => ExceptField("hasPreviousPage");
	public SubscriptionPriceConnectionPageInfoQueryBuilder WithStartCursor(string alias = null, IncludeDirective include = null, SkipDirective skip = null) => WithScalarField("startCursor", alias, [include, skip]);
	public SubscriptionPriceConnectionPageInfoQueryBuilder ExceptStartCursor() => ExceptField("startCursor");
	public SubscriptionPriceConnectionPageInfoQueryBuilder WithResolution(string alias = null, IncludeDirective include = null, SkipDirective skip = null) => WithScalarField("resolution", alias, [include, skip]);
	public SubscriptionPriceConnectionPageInfoQueryBuilder ExceptResolution() => ExceptField("resolution");
	public SubscriptionPriceConnectionPageInfoQueryBuilder WithCurrency(string alias = null, IncludeDirective include = null, SkipDirective skip = null) => WithScalarField("currency", alias, [include, skip]);
	public SubscriptionPriceConnectionPageInfoQueryBuilder ExceptCurrency() => ExceptField("currency");
	public SubscriptionPriceConnectionPageInfoQueryBuilder WithCount(string alias = null, IncludeDirective include = null, SkipDirective skip = null) => WithScalarField("count", alias, [include, skip]);
	public SubscriptionPriceConnectionPageInfoQueryBuilder ExceptCount() => ExceptField("count");
	public SubscriptionPriceConnectionPageInfoQueryBuilder WithPrecision(string alias = null, IncludeDirective include = null, SkipDirective skip = null) => WithScalarField("precision", alias, [include, skip]);
	public SubscriptionPriceConnectionPageInfoQueryBuilder ExceptPrecision() => ExceptField("precision");
	public SubscriptionPriceConnectionPageInfoQueryBuilder WithMinEnergy(string alias = null, IncludeDirective include = null, SkipDirective skip = null) => WithScalarField("minEnergy", alias, [include, skip]);
	public SubscriptionPriceConnectionPageInfoQueryBuilder ExceptMinEnergy() => ExceptField("minEnergy");
	public SubscriptionPriceConnectionPageInfoQueryBuilder WithMinTotal(string alias = null, IncludeDirective include = null, SkipDirective skip = null) => WithScalarField("minTotal", alias, [include, skip]);
	public SubscriptionPriceConnectionPageInfoQueryBuilder ExceptMinTotal() => ExceptField("minTotal");
	public SubscriptionPriceConnectionPageInfoQueryBuilder WithMaxEnergy(string alias = null, IncludeDirective include = null, SkipDirective skip = null) => WithScalarField("maxEnergy", alias, [include, skip]);
	public SubscriptionPriceConnectionPageInfoQueryBuilder ExceptMaxEnergy() => ExceptField("maxEnergy");
	public SubscriptionPriceConnectionPageInfoQueryBuilder WithMaxTotal(string alias = null, IncludeDirective include = null, SkipDirective skip = null) => WithScalarField("maxTotal", alias, [include, skip]);
	public SubscriptionPriceConnectionPageInfoQueryBuilder ExceptMaxTotal() => ExceptField("maxTotal");
}

public partial class SubscriptionPriceConnectionQueryBuilder : GraphQlQueryBuilder<SubscriptionPriceConnectionQueryBuilder>
{
	private static readonly GraphQlFieldMetadata[] AllFieldMetadata =
		  [
			  new GraphQlFieldMetadata {Name = "pageInfo", IsComplex = true, QueryBuilderType = typeof(SubscriptionPriceConnectionPageInfoQueryBuilder) },
				 new GraphQlFieldMetadata {Name = "edges", IsComplex = true, QueryBuilderType = typeof(SubscriptionPriceEdgeQueryBuilder) },
				 new GraphQlFieldMetadata {Name = "nodes", IsComplex = true, QueryBuilderType = typeof(PriceQueryBuilder) }
		  ];

	protected override string TypeName { get { return "SubscriptionPriceConnection"; } }
	public override IReadOnlyList<GraphQlFieldMetadata> AllFields { get { return AllFieldMetadata; } }
	public SubscriptionPriceConnectionQueryBuilder WithPageInfo(SubscriptionPriceConnectionPageInfoQueryBuilder subscriptionPriceConnectionPageInfoQueryBuilder, string alias = null, IncludeDirective include = null, SkipDirective skip = null) => WithObjectField("pageInfo", alias, subscriptionPriceConnectionPageInfoQueryBuilder, [include, skip]);
	public SubscriptionPriceConnectionQueryBuilder ExceptPageInfo() => ExceptField("pageInfo");
	public SubscriptionPriceConnectionQueryBuilder WithEdges(SubscriptionPriceEdgeQueryBuilder subscriptionPriceEdgeQueryBuilder, string alias = null, IncludeDirective include = null, SkipDirective skip = null) => WithObjectField("edges", alias, subscriptionPriceEdgeQueryBuilder, [include, skip]);
	public SubscriptionPriceConnectionQueryBuilder ExceptEdges() => ExceptField("edges");
	public SubscriptionPriceConnectionQueryBuilder WithNodes(PriceQueryBuilder priceQueryBuilder, string alias = null, IncludeDirective include = null, SkipDirective skip = null) => WithObjectField("nodes", alias, priceQueryBuilder, [include, skip]);
	public SubscriptionPriceConnectionQueryBuilder ExceptNodes() => ExceptField("nodes");
}

public partial class PriceInfoQueryBuilder : GraphQlQueryBuilder<PriceInfoQueryBuilder>
{
	private static readonly GraphQlFieldMetadata[] AllFieldMetadata =
		  [
			  new GraphQlFieldMetadata {Name = "current", IsComplex = true, QueryBuilderType = typeof(PriceQueryBuilder) },
				 new GraphQlFieldMetadata {Name = "today", IsComplex = true, QueryBuilderType = typeof(PriceQueryBuilder) },
				 new GraphQlFieldMetadata {Name = "tomorrow", IsComplex = true, QueryBuilderType = typeof(PriceQueryBuilder) },
				 new GraphQlFieldMetadata {Name = "range", IsComplex = true, QueryBuilderType = typeof(SubscriptionPriceConnectionQueryBuilder) }
		  ];

	protected override string TypeName { get { return "PriceInfo"; } }
	public override IReadOnlyList<GraphQlFieldMetadata> AllFields { get { return AllFieldMetadata; } }
	public PriceInfoQueryBuilder WithCurrent(PriceQueryBuilder priceQueryBuilder, string alias = null, IncludeDirective include = null, SkipDirective skip = null) => WithObjectField("current", alias, priceQueryBuilder, [include, skip]);
	public PriceInfoQueryBuilder ExceptCurrent() => ExceptField("current");
	public PriceInfoQueryBuilder WithToday(PriceQueryBuilder priceQueryBuilder, string alias = null, IncludeDirective include = null, SkipDirective skip = null) => WithObjectField("today", alias, priceQueryBuilder, [include, skip]);
	public PriceInfoQueryBuilder ExceptToday() => ExceptField("today");
	public PriceInfoQueryBuilder WithTomorrow(PriceQueryBuilder priceQueryBuilder, string alias = null, IncludeDirective include = null, SkipDirective skip = null) => WithObjectField("tomorrow", alias, priceQueryBuilder, [include, skip]);
	public PriceInfoQueryBuilder ExceptTomorrow() => ExceptField("tomorrow");

	public PriceInfoQueryBuilder WithRange(SubscriptionPriceConnectionQueryBuilder subscriptionPriceConnectionQueryBuilder, QueryBuilderParameter<PriceResolution> resolution, QueryBuilderParameter<int?> first = null, QueryBuilderParameter<int?> last = null, QueryBuilderParameter<string> before = null, QueryBuilderParameter<string> after = null, string alias = null, IncludeDirective include = null, SkipDirective skip = null)
	{
		var args = new List<QueryBuilderArgumentInfo>();
		args.Add(new QueryBuilderArgumentInfo { ArgumentName = "resolution", ArgumentValue = resolution });

		if (first != null) args.Add(new QueryBuilderArgumentInfo { ArgumentName = "first", ArgumentValue = first });
		if (last != null) args.Add(new QueryBuilderArgumentInfo { ArgumentName = "last", ArgumentValue = last });
		if (before != null) args.Add(new QueryBuilderArgumentInfo { ArgumentName = "before", ArgumentValue = before });
		if (after != null) args.Add(new QueryBuilderArgumentInfo { ArgumentName = "after", ArgumentValue = after });

		return WithObjectField("range", alias, subscriptionPriceConnectionQueryBuilder, [include, skip], args);
	}

	public PriceInfoQueryBuilder ExceptRange() => ExceptField("range");
}

public partial class PriceRatingEntryQueryBuilder : GraphQlQueryBuilder<PriceRatingEntryQueryBuilder>
{
	private static readonly GraphQlFieldMetadata[] AllFieldMetadata =
		  [
			  new GraphQlFieldMetadata {Name = "time"},
				 new GraphQlFieldMetadata {Name = "energy"},
				 new GraphQlFieldMetadata {Name = "total"},
				 new GraphQlFieldMetadata {Name = "tax"},
				 new GraphQlFieldMetadata {Name = "difference"},
				 new GraphQlFieldMetadata {Name = "level"}
		  ];

	protected override string TypeName { get { return "PriceRatingEntry"; } }
	public override IReadOnlyList<GraphQlFieldMetadata> AllFields { get { return AllFieldMetadata; } }
	public PriceRatingEntryQueryBuilder WithTime(string alias = null, IncludeDirective include = null, SkipDirective skip = null) => WithScalarField("time", alias, [include, skip]);
	public PriceRatingEntryQueryBuilder ExceptTime() => ExceptField("time");
	public PriceRatingEntryQueryBuilder WithEnergy(string alias = null, IncludeDirective include = null, SkipDirective skip = null) => WithScalarField("energy", alias, [include, skip]);
	public PriceRatingEntryQueryBuilder ExceptEnergy() => ExceptField("energy");
	public PriceRatingEntryQueryBuilder WithTotal(string alias = null, IncludeDirective include = null, SkipDirective skip = null) => WithScalarField("total", alias, [include, skip]);
	public PriceRatingEntryQueryBuilder ExceptTotal() => ExceptField("total");
	public PriceRatingEntryQueryBuilder WithTax(string alias = null, IncludeDirective include = null, SkipDirective skip = null) => WithScalarField("tax", alias, [include, skip]);
	public PriceRatingEntryQueryBuilder ExceptTax() => ExceptField("tax");
	public PriceRatingEntryQueryBuilder WithDifference(string alias = null, IncludeDirective include = null, SkipDirective skip = null) => WithScalarField("difference", alias, [include, skip]);
	public PriceRatingEntryQueryBuilder ExceptDifference() => ExceptField("difference");
	public PriceRatingEntryQueryBuilder WithLevel(string alias = null, IncludeDirective include = null, SkipDirective skip = null) => WithScalarField("level", alias, [include, skip]);
	public PriceRatingEntryQueryBuilder ExceptLevel() => ExceptField("level");
}

public partial class PriceRatingTypeQueryBuilder : GraphQlQueryBuilder<PriceRatingTypeQueryBuilder>
{
	private static readonly GraphQlFieldMetadata[] AllFieldMetadata =
		  [
			  new GraphQlFieldMetadata {Name = "minEnergy"},
				 new GraphQlFieldMetadata {Name = "maxEnergy"},
				 new GraphQlFieldMetadata {Name = "minTotal"},
				 new GraphQlFieldMetadata {Name = "maxTotal"},
				 new GraphQlFieldMetadata {Name = "currency"},
				 new GraphQlFieldMetadata {Name = "entries", IsComplex = true, QueryBuilderType = typeof(PriceRatingEntryQueryBuilder) }
		  ];

	protected override string TypeName { get { return "PriceRatingType"; } }
	public override IReadOnlyList<GraphQlFieldMetadata> AllFields { get { return AllFieldMetadata; } }
	public PriceRatingTypeQueryBuilder WithMinEnergy(string alias = null, IncludeDirective include = null, SkipDirective skip = null) => WithScalarField("minEnergy", alias, [include, skip]);
	public PriceRatingTypeQueryBuilder ExceptMinEnergy() => ExceptField("minEnergy");
	public PriceRatingTypeQueryBuilder WithMaxEnergy(string alias = null, IncludeDirective include = null, SkipDirective skip = null) => WithScalarField("maxEnergy", alias, [include, skip]);
	public PriceRatingTypeQueryBuilder ExceptMaxEnergy() => ExceptField("maxEnergy");
	public PriceRatingTypeQueryBuilder WithMinTotal(string alias = null, IncludeDirective include = null, SkipDirective skip = null) => WithScalarField("minTotal", alias, [include, skip]);
	public PriceRatingTypeQueryBuilder ExceptMinTotal() => ExceptField("minTotal");
	public PriceRatingTypeQueryBuilder WithMaxTotal(string alias = null, IncludeDirective include = null, SkipDirective skip = null) => WithScalarField("maxTotal", alias, [include, skip]);
	public PriceRatingTypeQueryBuilder ExceptMaxTotal() => ExceptField("maxTotal");
	public PriceRatingTypeQueryBuilder WithCurrency(string alias = null, IncludeDirective include = null, SkipDirective skip = null) => WithScalarField("currency", alias, [include, skip]);
	public PriceRatingTypeQueryBuilder ExceptCurrency() => ExceptField("currency");
	public PriceRatingTypeQueryBuilder WithEntries(PriceRatingEntryQueryBuilder priceRatingEntryQueryBuilder, string alias = null, IncludeDirective include = null, SkipDirective skip = null) => WithObjectField("entries", alias, priceRatingEntryQueryBuilder, [include, skip]);
	public PriceRatingTypeQueryBuilder ExceptEntries() => ExceptField("entries");
}

public partial class PriceRatingThresholdPercentagesQueryBuilder : GraphQlQueryBuilder<PriceRatingThresholdPercentagesQueryBuilder>
{
	private static readonly GraphQlFieldMetadata[] AllFieldMetadata =
		  [
			  new GraphQlFieldMetadata {Name = "high"},
				 new GraphQlFieldMetadata {Name = "low"}
		  ];

	protected override string TypeName { get { return "PriceRatingThresholdPercentages"; } }
	public override IReadOnlyList<GraphQlFieldMetadata> AllFields { get { return AllFieldMetadata; } }
	public PriceRatingThresholdPercentagesQueryBuilder WithHigh(string alias = null, IncludeDirective include = null, SkipDirective skip = null) => WithScalarField("high", alias, [include, skip]);
	public PriceRatingThresholdPercentagesQueryBuilder ExceptHigh() => ExceptField("high");
	public PriceRatingThresholdPercentagesQueryBuilder WithLow(string alias = null, IncludeDirective include = null, SkipDirective skip = null) => WithScalarField("low", alias, [include, skip]);
	public PriceRatingThresholdPercentagesQueryBuilder ExceptLow() => ExceptField("low");
}

public partial class PriceRatingQueryBuilder : GraphQlQueryBuilder<PriceRatingQueryBuilder>
{
	private static readonly GraphQlFieldMetadata[] AllFieldMetadata =
		  [
			  new GraphQlFieldMetadata {Name = "thresholdPercentages", IsComplex = true, QueryBuilderType = typeof(PriceRatingThresholdPercentagesQueryBuilder) },
				 new GraphQlFieldMetadata {Name = "hourly", IsComplex = true, QueryBuilderType = typeof(PriceRatingTypeQueryBuilder) },
				 new GraphQlFieldMetadata {Name = "daily", IsComplex = true, QueryBuilderType = typeof(PriceRatingTypeQueryBuilder) },
				 new GraphQlFieldMetadata {Name = "monthly", IsComplex = true, QueryBuilderType = typeof(PriceRatingTypeQueryBuilder) }
		  ];

	protected override string TypeName { get { return "PriceRating"; } }
	public override IReadOnlyList<GraphQlFieldMetadata> AllFields { get { return AllFieldMetadata; } }
	public PriceRatingQueryBuilder WithThresholdPercentages(PriceRatingThresholdPercentagesQueryBuilder priceRatingThresholdPercentagesQueryBuilder, string alias = null, IncludeDirective include = null, SkipDirective skip = null) => WithObjectField("thresholdPercentages", alias, priceRatingThresholdPercentagesQueryBuilder, [include, skip]);
	public PriceRatingQueryBuilder ExceptThresholdPercentages() => ExceptField("thresholdPercentages");
	public PriceRatingQueryBuilder WithHourly(PriceRatingTypeQueryBuilder priceRatingTypeQueryBuilder, string alias = null, IncludeDirective include = null, SkipDirective skip = null) => WithObjectField("hourly", alias, priceRatingTypeQueryBuilder, [include, skip]);
	public PriceRatingQueryBuilder ExceptHourly() => ExceptField("hourly");
	public PriceRatingQueryBuilder WithDaily(PriceRatingTypeQueryBuilder priceRatingTypeQueryBuilder, string alias = null, IncludeDirective include = null, SkipDirective skip = null) => WithObjectField("daily", alias, priceRatingTypeQueryBuilder, [include, skip]);
	public PriceRatingQueryBuilder ExceptDaily() => ExceptField("daily");
	public PriceRatingQueryBuilder WithMonthly(PriceRatingTypeQueryBuilder priceRatingTypeQueryBuilder, string alias = null, IncludeDirective include = null, SkipDirective skip = null) => WithObjectField("monthly", alias, priceRatingTypeQueryBuilder, [include, skip]);
	public PriceRatingQueryBuilder ExceptMonthly() => ExceptField("monthly");
}

public partial class SubscriptionQueryBuilder : GraphQlQueryBuilder<SubscriptionQueryBuilder>
{
	private static readonly GraphQlFieldMetadata[] AllFieldMetadata =
		  [
			  new GraphQlFieldMetadata {Name = "id"},
				 new GraphQlFieldMetadata {Name = "subscriber", IsComplex = true, QueryBuilderType = typeof(LegalEntityQueryBuilder) },
				 new GraphQlFieldMetadata {Name = "validFrom"},
				 new GraphQlFieldMetadata {Name = "validTo"},
				 new GraphQlFieldMetadata {Name = "status"},
				 new GraphQlFieldMetadata {Name = "priceInfo", IsComplex = true, QueryBuilderType = typeof(PriceInfoQueryBuilder) },
				 new GraphQlFieldMetadata {Name = "priceRating", IsComplex = true, QueryBuilderType = typeof(PriceRatingQueryBuilder) }
		  ];

	protected override string TypeName { get { return "Subscription"; } }
	public override IReadOnlyList<GraphQlFieldMetadata> AllFields { get { return AllFieldMetadata; } }
	public SubscriptionQueryBuilder WithId(string alias = null, IncludeDirective include = null, SkipDirective skip = null) => WithScalarField("id", alias, [include, skip]);
	public SubscriptionQueryBuilder ExceptId() => ExceptField("id");
	public SubscriptionQueryBuilder WithSubscriber(LegalEntityQueryBuilder legalEntityQueryBuilder, string alias = null, IncludeDirective include = null, SkipDirective skip = null) => WithObjectField("subscriber", alias, legalEntityQueryBuilder, [include, skip]);
	public SubscriptionQueryBuilder ExceptSubscriber() => ExceptField("subscriber");
	public SubscriptionQueryBuilder WithValidFrom(string alias = null, IncludeDirective include = null, SkipDirective skip = null) => WithScalarField("validFrom", alias, [include, skip]);
	public SubscriptionQueryBuilder ExceptValidFrom() => ExceptField("validFrom");
	public SubscriptionQueryBuilder WithValidTo(string alias = null, IncludeDirective include = null, SkipDirective skip = null) => WithScalarField("validTo", alias, [include, skip]);
	public SubscriptionQueryBuilder ExceptValidTo() => ExceptField("validTo");
	public SubscriptionQueryBuilder WithStatus(string alias = null, IncludeDirective include = null, SkipDirective skip = null) => WithScalarField("status", alias, [include, skip]);
	public SubscriptionQueryBuilder ExceptStatus() => ExceptField("status");
	public SubscriptionQueryBuilder WithPriceInfo(PriceInfoQueryBuilder priceInfoQueryBuilder, string alias = null, IncludeDirective include = null, SkipDirective skip = null) => WithObjectField("priceInfo", alias, priceInfoQueryBuilder, [include, skip]);
	public SubscriptionQueryBuilder ExceptPriceInfo() => ExceptField("priceInfo");
	public SubscriptionQueryBuilder WithPriceRating(PriceRatingQueryBuilder priceRatingQueryBuilder, string alias = null, IncludeDirective include = null, SkipDirective skip = null) => WithObjectField("priceRating", alias, priceRatingQueryBuilder, [include, skip]);
	public SubscriptionQueryBuilder ExceptPriceRating() => ExceptField("priceRating");
}

public partial class ConsumptionEntryQueryBuilder : GraphQlQueryBuilder<ConsumptionEntryQueryBuilder>
{
	private static readonly GraphQlFieldMetadata[] AllFieldMetadata =
		  [
			  new GraphQlFieldMetadata {Name = "from"},
				 new GraphQlFieldMetadata {Name = "to"},
				 new GraphQlFieldMetadata {Name = "unitPrice"},
				 new GraphQlFieldMetadata {Name = "unitPriceVAT"},
				 new GraphQlFieldMetadata {Name = "consumption"},
				 new GraphQlFieldMetadata {Name = "consumptionUnit"},
				 new GraphQlFieldMetadata {Name = "cost"},
				 new GraphQlFieldMetadata {Name = "currency"}
		  ];

	protected override string TypeName { get { return "Consumption"; } }
	public override IReadOnlyList<GraphQlFieldMetadata> AllFields { get { return AllFieldMetadata; } }
	public ConsumptionEntryQueryBuilder WithFrom(string alias = null, IncludeDirective include = null, SkipDirective skip = null) => WithScalarField("from", alias, [include, skip]);
	public ConsumptionEntryQueryBuilder ExceptFrom() => ExceptField("from");
	public ConsumptionEntryQueryBuilder WithTo(string alias = null, IncludeDirective include = null, SkipDirective skip = null) => WithScalarField("to", alias, [include, skip]);
	public ConsumptionEntryQueryBuilder ExceptTo() => ExceptField("to");
	public ConsumptionEntryQueryBuilder WithUnitPrice(string alias = null, IncludeDirective include = null, SkipDirective skip = null) => WithScalarField("unitPrice", alias, [include, skip]);
	public ConsumptionEntryQueryBuilder ExceptUnitPrice() => ExceptField("unitPrice");
	public ConsumptionEntryQueryBuilder WithUnitPriceVat(string alias = null, IncludeDirective include = null, SkipDirective skip = null) => WithScalarField("unitPriceVAT", alias, [include, skip]);
	public ConsumptionEntryQueryBuilder ExceptUnitPriceVat() => ExceptField("unitPriceVAT");
	public ConsumptionEntryQueryBuilder WithConsumption(string alias = null, IncludeDirective include = null, SkipDirective skip = null) => WithScalarField("consumption", alias, [include, skip]);
	public ConsumptionEntryQueryBuilder ExceptConsumption() => ExceptField("consumption");
	public ConsumptionEntryQueryBuilder WithConsumptionUnit(string alias = null, IncludeDirective include = null, SkipDirective skip = null) => WithScalarField("consumptionUnit", alias, [include, skip]);
	public ConsumptionEntryQueryBuilder ExceptConsumptionUnit() => ExceptField("consumptionUnit");
	public ConsumptionEntryQueryBuilder WithCost(string alias = null, IncludeDirective include = null, SkipDirective skip = null) => WithScalarField("cost", alias, [include, skip]);
	public ConsumptionEntryQueryBuilder ExceptCost() => ExceptField("cost");
	public ConsumptionEntryQueryBuilder WithCurrency(string alias = null, IncludeDirective include = null, SkipDirective skip = null) => WithScalarField("currency", alias, [include, skip]);
	public ConsumptionEntryQueryBuilder ExceptCurrency() => ExceptField("currency");
}

public partial class ProductionEntryQueryBuilder : GraphQlQueryBuilder<ProductionEntryQueryBuilder>
{
	private static readonly GraphQlFieldMetadata[] AllFieldMetadata =
		  [
			  new GraphQlFieldMetadata {Name = "from"},
				 new GraphQlFieldMetadata {Name = "to"},
				 new GraphQlFieldMetadata {Name = "unitPrice"},
				 new GraphQlFieldMetadata {Name = "unitPriceVAT"},
				 new GraphQlFieldMetadata {Name = "production"},
				 new GraphQlFieldMetadata {Name = "productionUnit"},
				 new GraphQlFieldMetadata {Name = "profit"},
				 new GraphQlFieldMetadata {Name = "currency"}
		  ];

	protected override string TypeName { get { return "Production"; } }
	public override IReadOnlyList<GraphQlFieldMetadata> AllFields { get { return AllFieldMetadata; } }
	public ProductionEntryQueryBuilder WithFrom(string alias = null, IncludeDirective include = null, SkipDirective skip = null) => WithScalarField("from", alias, [include, skip]);
	public ProductionEntryQueryBuilder ExceptFrom() => ExceptField("from");
	public ProductionEntryQueryBuilder WithTo(string alias = null, IncludeDirective include = null, SkipDirective skip = null) => WithScalarField("to", alias, [include, skip]);
	public ProductionEntryQueryBuilder ExceptTo() => ExceptField("to");
	public ProductionEntryQueryBuilder WithUnitPrice(string alias = null, IncludeDirective include = null, SkipDirective skip = null) => WithScalarField("unitPrice", alias, [include, skip]);
	public ProductionEntryQueryBuilder ExceptUnitPrice() => ExceptField("unitPrice");
	public ProductionEntryQueryBuilder WithUnitPriceVat(string alias = null, IncludeDirective include = null, SkipDirective skip = null) => WithScalarField("unitPriceVAT", alias, [include, skip]);
	public ProductionEntryQueryBuilder ExceptUnitPriceVat() => ExceptField("unitPriceVAT");
	public ProductionEntryQueryBuilder WithProduction(string alias = null, IncludeDirective include = null, SkipDirective skip = null) => WithScalarField("production", alias, [include, skip]);
	public ProductionEntryQueryBuilder ExceptProduction() => ExceptField("production");
	public ProductionEntryQueryBuilder WithProductionUnit(string alias = null, IncludeDirective include = null, SkipDirective skip = null) => WithScalarField("productionUnit", alias, [include, skip]);
	public ProductionEntryQueryBuilder ExceptProductionUnit() => ExceptField("productionUnit");
	public ProductionEntryQueryBuilder WithProfit(string alias = null, IncludeDirective include = null, SkipDirective skip = null) => WithScalarField("profit", alias, [include, skip]);
	public ProductionEntryQueryBuilder ExceptProfit() => ExceptField("profit");
	public ProductionEntryQueryBuilder WithCurrency(string alias = null, IncludeDirective include = null, SkipDirective skip = null) => WithScalarField("currency", alias, [include, skip]);
	public ProductionEntryQueryBuilder ExceptCurrency() => ExceptField("currency");
}

public partial class HomeConsumptionEdgeQueryBuilder : GraphQlQueryBuilder<HomeConsumptionEdgeQueryBuilder>
{
	private static readonly GraphQlFieldMetadata[] AllFieldMetadata =
		  [
			  new GraphQlFieldMetadata {Name = "cursor"},
				 new GraphQlFieldMetadata {Name = "node", IsComplex = true, QueryBuilderType = typeof(ConsumptionEntryQueryBuilder) }
		  ];

	protected override string TypeName { get { return "HomeConsumptionEdge"; } }
	public override IReadOnlyList<GraphQlFieldMetadata> AllFields { get { return AllFieldMetadata; } }
	public HomeConsumptionEdgeQueryBuilder WithCursor(string alias = null, IncludeDirective include = null, SkipDirective skip = null) => WithScalarField("cursor", alias, [include, skip]);
	public HomeConsumptionEdgeQueryBuilder ExceptCursor() => ExceptField("cursor");
	public HomeConsumptionEdgeQueryBuilder WithNode(ConsumptionEntryQueryBuilder consumptionEntryQueryBuilder, string alias = null, IncludeDirective include = null, SkipDirective skip = null) => WithObjectField("node", alias, consumptionEntryQueryBuilder, [include, skip]);

	public HomeConsumptionEdgeQueryBuilder ExceptNode() => ExceptField("node");
}

public partial class HomeProductionEdgeQueryBuilder : GraphQlQueryBuilder<HomeProductionEdgeQueryBuilder>
{
	private static readonly GraphQlFieldMetadata[] AllFieldMetadata =
		  [
			  new GraphQlFieldMetadata {Name = "cursor"},
				 new GraphQlFieldMetadata {Name = "node", IsComplex = true, QueryBuilderType = typeof(ProductionEntryQueryBuilder) }
		  ];

	protected override string TypeName { get { return "HomeProductionEdge"; } }
	public override IReadOnlyList<GraphQlFieldMetadata> AllFields { get { return AllFieldMetadata; } }
	public HomeProductionEdgeQueryBuilder WithCursor(string alias = null, IncludeDirective include = null, SkipDirective skip = null) => WithScalarField("cursor", alias, [include, skip]);
	public HomeProductionEdgeQueryBuilder ExceptCursor() => ExceptField("cursor");
	public HomeProductionEdgeQueryBuilder WithNode(ProductionEntryQueryBuilder productionEntryQueryBuilder, string alias = null, IncludeDirective include = null, SkipDirective skip = null) => WithObjectField("node", alias, productionEntryQueryBuilder, [include, skip]);
	public HomeProductionEdgeQueryBuilder ExceptNode() => ExceptField("node");
}

public partial class HomeConsumptionConnectionQueryBuilder : GraphQlQueryBuilder<HomeConsumptionConnectionQueryBuilder>
{
	private static readonly GraphQlFieldMetadata[] AllFieldMetadata =
		  [
			  new GraphQlFieldMetadata {Name = "pageInfo", IsComplex = true, QueryBuilderType = typeof(HomeConsumptionPageInfoQueryBuilder) },
				 new GraphQlFieldMetadata {Name = "nodes", IsComplex = true, QueryBuilderType = typeof(ConsumptionEntryQueryBuilder) },
				 new GraphQlFieldMetadata {Name = "edges", IsComplex = true, QueryBuilderType = typeof(HomeConsumptionEdgeQueryBuilder) }
		  ];

	protected override string TypeName { get { return "HomeConsumptionConnection"; } }
	public override IReadOnlyList<GraphQlFieldMetadata> AllFields { get { return AllFieldMetadata; } }
	public HomeConsumptionConnectionQueryBuilder WithPageInfo(HomeConsumptionPageInfoQueryBuilder homeConsumptionPageInfoQueryBuilder, string alias = null, IncludeDirective include = null, SkipDirective skip = null) => WithObjectField("pageInfo", alias, homeConsumptionPageInfoQueryBuilder, [include, skip]);
	public HomeConsumptionConnectionQueryBuilder ExceptPageInfo() => ExceptField("pageInfo");
	public HomeConsumptionConnectionQueryBuilder WithNodes(ConsumptionEntryQueryBuilder consumptionEntryQueryBuilder, string alias = null, IncludeDirective include = null, SkipDirective skip = null) => WithObjectField("nodes", alias, consumptionEntryQueryBuilder, [include, skip]);
	public HomeConsumptionConnectionQueryBuilder ExceptNodes() => ExceptField("nodes");
	public HomeConsumptionConnectionQueryBuilder WithEdges(HomeConsumptionEdgeQueryBuilder homeConsumptionEdgeQueryBuilder, string alias = null, IncludeDirective include = null, SkipDirective skip = null) => WithObjectField("edges", alias, homeConsumptionEdgeQueryBuilder, [include, skip]);
	public HomeConsumptionConnectionQueryBuilder ExceptEdges() => ExceptField("edges");
}

public partial class HomeProductionConnectionQueryBuilder : GraphQlQueryBuilder<HomeProductionConnectionQueryBuilder>
{
	private static readonly GraphQlFieldMetadata[] AllFieldMetadata =
		  [
			  new GraphQlFieldMetadata {Name = "pageInfo", IsComplex = true, QueryBuilderType = typeof(HomeProductionPageInfoQueryBuilder) },
				 new GraphQlFieldMetadata {Name = "nodes", IsComplex = true, QueryBuilderType = typeof(ProductionEntryQueryBuilder) },
				 new GraphQlFieldMetadata {Name = "edges", IsComplex = true, QueryBuilderType = typeof(HomeProductionEdgeQueryBuilder) }
		  ];

	protected override string TypeName { get { return "HomeProductionConnection"; } }

	public override IReadOnlyList<GraphQlFieldMetadata> AllFields { get { return AllFieldMetadata; } }

	public HomeProductionConnectionQueryBuilder WithPageInfo(HomeProductionPageInfoQueryBuilder homeProductionPageInfoQueryBuilder, string alias = null, IncludeDirective include = null, SkipDirective skip = null) => WithObjectField("pageInfo", alias, homeProductionPageInfoQueryBuilder, [include, skip]);

	public HomeProductionConnectionQueryBuilder ExceptPageInfo() => ExceptField("pageInfo");

	public HomeProductionConnectionQueryBuilder WithNodes(ProductionEntryQueryBuilder productionEntryQueryBuilder, string alias = null, IncludeDirective include = null, SkipDirective skip = null) => WithObjectField("nodes", alias, productionEntryQueryBuilder, [include, skip]);

	public HomeProductionConnectionQueryBuilder ExceptNodes() => ExceptField("nodes");

	public HomeProductionConnectionQueryBuilder WithEdges(HomeProductionEdgeQueryBuilder homeProductionEdgeQueryBuilder, string alias = null, IncludeDirective include = null, SkipDirective skip = null) => WithObjectField("edges", alias, homeProductionEdgeQueryBuilder, [include, skip]);

	public HomeProductionConnectionQueryBuilder ExceptEdges() => ExceptField("edges");
}

public partial class MeteringPointDataQueryBuilder : GraphQlQueryBuilder<MeteringPointDataQueryBuilder>
{
	private static readonly GraphQlFieldMetadata[] AllFieldMetadata =
		  [
				new GraphQlFieldMetadata {Name = "consumptionEan"},
				new GraphQlFieldMetadata {Name = "gridCompany"},
				new GraphQlFieldMetadata {Name = "gridAreaCode"},
				new GraphQlFieldMetadata {Name = "priceAreaCode"},
				new GraphQlFieldMetadata {Name = "productionEan"},
				new GraphQlFieldMetadata {Name = "energyTaxType"},
				new GraphQlFieldMetadata {Name = "vatType"},
				new GraphQlFieldMetadata {Name = "estimatedAnnualConsumption"}
		  ];

	protected override string TypeName { get { return "MeteringPointData"; } }
	public override IReadOnlyList<GraphQlFieldMetadata> AllFields { get { return AllFieldMetadata; } }
	public MeteringPointDataQueryBuilder WithConsumptionEan(string alias = null, IncludeDirective include = null, SkipDirective skip = null) => WithScalarField("consumptionEan", alias, [include, skip]);
	public MeteringPointDataQueryBuilder ExceptConsumptionEan() => ExceptField("consumptionEan");
	public MeteringPointDataQueryBuilder WithGridCompany(string alias = null, IncludeDirective include = null, SkipDirective skip = null) => WithScalarField("gridCompany", alias, [include, skip]);
	public MeteringPointDataQueryBuilder ExceptGridCompany() => ExceptField("gridCompany");
	public MeteringPointDataQueryBuilder WithGridAreaCode(string alias = null, IncludeDirective include = null, SkipDirective skip = null) => WithScalarField("gridAreaCode", alias, [include, skip]);
	public MeteringPointDataQueryBuilder ExceptGridAreaCode() => ExceptField("gridAreaCode");
	public MeteringPointDataQueryBuilder WithPriceAreaCode(string alias = null, IncludeDirective include = null, SkipDirective skip = null) => WithScalarField("priceAreaCode", alias, [include, skip]);
	public MeteringPointDataQueryBuilder ExceptPriceAreaCode() => ExceptField("priceAreaCode");
	public MeteringPointDataQueryBuilder WithProductionEan(string alias = null, IncludeDirective include = null, SkipDirective skip = null) => WithScalarField("productionEan", alias, [include, skip]);
	public MeteringPointDataQueryBuilder ExceptProductionEan() => ExceptField("productionEan");
	public MeteringPointDataQueryBuilder WithEnergyTaxType(string alias = null, IncludeDirective include = null, SkipDirective skip = null) => WithScalarField("energyTaxType", alias, [include, skip]);
	public MeteringPointDataQueryBuilder ExceptEnergyTaxType() => ExceptField("energyTaxType");
	public MeteringPointDataQueryBuilder WithVatType(string alias = null, IncludeDirective include = null, SkipDirective skip = null) => WithScalarField("vatType", alias, [include, skip]);
	public MeteringPointDataQueryBuilder ExceptVatType() => ExceptField("vatType");
	public MeteringPointDataQueryBuilder WithEstimatedAnnualConsumption(string alias = null, IncludeDirective include = null, SkipDirective skip = null) => WithScalarField("estimatedAnnualConsumption", alias, [include, skip]);
	public MeteringPointDataQueryBuilder ExceptEstimatedAnnualConsumption() => ExceptField("estimatedAnnualConsumption");
}

public partial class HomeFeaturesQueryBuilder : GraphQlQueryBuilder<HomeFeaturesQueryBuilder>
{
	private static readonly GraphQlFieldMetadata[] AllFieldMetadata = [ new GraphQlFieldMetadata { Name = "realTimeConsumptionEnabled" } ];

	protected override string TypeName { get { return "HomeFeatures"; } }

	public override IReadOnlyList<GraphQlFieldMetadata> AllFields { get { return AllFieldMetadata; } }

	public HomeFeaturesQueryBuilder WithRealTimeConsumptionEnabled(string alias = null, IncludeDirective include = null, SkipDirective skip = null) => WithScalarField("realTimeConsumptionEnabled", alias, [include, skip]);

	public HomeFeaturesQueryBuilder ExceptRealTimeConsumptionEnabled() => ExceptField("realTimeConsumptionEnabled");
}

public partial class HomeQueryBuilder : GraphQlQueryBuilder<HomeQueryBuilder>
{
	private static readonly GraphQlFieldMetadata[] AllFieldMetadata =
		  [
				new GraphQlFieldMetadata {Name = "id"},
				new GraphQlFieldMetadata {Name = "timeZone"},
				new GraphQlFieldMetadata {Name = "appNickname"},
				new GraphQlFieldMetadata {Name = "appAvatar"},
				new GraphQlFieldMetadata {Name = "size"},
				new GraphQlFieldMetadata {Name = "type"},
				new GraphQlFieldMetadata {Name = "numberOfResidents"},
				new GraphQlFieldMetadata {Name = "primaryHeatingSource"},
				new GraphQlFieldMetadata {Name = "hasVentilationSystem"},
				new GraphQlFieldMetadata {Name = "mainFuseSize"},
				new GraphQlFieldMetadata {Name = "address", IsComplex = true, QueryBuilderType = typeof(AddressQueryBuilder) },
				new GraphQlFieldMetadata {Name = "owner", IsComplex = true, QueryBuilderType = typeof(LegalEntityQueryBuilder) },
				new GraphQlFieldMetadata {Name = "meteringPointData", IsComplex = true, QueryBuilderType = typeof(MeteringPointDataQueryBuilder) },
				new GraphQlFieldMetadata {Name = "currentSubscription", IsComplex = true, QueryBuilderType = typeof(SubscriptionQueryBuilder) },
				new GraphQlFieldMetadata {Name = "subscriptions", IsComplex = true, QueryBuilderType = typeof(SubscriptionQueryBuilder) },
				new GraphQlFieldMetadata {Name = "consumption", IsComplex = true, QueryBuilderType = typeof(HomeConsumptionConnectionQueryBuilder) },
				new GraphQlFieldMetadata {Name = "production", IsComplex = true, QueryBuilderType = typeof(HomeProductionConnectionQueryBuilder) },
				new GraphQlFieldMetadata {Name = "features", IsComplex = true, QueryBuilderType = typeof(HomeFeaturesQueryBuilder) }
		  ];

	protected override string TypeName { get { return "Home"; } }
	public override IReadOnlyList<GraphQlFieldMetadata> AllFields { get { return AllFieldMetadata; } }
	public HomeQueryBuilder WithId(string alias = null, IncludeDirective include = null, SkipDirective skip = null) => WithScalarField("id", alias, [include, skip]);
	public HomeQueryBuilder ExceptId() => ExceptField("id");
	public HomeQueryBuilder WithTimeZone(string alias = null, IncludeDirective include = null, SkipDirective skip = null) => WithScalarField("timeZone", alias, [include, skip]);
	public HomeQueryBuilder ExceptTimeZone() => ExceptField("timeZone");
	public HomeQueryBuilder WithAppNickname(string alias = null, IncludeDirective include = null, SkipDirective skip = null) => WithScalarField("appNickname", alias, [include, skip]);
	public HomeQueryBuilder ExceptAppNickname() => ExceptField("appNickname");
	public HomeQueryBuilder WithAppAvatar(string alias = null, IncludeDirective include = null, SkipDirective skip = null) => WithScalarField("appAvatar", alias, [include, skip]);
	public HomeQueryBuilder ExceptAppAvatar() => ExceptField("appAvatar");
	public HomeQueryBuilder WithSize(string alias = null, IncludeDirective include = null, SkipDirective skip = null) => WithScalarField("size", alias, [include, skip]);
	public HomeQueryBuilder ExceptSize() => ExceptField("size");
	public HomeQueryBuilder WithType(string alias = null, IncludeDirective include = null, SkipDirective skip = null) => WithScalarField("type", alias, [include, skip]);
	public HomeQueryBuilder ExceptType() => ExceptField("type");
	public HomeQueryBuilder WithNumberOfResidents(string alias = null, IncludeDirective include = null, SkipDirective skip = null) => WithScalarField("numberOfResidents", alias, [include, skip]);
	public HomeQueryBuilder ExceptNumberOfResidents() => ExceptField("numberOfResidents");
	public HomeQueryBuilder WithPrimaryHeatingSource(string alias = null, IncludeDirective include = null, SkipDirective skip = null) => WithScalarField("primaryHeatingSource", alias, [include, skip]);
	public HomeQueryBuilder ExceptPrimaryHeatingSource() => ExceptField("primaryHeatingSource");
	public HomeQueryBuilder WithHasVentilationSystem(string alias = null, IncludeDirective include = null, SkipDirective skip = null) => WithScalarField("hasVentilationSystem", alias, [include, skip]);
	public HomeQueryBuilder ExceptHasVentilationSystem() => ExceptField("hasVentilationSystem");
	public HomeQueryBuilder WithMainFuseSize(string alias = null, IncludeDirective include = null, SkipDirective skip = null) => WithScalarField("mainFuseSize", alias, [include, skip]);
	public HomeQueryBuilder ExceptMainFuseSize() => ExceptField("mainFuseSize");
	public HomeQueryBuilder WithAddress(AddressQueryBuilder addressQueryBuilder, string alias = null, IncludeDirective include = null, SkipDirective skip = null) => WithObjectField("address", alias, addressQueryBuilder, [include, skip]);
	public HomeQueryBuilder ExceptAddress() => ExceptField("address");
	public HomeQueryBuilder WithOwner(LegalEntityQueryBuilder legalEntityQueryBuilder, string alias = null, IncludeDirective include = null, SkipDirective skip = null) => WithObjectField("owner", alias, legalEntityQueryBuilder, [include, skip]);
	public HomeQueryBuilder ExceptOwner() => ExceptField("owner");
	public HomeQueryBuilder WithMeteringPointData(MeteringPointDataQueryBuilder meteringPointDataQueryBuilder, string alias = null, IncludeDirective include = null, SkipDirective skip = null) => WithObjectField("meteringPointData", alias, meteringPointDataQueryBuilder, [include, skip]);
	public HomeQueryBuilder ExceptMeteringPointData() => ExceptField("meteringPointData");
	public HomeQueryBuilder WithCurrentSubscription(SubscriptionQueryBuilder subscriptionQueryBuilder, string alias = null, IncludeDirective include = null, SkipDirective skip = null) => WithObjectField("currentSubscription", alias, subscriptionQueryBuilder, [include, skip]);
	public HomeQueryBuilder ExceptCurrentSubscription() => ExceptField("currentSubscription");
	public HomeQueryBuilder WithSubscriptions(SubscriptionQueryBuilder subscriptionQueryBuilder, string alias = null, IncludeDirective include = null, SkipDirective skip = null) => WithObjectField("subscriptions", alias, subscriptionQueryBuilder, [include, skip]);
	public HomeQueryBuilder ExceptSubscriptions() => ExceptField("subscriptions");

	public HomeQueryBuilder WithConsumption(HomeConsumptionConnectionQueryBuilder homeConsumptionConnectionQueryBuilder, QueryBuilderParameter<EnergyResolution> resolution, QueryBuilderParameter<int?> first = null, QueryBuilderParameter<int?> last = null, QueryBuilderParameter<string> before = null, QueryBuilderParameter<string> after = null, QueryBuilderParameter<bool?> filterEmptyNodes = null, string alias = null, IncludeDirective include = null, SkipDirective skip = null)
	{
		var args = new List<QueryBuilderArgumentInfo>();
		args.Add(new QueryBuilderArgumentInfo { ArgumentName = "resolution", ArgumentValue = resolution });

		if (first != null) args.Add(new QueryBuilderArgumentInfo { ArgumentName = "first", ArgumentValue = first });
		if (last != null) args.Add(new QueryBuilderArgumentInfo { ArgumentName = "last", ArgumentValue = last });
		if (before != null) args.Add(new QueryBuilderArgumentInfo { ArgumentName = "before", ArgumentValue = before });
		if (after != null) args.Add(new QueryBuilderArgumentInfo { ArgumentName = "after", ArgumentValue = after });
		if (filterEmptyNodes != null) args.Add(new QueryBuilderArgumentInfo { ArgumentName = "filterEmptyNodes", ArgumentValue = filterEmptyNodes });

		return WithObjectField("consumption", alias, homeConsumptionConnectionQueryBuilder, [include, skip], args);
	}

	public HomeQueryBuilder ExceptConsumption() => ExceptField("consumption");

	public HomeQueryBuilder WithProduction(HomeProductionConnectionQueryBuilder homeProductionConnectionQueryBuilder, QueryBuilderParameter<EnergyResolution> resolution, QueryBuilderParameter<int?> first = null, QueryBuilderParameter<int?> last = null, QueryBuilderParameter<string> before = null, QueryBuilderParameter<string> after = null, QueryBuilderParameter<bool?> filterEmptyNodes = null, string alias = null, IncludeDirective include = null, SkipDirective skip = null)
	{
		var args = new List<QueryBuilderArgumentInfo>();
		args.Add(new QueryBuilderArgumentInfo { ArgumentName = "resolution", ArgumentValue = resolution });

		if (first != null) args.Add(new QueryBuilderArgumentInfo { ArgumentName = "first", ArgumentValue = first });
		if (last != null) args.Add(new QueryBuilderArgumentInfo { ArgumentName = "last", ArgumentValue = last });
		if (before != null) args.Add(new QueryBuilderArgumentInfo { ArgumentName = "before", ArgumentValue = before });
		if (after != null) args.Add(new QueryBuilderArgumentInfo { ArgumentName = "after", ArgumentValue = after });
		if (filterEmptyNodes != null) args.Add(new QueryBuilderArgumentInfo { ArgumentName = "filterEmptyNodes", ArgumentValue = filterEmptyNodes });

		return WithObjectField("production", alias, homeProductionConnectionQueryBuilder, [include, skip], args);
	}

	public HomeQueryBuilder ExceptProduction() => ExceptField("production");
	public HomeQueryBuilder WithFeatures(HomeFeaturesQueryBuilder homeFeaturesQueryBuilder, string alias = null, IncludeDirective include = null, SkipDirective skip = null) => WithObjectField("features", alias, homeFeaturesQueryBuilder, [include, skip]);
	public HomeQueryBuilder ExceptFeatures() => ExceptField("features");
}

public partial class ViewerQueryBuilder : GraphQlQueryBuilder<ViewerQueryBuilder>
{
	private static readonly GraphQlFieldMetadata[] AllFieldMetadata =
		  [
			  new GraphQlFieldMetadata {Name = "login"},
				 new GraphQlFieldMetadata {Name = "userId"},
				 new GraphQlFieldMetadata {Name = "name"},
				 new GraphQlFieldMetadata {Name = "accountType", IsComplex = true },
				 new GraphQlFieldMetadata {Name = "homes", IsComplex = true, QueryBuilderType = typeof(HomeQueryBuilder) },
				 new GraphQlFieldMetadata {Name = "home", IsComplex = true, QueryBuilderType = typeof(HomeQueryBuilder) },
				 new GraphQlFieldMetadata {Name = "websocketSubscriptionUrl" }
		  ];

	protected override string TypeName { get { return "Viewer"; } }
	public override IReadOnlyList<GraphQlFieldMetadata> AllFields { get { return AllFieldMetadata; } }
	public ViewerQueryBuilder WithLogin(string alias = null, IncludeDirective include = null, SkipDirective skip = null) => WithScalarField("login", alias, [include, skip]);
	public ViewerQueryBuilder ExceptLogin() => ExceptField("login");
	public ViewerQueryBuilder WithUserId(string alias = null, IncludeDirective include = null, SkipDirective skip = null) => WithScalarField("userId", alias, [include, skip]);
	public ViewerQueryBuilder ExceptUserId() => ExceptField("userId");
	public ViewerQueryBuilder WithName(string alias = null, IncludeDirective include = null, SkipDirective skip = null) => WithScalarField("name", alias, [include, skip]);
	public ViewerQueryBuilder ExceptName() => ExceptField("name");
	public ViewerQueryBuilder WithAccountType(string alias = null, IncludeDirective include = null, SkipDirective skip = null) => WithScalarField("accountType", alias, [include, skip]);
	public ViewerQueryBuilder ExceptAccountType() => ExceptField("accountType");
	public ViewerQueryBuilder WithHomes(HomeQueryBuilder homeQueryBuilder, string alias = null, IncludeDirective include = null, SkipDirective skip = null) => WithObjectField("homes", alias, homeQueryBuilder, [include, skip]);
	public ViewerQueryBuilder ExceptHomes() => ExceptField("homes");

	public ViewerQueryBuilder WithHome(HomeQueryBuilder homeQueryBuilder, QueryBuilderParameter<Guid> id, string alias = null, IncludeDirective include = null, SkipDirective skip = null)
	{
		var args = new List<QueryBuilderArgumentInfo>();
		args.Add(new QueryBuilderArgumentInfo { ArgumentName = "id", ArgumentValue = id });

		return WithObjectField("home", alias, homeQueryBuilder, [include, skip], args);
	}

	public ViewerQueryBuilder ExceptHome() => ExceptField("home");
	public ViewerQueryBuilder WithWebsocketSubscriptionUrl(string alias = null, IncludeDirective include = null, SkipDirective skip = null) => WithScalarField("websocketSubscriptionUrl", alias, [include, skip]);
	public ViewerQueryBuilder ExceptWebsocketSubscriptionUrl() => ExceptField("websocketSubscriptionUrl");
}

public partial class TibberQueryBuilder(string operationName = null) : GraphQlQueryBuilder<TibberQueryBuilder>("query", operationName)
{
	private static readonly GraphQlFieldMetadata[] AllFieldMetadata =
		  [
			  new GraphQlFieldMetadata { Name = "viewer", IsComplex = true, QueryBuilderType = typeof(ViewerQueryBuilder) }
		  ];

	protected override string TypeName { get { return "Query"; } }
	public override IReadOnlyList<GraphQlFieldMetadata> AllFields { get { return AllFieldMetadata; } }
	public TibberQueryBuilder WithParameter<T>(GraphQlQueryParameter<T> parameter) => WithParameterInternal(parameter);
	public TibberQueryBuilder WithViewer(ViewerQueryBuilder viewerQueryBuilder, string alias = null, IncludeDirective include = null, SkipDirective skip = null) => WithObjectField("viewer", alias, viewerQueryBuilder, [include, skip]);
	public TibberQueryBuilder ExceptViewer() => ExceptField("viewer");
}

public partial class MeterReadingResponseQueryBuilder : GraphQlQueryBuilder<MeterReadingResponseQueryBuilder>
{
	private static readonly GraphQlFieldMetadata[] AllFieldMetadata =
		  [
			  new GraphQlFieldMetadata {Name = "homeId"},
				 new GraphQlFieldMetadata {Name = "time"},
				 new GraphQlFieldMetadata {Name = "reading"}
		  ];

	protected override string TypeName { get { return "MeterReadingResponse"; } }
	public override IReadOnlyList<GraphQlFieldMetadata> AllFields { get { return AllFieldMetadata; } }
	public MeterReadingResponseQueryBuilder WithHomeId(string alias = null, IncludeDirective include = null, SkipDirective skip = null) => WithScalarField("homeId", alias, [include, skip]);
	public MeterReadingResponseQueryBuilder ExceptHomeId() => ExceptField("homeId");
	public MeterReadingResponseQueryBuilder WithTime(string alias = null, IncludeDirective include = null, SkipDirective skip = null) => WithScalarField("time", alias, [include, skip]);
	public MeterReadingResponseQueryBuilder ExceptTime() => ExceptField("time");
	public MeterReadingResponseQueryBuilder WithReading(string alias = null, IncludeDirective include = null, SkipDirective skip = null) => WithScalarField("reading", alias, [include, skip]);
	public MeterReadingResponseQueryBuilder ExceptReading() => ExceptField("reading");
}

public partial class LiveMeasurementQueryBuilder : GraphQlQueryBuilder<LiveMeasurementQueryBuilder>
{
	private static readonly GraphQlFieldMetadata[] AllFieldMetadata =
		  [
				new GraphQlFieldMetadata {Name = "timestamp"},
				new GraphQlFieldMetadata {Name = "power"},
				new GraphQlFieldMetadata {Name = "lastMeterConsumption"},
				new GraphQlFieldMetadata {Name = "accumulatedConsumption"},
				new GraphQlFieldMetadata {Name = "accumulatedProduction"},
				new GraphQlFieldMetadata {Name = "accumulatedConsumptionLastHour"},
				new GraphQlFieldMetadata {Name = "accumulatedProductionLastHour"},
				new GraphQlFieldMetadata {Name = "accumulatedCost"},
				new GraphQlFieldMetadata {Name = "accumulatedReward"},
				new GraphQlFieldMetadata {Name = "currency"},
				new GraphQlFieldMetadata {Name = "minPower"},
				new GraphQlFieldMetadata {Name = "averagePower" },
				new GraphQlFieldMetadata {Name = "maxPower"},
				new GraphQlFieldMetadata {Name = "powerProduction"},
				new GraphQlFieldMetadata {Name = "powerReactive"},
				new GraphQlFieldMetadata {Name = "powerProductionReactive"},
				new GraphQlFieldMetadata {Name = "minPowerProduction"},
				new GraphQlFieldMetadata {Name = "maxPowerProduction"},
				new GraphQlFieldMetadata {Name = "lastMeterProduction"},
				new GraphQlFieldMetadata {Name = "powerFactor"},
				new GraphQlFieldMetadata {Name = "voltagePhase1"},
				new GraphQlFieldMetadata {Name = "voltagePhase2"},
				new GraphQlFieldMetadata {Name = "voltagePhase3"},
				new GraphQlFieldMetadata {Name = "currentL1"},
				new GraphQlFieldMetadata {Name = "currentL2"},
				new GraphQlFieldMetadata {Name = "currentL3"},
				new GraphQlFieldMetadata {Name = "signalStrength"}
		  ];

	protected override string TypeName { get { return "LiveMeasurement"; } }
	public override IReadOnlyList<GraphQlFieldMetadata> AllFields { get { return AllFieldMetadata; } }
	public LiveMeasurementQueryBuilder WithTimestamp(string alias = null, IncludeDirective include = null, SkipDirective skip = null) => WithScalarField("timestamp", alias, [include, skip]);
	public LiveMeasurementQueryBuilder ExceptTimestamp() => ExceptField("timestamp");
	public LiveMeasurementQueryBuilder WithPower(string alias = null, IncludeDirective include = null, SkipDirective skip = null) => WithScalarField("power", alias, [include, skip]);
	public LiveMeasurementQueryBuilder ExceptPower() => ExceptField("power");
	public LiveMeasurementQueryBuilder WithLastMeterConsumption(string alias = null, IncludeDirective include = null, SkipDirective skip = null) => WithScalarField("lastMeterConsumption", alias, [include, skip]);
	public LiveMeasurementQueryBuilder ExceptLastMeterConsumption() => ExceptField("lastMeterConsumption");
	public LiveMeasurementQueryBuilder WithAccumulatedConsumption(string alias = null, IncludeDirective include = null, SkipDirective skip = null) => WithScalarField("accumulatedConsumption", alias, [include, skip]);
	public LiveMeasurementQueryBuilder ExceptAccumulatedConsumption() => ExceptField("accumulatedConsumption");
	public LiveMeasurementQueryBuilder WithAccumulatedProduction(string alias = null, IncludeDirective include = null, SkipDirective skip = null) => WithScalarField("accumulatedProduction", alias, [include, skip]);
	public LiveMeasurementQueryBuilder ExceptAccumulatedProduction() => ExceptField("accumulatedProduction");
	public LiveMeasurementQueryBuilder WithAccumulatedConsumptionLastHour(string alias = null, IncludeDirective include = null, SkipDirective skip = null) => WithScalarField("accumulatedConsumptionLastHour", alias, [include, skip]);
	public LiveMeasurementQueryBuilder ExceptAccumulatedConsumptionLastHour() => ExceptField("accumulatedConsumptionLastHour");
	public LiveMeasurementQueryBuilder WithAccumulatedProductionLastHour(string alias = null, IncludeDirective include = null, SkipDirective skip = null) => WithScalarField("accumulatedProductionLastHour", alias, [include, skip]);
	public LiveMeasurementQueryBuilder ExceptAccumulatedProductionLastHour() => ExceptField("accumulatedProductionLastHour");
	public LiveMeasurementQueryBuilder WithAccumulatedCost(string alias = null, IncludeDirective include = null, SkipDirective skip = null) => WithScalarField("accumulatedCost", alias, [include, skip]);
	public LiveMeasurementQueryBuilder ExceptAccumulatedCost() => ExceptField("accumulatedCost");
	public LiveMeasurementQueryBuilder WithAccumulatedReward(string alias = null, IncludeDirective include = null, SkipDirective skip = null) => WithScalarField("accumulatedReward", alias, [include, skip]);
	public LiveMeasurementQueryBuilder ExceptAccumulatedReward() => ExceptField("accumulatedReward");
	public LiveMeasurementQueryBuilder WithCurrency(string alias = null, IncludeDirective include = null, SkipDirective skip = null) => WithScalarField("currency", alias, [include, skip]);
	public LiveMeasurementQueryBuilder ExceptCurrency() => ExceptField("currency");
	public LiveMeasurementQueryBuilder WithMinPower(string alias = null, IncludeDirective include = null, SkipDirective skip = null) => WithScalarField("minPower", alias, [include, skip]);
	public LiveMeasurementQueryBuilder ExceptMinPower() => ExceptField("minPower");
	public LiveMeasurementQueryBuilder WithAveragePower(string alias = null, IncludeDirective include = null, SkipDirective skip = null) => WithScalarField("averagePower", alias, [include, skip]);
	public LiveMeasurementQueryBuilder ExceptAveragePower() => ExceptField("averagePower");
	public LiveMeasurementQueryBuilder WithMaxPower(string alias = null, IncludeDirective include = null, SkipDirective skip = null) => WithScalarField("maxPower", alias, [include, skip]);
	public LiveMeasurementQueryBuilder ExceptMaxPower() => ExceptField("maxPower");
	public LiveMeasurementQueryBuilder WithPowerProduction(string alias = null, IncludeDirective include = null, SkipDirective skip = null) => WithScalarField("powerProduction", alias, [include, skip]);
	public LiveMeasurementQueryBuilder ExceptPowerProduction() => ExceptField("powerProduction");
	public LiveMeasurementQueryBuilder WithPowerReactive(string alias = null, IncludeDirective include = null, SkipDirective skip = null) => WithScalarField("powerReactive", alias, [include, skip]);
	public LiveMeasurementQueryBuilder ExceptPowerReactive() => ExceptField("powerReactive");
	public LiveMeasurementQueryBuilder WithPowerProductionReactive(string alias = null, IncludeDirective include = null, SkipDirective skip = null) => WithScalarField("powerProductionReactive", alias, [include, skip]);
	public LiveMeasurementQueryBuilder ExceptPowerProductionReactive() => ExceptField("powerProductionReactive");
	public LiveMeasurementQueryBuilder WithMinPowerProduction(string alias = null, IncludeDirective include = null, SkipDirective skip = null) => WithScalarField("minPowerProduction", alias, [include, skip]);
	public LiveMeasurementQueryBuilder ExceptMinPowerProduction() => ExceptField("minPowerProduction");
	public LiveMeasurementQueryBuilder WithMaxPowerProduction(string alias = null, IncludeDirective include = null, SkipDirective skip = null) => WithScalarField("maxPowerProduction", alias, [include, skip]);
	public LiveMeasurementQueryBuilder ExceptMaxPowerProduction() => ExceptField("maxPowerProduction");
	public LiveMeasurementQueryBuilder WithLastMeterProduction(string alias = null, IncludeDirective include = null, SkipDirective skip = null) => WithScalarField("lastMeterProduction", alias, [include, skip]);
	public LiveMeasurementQueryBuilder ExceptLastMeterProduction() => ExceptField("lastMeterProduction");
	public LiveMeasurementQueryBuilder WithPowerFactor(string alias = null, IncludeDirective include = null, SkipDirective skip = null) => WithScalarField("powerFactor", alias, [include, skip]);
	public LiveMeasurementQueryBuilder ExceptPowerFactor() => ExceptField("powerFactor");
	public LiveMeasurementQueryBuilder WithVoltagePhase1(string alias = null, IncludeDirective include = null, SkipDirective skip = null) => WithScalarField("voltagePhase1", alias, [include, skip]);
	public LiveMeasurementQueryBuilder ExceptVoltagePhase1() => ExceptField("voltagePhase1");
	public LiveMeasurementQueryBuilder WithVoltagePhase2(string alias = null, IncludeDirective include = null, SkipDirective skip = null) => WithScalarField("voltagePhase2", alias, [include, skip]);
	public LiveMeasurementQueryBuilder ExceptVoltagePhase2() => ExceptField("voltagePhase2");
	public LiveMeasurementQueryBuilder WithVoltagePhase3(string alias = null, IncludeDirective include = null, SkipDirective skip = null) => WithScalarField("voltagePhase3", alias, [include, skip]);
	public LiveMeasurementQueryBuilder ExceptVoltagePhase3() => ExceptField("voltagePhase3");
	public LiveMeasurementQueryBuilder WithCurrentL1(string alias = null, IncludeDirective include = null, SkipDirective skip = null) => WithScalarField("currentL1", alias, [include, skip]);
	public LiveMeasurementQueryBuilder ExceptCurrentL1() => ExceptField("currentL1");
	public LiveMeasurementQueryBuilder WithCurrentL2(string alias = null, IncludeDirective include = null, SkipDirective skip = null) => WithScalarField("currentL2", alias, [include, skip]);
	public LiveMeasurementQueryBuilder ExceptCurrentL2() => ExceptField("currentL2");
	public LiveMeasurementQueryBuilder WithCurrentL3(string alias = null, IncludeDirective include = null, SkipDirective skip = null) => WithScalarField("currentL3", alias, [include, skip]);
	public LiveMeasurementQueryBuilder ExceptCurrentL3() => ExceptField("currentL3");
	public LiveMeasurementQueryBuilder WithSignalStrength(string alias = null, IncludeDirective include = null, SkipDirective skip = null) => WithScalarField("signalStrength", alias, [include, skip]);
	public LiveMeasurementQueryBuilder ExceptSignalStrength() => ExceptField("signalStrength");
}

public partial class PushNotificationResponseQueryBuilder : GraphQlQueryBuilder<PushNotificationResponseQueryBuilder>
{
	private static readonly GraphQlFieldMetadata[] AllFieldMetadata = [ new GraphQlFieldMetadata {Name = "successful"}, new GraphQlFieldMetadata {Name = "pushedToNumberOfDevices"} ];

	protected override string TypeName { get { return "PushNotificationResponse"; } }

	public override IReadOnlyList<GraphQlFieldMetadata> AllFields { get { return AllFieldMetadata; } }

	public PushNotificationResponseQueryBuilder WithSuccessful(string alias = null, IncludeDirective include = null, SkipDirective skip = null) => WithScalarField("successful", alias, [include, skip]);

	public PushNotificationResponseQueryBuilder ExceptSuccessful() => ExceptField("successful");

	public PushNotificationResponseQueryBuilder WithPushedToNumberOfDevices(string alias = null, IncludeDirective include = null, SkipDirective skip = null) => WithScalarField("pushedToNumberOfDevices", alias, [include, skip]);

	public PushNotificationResponseQueryBuilder ExceptPushedToNumberOfDevices() => ExceptField("pushedToNumberOfDevices");
}

public partial class TibberMutationQueryBuilder(string operationName = null) : GraphQlQueryBuilder<TibberMutationQueryBuilder>("mutation", operationName)
{
	private static readonly GraphQlFieldMetadata[] AllFieldMetadata =
		  [
				new GraphQlFieldMetadata {Name = "sendMeterReading", IsComplex = true, QueryBuilderType = typeof(MeterReadingResponseQueryBuilder) },
				new GraphQlFieldMetadata {Name = "updateHome", IsComplex = true, QueryBuilderType = typeof(HomeQueryBuilder) },
				new GraphQlFieldMetadata {Name = "sendPushNotification", IsComplex = true, QueryBuilderType = typeof(PushNotificationResponseQueryBuilder) }
		  ];

	protected override string TypeName { get { return "RootMutation"; } }

	public override IReadOnlyList<GraphQlFieldMetadata> AllFields { get { return AllFieldMetadata; } }

	public TibberMutationQueryBuilder WithParameter<T>(GraphQlQueryParameter<T> parameter) => WithParameterInternal(parameter);

	public TibberMutationQueryBuilder WithSendMeterReading(MeterReadingResponseQueryBuilder meterReadingResponseQueryBuilder, QueryBuilderParameter<MeterReadingInput> input, string alias = null, IncludeDirective include = null, SkipDirective skip = null)
	{
		var args = new List<QueryBuilderArgumentInfo>
		{
			new QueryBuilderArgumentInfo { ArgumentName = "input", ArgumentValue = input }
		};

		return WithObjectField("sendMeterReading", alias, meterReadingResponseQueryBuilder, [include, skip], args);
	}

	public TibberMutationQueryBuilder ExceptSendMeterReading() => ExceptField("sendMeterReading");

	public TibberMutationQueryBuilder WithUpdateHome(HomeQueryBuilder homeQueryBuilder, QueryBuilderParameter<UpdateHomeInput> input, string alias = null, IncludeDirective include = null, SkipDirective skip = null)
	{
		var args = new List<QueryBuilderArgumentInfo>
		{
			new QueryBuilderArgumentInfo { ArgumentName = "input", ArgumentValue = input }
		};

		return WithObjectField("updateHome", alias, homeQueryBuilder, [include, skip], args);
	}

	public TibberMutationQueryBuilder ExceptUpdateHome() => ExceptField("updateHome");

	public TibberMutationQueryBuilder WithSendPushNotification(PushNotificationResponseQueryBuilder pushNotificationResponseQueryBuilder, QueryBuilderParameter<PushNotificationInput> input, string alias = null, IncludeDirective include = null, SkipDirective skip = null)
	{
		var args = new List<QueryBuilderArgumentInfo>();
		args.Add(new QueryBuilderArgumentInfo { ArgumentName = "input", ArgumentValue = input });

		return WithObjectField("sendPushNotification", alias, pushNotificationResponseQueryBuilder, [include, skip], args);
	}

	public TibberMutationQueryBuilder ExceptSendPushNotification() => ExceptField("sendPushNotification");
}

public partial class RootSubscriptionQueryBuilder(string operationName = null) : GraphQlQueryBuilder<RootSubscriptionQueryBuilder>("subscription", operationName)
{
	private static readonly GraphQlFieldMetadata[] AllFieldMetadata =
		  [
				new GraphQlFieldMetadata {Name = "liveMeasurement", IsComplex = true, QueryBuilderType = typeof(LiveMeasurementQueryBuilder) },
				new GraphQlFieldMetadata {Name = "testMeasurement", IsComplex = true, QueryBuilderType = typeof(LiveMeasurementQueryBuilder) }
		  ];

	protected override string TypeName { get { return "RootSubscription"; } }

	public override IReadOnlyList<GraphQlFieldMetadata> AllFields { get { return AllFieldMetadata; } }

	public RootSubscriptionQueryBuilder WithParameter<T>(GraphQlQueryParameter<T> parameter) => WithParameterInternal(parameter);

	public RootSubscriptionQueryBuilder WithLiveMeasurement(LiveMeasurementQueryBuilder liveMeasurementQueryBuilder, QueryBuilderParameter<Guid> homeId, string alias = null, IncludeDirective include = null, SkipDirective skip = null)
	{
		var args = new List<QueryBuilderArgumentInfo>
		{
			new() { ArgumentName = "homeId", ArgumentValue = homeId }
		};

		return WithObjectField("liveMeasurement", alias, liveMeasurementQueryBuilder, [include, skip], args);
	}

	public RootSubscriptionQueryBuilder ExceptLiveMeasurement() => ExceptField("liveMeasurement");

	public RootSubscriptionQueryBuilder WithTestMeasurement(LiveMeasurementQueryBuilder liveMeasurementQueryBuilder, QueryBuilderParameter<Guid> homeId, string alias = null, IncludeDirective include = null, SkipDirective skip = null)
	{
		var args = new List<QueryBuilderArgumentInfo>
		{
			new() { ArgumentName = "homeId", ArgumentValue = homeId }
		};

		return WithObjectField("testMeasurement", alias, liveMeasurementQueryBuilder, [include, skip], args);
	}

	public RootSubscriptionQueryBuilder ExceptTestMeasurement() => ExceptField("testMeasurement");
}
#endregion

#region input classes
public partial class MeterReadingInput : IGraphQlInputObject
{
	private InputPropertyInfo _homeId;
	private InputPropertyInfo _time;
	private InputPropertyInfo _reading;

#if !GRAPHQL_GENERATOR_DISABLE_NEWTONSOFT_JSON
	[JsonConverter(typeof(QueryBuilderParameterConverter<Guid?>))]
#endif
	public QueryBuilderParameter<Guid?> HomeId
	{
		get { return (QueryBuilderParameter<Guid?>)_homeId.Value; }
		set { _homeId = new InputPropertyInfo { Name = "homeId", Value = value }; }
	}

#if !GRAPHQL_GENERATOR_DISABLE_NEWTONSOFT_JSON
	[JsonConverter(typeof(QueryBuilderParameterConverter<string>))]
#endif
	public QueryBuilderParameter<string> Time
	{
		get { return (QueryBuilderParameter<string>)_time.Value; }
		set { _time = new InputPropertyInfo { Name = "time", Value = value }; }
	}

#if !GRAPHQL_GENERATOR_DISABLE_NEWTONSOFT_JSON
	[JsonConverter(typeof(QueryBuilderParameterConverter<int?>))]
#endif
	public QueryBuilderParameter<int?> Reading
	{
		get { return (QueryBuilderParameter<int?>)_reading.Value; }
		set { _reading = new InputPropertyInfo { Name = "reading", Value = value }; }
	}

	IEnumerable<InputPropertyInfo> IGraphQlInputObject.GetPropertyValues()
	{
		if (_homeId.Name != null) yield return _homeId;
		if (_time.Name != null) yield return _time;
		if (_reading.Name != null) yield return _reading;
	}
}

public partial class UpdateHomeInput : IGraphQlInputObject
{
	private InputPropertyInfo _homeId;
	private InputPropertyInfo _appNickname;
	private InputPropertyInfo _appAvatar;
	private InputPropertyInfo _size;
	private InputPropertyInfo _type;
	private InputPropertyInfo _numberOfResidents;
	private InputPropertyInfo _primaryHeatingSource;
	private InputPropertyInfo _hasVentilationSystem;
	private InputPropertyInfo _mainFuseSize;

#if !GRAPHQL_GENERATOR_DISABLE_NEWTONSOFT_JSON
	[JsonConverter(typeof(QueryBuilderParameterConverter<Guid?>))]
#endif
	public QueryBuilderParameter<Guid?> HomeId
	{
		get { return (QueryBuilderParameter<Guid?>)_homeId.Value; }
		set { _homeId = new InputPropertyInfo { Name = "homeId", Value = value }; }
	}

#if !GRAPHQL_GENERATOR_DISABLE_NEWTONSOFT_JSON
	[JsonConverter(typeof(QueryBuilderParameterConverter<string>))]
#endif
	public QueryBuilderParameter<string> AppNickname
	{
		get { return (QueryBuilderParameter<string>)_appNickname.Value; }
		set { _appNickname = new InputPropertyInfo { Name = "appNickname", Value = value }; }
	}

#if !GRAPHQL_GENERATOR_DISABLE_NEWTONSOFT_JSON
	[JsonConverter(typeof(QueryBuilderParameterConverter<HomeAvatar?>))]
#endif
	public QueryBuilderParameter<HomeAvatar?> AppAvatar
	{
		get { return (QueryBuilderParameter<HomeAvatar?>)_appAvatar.Value; }
		set { _appAvatar = new InputPropertyInfo { Name = "appAvatar", Value = value }; }
	}

#if !GRAPHQL_GENERATOR_DISABLE_NEWTONSOFT_JSON
	[JsonConverter(typeof(QueryBuilderParameterConverter<int?>))]
#endif
	public QueryBuilderParameter<int?> Size
	{
		get { return (QueryBuilderParameter<int?>)_size.Value; }
		set { _size = new InputPropertyInfo { Name = "size", Value = value }; }
	}

#if !GRAPHQL_GENERATOR_DISABLE_NEWTONSOFT_JSON
	[JsonConverter(typeof(QueryBuilderParameterConverter<HomeType?>))]
#endif
	public QueryBuilderParameter<HomeType?> Type
	{
		get { return (QueryBuilderParameter<HomeType?>)_type.Value; }
		set { _type = new InputPropertyInfo { Name = "type", Value = value }; }
	}

#if !GRAPHQL_GENERATOR_DISABLE_NEWTONSOFT_JSON
	[JsonConverter(typeof(QueryBuilderParameterConverter<int?>))]
#endif
	public QueryBuilderParameter<int?> NumberOfResidents
	{
		get { return (QueryBuilderParameter<int?>)_numberOfResidents.Value; }
		set { _numberOfResidents = new InputPropertyInfo { Name = "numberOfResidents", Value = value }; }
	}

#if !GRAPHQL_GENERATOR_DISABLE_NEWTONSOFT_JSON
	[JsonConverter(typeof(QueryBuilderParameterConverter<HeatingSource?>))]
#endif
	public QueryBuilderParameter<HeatingSource?> PrimaryHeatingSource
	{
		get { return (QueryBuilderParameter<HeatingSource?>)_primaryHeatingSource.Value; }
		set { _primaryHeatingSource = new InputPropertyInfo { Name = "primaryHeatingSource", Value = value }; }
	}

#if !GRAPHQL_GENERATOR_DISABLE_NEWTONSOFT_JSON
	[JsonConverter(typeof(QueryBuilderParameterConverter<bool?>))]
#endif
	public QueryBuilderParameter<bool?> HasVentilationSystem
	{
		get { return (QueryBuilderParameter<bool?>)_hasVentilationSystem.Value; }
		set { _hasVentilationSystem = new InputPropertyInfo { Name = "hasVentilationSystem", Value = value }; }
	}

#if !GRAPHQL_GENERATOR_DISABLE_NEWTONSOFT_JSON
	[JsonConverter(typeof(QueryBuilderParameterConverter<int?>))]
#endif
	public QueryBuilderParameter<int?> MainFuseSize
	{
		get { return (QueryBuilderParameter<int?>)_mainFuseSize.Value; }
		set { _mainFuseSize = new InputPropertyInfo { Name = "mainFuseSize", Value = value }; }
	}

	IEnumerable<InputPropertyInfo> IGraphQlInputObject.GetPropertyValues()
	{
		if (_homeId.Name != null) yield return _homeId;
		if (_appNickname.Name != null) yield return _appNickname;
		if (_appAvatar.Name != null) yield return _appAvatar;
		if (_size.Name != null) yield return _size;
		if (_type.Name != null) yield return _type;
		if (_numberOfResidents.Name != null) yield return _numberOfResidents;
		if (_primaryHeatingSource.Name != null) yield return _primaryHeatingSource;
		if (_hasVentilationSystem.Name != null) yield return _hasVentilationSystem;
		if (_mainFuseSize.Name != null) yield return _mainFuseSize;
	}
}

public partial class PushNotificationInput : IGraphQlInputObject
{
	private InputPropertyInfo _title;
	private InputPropertyInfo _message;
	private InputPropertyInfo _screenToOpen;

#if !GRAPHQL_GENERATOR_DISABLE_NEWTONSOFT_JSON
	[JsonConverter(typeof(QueryBuilderParameterConverter<string>))]
#endif
	public QueryBuilderParameter<string> Title
	{
		get { return (QueryBuilderParameter<string>)_title.Value; }
		set { _title = new InputPropertyInfo { Name = "title", Value = value }; }
	}

#if !GRAPHQL_GENERATOR_DISABLE_NEWTONSOFT_JSON
	[JsonConverter(typeof(QueryBuilderParameterConverter<string>))]
#endif
	public QueryBuilderParameter<string> Message
	{
		get { return (QueryBuilderParameter<string>)_message.Value; }
		set { _message = new InputPropertyInfo { Name = "message", Value = value }; }
	}

#if !GRAPHQL_GENERATOR_DISABLE_NEWTONSOFT_JSON
	[JsonConverter(typeof(QueryBuilderParameterConverter<AppScreen?>))]
#endif
	public QueryBuilderParameter<AppScreen?> ScreenToOpen
	{
		get { return (QueryBuilderParameter<AppScreen?>)_screenToOpen.Value; }
		set { _screenToOpen = new InputPropertyInfo { Name = "screenToOpen", Value = value }; }
	}

	IEnumerable<InputPropertyInfo> IGraphQlInputObject.GetPropertyValues()
	{
		if (_title.Name != null) yield return _title;
		if (_message.Name != null) yield return _message;
		if (_screenToOpen.Name != null) yield return _screenToOpen;
	}
}
#endregion

#region data classes
public partial class Address
{
	public string Address1;          // { get; set; }
	public string Address2;          // { get; set; }
	public string Address3;          // { get; set; }
	public string City;              // { get; set; }
	public string PostalCode;        // { get; set; }
	public string Country;           // { get; set; }
	public string Latitude;          // { get; set; }
	public string Longitude;         // { get; set; }
}

public partial class ContactInfo
{
	public string Email;              // { get; set; }
	public string Mobile;             // { get; set; }
}

public partial class LegalEntity
{
	public Guid? Id;                  // { get; set; }
	public string FirstName;          // { get; set; }
	public bool? IsCompany;           // { get; set; }
	public string Name;               // { get; set; }
	public string MiddleName;         // { get; set; }
	public string LastName;           // { get; set; }
	public string OrganizationNo;     // { get; set; }
	public string Language;           // { get; set; }
	public ContactInfo ContactInfo;   // { get; set; }
	public Address Address;           // { get; set; }
}

[GraphQlObjectType("HomeConsumptionPageInfo")]
public partial class HomeConsumptionPageInfo : IPageInfo
{
	public string EndCursor { get; set; }
	public bool? HasNextPage { get; set; }
	public bool? HasPreviousPage { get; set; }
	public string StartCursor { get; set; }
	public int? Count;                // { get; set; }
	public string Currency;           // { get; set; }
	public decimal? TotalCost;        // { get; set; }
	public decimal? TotalConsumption; // { get; set; }
	public int? Filtered;             // { get; set; }
}

[GraphQlObjectType("HomeProductionPageInfo")]
public partial class HomeProductionPageInfo : IPageInfo
{
	public string EndCursor { get; set; }
	public bool? HasNextPage { get; set; }
	public bool? HasPreviousPage { get; set; }
	public string StartCursor { get; set; }
	public int? Count;               // { get; set; }
	public string Currency;          // { get; set; }
	public decimal? TotalProfit;     // { get; set; }
	public decimal? TotalProduction; // { get; set; }
	public int? Filtered;            // { get; set; }
}

public partial class Price
{
	public decimal? Total;            // { get; set; }
	public decimal? Energy;           // { get; set; }
	public decimal? Tax;              // { get; set; }
	public string StartsAt;           // { get; set; }
	public string Currency;           // { get; set; }
	public PriceLevel? Level;         // { get; set; }
}

public partial class SubscriptionPriceEdge
{
	public string Cursor;             // { get; set; }
	public Price Node;                // { get; set; }
}

public partial interface IPageInfo
{
	string EndCursor { get; set; }
	bool? HasNextPage { get; set; }
	bool? HasPreviousPage { get; set; }
	string StartCursor { get; set; }
}

[GraphQlObjectType("SubscriptionPriceConnectionPageInfo")]
public partial class SubscriptionPriceConnectionPageInfo : IPageInfo
{
	public string EndCursor { get; set; }
	public bool? HasNextPage { get; set; }
	public bool? HasPreviousPage { get; set; }
	public string StartCursor { get; set; }
	public string Resolution { get; set; }
	public string Currency { get; set; }
	public int? Count { get; set; }
	public string Precision { get; set; }
	public decimal? MinEnergy { get; set; }
	public decimal? MinTotal { get; set; }
	public decimal? MaxEnergy { get; set; }
	public decimal? MaxTotal { get; set; }
}

public partial class SubscriptionPriceConnection
{
	public SubscriptionPriceConnectionPageInfo PageInfo;         // { get; set; }
	public ICollection<SubscriptionPriceEdge> Edges;             // { get; set; }
	public ICollection<Price> Nodes;                             // { get; set; }
}

public partial class PriceInfo
{
	public Price Current;                                        // { get; set; }
	public ICollection<Price> Today;                             // { get; set; }
	public ICollection<Price> Tomorrow;                          // { get; set; }
	public SubscriptionPriceConnection Range;                    // { get; set; }
}

public partial class PriceRatingEntry
{
	public string Time;                                          // { get; set; }
	public decimal? Energy;                                      // { get; set; }
	public decimal? Total;                                       // { get; set; }
	public decimal? Tax;                                         // { get; set; }
	public decimal? Difference;                                  // { get; set; }
	public PriceRatingLevel? Level;                              // { get; set; }
}

public partial class PriceRatingType
{
	public decimal? MinEnergy;                                   // { get; set; }
	public decimal? MaxEnergy;                                   // { get; set; }
	public decimal? MinTotal;                                    // { get; set; }
	public decimal? MaxTotal;                                    // { get; set; }
	public string Currency;                                      // { get; set; }
	public ICollection<PriceRatingEntry> Entries;                // { get; set; }
}

public partial class PriceRatingThresholdPercentages
{
	public decimal? High;                                        // { get; set; }
	public decimal? Low;                                         // { get; set; }
}

public partial class PriceRating
{
	public PriceRatingThresholdPercentages ThresholdPercentages; // { get; set; }
	public PriceRatingType Hourly;                               // { get; set; }
	public PriceRatingType Daily;                                // { get; set; }
	public PriceRatingType Monthly;                              // { get; set; }
}

public partial class Subscription
{
	public Guid? Id;                                             // { get; set; }
	public LegalEntity Subscriber;                               // { get; set; }
	public DateTimeOffset? ValidFrom;                            // { get; set; }
	public DateTimeOffset? ValidTo;                              // { get; set; }
	public string Status;                                        // { get; set; }
	public PriceInfo PriceInfo;                                  // { get; set; }
	public PriceRating PriceRating;                              // { get; set; }
}

public partial class ConsumptionEntry
{
	public DateTimeOffset? From;                                 // { get; set; }
	public DateTimeOffset? To;                                   // { get; set; }
	public decimal? UnitPrice;                                   // { get; set; }
	public decimal? UnitPriceVat;                                // { get; set; }
	public decimal? Consumption;                                 // { get; set; }
	public string ConsumptionUnit;                               // { get; set; }
	public decimal? Cost;                                        // { get; set; }
	public string Currency;                                      // { get; set; }
}

public partial class ProductionEntry
{
	public DateTimeOffset? From;                                 // { get; set; }
	public DateTimeOffset? To;                                   // { get; set; }
	public decimal? UnitPrice;                                   // { get; set; }
	public decimal? UnitPriceVat;                                // { get; set; }
	public decimal? Production;                                  // { get; set; }
	public string ProductionUnit;                                // { get; set; }
	public decimal? Profit;                                      // { get; set; }
	public string Currency;                                      // { get; set; }
}

public partial class HomeConsumptionEdge
{
	public string Cursor;                                        // { get; set; }
	public ConsumptionEntry Node;                                // { get; set; }
}

public partial class HomeProductionEdge
{
	public string Cursor;                                        // { get; set; }
	public ProductionEntry Node;                                 // { get; set; }
}

public partial class HomeConsumptionConnection
{
	public HomeConsumptionPageInfo PageInfo;                     // { get; set; }
	public ICollection<ConsumptionEntry> Nodes;                  // { get; set; }
	public ICollection<HomeConsumptionEdge> Edges;               // { get; set; }
}

public partial class HomeProductionConnection
{
	public HomeProductionPageInfo PageInfo;                      // { get; set; }
	public ICollection<ProductionEntry> Nodes;                   // { get; set; }
	public ICollection<HomeProductionEdge> Edges;                // { get; set; }
}

public partial class MeteringPointData
{
	public string ConsumptionEan;                                // { get; set; }
	public string GridCompany;                                   // { get; set; }
	public string GridAreaCode;                                  // { get; set; }
	public string PriceAreaCode;                                 // { get; set; }
	public string ProductionEan;                                 // { get; set; }
	public string EnergyTaxType;                                 // { get; set; }
	public string VatType;                                       // { get; set; }
	public int? EstimatedAnnualConsumption;                      // { get; set; }
}

public partial class HomeFeatures
{
	public bool? RealTimeConsumptionEnabled;                     // { get; set; }
}

public partial class Home
{
	public Guid? Id;                                             // { get; set; }
	public string TimeZone;                                      // { get; set; }
	public string AppNickname;                                   // { get; set; }
	public HomeAvatar? AppAvatar;                                // { get; set; }
	public int? Size;                                            // { get; set; }
	public HomeType? Type;                                       // { get; set; }
	public int? NumberOfResidents;                               // { get; set; }
	public HeatingSource? PrimaryHeatingSource;                  // { get; set; }
	public bool? HasVentilationSystem;                           // { get; set; }
	public int? MainFuseSize;                                    // { get; set; }
	public Address Address;                                      // { get; set; }
	public LegalEntity Owner;                                    // { get; set; }
	public MeteringPointData MeteringPointData;                  // { get; set; }
	public Subscription CurrentSubscription;                     // { get; set; }
	public ICollection<Subscription> Subscriptions;              // { get; set; }
	public HomeConsumptionConnection Consumption;                // { get; set; }
	public HomeProductionConnection Production;                  // { get; set; }
	public HomeFeatures Features;                                // { get; set; }
}

public partial class Viewer
{
	public string Login;                                         // { get; set; }
	public string UserId;                                        // { get; set; }
	public string Name;                                          // { get; set; }
	public ICollection<string> AccountType;                      // { get; set; }
	public ICollection<Home> Homes;                              // { get; set; }
	public Home Home;                                            // { get; set; }
	public string WebsocketSubscriptionUrl;                      // { get; set; }
}

public partial class Tibber
{
	public Viewer Viewer;                                        // { get; set; }
}

public partial class MeterReadingResponse
{
	public Guid? HomeId;                                         // { get; set; }
	public string Time;                                          // { get; set; }
	public int? Reading;                                         // { get; set; }
}

public partial class LiveMeasurement
{
	public DateTimeOffset? Timestamp;                            // { get; set; }
	public decimal? Power;                                       // { get; set; }
	public decimal? LastMeterConsumption;                        // { get; set; }
	public decimal? AccumulatedConsumption;                      // { get; set; }
	public decimal? AccumulatedProduction;                       // { get; set; }
	public decimal? AccumulatedConsumptionLastHour;              // { get; set; }
	public decimal? AccumulatedProductionLastHour;               // { get; set; }
	public decimal? AccumulatedCost;                             // { get; set; }
	public decimal? AccumulatedReward;                           // { get; set; }
	public string Currency;                                      // { get; set; }
	public decimal? MinPower;                                    // { get; set; }
	public decimal? AveragePower;                                // { get; set; }
	public decimal? MaxPower;                                    // { get; set; }
	public decimal? PowerProduction;                             // { get; set; }
	public decimal? PowerReactive;                               // { get; set; }
	public decimal? PowerProductionReactive;                     // { get; set; }
	public decimal? MinPowerProduction;                          // { get; set; }
	public decimal? MaxPowerProduction;                          // { get; set; }
	public decimal? LastMeterProduction;                         // { get; set; }
	public decimal? PowerFactor;                                 // { get; set; }
	public decimal? VoltagePhase1;                               // { get; set; }
	public decimal? VoltagePhase2;                               // { get; set; }
	public decimal? VoltagePhase3;                               // { get; set; }
	public decimal? CurrentL1;                                   // { get; set; }
	public decimal? CurrentL2;                                   // { get; set; }
	public decimal? CurrentL3;                                   // { get; set; }
	public int? SignalStrength;                                  // { get; set; }
}

public partial class PushNotificationResponse
{
	public bool? Successful;                                     // { get; set; }
	public int? PushedToNumberOfDevices;                         // { get; set; }
}

public partial class TibberMutation
{
	public MeterReadingResponse SendMeterReading;                // { get; set; }
	public Home UpdateHome;                                      // { get; set; }
	public PushNotificationResponse SendPushNotification;        // { get; set; }
}

public partial class RootSubscription
{
	public LiveMeasurement LiveMeasurement;                      // { get; set; }
	public LiveMeasurement TestMeasurement;                      // { get; set; }
}
#endregion

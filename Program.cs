using System.Reflection;
using Newtonsoft.Json.Linq;

const string NAMESPACE = "TypeGen";

var processedTypes = new HashSet<Type>();

Assembly
	.GetExecutingAssembly()
	.GetTypes()
	.Where(t => t.IsClass && t.Namespace == NAMESPACE)
	.ToList()
	.ForEach(type =>
	{
		if (processedTypes.Contains(type))
		{
			return; // Skip if the type has already been processed
		}

		if (type.Name.Contains("<>"))
		{
			return;
		}

		Directory.CreateDirectory("GeneratedJsonTypes");
		var jsonTypeDef = type.IsAbstract
			? GenerateDiscriminatorTypeDefinition(
				type,
				processedTypes
			)
			: GenerateJsonTypeDefinition(type);

		if (jsonTypeDef is null)
		{
			return;
		}

		File.WriteAllText(
			$"GeneratedJsonTypes/{type.Name}.json",
			jsonTypeDef.ToString()
		);
	});

JObject GenerateJsonTypeDefinition(Type type)
{
	var properties = new JObject();
	foreach (var property in type.GetProperties())
	{
		properties.Add(
			ToCamelCase(property.Name),
			GeneratePropertyDefinition(property.PropertyType)
		);
	}

	return new JObject { ["properties"] = properties };
}

JObject GenerateDiscriminatorTypeDefinition(
	Type abstractType,
	HashSet<Type> processedTypes
)
{
	// Find the abstract enum property
	var discriminatorProperty = abstractType
		.GetProperties()
		.FirstOrDefault(
			prop =>
				prop.PropertyType.IsEnum
				&& prop.GetGetMethod().IsAbstract
		);

	if (discriminatorProperty == null)
	{
		Console.WriteLine(
			$"No abstract property found in type {abstractType.Name} to use as a discriminator, skipping."
		);
		return null;
	}

	var discriminator = ToCamelCase(discriminatorProperty.Name);
	var mapping = new JObject();

	// Find all derived types
	var derivedTypes = Assembly
		.GetExecutingAssembly()
		.GetTypes()
		.Where(
			t =>
				t.IsClass
				&& !t.IsAbstract
				&& t.IsSubclassOf(abstractType)
		);

	foreach (var derivedType in derivedTypes)
	{
		var derivedTypeName = derivedType.Name; // Assuming the derived type name matches the enum value
		mapping[derivedTypeName] = GenerateJsonTypeDefinition(
			derivedType
		);
		processedTypes.Add(derivedType); // Mark as processed
	}

	return new JObject
	{
		["discriminator"] = discriminator,
		["mapping"] = mapping
	};
}

JToken GeneratePropertyDefinition(Type? type)
{
	if (type is null)
	{
		throw new ArgumentNullException(nameof(type));
	}

	var propertyDef = new JObject();

	if (type == typeof(string))
	{
		propertyDef["type"] = "string";
	}
	else if (type!.IsEnum)
	{
		var enumValues = Enum.GetNames(type);
		return new JObject { ["enum"] = new JArray(enumValues) };
	}
	else if (type == typeof(bool))
	{
		propertyDef["type"] = "boolean";
	}
	else if (type.IsValueType)
	{
		propertyDef["type"] = MapTypeToJsonType(type);
	}
	else if (type.IsArray)
	{
		propertyDef["elements"] = GeneratePropertyDefinition(
			type.GetElementType()
		);
	}
	else if (
		type.IsGenericType
		&& type.GetGenericTypeDefinition() == typeof(IEnumerable<>)
	)
	{
		propertyDef["elements"] = GeneratePropertyDefinition(
			type.GetGenericArguments()[0]
		);
	}
	else // Handle custom types
	{
		propertyDef["metadata"] = new JObject
		{
			["rustType"] = type.Name
		};
		propertyDef["type"] = "string"; // Assuming string representation for custom types
	}

	return propertyDef;
}

string MapTypeToJsonType(Type type)
{
	return type switch
	{
		_ when type == typeof(float) => "float32",
		_ when type == typeof(double) => "float64",
		_ when type == typeof(sbyte) => "int8",
		_ when type == typeof(byte) => "uint8",
		_ when type == typeof(short) => "int16",
		_ when type == typeof(ushort) => "uint16",
		_ when type == typeof(int) => "int32",
		_ when type == typeof(uint) => "uint32",
		_ when type == typeof(DateTime) => "timestamp",
		_ => "string" // Default case for custom or unsupported types
	};
}

string ToCamelCase(string str)
{
	if (!string.IsNullOrEmpty(str) && char.IsUpper(str[0]))
	{
		return char.ToLowerInvariant(str[0]) + str.Substring(1);
	}
	return str;
}

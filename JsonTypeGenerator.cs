namespace TypeGen
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;
	using System.Reflection;
	using Newtonsoft.Json.Linq;

	public static class JsonTypeGenerator
	{
		public static void GenerateJsonTypes()
		{
			Assembly
				.GetExecutingAssembly()
				.GetTypes()
				.Where(t => t.IsClass && t.Namespace == "TypeGen")
				.ToList()
				.ForEach(type =>
				{
					Directory.CreateDirectory("GeneratedJsonTypes");
					var jsonTypeDef = type.IsAbstract
						? GenerateDiscriminatorTypeDefinition(type)
						: GenerateJsonTypeDefinition(type);

					File.WriteAllText(
						$"GeneratedJsonTypes/{type.Name}.json",
						jsonTypeDef.ToString()
					);
				});
		}

		static JObject GenerateJsonTypeDefinition(Type type)
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

		static JObject GenerateDiscriminatorTypeDefinition(
			Type abstractType
		)
		{
			var discriminator = "incomeType"; // Assuming "incomeType" is the discriminator property
			var mapping = new JObject();

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
			}

			return new JObject
			{
				["discriminator"] = discriminator,
				["mapping"] = mapping
			};
		}

		static JToken GeneratePropertyDefinition(Type? type)
		{
			type.ThrowIfNull("Type is null");

			var propertyDef = new JObject();

			if (type == typeof(string))
			{
				propertyDef["type"] = "string";
			}
			else if (type!.IsEnum)
			{
				var enumValues = Enum.GetNames(type);
				return new JObject
				{
					["enum"] = new JArray(enumValues)
				};
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
				&& type.GetGenericTypeDefinition()
					== typeof(IEnumerable<>)
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

		static string MapTypeToJsonType(Type type)
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

		static string ToCamelCase(string str)
		{
			if (!string.IsNullOrEmpty(str) && char.IsUpper(str[0]))
			{
				return char.ToLowerInvariant(str[0])
					+ str.Substring(1);
			}
			return str;
		}
	}
}

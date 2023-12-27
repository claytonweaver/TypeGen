using System.Reflection;
using System.Runtime.CompilerServices;
using Newtonsoft.Json.Schema;
using Newtonsoft.Json.Schema.Generation;

namespace TypeGen;

public class NewtonsoftTypeGenerator : JSchemaGenerationProvider
{
	public void GenerateTypes()
	{
		Assembly
			.GetExecutingAssembly()
			.GetTypes()
			.Where(
				t =>
					t.IsClass
					&& t.Namespace == "TypeGen"
					&& IsValidTypeName(t.Name)
			)
			.ToList()
			.ForEach(type =>
			{
				var generator = new JSchemaGenerator();
				generator.GenerationProviders.Add(this);

				JSchema schema = generator.Generate(type);

				Directory.CreateDirectory("GeneratedJsonTypes");
				File.WriteAllText(
					$"GeneratedJsonTypes/{type.Name}.json",
					schema.ToString()
				);
			});
	}

	public override JSchema GetSchema(
		JSchemaTypeGenerationContext context
	)
	{
		if (IsCustomType(context.ObjectType))
		{
			// For custom types, return a reference schema
			var schema = new JSchema
			{
				Id = new Uri(
					$"#{context.ObjectType.Name}",
					UriKind.Relative
				),
				Type = JSchemaType.Object
			};

			// Add custom metadata
			schema.ExtensionData.Add(
				"rustType",
				context.ObjectType.Name
			);

			return schema;
		}

		// Return null to use the default generator for non-custom types
		return null;
	}

	static bool IsValidTypeName(string typeName)
	{
		// Filter out types with names that contain characters not allowed in file names
		return !typeName.Contains('<') && !typeName.Contains('>');
	}

	private bool IsCustomType(Type type)
	{
		// Define what you consider as a custom type. For instance:
		// A custom type could be any type that's not a built-in .NET type
		// and is defined in your specific namespace
		return type.Namespace == "TypeGen";
	}
}

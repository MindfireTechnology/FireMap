using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FireMap
{
	[Generator]
	public class Mapper : ISourceGenerator
	{
		protected const string ClassAttributeText = @"
using System;
namespace FireMap
{
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
	public sealed class MapClassAttribute : Attribute
	{
		public Type MappingType { get; }

		public MapClassAttribute(Type mappingType)
		{
			MappingType = mappingType;
		}
	}
}
";
		protected const string PropertyAttributeText = @"
using System;
namespace FireMap
{
	[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = true, Inherited = true)]
	public sealed class MapMemberAttribute : Attribute
	{
		public Type MappingType { get; }
		public string Name { get; }

		public MapMemberAttribute(Type mappingType, string name = """")
		{
			MappingType = mappingType;
			Name = name;
		}
	}
}
";

		public void Initialize(InitializationContext context)
		{
			context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());
		}

		public void Execute(SourceGeneratorContext context)
		{
			context.AddSource("MapClassAttribute", SourceText.From(ClassAttributeText, Encoding.UTF8));
			context.AddSource("MapMemberAttribute", SourceText.From(PropertyAttributeText, Encoding.UTF8));

			if (!(context.SyntaxReceiver is SyntaxReceiver receiver))
				return;

			// create a new compilation that contains the attribute
			var options = (context.Compilation as CSharpCompilation).SyntaxTrees[0].Options as CSharpParseOptions;
			var compilation = context.Compilation.AddSyntaxTrees(CSharpSyntaxTree.ParseText(SourceText.From(ClassAttributeText, Encoding.UTF8), options));
			compilation = compilation.AddSyntaxTrees(CSharpSyntaxTree.ParseText(SourceText.From(PropertyAttributeText, Encoding.UTF8), options));

			// get the newly bound attribute
			var classAttributeSymbol = compilation.GetTypeByMetadataName("FireMap.MapClassAttribute");
			var propAttributeSymbol = compilation.GetTypeByMetadataName("FireMap.MapMemberAttribute");

			var classSource = new StringBuilder(@"
namespace FireMap
{
	public class Mapper : IMapper
	{
");
			var interfaceSource = new StringBuilder(@"
namespace FireMap
{
	public interface IMapper
	{
");

			foreach(var classSymbol in receiver.Classes)
			{
				var model = compilation.GetSemanticModel(classSymbol.SyntaxTree);
				var symbol = model.GetSymbolInfo(classSymbol);

				if (!symbol.Symbol.Equals(symbol.Symbol.ContainingNamespace, SymbolEqualityComparer.Default))
				{
					continue;
					// Could issue a diagnostic message that the class must be top level
				}

				var attributeSymbol = symbol.Symbol.GetAttributes().FirstOrDefault(a => a.AttributeClass.MetadataName == "FireMap.MapClassAttribute");

				if (attributeSymbol == null)
					continue;

				foreach (var member in )
				{
					var memberSymbol = model.GetDeclaredSymbol(member);

					var kind = member.Kind();
					if (kind == SyntaxKind.FieldDeclaration)
					{

					}
					else if (kind == SyntaxKind.PropertyDeclaration)
					{

					}

					// foreach over the members in the class
					// if it's a field or property, then add it to the list of members.
					// If it has FireMap attributes, then use those to determine types
				}
			}

			classSource.AppendLine(@"
	}
}");
			interfaceSource.AppendLine(@"
	}
}");
		}
	}
}

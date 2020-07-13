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

			foreach(var classSyntax in receiver.Classes)
			{
				var classModel = compilation.GetSemanticModel(classSyntax.SyntaxTree);
				//var classSymbol = classModel.GetSymbolInfo(classSyntax);
				var classSymbol = classModel.GetDeclaredSymbol(classSyntax);
				var classType = classModel.GetTypeInfo(classSyntax);

				// Only work with top-level classes. Can remove if we want to support nested classes
				if (!classSymbol.ContainingSymbol.Equals(classSymbol.ContainingNamespace, SymbolEqualityComparer.Default))
				{
					continue;
					// TODO: Could issue a diagnostic message that the class must be top level
				}

				var classAttributeData = classSymbol.GetAttributes().FirstOrDefault(a => a.AttributeClass.Equals(classAttributeSymbol, SymbolEqualityComparer.Default));

				var members = classSymbol.GetMembers();

				if (classAttributeData == null && !members.Any(s => s.GetAttributes().Any(a => a.AttributeClass.Equals(propAttributeSymbol, SymbolEqualityComparer.Default))))
					continue;

				var attributeArguments = classAttributeData.ConstructorArguments;
				var typeAttributeArgument = attributeArguments.First();
				var mappingType = (typeAttributeArgument.Value as INamedTypeSymbol).OriginalDefinition;
				//var mappingType = typeAttributeArgument.Value.GetType();

				if (!compilation.ContainsSymbolsWithName(mappingType.MetadataName))
				{
					continue;
					// TODO: Output a diagnostic message here that the class couldn't be found
				}

				var mappingSymbol = compilation.GetTypeByMetadataName(mappingType.ToDisplayString());

				if (mappingSymbol == null)
				{
					continue;
					// TODO: Output a diagnostic message here that the class couldn't be found.
				}

				var mappingMembers = mappingSymbol.GetMembers();

				var methodSignature = $"{mappingType.ToDisplayString()} To{mappingType.Name}({classSymbol.ToDisplayString()} source)";
				interfaceSource.AppendLine($@"
		/// <summary>Convert from {classSymbol.ToDisplayString()} to {mappingType.ToDisplayString()}</summary>
		{methodSignature};
");

				classSource.AppendLine($@"
		public virtual {methodSignature} => new {mappingType.ToDisplayString()}
		{{
");
				var memberMappings = new List<string>();

				foreach (var member in members)
				{
					if (member.IsImplicitlyDeclared || member.IsStatic || member.DeclaredAccessibility != Accessibility.Public
						|| (member.Kind != SymbolKind.Field && member.Kind != SymbolKind.Property))
						continue;

					var memberAttribute = member.GetAttributes().FirstOrDefault(a =>
					{
						var attributeClass = a.ConstructorArguments.First();
						return a.AttributeClass.Equals(propAttributeSymbol, SymbolEqualityComparer.Default)
							&& SymbolEqualityComparer.Default.Equals(attributeClass.Type, classType.Type);
					});

					if (memberAttribute != null)
					{
						var name = memberAttribute.ConstructorArguments.Last().Value as string;

						var mappingMember = mappingMembers.FirstOrDefault(mm => mm.Name == name);

						if (mappingMember != null)
							memberMappings.Add($"{mappingMember.Name} = source.{member.ToDisplayString()}");
					}
					else
					{
						var mappingMember = mappingMembers.FirstOrDefault(mm => mm.Name == member.Name);

						if (mappingMember != null)
							memberMappings.Add($"{member.Name} = source.{member.Name}");
					}
				}

				classSource.AppendLine($@"
				{string.Join(@",
", memberMappings)}
		}};
");
			}

			classSource.AppendLine(@"
	}
}");
			interfaceSource.AppendLine(@"
	}
}");

			context.AddSource("IMapping.cs", SourceText.From(interfaceSource.ToString(), Encoding.UTF8));
			context.AddSource("Mapping.cs", SourceText.From(classSource.ToString(), Encoding.UTF8));
		}
	}
}

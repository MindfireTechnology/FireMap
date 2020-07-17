using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
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
		protected const string MapClassToAttributeText = @"
using System;
namespace FireMap
{
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
	public sealed class MapClassToAttribute : Attribute
	{
		public Type MappingType { get; }
		public bool Reverse {get; set; }

		public MapClassToAttribute(Type mappingType)
		{
			MappingType = mappingType;
		}
	}
}
";
		protected const string MapClassFromAttributeText = @"
using System;
namespace FireMap
{
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
	public sealed class MapClassFromAttribute : Attribute
	{
		public Type MappingType { get; }

		public MapClassFromAttribute(Type mappingType)
		{
			MappingType = mappingType;
		}
	}
}
";

		protected const string MapPropertyToAttributeText = @"
using System;
namespace FireMap
{
	[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = true, Inherited = true)]
	public sealed class MapMemberToAttribute : Attribute
	{
		public Type MappingType { get; }
		public string Name { get; set; } = """";

		public MapMemberToAttribute(Type mappingType)
		{
			MappingType = mappingType;
		}
	}
}
";
		protected const string MapPropertyFromAttributeText = @"
using System;
namespace FireMap
{
	[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = true, Inherited = true)]
	public sealed class MapMemberFromAttribute : Attribute
	{
		public Type MappingType { get; }
		public string Name { get; set; } = """";

		public MapMemberFromAttribute(Type mappingType)
		{
			MappingType = mappingType;
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
			context.AddSource("MapClassToAttribute", SourceText.From(MapClassToAttributeText, Encoding.UTF8));
			context.AddSource("MapMemberToAttribute", SourceText.From(MapPropertyToAttributeText, Encoding.UTF8));

			if (!(context.SyntaxReceiver is SyntaxReceiver receiver))
				return;

			// create a new compilation that contains the attribute
			var options = (context.Compilation as CSharpCompilation).SyntaxTrees[0].Options as CSharpParseOptions;
			var compilation = context.Compilation.AddSyntaxTrees(CSharpSyntaxTree.ParseText(SourceText.From(MapClassToAttributeText, Encoding.UTF8), options));
			compilation = compilation.AddSyntaxTrees(CSharpSyntaxTree.ParseText(SourceText.From(MapPropertyToAttributeText, Encoding.UTF8), options));

			// get the newly bound attribute
			var classAttributeSymbol = compilation.GetTypeByMetadataName("FireMap.MapClassToAttribute");
			var propAttributeSymbol = compilation.GetTypeByMetadataName("FireMap.MapMemberToAttribute");

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
				var (classMehthods, interfaceMethods) = BuildMapMethods(compilation, classSyntax, classAttributeSymbol, propAttributeSymbol);

				classSource.AppendLine(classMehthods);
				interfaceSource.AppendLine(interfaceMethods);
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

		/// <summary>
		/// Builds the methods that will map the source to the destinations it specifies
		/// </summary>
		/// <param name="compilation"></param>
		/// <param name="sourceSyntax"></param>
		/// <param name="classAttributeSymbol"></param>
		/// <param name="propAttributeSymbol"></param>
		/// <param name="classSource"></param>
		/// <param name="interfaceSource"></param>
		protected (string classMethods, string interfaceMethods) BuildMapMethods(Compilation compilation, TypeDeclarationSyntax sourceSyntax, INamedTypeSymbol classAttributeSymbol, INamedTypeSymbol propAttributeSymbol)
		{
			var classBuilder = new StringBuilder();
			var interfaceBuilder = new StringBuilder();

			var sourceModel = compilation.GetSemanticModel(sourceSyntax.SyntaxTree);
			var sourceSymbol = sourceModel.GetDeclaredSymbol(sourceSyntax);

			// Only work with top - level classes. Can remove if we want to support nested classes
			if (!sourceSymbol.ContainingSymbol.Equals(sourceSymbol.ContainingNamespace, SymbolEqualityComparer.Default))
			{
				return (string.Empty, string.Empty);
				// TODO: Could issue a diagnostic message that the class must be top level
			}

			var destinations = GetMappingPairsFromAttributes(sourceSymbol, classAttributeSymbol, propAttributeSymbol);

			foreach(var destination in destinations)
			{
				var mappingSymbol = compilation.GetTypeByMetadataName(destination.ToDisplayString());

				if (mappingSymbol == null)
				{
					// TODO: Output a diagnostic message about the type not being found?
					continue;
				}

				var (classMethod, interfaceMethod) = CreateClassMap(sourceSymbol, destination, propAttributeSymbol);

				classBuilder.AppendLine(classMethod);
				interfaceBuilder.AppendLine(interfaceMethod);
			}

			return (classBuilder.ToString(), interfaceBuilder.ToString());
		}

		/// <summary>
		/// Get the destination types from both the class level attribute and the property level attributes
		/// </summary>
		/// <param name="sourceSymbol">The symbol that represents the source class being mapped</param>
		/// <param name="classAttributeSymbol">The symbol that represents the MapClassToAttribute</param>
		/// <param name="propertyAttributeSymbol">The symbol that represents the MepMemberAttribute</param>
		/// <returns>Collection of the destination symbols</returns>
		protected IEnumerable<INamedTypeSymbol> GetMappingPairsFromAttributes(INamedTypeSymbol sourceSymbol, INamedTypeSymbol classAttributeSymbol, INamedTypeSymbol propertyAttributeSymbol)
		{
			var destinations = new HashSet<INamedTypeSymbol>();

			var classAttributes = sourceSymbol.GetAttributes().Where(a => a.AttributeClass.Equals(classAttributeSymbol, SymbolEqualityComparer.Default));
			var members = sourceSymbol.GetMembers();
			var propertyAttributes = members.SelectMany(m => m.GetAttributes().Where(a => a.AttributeClass.Equals(propertyAttributeSymbol, SymbolEqualityComparer.Default)));

			foreach(var classAttribute in classAttributes)
			{
				var attributeArguments = classAttribute.ConstructorArguments;
				var typeAttributeArgument = attributeArguments.First();
				var mappingType = (typeAttributeArgument.Value as INamedTypeSymbol).OriginalDefinition;

				destinations.Add(mappingType);
			}

			foreach(var propertyAttribute in propertyAttributes)
			{
				var attributeArguments = propertyAttribute.ConstructorArguments;
				var typeAttributeArgument = attributeArguments.First();
				var mappingType = (typeAttributeArgument.Value as INamedTypeSymbol).OriginalDefinition;

				destinations.Add(mappingType);
			}

			return destinations;
		}

		/// <summary>
		/// Create the mapping method that will map the source type to the destination type
		/// </summary>
		/// <param name="sourceSymbol">The symbol for the source class</param>
		/// <param name="destinationSymbol">The symbol for the destination class</param>
		/// <param name="propAttributeSymbol">The symbol for the proprety mapping attribute</param>
		/// <returns></returns>
		protected (string classMethod, string interfaceMethod) CreateClassMap(INamedTypeSymbol sourceSymbol, INamedTypeSymbol destinationSymbol, INamedTypeSymbol propAttributeSymbol)
		{
			var classBuilder = new StringBuilder();
			var interfaceBuilder = new StringBuilder();

			var sourceMembers = sourceSymbol.GetMembers();
			var destinationMembers = destinationSymbol.GetMembers();

			var methodSignature = $"{destinationSymbol.ToDisplayString()} To{destinationSymbol.ToDisplayString().Replace(".", "_")}({sourceSymbol.ToDisplayString()} source)";
			interfaceBuilder.AppendLine($@"
		/// <summary>Convert from {sourceSymbol.ToDisplayString()} to {destinationSymbol.ToDisplayString()}</summary>
		{methodSignature};
");

			classBuilder.AppendLine($@"
		public virtual {methodSignature} => new {destinationSymbol.ToDisplayString()}
		{{
");
			var memberMappings = new List<string>();

			foreach (var member in sourceMembers)
			{
				if (member.IsImplicitlyDeclared || member.IsStatic || member.DeclaredAccessibility != Accessibility.Public
					|| (member.Kind != SymbolKind.Field && member.Kind != SymbolKind.Property))
					continue;

				var memberMap = CreatePropertyMap(member, destinationSymbol, destinationMembers, propAttributeSymbol);

				if (!string.IsNullOrWhiteSpace(memberMap))
					memberMappings.Add(memberMap);
			}

			classBuilder.AppendLine($@"
				{string.Join(@",
", memberMappings)}
		}};
");

			return (classBuilder.ToString(), interfaceBuilder.ToString());
		}

		protected string CreatePropertyMap(ISymbol sourceMember, INamedTypeSymbol destinationSymbol, IEnumerable<ISymbol> destinationMembers, INamedTypeSymbol propAttributeSymbol)
		{
			if (!destinationMembers.Any())
				return string.Empty;

			var attributes = sourceMember.GetAttributes();
			var memberAttribute = attributes.FirstOrDefault(a =>
			{
				var attributeDestinationClass = a.ConstructorArguments.First();
				var symbol = attributeDestinationClass.Value as INamedTypeSymbol;

				return a.AttributeClass.Equals(propAttributeSymbol, SymbolEqualityComparer.Default)
					&& SymbolEqualityComparer.Default.Equals(symbol, destinationSymbol);
			});

			if (memberAttribute != null)
			{
				var name = memberAttribute.NamedArguments.FirstOrDefault(na => na.Key == "Name").Value.Value as string;

				var mappingMember = destinationMembers.FirstOrDefault(mm => mm.Name == name);

				if (mappingMember != null)
					return $"{mappingMember.Name} = source.{sourceMember.Name}";
			}
			else
			{
				var mappingMember = destinationMembers.FirstOrDefault(mm => mm.Name == sourceMember.Name);

				if (mappingMember != null)
					return $"{sourceMember.Name} = source.{sourceMember.Name}";
			}

			return string.Empty;
		}
	}
}

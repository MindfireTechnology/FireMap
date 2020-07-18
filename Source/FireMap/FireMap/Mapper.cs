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
		protected const string MapClassToAttribute = @"
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
		protected const string MapClassFromAttribute = @"
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

		protected const string MapMemberToAttribute = @"
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
		protected const string MapMemberFromAttribute = @"
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
			context.AddSource(nameof(MapClassToAttribute), SourceText.From(MapClassToAttribute, Encoding.UTF8));
			context.AddSource(nameof(MapClassFromAttribute), SourceText.From(MapClassFromAttribute, Encoding.UTF8));
			context.AddSource(nameof(MapMemberToAttribute), SourceText.From(MapMemberToAttribute, Encoding.UTF8));
			context.AddSource(nameof(MapMemberFromAttribute), SourceText.From(MapMemberFromAttribute, Encoding.UTF8));

			if (!(context.SyntaxReceiver is SyntaxReceiver receiver))
				return;

			// create a new compilation that contains the attribute
			var options = (context.Compilation as CSharpCompilation).SyntaxTrees[0].Options as CSharpParseOptions;
			var compilation = context.Compilation.AddSyntaxTrees(CSharpSyntaxTree.ParseText(SourceText.From(MapClassToAttribute, Encoding.UTF8), options));
			compilation = compilation.AddSyntaxTrees(CSharpSyntaxTree.ParseText(SourceText.From(MapMemberToAttribute, Encoding.UTF8), options));
			compilation = compilation.AddSyntaxTrees(CSharpSyntaxTree.ParseText(SourceText.From(MapClassFromAttribute, Encoding.UTF8), options));
			compilation = compilation.AddSyntaxTrees(CSharpSyntaxTree.ParseText(SourceText.From(MapMemberFromAttribute, Encoding.UTF8), options));

			// get the newly bound attribute
			var classToAttributeSymbol = compilation.GetTypeByMetadataName("FireMap.MapClassToAttribute");
			var propToAttributeSymbol = compilation.GetTypeByMetadataName("FireMap.MapMemberToAttribute");
			var classFromAttributeSymbol = compilation.GetTypeByMetadataName("FireMap.MapClassFromAttribute");
			var propFromAttributeSymbol = compilation.GetTypeByMetadataName("FireMap.MapMemberFromAttribute");

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
				var (classMethods, interfaceMethods) = BuildMap(compilation, classSyntax, classToAttributeSymbol, propToAttributeSymbol, classFromAttributeSymbol, propFromAttributeSymbol);

				classSource.AppendLine(classMethods);
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

		protected (string classMethods, string interfaceMethods) BuildMap(Compilation compilation, TypeDeclarationSyntax sourceSyntax, INamedTypeSymbol classToAttributeSymbol,
			INamedTypeSymbol propertyToAttributeSymbol, INamedTypeSymbol classFromAttributeSymbol, INamedTypeSymbol propertyFromAttributeSymbol)
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

			var mapToRecords = GetMapToRecords(sourceSymbol, classToAttributeSymbol, propertyToAttributeSymbol);
			var mapFromRecords = GetMapFromRecords(sourceSymbol, classFromAttributeSymbol, propertyFromAttributeSymbol);

			var mapRecords = mapToRecords.Concat(mapFromRecords).Aggregate(new Dictionary<string, MappingRecord>(), (dict, current) =>
			{
				string key = $"{current.Source.ToDisplayString()}|{current.Destination.ToDisplayString()}";

				if (!dict.ContainsKey(key))
				{
					dict.Add(key, current);
				}

				return dict;
			}).Values;

			foreach(var record in mapRecords)
			{
				var (classMethod, interfaceMethod) = BuildMapMethod(record);
				classBuilder.AppendLine(classMethod);
				interfaceBuilder.AppendLine(interfaceMethod);
			}

			return (classBuilder.ToString(), interfaceBuilder.ToString());
		}

		protected (string classMethod, string interfaceMethod) BuildMapMethod(MappingRecord record)
		{
			var classBuilder = new StringBuilder();

			var sourceMembers = record.Source.GetMembers();
			var destinationMembers = record.Destination.GetMembers();

			var methodSignature = $"{record.Destination.ToDisplayString()} To{record.Destination.ToDisplayString().Replace(".", "_")}({record.Source.ToDisplayString()} source)";
			string interfaceMethod = $@"
		/// <summary>Convert from {record.Source.ToDisplayString()} to {record.Destination.ToDisplayString()}</summary>
		{methodSignature};
";

			classBuilder.AppendLine($@"
		public virtual {methodSignature} => new {record.Destination.ToDisplayString()}
		{{
");

			var assignments = new List<string>();
			foreach(var member in record.Members)
			{
				assignments.Add($"{member.destinationMember} = source.{member.sourceMember}");
			}

			classBuilder.AppendLine($@"
			{string.Join(@",
", assignments)}
		}};
");

			return (classBuilder.ToString(), interfaceMethod);
		}

		protected IEnumerable<MappingRecord> GetMapToRecords(INamedTypeSymbol sourceSymbol, INamedTypeSymbol mapToAttributeSymbol, INamedTypeSymbol mapMemberToAttributeSymbol)
		{
			var classAttributes = sourceSymbol.GetAttributes().Where(a => a.AttributeClass.Equals(mapToAttributeSymbol, SymbolEqualityComparer.Default));
			var records = new List<MappingRecord>();

			var sourceMembers = sourceSymbol.GetMembers();

			foreach(var classAttribute in classAttributes)
			{
				var destinationArgument = classAttribute.ConstructorArguments.First();
				var destinationSymbol = (destinationArgument.Value as INamedTypeSymbol).OriginalDefinition;
				var destinationMembers = destinationSymbol.GetMembers();

				bool reverse = ((bool?)classAttribute.NamedArguments.FirstOrDefault(kv => kv.Key == "Reverse").Value.Value) == true;

				var members = MapMembers(sourceMembers, destinationSymbol, destinationMembers, mapMemberToAttributeSymbol);

				records.Add(new MappingRecord
				{
					Source = sourceSymbol,
					Destination = destinationSymbol,
					Members = members
				});

				if (reverse)
				{
					records.Add(new MappingRecord
					{
						Source = destinationSymbol,
						Destination = sourceSymbol,
						Members = members.Select(t => (t.destination, t.source))
					});
				}
			}

			return records;
		}

		protected IEnumerable<MappingRecord> GetMapFromRecords(INamedTypeSymbol sourceSymbol, INamedTypeSymbol mapFromAttributeSymbol, INamedTypeSymbol mapMemberFromAttributeSymbol)
		{
			var classAttributes = sourceSymbol.GetAttributes().Where(a => a.AttributeClass.Equals(mapFromAttributeSymbol, SymbolEqualityComparer.Default));
			var records = new List<MappingRecord>();

			var sourceMembers = sourceSymbol.GetMembers();

			foreach (var classAttribute in classAttributes)
			{
				var destinationArgument = classAttribute.ConstructorArguments.First();
				var destinationSymbol = (destinationArgument.Value as INamedTypeSymbol).OriginalDefinition;
				var destinationMembers = destinationSymbol.GetMembers();

				var members = MapMembers(sourceMembers, destinationSymbol, destinationMembers, mapMemberFromAttributeSymbol);

				//var members = new List<(string source, string destination)>();

				//foreach (var sourceMember in sourceMembers)
				//{
				//	if (sourceMember.IsImplicitlyDeclared || sourceMember.IsStatic || sourceMember.DeclaredAccessibility != Accessibility.Public
				//	|| (sourceMember.Kind != SymbolKind.Field && sourceMember.Kind != SymbolKind.Property))
				//		continue;

				//	var memberAttribute = sourceMember.GetAttributes().FirstOrDefault(a =>
				//	{
				//		var destinationAttribute = a.ConstructorArguments.First();
				//		var symbol = destinationAttribute.Value as INamedTypeSymbol;

				//		return a.AttributeClass.Equals(mapMemberFromAttributeSymbol, SymbolEqualityComparer.Default)
				//			&& symbol.Equals(destinationSymbol, SymbolEqualityComparer.Default);
				//	});

				//	if (memberAttribute != null)
				//	{
				//		var name = memberAttribute.NamedArguments.FirstOrDefault(na => na.Key == "Name").Value.Value as string;

				//		members.Add((name, sourceMember.Name));
				//	}
				//	else if (destinationmembers.Any(dm => dm.Name == sourceMember.Name))
				//	{
				//		members.Add((sourceMember.Name, sourceMember.Name));
				//	}
				//}

				records.Add(new MappingRecord
				{
					Source = destinationSymbol,
					Destination = sourceSymbol,
					Members = members.Select(t => (t.destination, t.source))
				});
			}

			return records;
		}

		protected IEnumerable<(string source, string destination)> MapMembers(IEnumerable<ISymbol> sourceMembers, INamedTypeSymbol destinationSymbol, IEnumerable<ISymbol> destinationMembers, INamedTypeSymbol memberAttributeSymbol)
		{
			var members = new List<(string source, string destination)>();

			foreach (var sourceMember in sourceMembers)
			{
				if (sourceMember.IsImplicitlyDeclared || sourceMember.IsStatic || sourceMember.DeclaredAccessibility != Accessibility.Public
				|| (sourceMember.Kind != SymbolKind.Field && sourceMember.Kind != SymbolKind.Property))
					continue;

				var memberAttribute = sourceMember.GetAttributes().FirstOrDefault(a =>
				{
					var destinationAttribute = a.ConstructorArguments.First();
					var symbol = destinationAttribute.Value as INamedTypeSymbol;

					return a.AttributeClass.Equals(memberAttributeSymbol, SymbolEqualityComparer.Default)
						&& symbol.Equals(destinationSymbol, SymbolEqualityComparer.Default);
				});

				if (memberAttribute != null)
				{
					var name = memberAttribute.NamedArguments.FirstOrDefault(na => na.Key == "Name").Value.Value as string;

					members.Add((sourceMember.Name, name));
				}
				else if (destinationMembers.Any(dm => dm.Name == sourceMember.Name))
				{
					members.Add((sourceMember.Name, sourceMember.Name));
				}
			}

			return members;
		}
	}
}

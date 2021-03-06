using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Text;

namespace FireMap
{
	internal class SyntaxReceiver : ISyntaxReceiver
	{
		public IEnumerable<TypeDeclarationSyntax> Classes => ClassList;
		protected HashSet<TypeDeclarationSyntax> ClassList { get; } = new HashSet<TypeDeclarationSyntax>();

		public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
		{
			if (syntaxNode is TypeDeclarationSyntax typeDeclarationSyntax
				&& typeDeclarationSyntax.AttributeLists.Count > 0)
			{
				ClassList.Add(typeDeclarationSyntax);
			}
			else if (syntaxNode is PropertyDeclarationSyntax propertyDeclarationSyntax
				&& propertyDeclarationSyntax.AttributeLists.Count > 0
				&& propertyDeclarationSyntax.Parent != null
				&& propertyDeclarationSyntax.Parent is TypeDeclarationSyntax propertyClassDeclarationSyntax)
			{
				ClassList.Add(propertyClassDeclarationSyntax);
			}
			else if (syntaxNode is PropertyDeclarationSyntax fieldDeclarationSyntax
				&& fieldDeclarationSyntax.AttributeLists.Count > 0
				&& fieldDeclarationSyntax.Parent != null
				&& fieldDeclarationSyntax.Parent is TypeDeclarationSyntax fieldClassDeclarationSyntax)
			{
				ClassList.Add(fieldClassDeclarationSyntax);
			}
		}
	}
}

using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace TestCategoryManager
{
    public class TestCategoryAdder : CSharpSyntaxRewriter
    {
        private readonly string _category;

        public TestCategoryAdder(string category)
        {
            _category = category;
        }

        public override SyntaxNode VisitMethodDeclaration(MethodDeclarationSyntax node)
        {
            if (node.IsTestMethod() & !node.HasTestCategory(_category))
            {
                Console.WriteLine($"-> Adding {_category} category to method {node.Identifier}");
                return node.AddCategoryAttribute(_category);
            }

            return node;
        }
    }
}
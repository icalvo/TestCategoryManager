using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace TestCategoryManager
{
    public static class SyntaxExtensions
    {
        public static bool IsTestMethod(this BaseMethodDeclarationSyntax @this)
        {
            var attributeLists = @this.AttributeLists.AsEnumerable() ?? Enumerable.Empty<AttributeListSyntax>();
            var attributes = attributeLists.SelectMany(x => x.Attributes);
            return attributes.Any(x => x.Name.ToString() == "TestMethod");
        }

        public static bool HasTestCategory(this BaseMethodDeclarationSyntax @this, string category)
        {
            var attributeLists = @this.AttributeLists.AsEnumerable() ?? Enumerable.Empty<AttributeListSyntax>();
            var attributes = attributeLists.SelectMany(x => x.Attributes);
            return attributes.Any(x => x.IsTestCategory(category));
        }

        public static bool IsTestCategory(this AttributeSyntax @this, string category)
        {
            return @this.Name.ToString() == "TestCategory" && @this.ArgumentIsCategory(category);
        }

        private static bool ArgumentIsCategory(this AttributeSyntax @this, string category)
        {
            var literal = SyntaxFactory.LiteralExpression(
                SyntaxKind.StringLiteralExpression,
                SyntaxFactory.Literal(category));
            var arg = @this.ArgumentList.Arguments.First();
            return arg.Expression.ToString() == literal.ToString();
        }

        public static MethodDeclarationSyntax AddCategoryAttribute(
            this MethodDeclarationSyntax @this,
            string category)
        {
            var newTestCategoryAttribute = SyntaxFactory.AttributeList(
                SyntaxFactory.SingletonSeparatedList(
                    SyntaxFactory.Attribute(
                            SyntaxFactory.IdentifierName("TestCategory"))
                        .WithArgumentList(
                            SyntaxFactory.AttributeArgumentList(
                                SyntaxFactory.SingletonSeparatedList(
                                    SyntaxFactory.AttributeArgument(
                                        SyntaxFactory.LiteralExpression(
                                            SyntaxKind.StringLiteralExpression,
                                            SyntaxFactory.Literal(category)))))))).NormalizeWhitespace();
            var attributes =
                @this.AttributeLists.Add(newTestCategoryAttribute);

            return @this.WithAttributeLists(attributes);
        }

        public static SyntaxList<T> ToSyntaxList<T>(this IEnumerable<T> @this) where T : SyntaxNode
        {
            return new SyntaxList<T>().AddRange(@this);
        }
    }
}
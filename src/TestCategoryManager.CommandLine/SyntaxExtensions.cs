using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace TestCategoryManager
{
    public static class SyntaxExtensions
    {
        public static bool IsTestMethod(this BaseMethodDeclarationSyntax @this)
        {
            return (
                from attributeList in @this.AttributeLists
                from attribute in attributeList.Attributes
                where attribute.Name.ToString() == "TestMethod"
                select 0)
                .Any();
        }

        public static bool HasTestCategory(this BaseMethodDeclarationSyntax @this, string category)
        {
            var attributeLists = @this.AttributeLists.AsEnumerable() ?? Enumerable.Empty<AttributeListSyntax>();
            var attributes = attributeLists.SelectMany(x => x.Attributes);
            return attributes.Any(x => x.IsTestCategory(category));
        }

        private static bool IsTestCategory(this AttributeSyntax @this, string category)
        {
            return
                @this.Name.ToString() == "TestCategory" &&
                @this.ArgumentList.Arguments.Count == 1 &&
                @this.ArgumentList.Arguments.Single().ArgumentIsCategory(category);
        }

        public static AttributeSyntax[] TestCategoryAttributes(this AttributeListSyntax @this, string category)
        {
            return
                @this
                    .Attributes
                    .Where(attribute => attribute.IsTestCategory(category))
                    .ToArray();
        }

        private static bool ArgumentIsCategory(this AttributeArgumentSyntax @this, string category)
        {
            return @this.Expression.ToString() == CategoryLiteral(category).ToString();
        }

        public static LiteralExpressionSyntax CategoryLiteral(string category)
        {
            var literal = LiteralExpression(
                SyntaxKind.StringLiteralExpression,
                Literal(category));
            return literal;
        }

        public static MethodDeclarationSyntax AddCategoryAttribute(
            this MethodDeclarationSyntax @this,
            string category)
        {
            var newTestCategoryAttribute = AttributeList(
                SingletonSeparatedList(
                    Attribute(
                            IdentifierName("TestCategory"))
                        .WithArgumentList(
                            AttributeArgumentList(
                                SingletonSeparatedList(
                                    AttributeArgument(
                                        CategoryLiteral(category))))))).NormalizeWhitespace();
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
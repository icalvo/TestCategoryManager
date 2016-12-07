using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace TestCategoryManager
{
    public class TestCategoryRenamer : CSharpSyntaxRewriter
    {
        private readonly string _category;
        private readonly string _newCategory;

        public TestCategoryRenamer(string category, string newCategory)
        {
            _category = category;
            _newCategory = newCategory;
        }

        public override SyntaxNode VisitMethodDeclaration(MethodDeclarationSyntax node)
        {
            if (!node.IsTestMethod())
            {
                return node;
            }

            var newAttributeLists =
                from attributeList in node.AttributeLists
                let attributesToRename = attributeList.TestCategoryAttributes(_category).Select(x => x.ArgumentList.Arguments.Single().Expression)
                select attributeList.ReplaceNodes(attributesToRename, (attr1, attr2) => SyntaxExtensions.CategoryLiteral(_newCategory));

            return node.WithAttributeLists(newAttributeLists.ToSyntaxList());
        }
    }
}
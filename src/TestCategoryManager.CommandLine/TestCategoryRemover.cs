using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace TestCategoryManager
{
    public class TestCategoryRemover : CSharpSyntaxRewriter
    {
        private readonly string _category;

        public TestCategoryRemover(string category)
        {
            _category = category;
        }

        public override SyntaxNode VisitMethodDeclaration(MethodDeclarationSyntax node)
        {
            if (!node.IsTestMethod())
            {
                return node;
            }

            var newAttributeLists = 
                from attributeList in node.AttributeLists
                let attributesToRemove = attributeList.TestCategoryAttributes(_category)
                where !AllAttributesWillBeRemoved(attributesToRemove, attributeList)
                select attributeList.RemoveNodes(attributesToRemove, SyntaxRemoveOptions.KeepNoTrivia);

            return node.WithAttributeLists(newAttributeLists.ToSyntaxList());
        }

        private static bool AllAttributesWillBeRemoved(AttributeSyntax[] nodesToRemove, AttributeListSyntax attributeList)
        {
            return nodesToRemove.Length == attributeList.Attributes.Count;
        }
    }
}
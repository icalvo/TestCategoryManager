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
                let attributesToRemove = AttributesToRemove(attributeList.Attributes)
                where !AllAttributesWillBeRemoved(attributesToRemove, attributeList)
                select attributeList.RemoveNodes(attributesToRemove, SyntaxRemoveOptions.KeepNoTrivia);

            return node.WithAttributeLists(newAttributeLists.ToSyntaxList());
        }



        private AttributeSyntax[] AttributesToRemove(SeparatedSyntaxList<AttributeSyntax> attributeList)
        {
            return (from attribute in attributeList
                    where attribute.IsTestCategory(_category)
                    select attribute)
                .ToArray();
        }

        private static bool AllAttributesWillBeRemoved(AttributeSyntax[] nodesToRemove, AttributeListSyntax attributeList)
        {
            return nodesToRemove.Length == attributeList.Attributes.Count;
        }
    }
}
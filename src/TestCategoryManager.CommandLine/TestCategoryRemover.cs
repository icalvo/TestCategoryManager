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

            var query = 
                from attributeList in node.AttributeLists
                let nodesToRemove =
                (from attribute in attributeList.Attributes
                        where attribute.IsTestCategory(_category)
                        select attribute)
                    .ToArray()
                where nodesToRemove.Length != attributeList.Attributes.Count
                select attributeList.RemoveNodes(nodesToRemove, SyntaxRemoveOptions.KeepNoTrivia);

            var newAttributes = new SyntaxList<AttributeListSyntax>();
            newAttributes.AddRange(query);
            return node.WithAttributeLists(newAttributes);
        }
    }
}
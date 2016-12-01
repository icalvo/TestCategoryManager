using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace TestCategoryManager
{
    internal static class Program
    {
        private static TestCategoryAdder _visitor;

        private static void Main(string[] args)
        {
            var workspace = MSBuildWorkspace.Create();
            MainAsync(args[0], args[1], args[2], workspace).GetAwaiter().GetResult();
        }

        private static async Task MainAsync(string verb, string solutionFileName, string category, MSBuildWorkspace workspace)
        {
            switch (verb)
            {
                case "add":
                    _visitor = new TestCategoryAdder(category);
                    break;
                case "del":
                case "delete":
                    break;
                default:
                    throw new Exception();
            }

            Solution originalSolution = await workspace.OpenSolutionAsync(solutionFileName);
            var newSolution =
                await originalSolution.ReplaceDocumentsAsync(
                    (solution, id, repl) => ProcessDocumentAsync(solution, id, category, repl));

            // Actually apply the accumulated changes and save them to disk. At this point
            // workspace.CurrentSolution is updated to point to the new solution.
            if (workspace.TryApplyChanges(newSolution))
            {
                Console.WriteLine("Solution updated.");
            }
            else
            {
                Console.WriteLine("Update failed!");
            }

            Console.ReadLine();
        }

        private static Task<Solution> ReplaceDocumentsAsync(
            this Solution @this,
            Func<Solution, DocumentId, CancellationToken, Task<Solution>> replacement)
        {
            return @this.ProjectIds
                .AggregateAsync(
                    @this,
                    (currentSolution, projectId) =>
                        currentSolution.GetProject(projectId).DocumentIds
                            .AggregateAsync(currentSolution, replacement));
        }

        private static async Task<Solution> ProcessDocumentAsync(Solution solution, DocumentId documentId, string category, CancellationToken token)
        {
            Document document = solution.GetDocument(documentId);
            Console.WriteLine($"Processing {document.FilePath}");
            var root = await document.GetSyntaxRootAsync(token);
            var newRoot = _visitor.Visit(root);
            var newDocument = await Formatter.FormatAsync(document.WithSyntaxRoot(newRoot), cancellationToken: token);
            return newDocument.Project.Solution;
        }

        private class TestCategoryAdder : CSharpSyntaxRewriter
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

        private static bool IsTestMethod(this BaseMethodDeclarationSyntax @this)
        {
            var attributeLists = @this.AttributeLists.AsEnumerable() ?? Enumerable.Empty<AttributeListSyntax>();
            var attributes = attributeLists.SelectMany(x => x.Attributes);
            return attributes.Any(x => x.Name.ToString() == "TestMethod");
        }

        private static bool HasTestCategory(this BaseMethodDeclarationSyntax @this, string category)
        {
            var attributeLists = @this.AttributeLists.AsEnumerable() ?? Enumerable.Empty<AttributeListSyntax>();
            var attributes = attributeLists.SelectMany(x => x.Attributes);
            return attributes.Any(x => x.IsTestCategory(category));
        }

        private static bool IsTestCategory(this AttributeSyntax @this, string category)
        {
            return @this.Name.ToString() == "TestCategory" && @this.ArgumentIsCategory(category);
        }

        private static bool ArgumentIsCategory(this AttributeSyntax @this, string category)
        {
            var literal = LiteralExpression(
                SyntaxKind.StringLiteralExpression,
                Literal(category));
            var arg = @this.ArgumentList.Arguments.First();
            return arg.Expression.ToString() == literal.ToString();
        }

        private static MethodDeclarationSyntax AddCategoryAttribute(
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
                                        LiteralExpression(
                                            SyntaxKind.StringLiteralExpression,
                                            Literal(category)))))))).NormalizeWhitespace();
            var attributes =
                @this.AttributeLists.Add(newTestCategoryAttribute);

            return @this.WithAttributeLists(attributes);
        }

        private static bool IsMsTestTestClassAttribute(SyntaxNodeAnalysisContext context, AttributeSyntax attribute)
        {
            var memberSymbol =
                context.SemanticModel.GetSymbolInfo(attribute).Symbol as IMethodSymbol;
            const string testClassAttributeName =
                "Microsoft.VisualStudio.TestTools.UnitTesting.TestClassAttribute.TestClassAttribute()";
            return memberSymbol?.ToString() == testClassAttributeName;
        }
    }


    public static class AsynchronousEnumerable
    {
        public static Task<TResult> AggregateAsync<TSource, TResult>(
            this IEnumerable<TSource> source,
            TResult seed,
            Func<TResult, TSource, Task<TResult>> func)
        {
            return source.AggregateAsync(seed, CancellationToken.None, (result, item, token) => func(result, item));
        }

        public static Task<TResult> AggregateAsync<TSource, TResult>(
            this IEnumerable<TSource> source,
            TResult seed,
            Func<TResult, TSource, CancellationToken, Task<TResult>> func)
        {
            return source.AggregateAsync(seed, CancellationToken.None, func);
        }

        public static async Task<TResult> AggregateAsync<TSource, TResult>(
            this IEnumerable<TSource> source,
            TResult seed,
            CancellationToken token,
            Func<TResult, TSource, CancellationToken, Task<TResult>> func)
        {
            TResult result = seed;
            foreach (var item in source)
            {
                result = await func(result, item, token);
            }

            return result;
        }
    }
}
using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Formatting;

namespace TestCategoryManager
{
    internal static class Program
    {
        private static CSharpSyntaxRewriter _visitor;

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
                case "rm":
                    _visitor = new TestCategoryRemover(category);
                    break;
                case "ren":
                case "rename":
                case "mv":
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
        
        //private static bool IsMsTestTestClassAttribute(SyntaxNodeAnalysisContext context, AttributeSyntax attribute)
        //{
        //    var memberSymbol =
        //        context.SemanticModel.GetSymbolInfo(attribute).Symbol as IMethodSymbol;
        //    const string testClassAttributeName =
        //        "Microsoft.VisualStudio.TestTools.UnitTesting.TestClassAttribute.TestClassAttribute()";
        //    return memberSymbol?.ToString() == testClassAttributeName;
        //}
    }
}
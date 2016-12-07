using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.MSBuild;

namespace TestCategoryManager
{
    public sealed class SolutionRewriteProgram : ConsoleProgram
    {
        private readonly string[] _expectedVerbs;
        private readonly Func<string[], CSharpSyntaxRewriter> _rewriter;
        private string[] _args;

        public SolutionRewriteProgram(Func<string[], CSharpSyntaxRewriter> rewriter, params string[] expectedVerbs)
        {
            _expectedVerbs = expectedVerbs;
            _rewriter = rewriter;
        }

        protected CSharpSyntaxRewriter Rewriter(string[] args)
        {
            return _rewriter(args);
        }

        public override bool Matches(string[] request)
        {
            return _expectedVerbs.Contains(request[0]);
        }


        protected override async Task ExecuteAsync(string[] args)
        {
            _args = args;
            var workspace = MSBuildWorkspace.Create();
            var solutionFileName = args[1];
            Solution originalSolution = await workspace.OpenSolutionAsync(solutionFileName);
            Solution newSolution = await ReplaceDocumentsAsync(originalSolution, ProcessDocumentAsync);

            if (workspace.TryApplyChanges(newSolution))
            {
                Console.WriteLine("Solution updated.");
            }
            else
            {
                Console.WriteLine("Update failed!");
            }
        }

        private static Task<Solution> ReplaceDocumentsAsync(
            Solution @this,
            Func<Solution, DocumentId, CancellationToken, Task<Solution>> replacement)
        {
            return @this.ProjectIds
                .AggregateAsync(
                    @this,
                    (currentSolution, projectId) =>
                        currentSolution.GetProject(projectId).DocumentIds
                            .AggregateAsync(currentSolution, replacement));
        }

        private async Task<Solution> ProcessDocumentAsync(Solution solution, DocumentId documentId, CancellationToken token)
        {
            Document document = solution.GetDocument(documentId);
            Console.WriteLine($"Processing {document.FilePath}");
            var root = await document.GetSyntaxRootAsync(token);
            var newRoot = Rewriter(_args).Visit(root);
            var newDocument = await Formatter.FormatAsync(document.WithSyntaxRoot(newRoot), cancellationToken: token);
            return newDocument.Project.Solution;
        }
    }
}
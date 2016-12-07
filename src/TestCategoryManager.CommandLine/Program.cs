using System;
using System.Collections.Generic;
using System.Linq;

namespace TestCategoryManager
{
    internal static class Program
    {
        private static void Main(string[] args)
        {
            IProgram<string[]>[] programs =
            {
                new SolutionRewriteProgram(a => new TestCategoryAdder(a[2]), "add"),
                new SolutionRewriteProgram(a => new TestCategoryRemover(a[2]), "del", "delete", "rm"),
                new SolutionRewriteProgram(a => new TestCategoryRenamer(a[2], a[3]), "ren", "mv"),
                new FailProgram(),
            };

            programs.LinkChainOfResponsibility();
            var root = programs[0];
            root.Execute(args);
            Console.ReadLine();
        }

        private static void LinkChainOfResponsibility(this IProgram<string[]>[] programs)
        {
            var root = programs[0];
            programs.Aggregate(
                root,
                (current, suc) =>
                {
                    current.Successor = suc;
                    return suc;
                });
        }
    }
}
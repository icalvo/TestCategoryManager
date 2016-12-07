using System;

namespace TestCategoryManager
{
    public class FailProgram : IProgram<string[]>
    {
        public IProgram<string[]> Successor { get; set; }

        public void Execute(string[] request)
        {
            throw new Exception("End of chain!!!");
        }
    }
}
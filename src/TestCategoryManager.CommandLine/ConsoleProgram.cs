using System.Threading.Tasks;

namespace TestCategoryManager
{
    public abstract class ConsoleProgram : IProgram<string[]>
    {
        protected ConsoleProgram()
        {
        }

        public IProgram<string[]> Successor { get; set; }

        public abstract bool Matches(string[] request);

        void IProgram<string[]>.Execute(string[] request)
        {
            if (Matches(request))
            {
                ExecuteAsync(request).GetAwaiter().GetResult();
            }
            else
            {
                Successor?.Execute(request);
            }
        }

        protected abstract Task ExecuteAsync(string[] args);
    }
}
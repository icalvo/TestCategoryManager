namespace TestCategoryManager
{
    public interface IProgram<TRequest>
    {
        IProgram<TRequest> Successor { set; }

        void Execute(TRequest request);
    }
}
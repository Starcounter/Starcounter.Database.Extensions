namespace Starcounter.Database.Extensions
{
    public static class ContextSponsor
    {
        public static bool IsNested(this IDatabaseContext context) => context is NestedTransactor.NestedTransactionContext;
    }
}
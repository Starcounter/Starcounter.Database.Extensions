using System;

namespace Starcounter.Database.Extensions
{
    public static class ContextSponsor
    {
        /// <summary>
        /// Returns a value indicating if the current context represent a
        /// transaction that is nested within an outer transaction.
        /// <remarks>
        /// When using multiple custom transactors, make sure to use the
        /// <c>NestedTransactor</c> as the most outer one in the decoration
        /// chain for this method to properly work.
        /// </remarks>
        /// </summary>
        public static bool IsNested(this IDatabaseContext context) 
            => context is IServiceProvider sp 
                ? sp.GetService(typeof(NestedTransactor.NestedTransactionContext)) != null 
                : false;
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Options;

namespace Starcounter.Database.Extensions
{
    public class PostCommitTransactor : OnCommitTransactor<PostCommitTransactorContext>
    {
        readonly PostCommitOptions _hookOptions;

        public PostCommitTransactor(ITransactor transactor, IOptions<PostCommitOptions> postCommitHookOptions)
            : base(transactor)
            => _hookOptions = postCommitHookOptions.Value;

        protected override void LeaveDatabaseContext(PostCommitTransactorContext transactorContext, IDatabaseContext db, bool exceptionThrown)
        {
            // The IEnumerable has to be materialized here, because iterating over the transactions changes require database access.
            transactorContext.Hooks = SelectHooks(db, _hookOptions.Delegates).ToList();
        }

        protected override PostCommitTransactorContext EnterTransactorContext() => new PostCommitTransactorContext();

        protected override void LeaveTransactorContext(PostCommitTransactorContext transactorContext)
        {
            foreach (var hook in transactorContext.Hooks)
            {
                hook.Action(hook.Change);
            }
        }
    }
}

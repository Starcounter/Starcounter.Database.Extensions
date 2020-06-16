using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Options;
using Starcounter.Database.ChangeTracking;

namespace Starcounter.Database.Extensions
{
    public class PostCommitTransactor : TransactorBase<PostCommitTransactorContext>
    {
        readonly PostCommitOptions _hookOptions;

        public PostCommitTransactor(ITransactor transactor, IOptions<PostCommitOptions> postCommitHookOptions)
            : base(transactor)
            => _hookOptions = postCommitHookOptions.Value;

        protected override void LeaveDatabaseContext(PostCommitTransactorContext transactorContext, IDatabaseContext db, bool exceptionThrown)
        {
            transactorContext.Changes = new List<KeyValuePair<Type, Change>>();

            foreach (var change in db.ChangeTracker.Changes.Where(c => c.Type != ChangeType.Delete))
            {
                var realType = db.GetRealType(change.Oid);

                transactorContext.Changes.Add(new KeyValuePair<Type, Change>(realType, change));
            }
        }

        protected override PostCommitTransactorContext EnterTransactorContext() => new PostCommitTransactorContext();

        protected override void LeaveTransactorContext(PostCommitTransactorContext transactorContext)
        {
            if (transactorContext.Changes?.Any() != true)
            {
                return;
            }

            ExecutePostCommitHooks(transactorContext.Changes);
        }

        protected virtual void ExecutePostCommitHooks(List<KeyValuePair<Type, Change>> changes)
        {
            foreach (var change in changes)
            {
                if (_hookOptions.Delegates.TryGetValue(change.Key, out Action<Change> action))
                {
                    action(change.Value);
                }
            }
        }
    }
}

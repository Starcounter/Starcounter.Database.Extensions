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

        protected override void LeaveContext(PostCommitTransactorContext transactorContext, IDatabaseContext db, bool exceptionThrown)
        {
            transactorContext.Changes = new List<KeyValuePair<Type, Change>>();

            foreach (var change in db.ChangeTracker.Changes.Where(c => c.Type != ChangeType.Delete))
            {
                var proxy = db.Get<object>(change.Oid);
                var realType = proxy.GetType().BaseType;

                transactorContext.Changes.Add(new KeyValuePair<Type, Change>(realType, change));
            }
        }

        protected override void LeftContext(PostCommitTransactorContext transactorContext)
        {
            if (transactorContext.Changes?.Any() != true)
            {
                return;
            }

            ExecutePostCommitHooks(transactorContext.Changes, _hookOptions);
        }

        protected virtual void ExecutePostCommitHooks(List<KeyValuePair<Type, Change>> changes, PostCommitOptions options)
        {
            foreach (var change in changes)
            {
                if (options.Delegates.TryGetValue(change.Key, out Action<Change> action))
                {
                    action(change.Value);
                }
            }
        }

        protected override PostCommitTransactorContext CreateTransactorContext() => new PostCommitTransactorContext();
    }
}

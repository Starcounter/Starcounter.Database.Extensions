using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Starcounter.Database.ChangeTracking;

namespace Starcounter.Database.Extensions
{
    public class PostCommitTransactor : TransactorBase
    {
        protected AsyncLocal<List<KeyValuePair<Type, Change>>> _lastChanges = new AsyncLocal<List<KeyValuePair<Type, Change>>>();
        protected readonly PostCommitOptions _hookOptions;

        public PostCommitTransactor(ITransactor transactor, IOptions<PostCommitOptions> postCommitHookOptions)
            : base(transactor)
            => _hookOptions = postCommitHookOptions.Value;

        protected override void LeaveContext(IDatabaseContext db, bool exceptionThrown)
        {
            List<KeyValuePair<Type, Change>> changes = new List<KeyValuePair<Type, Change>>();

            foreach (var change in db.ChangeTracker.Changes.Where(c => c.Type != ChangeType.Delete))
            {
                var proxy = db.Get<object>(change.Oid);
                var realType = proxy.GetType().BaseType;

                changes.Add(new KeyValuePair<Type, Change>(realType, change));
            }

            _lastChanges.Value = changes;
        }

        protected override void LeftContext()
        {
            ExecutePostCommitHooks(_hookOptions);
            _lastChanges.Value = null;
        }

        protected virtual void ExecutePostCommitHooks(PostCommitOptions options)
        {
            foreach (var change in _lastChanges.Value)
            {
                if (options.Delegates.TryGetValue(change.Key, out Action<Change> action))
                {
                    Task.Run(() => action(change.Value));
                }
            }
        }
    }
}

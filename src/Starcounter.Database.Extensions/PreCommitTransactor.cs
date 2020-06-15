using System;
using Starcounter.Database.ChangeTracking;
using System.Linq;
using Microsoft.Extensions.Options;

namespace Starcounter.Database.Extensions
{
    public class PreCommitTransactor : TransactorBase<ITransactorContext>
    {
        readonly PreCommitOptions hookOptions;

        public PreCommitTransactor(ITransactor transactor, IOptions<PreCommitOptions> preCommitHookOptions)
            : base(transactor)
            => hookOptions = preCommitHookOptions.Value;

        protected override void LeaveContext(ITransactorContext transactorContext, IDatabaseContext db, bool exceptionThrown)
        {
            if (!exceptionThrown)
            {
                ExecutePreCommitHooks(db, hookOptions);
            }
        }

        protected virtual void ExecutePreCommitHooks(IDatabaseContext db, PreCommitOptions options)
        {
            foreach (var change in db.ChangeTracker.Changes.Where(c => c.Type != ChangeType.Delete))
            {
                var proxy = db.Get<object>(change.Oid);
                var realType = proxy.GetType().BaseType;

                if (options.Delegates.TryGetValue(realType, out Action<IDatabaseContext, Change> action))
                {
                    action(db, change);
                }
            }
        }
    }
}
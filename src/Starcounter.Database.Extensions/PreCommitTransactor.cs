using System;
using Starcounter.Database.ChangeTracking;
using System.Linq;
using Microsoft.Extensions.Options;

namespace Starcounter.Database.Extensions
{
    public class PreCommitTransactor : TransactorBase<object>
    {
        readonly PreCommitOptions _hookOptions;

        public PreCommitTransactor(ITransactor transactor, IOptions<PreCommitOptions> preCommitHookOptions)
            : base(transactor)
            => _hookOptions = preCommitHookOptions.Value;

        protected override void LeaveDatabaseContext(object transactorContext, IDatabaseContext db, bool exceptionThrown)
        {
            if (!exceptionThrown)
            {
                ExecutePreCommitHooks(db);
            }
        }

        protected virtual void ExecutePreCommitHooks(IDatabaseContext db)
        {
            foreach (var change in db.ChangeTracker.Changes.Where(c => c.Type != ChangeType.Delete))
            {
                var realType = db.GetRealType(change.Oid);

                if (_hookOptions.Delegates.TryGetValue(realType, out Action<IDatabaseContext, Change> action))
                {
                    action(db, change);
                }
            }
        }
    }
}
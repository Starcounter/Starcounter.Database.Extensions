using System;
using Starcounter.Database.ChangeTracking;
using System.Linq;
using Microsoft.Extensions.Options;

namespace Starcounter.Database.Extensions
{
    public class PreCommitTransactor : OnCommitTransactor<object>
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
            foreach ((Change Change, Action<IDatabaseContext, Change> Action) item in SelectHooks(db, _hookOptions.Delegates))
            {
                item.Action(db, item.Change);
            }
        }
    }
}
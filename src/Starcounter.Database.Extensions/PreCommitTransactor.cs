using System;
using Starcounter.Database.ChangeTracking;
using System.Linq;
using Microsoft.Extensions.Options;

namespace Starcounter.Database.Extensions
{
    public class PreCommitTransactor : TransactorBase
    {
        class PreCommitContext : ContextBase
        {
            public PreCommitContext(IDatabaseContext context) : base(context) { }

            public void ExecutePreCommitHooks(PreCommitOptions options)
            {
                foreach (var change in ChangeTracker.Changes.Where(c => c.Type != ChangeType.Delete))
                {
                    var proxy = Get<object>(change.Oid);
                    var realType = proxy.GetType().BaseType;

                    if (options.Delegates.TryGetValue(realType, out Action<IDatabaseContext, Change> action))
                    {
                        action(this, change);
                    }
                }
            }
        }

        readonly PreCommitOptions hookOptions;

        public PreCommitTransactor(ITransactor transactor, IOptions<PreCommitOptions> preCommitHookOptions)
            : base(transactor)
            => hookOptions = preCommitHookOptions.Value;

        protected override IDatabaseContext EnterContext(IDatabaseContext db) => new PreCommitContext(db);

        protected override void LeaveContext(IDatabaseContext db, bool exceptionThrown)
        {
            if (!exceptionThrown)
            {
                ((PreCommitContext)db).ExecutePreCommitHooks(hookOptions);
            }
        }
    }
}
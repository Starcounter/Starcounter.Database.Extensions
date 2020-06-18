using System;
using Microsoft.Extensions.Options;

namespace Starcounter.Database.Extensions
{
    public class OnCommitTransactor<TUserContextType> : TransactorBase<OnCommitTransactorContext<TUserContextType>>
    {
        readonly OnCommitTransactorOptions<TUserContextType> _options;

        public OnCommitTransactor(ITransactor transactor, IOptions<OnCommitTransactorOptions<TUserContextType>> options) : base(transactor)
        {
            _options = options.Value;
        }

        protected override OnCommitTransactorContext<TUserContextType> EnterTransactorContext()
        {
            return new OnCommitTransactorContext<TUserContextType>();
        }

        protected override void LeaveDatabaseContext(OnCommitTransactorContext<TUserContextType> transactorContext, IDatabaseContext db, bool exceptionThrown)
        {
            if (!exceptionThrown)
            {
                transactorContext.UserContext = _options.OnBeforeCommit(db);
            }
        }

        protected override void LeaveTransactorContext(OnCommitTransactorContext<TUserContextType> transactorContext)
        {
            _options.OnAfterCommit(transactorContext.UserContext);
        }
    }
}

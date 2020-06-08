using System;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;

namespace Starcounter.Database.Extensions
{
    /// <summary>
    /// Custom transactor that allow nesting of transactions, and running the
    /// given delegate as a part of an other transaction, if given to a Transact
    /// call within an already executing transaction.
    /// <remarks>
    /// When using multiple custom transactors, it is recommended to use the
    /// <c>NestedTransactor</c> as the most outer one in the decoration chain. 
    /// </remarks>
    /// </summary>
    public class NestedTransactor : TransactorBase, INestedTransactor
    {
        class NestingContext
        {
            public readonly IDatabaseContext DatabaseContext;

            public Exception InnerException;

            public NestingContext(IDatabaseContext context)
                => DatabaseContext = context ?? throw new ArgumentNullException(nameof(context));
        }

        AsyncLocal<NestingContext> _current = new AsyncLocal<NestingContext>();

        internal class NestedTransactionContext : ContextBase
        {
            public NestedTransactionContext(IDatabaseContext inner) : base(inner) { }
        }

        public NestedTransactor(ITransactor innerTransactor) : base(innerTransactor) { }

        public override void Transact(Action<IDatabaseContext> action, TransactOptions options = null)
        {
            IDatabaseContext nested = _current.Value?.DatabaseContext;

            if (nested == null)
            {
                base.Transact(action, options);
                RestartIfWithinReadOnlyTransaction();
                return;
            }

            var context = new NestedTransactionContext(nested);

            try
            {
                action(context);
                FailOuterIfInnerFailed();
            }
            catch (Exception ex)
            {
                CaptureException(ex);
                throw;
            }
        }

        public override T Transact<T>(Func<IDatabaseContext, T> function, TransactOptions options = null)
        {
            IDatabaseContext nested = _current.Value?.DatabaseContext;

            if (nested == null)
            {
                var r = base.Transact(function, options);
                RestartIfWithinReadOnlyTransaction(); ;
                return r;
            }

            var context = new NestedTransactionContext(nested);

            try
            {
                T t = function(context);
                FailOuterIfInnerFailed();
                return t;
            }
            catch (Exception ex)
            {
                CaptureException(ex);
                throw;
            }
        }

        public override async Task TransactAsync(Action<IDatabaseContext> action, TransactOptions options = null)
        {
            IDatabaseContext nested = _current.Value?.DatabaseContext;

            if (nested == null)
            {
                await base.TransactAsync(action, options);
                RestartIfWithinReadOnlyTransaction();
                return;
            }

            var context = new NestedTransactionContext(nested);

            try
            {
                action(context);
                FailOuterIfInnerFailed();
            }
            catch (Exception ex)
            {
                CaptureException(ex);
                throw;
            }
        }

        public override async Task<T> TransactAsync<T>(Func<IDatabaseContext, T> function, TransactOptions options = null)
        {
            IDatabaseContext nested = _current.Value?.DatabaseContext;

            if (nested == null)
            {
                var r = await base.TransactAsync(function, options);
                RestartIfWithinReadOnlyTransaction();
                return r;
            }

            var context = new NestedTransactionContext(nested);

            try
            {
                T t = function(context);
                FailOuterIfInnerFailed();
                return t;
            }
            catch (Exception ex)
            {
                CaptureException(ex);
                throw;
            }
        }

        public override async Task TransactAsync(Func<IDatabaseContext, Task> function, TransactOptions options = null)
        {
            IDatabaseContext nested = _current.Value?.DatabaseContext;

            if (nested == null)
            {
                await base.TransactAsync(function, options);
                RestartIfWithinReadOnlyTransaction();
                return;
            }

            var context = new NestedTransactionContext(nested);

            try
            {
                await function(context);
                FailOuterIfInnerFailed();
            }
            catch (Exception ex)
            {
                CaptureException(ex);
                throw;
            }
        }

        public override async Task<T> TransactAsync<T>(Func<IDatabaseContext, Task<T>> function, TransactOptions options = null)
        {
            IDatabaseContext nested = _current.Value?.DatabaseContext;

            if (nested == null)
            {
                var r = await base.TransactAsync(function, options);
                RestartIfWithinReadOnlyTransaction();
                return r;
            }

            var context = new NestedTransactionContext(nested);

            try
            {
                T t = await function(context);
                FailOuterIfInnerFailed();
                return t;
            }
            catch (Exception ex)
            {
                CaptureException(ex);
                throw;
            }
        }

        public override bool TryTransact(Action<IDatabaseContext> action, TransactOptions options = null)
        {
            IDatabaseContext nested = _current.Value?.DatabaseContext;

            if (nested == null)
            {
                var r = base.TryTransact(action, options);
                RestartIfWithinReadOnlyTransaction();
                return r;
            }

            var context = new NestedTransactionContext(nested);

            try
            {
                action(context);
                FailOuterIfInnerFailed();
                return true;
            }
            catch (Exception ex)
            {
                CaptureException(ex);
                throw;
            }
        }

        protected override IDatabaseContext EnterContext(IDatabaseContext db)
        {
            if (!IsWithinReadOnlyTransaction())
            {
                _current.Value = new NestingContext(db);
            }

            return db;
        }

        protected override void LeaveContext(IDatabaseContext db, bool exceptionThrown)
        {
            try
            {
                if (!exceptionThrown)
                {
                    FailOuterIfInnerFailed();
                }
            }
            finally
            {
                _current.Value = null;
            }
        }

        protected void CaptureException(Exception ex) => _current.Value.InnerException = ex;

        private void FailOuterIfInnerFailed()
        {
            if (_current.Value?.InnerException != null)
            {
                throw new TransactionAbortedException("Nested transaction failed", _current.Value.InnerException);
            }
        }

        private bool IsWithinReadOnlyTransaction()
        {
            return Data.Transaction.Current != null && (Data.Transaction.Current.Flags & TransactionFlags.ReadOnly) == TransactionFlags.ReadOnly;
        }

        private void RestartIfWithinReadOnlyTransaction()
        {
            if (IsWithinReadOnlyTransaction())
            {
                Data.Transaction.Current.Restart();
            }
        }
    }
}

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
            public NestingContext(IDatabaseContext context) => DatabaseContext = context ?? throw new ArgumentNullException(nameof(context));
            public Exception InnerException;
        }

        void FailOuterIfInnerFailed()
        {
            if (_current.Value.InnerException != null)
                throw new TransactionAbortedException("Nested transaction failed", _current.Value.InnerException);
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
                Rollback(context, ex);
                throw;
            }
        }

        public override T Transact<T>(Func<IDatabaseContext, T> function, TransactOptions options = null)
        {
            IDatabaseContext nested = _current.Value?.DatabaseContext;

            if (nested == null)
            {
                return base.Transact(function, options);
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
                Rollback(context, ex);
                throw;
            }
        }

        public override async Task TransactAsync(Action<IDatabaseContext> action, TransactOptions options = null)
        {
            IDatabaseContext nested = _current.Value?.DatabaseContext;

            if (nested == null)
            {
                await base.TransactAsync(action, options);
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
                Rollback(context, ex);
                throw;
            }
        }

        public override async Task<T> TransactAsync<T>(Func<IDatabaseContext, T> function, TransactOptions options = null)
        {
            IDatabaseContext nested = _current.Value?.DatabaseContext;

            if (nested == null)
            {
                return await base.TransactAsync(function, options);
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
                Rollback(context, ex);
                throw;
            }
        }

        public override async Task TransactAsync(Func<IDatabaseContext, Task> function, TransactOptions options = null)
        {
            IDatabaseContext nested = _current.Value?.DatabaseContext;

            if (nested == null)
            {
                await base.TransactAsync(function, options);
            }
            else
            {
                var context = new NestedTransactionContext(nested);

                try
                {
                    await function(context);
                    FailOuterIfInnerFailed();
                }
                catch (Exception ex)
                {
                    Rollback(context, ex);
                    throw;
                }
            }
        }

        public override async Task<T> TransactAsync<T>(Func<IDatabaseContext, Task<T>> function, TransactOptions options = null)
        {
            IDatabaseContext nested = _current.Value?.DatabaseContext;

            if (nested == null)
            {
                return await base.TransactAsync(function, options);
            }
            else
            {
                var context = new NestedTransactionContext(nested);

                try
                {
                    T t = await function(context);
                    FailOuterIfInnerFailed();
                    return t;
                }
                catch (Exception ex)
                {
                    Rollback(context, ex);
                    throw;
                }
            }
        }

        public override bool TryTransact(Action<IDatabaseContext> action, TransactOptions options = null)
        {
            IDatabaseContext nested = _current.Value?.DatabaseContext;

            if (nested == null)
            {
                return base.TryTransact(action, options);
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
                Rollback(context, ex);
                throw;
            }
        }

        protected override IDatabaseContext EnterContext(IDatabaseContext db)
        {
            _current.Value = new NestingContext(db);
            return db;
        }

        protected override void LeaveContext(IDatabaseContext db, bool exception_thrown)
        {
            try
            {
                if (!exception_thrown)
                    FailOuterIfInnerFailed();
            }
            finally
            {
                _current.Value = null;
            }
        }

        protected void Rollback(IDatabaseContext db, Exception ex)
        {
            db.Transaction.Rollback();
            _current.Value.InnerException = ex;
        }
    }
}

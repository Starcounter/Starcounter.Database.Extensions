using System;
using System.Threading;
using System.Threading.Tasks;

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
        AsyncLocal<IDatabaseContext> _current = new AsyncLocal<IDatabaseContext>();

        internal class NestedTransactionContext : ContextBase
        {
            public NestedTransactionContext(IDatabaseContext inner) : base(inner) { }
        }

        public NestedTransactor(ITransactor innerTransactor) : base(innerTransactor) { }

        public override void Transact(Action<IDatabaseContext> action, TransactOptions options = null)
        {
            var nested = _current.Value;
            if (nested != null)
            {
                var context = new NestedTransactionContext(nested);
                try
                {
                    action(context);
                    return;
                }
                catch
                {
                    Rollback(context);
                    throw;
                }
            }

            base.Transact(action, options);
        }

        public override T Transact<T>(Func<IDatabaseContext, T> function, TransactOptions options = null)
        {
            var nested = _current.Value;
            if (nested != null)
            {
                var context = new NestedTransactionContext(nested);
                try
                {
                    return function(context);
                }
                catch
                {
                    Rollback(context);
                    throw;
                }

            }

            return base.Transact(function, options);
        }

        public override async Task TransactAsync(Action<IDatabaseContext> action, TransactOptions options = null)
        {
            var nested = _current.Value;
            if (nested != null)
            {
                var context = new NestedTransactionContext(nested);
                try
                {
                    action(context);
                }
                catch
                {
                    Rollback(context);
                    throw;
                }
            }
            else
            {
                await base.TransactAsync(action, options);
            }
        }

        public override async Task<T> TransactAsync<T>(Func<IDatabaseContext, T> function, TransactOptions options = null)
        {
            var nested = _current.Value;
            if (nested != null)
            {
                var context = new NestedTransactionContext(nested);
                try
                {
                    return function(context);
                }
                catch
                {
                    Rollback(context);
                    throw;
                }
            }
            else
            {
                return await base.TransactAsync(function, options);
            }
        }

        public override Task TransactAsync(Func<IDatabaseContext, Task> function, TransactOptions options = null)
        {
            var nested = _current.Value;
            if (nested != null)
            {
                var context = new NestedTransactionContext(nested);
                try
                {
                    return function(context);
                }
                catch
                {
                    Rollback(context);
                    throw;
                }
            }

            return base.TransactAsync(function, options);
        }

        public override Task<T> TransactAsync<T>(Func<IDatabaseContext, Task<T>> function, TransactOptions options = null)
        {
            var nested = _current.Value;
            if (nested != null)
            {
                var context = new NestedTransactionContext(nested);
                try
                {
                    return function(context);
                }
                catch
                {
                    Rollback(context);
                    throw;
                }
            }

            return base.TransactAsync(function, options);
        }

        public override bool TryTransact(Action<IDatabaseContext> action, TransactOptions options = null)
        {
            var nested = _current.Value;
            if (nested != null)
            {
                var context = new NestedTransactionContext(nested);
                try
                {
                    action(context);
                    return true;
                }
                catch
                {
                    Rollback(context);
                    throw;
                }
            }

            return base.TryTransact(action, options);
        }

        protected override IDatabaseContext EnterContext(IDatabaseContext db) => _current.Value = db;

        protected override void LeaveContext(IDatabaseContext db) { _current.Value = null; }

        protected void Rollback(IDatabaseContext db)
        {
            db.Transaction.Rollback();
        }
    }
}

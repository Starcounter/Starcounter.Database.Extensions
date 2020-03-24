using System;
using System.Threading;
using System.Threading.Tasks;

namespace Starcounter.Database.Extensions
{
    public class NestedTransactor : TransactorBase
    {
        ThreadLocal<IDatabaseContext> _current = new ThreadLocal<IDatabaseContext>();

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
                action(new NestedTransactionContext(nested));
                return;
            }

            base.Transact(action, options);
        }

        public override T Transact<T>(Func<IDatabaseContext, T> function, TransactOptions options = null)
        {
            var nested = _current.Value;
            if (nested != null)
            {
                return function(new NestedTransactionContext(nested));
            }

            return base.Transact(function, options);
        }

        public override Task TransactAsync(Action<IDatabaseContext> action, TransactOptions options = null)
        {
            var nested = _current.Value;
            if (nested != null)
            {
                action(new NestedTransactionContext(nested));
                return Task.CompletedTask;
            }

            return base.TransactAsync(action, options);
        }

        public override Task<T> TransactAsync<T>(Func<IDatabaseContext, T> function, TransactOptions options = null)
        {
            var nested = _current.Value;
            if (nested != null)
            {
                T result = function(new NestedTransactionContext(nested));
                return Task.FromResult(result);
            }

            return base.TransactAsync(function, options);
        }

        public override Task TransactAsync(Func<IDatabaseContext, Task> function, TransactOptions options = null)
        {
            var nested = _current.Value;
            if (nested != null)
            {
                return function(new NestedTransactionContext(nested));
            }

            return base.TransactAsync(function, options);
        }

        public override Task<T> TransactAsync<T>(Func<IDatabaseContext, Task<T>> function, TransactOptions options = null)
        {
            var nested = _current.Value;
            if (nested != null)
            {
                return function(new NestedTransactionContext(nested));
            }

            return base.TransactAsync(function, options);
        }

        public override bool TryTransact(Action<IDatabaseContext> action, TransactOptions options = null)
        {
            var nested = _current.Value;
            if (nested != null)
            {
                action(new NestedTransactionContext(nested));
                return true;
            }

            return base.TryTransact(action, options);
        }

        protected override IDatabaseContext EnterContext(IDatabaseContext db) => _current.Value = db;

        protected override void LeaveContext(IDatabaseContext db) { _current.Value = null; }
    }
}
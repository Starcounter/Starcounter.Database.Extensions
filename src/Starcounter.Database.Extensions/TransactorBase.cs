using System;
using System.Threading.Tasks;

namespace Starcounter.Database.Extensions
{
    public abstract class TransactorBase<TTransactorContext> : ITransactor
    {
        readonly ITransactor _inner;

        protected ITransactor InnerTransactor { get => _inner; }

        protected TransactorBase(ITransactor innerTransactor)
            => _inner = innerTransactor ?? throw new ArgumentNullException(nameof(innerTransactor));

        public virtual void Transact(Action<IDatabaseContext> action, TransactOptions options = null)
        {
            var context = EnterTransactorContext();
            _inner.Transact(db => ExecuteCallback(context, db, action), options);
            LeaveTransactorContext(context);
        }

        public virtual T Transact<T>(Func<IDatabaseContext, T> function, TransactOptions options = null)
        {
            var context = EnterTransactorContext();
            var r = _inner.Transact(db => ExecuteCallback(context, db, function), options);
            LeaveTransactorContext(context);
            return r;
        }

        public async virtual Task TransactAsync(Action<IDatabaseContext> action, TransactOptions options = null)
        {
            var context = EnterTransactorContext();
            await _inner.TransactAsync(db => ExecuteCallback(context, db, action), options);
            LeaveTransactorContext(context);
        }

        public async virtual Task TransactAsync(Func<IDatabaseContext, Task> function, TransactOptions options = null)
        {
            var context = EnterTransactorContext();
            await _inner.TransactAsync(db => ExecuteCallback(context, db, function), options);
            LeaveTransactorContext(context);
        }

        public async virtual Task<T> TransactAsync<T>(Func<IDatabaseContext, T> function, TransactOptions options = null)
        {
            var context = EnterTransactorContext();
            var r = await _inner.TransactAsync(db => ExecuteCallback(context, db, function), options);
            LeaveTransactorContext(context);
            return r;
        }

        public async virtual Task<T> TransactAsync<T>(Func<IDatabaseContext, Task<T>> function, TransactOptions options = null)
        {
            var context = EnterTransactorContext();
            var r = await _inner.TransactAsync(db => ExecuteCallback(context, db, function), options);
            LeaveTransactorContext(context);
            return r;
        }

        public virtual bool TryTransact(Action<IDatabaseContext> action, TransactOptions options = null)
        {
            var context = EnterTransactorContext();
            var r = _inner.TryTransact(db => ExecuteCallback(context, db, action), options);
            
            if (r)
            {
                LeaveTransactorContext(context);
            }

            return r;
        }

        protected virtual void ExecuteCallback(TTransactorContext transactorContext, IDatabaseContext db, Action<IDatabaseContext> action)
        {
            var dbContext = EnterDatabaseContext(transactorContext, db);
            bool exceptionThrown = false;
            try
            {
                action(dbContext);
            }
            catch
            {
                exceptionThrown = true;
                throw;
            }
            finally
            {
                LeaveDatabaseContext(transactorContext, dbContext, exceptionThrown);
            }
        }

        protected virtual T ExecuteCallback<T>(TTransactorContext transactorContext, IDatabaseContext db, Func<IDatabaseContext, T> function)
        {
            var dbContext = EnterDatabaseContext(transactorContext, db);
            bool exception_thrown = false;
            try
            {
                return function(dbContext);
            }
            catch
            {
                exception_thrown = true;
                throw;
            }
            finally
            {
                LeaveDatabaseContext(transactorContext, dbContext, exception_thrown);
            }
        }

        protected virtual async Task ExecuteCallback(TTransactorContext transactorContext, IDatabaseContext db, Func<IDatabaseContext, Task> function)
        {
            var dbContext = EnterDatabaseContext(transactorContext, db);
            bool exception_thrown = false;
            try
            {
                await function(dbContext);
            }
            catch
            {
                exception_thrown = true;
                throw;
            }
            finally
            {
                LeaveDatabaseContext(transactorContext, dbContext, exception_thrown);
            }
        }

        protected virtual async Task<T> ExecuteCallback<T>(TTransactorContext transactorContext, IDatabaseContext db, Func<IDatabaseContext, Task<T>> function)
        {
            var dbContext = EnterDatabaseContext(transactorContext, db);
            bool exception_thrown = false;
            try
            {
                return await function(dbContext);
            }
            catch
            {
                exception_thrown = true;
                throw;
            }
            finally
            {
                LeaveDatabaseContext(transactorContext, dbContext, exception_thrown);
            }
        }

        /// <summary>
        /// Invoked when a transaction has been created and bound to the current thread,
        /// in a task that execute with a kernel context. The user delegate have not been
        /// invoked yet.  
        /// </summary>
        /// <param name="db">The default database context</param>
        /// <returns>A database context that will be passed to the delegate.</returns>
        protected virtual IDatabaseContext EnterDatabaseContext(TTransactorContext transactorContext, IDatabaseContext db) => db;

        /// <summary>
        /// Invoked right after the user delegate has been executed, but when we are still
        /// within the scope of the transaction and the kernel context.
        /// </summary>
        /// <param name="db">The database context returned by EnterContext.</param>
        /// <param name="exceptionThrown">True if an exception was thrown when invoking the
        /// delegate; false otherwise.</param>
        protected virtual void LeaveDatabaseContext(TTransactorContext transactorContext, IDatabaseContext db, bool exceptionThrown) { }

        /// <summary>
        /// Invoked right before a transaction creation and creates a decorator specific context.
        /// Returns null by default.
        /// </summary>
        /// <returns></returns>
        protected virtual TTransactorContext EnterTransactorContext() => default;

        /// <summary>
        /// Invoked right after a transaction commit and outside of its scope.
        /// This method is not invoked if the transaction fails.
        /// </summary>
        protected virtual void LeaveTransactorContext(TTransactorContext transactorContext) { }
    }
}
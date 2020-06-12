using System;
using System.Threading.Tasks;

namespace Starcounter.Database.Extensions
{
    public abstract class TransactorBase : ITransactor
    {
        readonly ITransactor _inner;

        protected ITransactor InnerTransactor { get => _inner; }

        protected TransactorBase(ITransactor innerTransactor)
            => _inner = innerTransactor ?? throw new ArgumentNullException(nameof(innerTransactor));

        public virtual void Transact(Action<IDatabaseContext> action, TransactOptions options = null)
        {
            _inner.Transact(db => ExecuteCallback(db, action), options);
            LeftContext();
        }

        public virtual T Transact<T>(Func<IDatabaseContext, T> function, TransactOptions options = null)
        { 
            var r = _inner.Transact(db => ExecuteCallback(db, function), options);
            LeftContext();
            return r;
        }

        public async virtual Task TransactAsync(Action<IDatabaseContext> action, TransactOptions options = null)
        { 
            await _inner.TransactAsync(db => ExecuteCallback(db, action), options);
            LeftContext();
        }

        public async virtual Task TransactAsync(Func<IDatabaseContext, Task> function, TransactOptions options = null)
        {
            await _inner.TransactAsync(db => ExecuteCallback(db, function), options);
            LeftContext();
        }

        public async virtual Task<T> TransactAsync<T>(Func<IDatabaseContext, T> function, TransactOptions options = null)
        {
            var r = await _inner.TransactAsync(db => ExecuteCallback(db, function), options);
            LeftContext();
            return r;
        }

        public async virtual Task<T> TransactAsync<T>(Func<IDatabaseContext, Task<T>> function, TransactOptions options = null)
        { 
            var r = await _inner.TransactAsync(db => ExecuteCallback(db, function), options);
            LeftContext();
            return r;
        }

        public virtual bool TryTransact(Action<IDatabaseContext> action, TransactOptions options = null)
        { 
            var r = _inner.TryTransact(db => ExecuteCallback(db, action), options);
            
            if (r)
            {
                LeftContext();
            }

            return r;
        }

        protected virtual void ExecuteCallback(IDatabaseContext db, Action<IDatabaseContext> action)
        {
            var context = EnterContext(db);
            bool exceptionThrown = false;
            try
            {
                action(context);
            }
            catch
            {
                exceptionThrown = true;
                throw;
            }
            finally
            {
                LeaveContext(context, exceptionThrown);
            }
        }

        protected virtual T ExecuteCallback<T>(IDatabaseContext db, Func<IDatabaseContext, T> function)
        {
            var context = EnterContext(db);
            bool exception_thrown = false;
            try
            {
                return function(context);
            }
            catch
            {
                exception_thrown = true;
                throw;
            }
            finally
            {
                LeaveContext(context, exception_thrown);
            }
        }

        protected virtual async Task ExecuteCallback(IDatabaseContext db, Func<IDatabaseContext, Task> function)
        {
            var context = EnterContext(db);
            bool exception_thrown = false;
            try
            {
                await function(context);
            }
            catch
            {
                exception_thrown = true;
                throw;
            }
            finally
            {
                LeaveContext(context, exception_thrown);
            }
        }

        protected virtual async Task<T> ExecuteCallback<T>(IDatabaseContext db, Func<IDatabaseContext, Task<T>> function)
        {
            var context = EnterContext(db);
            bool exception_thrown = false;
            try
            {
                return await function(context);
            }
            catch
            {
                exception_thrown = true;
                throw;
            }
            finally
            {
                LeaveContext(context, exception_thrown);
            }
        }

        /// <summary>
        /// Invoked when a transaction has been created and bound to the current thread,
        /// in a task that execute with a kernel context. The user delegate have not been
        /// invoked yet.  
        /// </summary>
        /// <param name="db">The default database context</param>
        /// <returns>A database context that will be passed to the delegate.</returns>
        protected virtual IDatabaseContext EnterContext(IDatabaseContext db) => db;

        /// <summary>
        /// Invoked right after the user delegate has been executed, but when we are still
        /// within the scope of the transaction and the kernel context.
        /// </summary>
        /// <param name="db">The database context returned by EnterContext.</param>
        /// <param name="exceptionThrown">True if an exception was thrown when invoking the
        /// delegate; false otherwise.</param>
        protected virtual void LeaveContext(IDatabaseContext db, bool exceptionThrown) { }

        /// <summary>
        /// Invoked right after a transaction commit and outside of its scope.
        /// This method is not invoked if the transaction fails.
        /// </summary>
        protected virtual void LeftContext() { }
    }
}
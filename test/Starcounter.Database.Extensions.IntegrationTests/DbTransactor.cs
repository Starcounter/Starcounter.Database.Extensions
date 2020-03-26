using System;
using System.Threading.Tasks;

namespace Starcounter.Database.Extensions.IntegrationTests
{
    class DbTransactor : ITransactor
    {
        readonly DbStorage _storage;
        readonly DbProxyTypeGenerator _proxyTypeGenerator;
        IDatabaseContext _currentContext;

        public DbTransactor(DbStorage storage, DbProxyTypeGenerator proxyTypeGenerator)
        {
            _storage = storage;
            _proxyTypeGenerator = proxyTypeGenerator;
        }

        public void Transact(Action<IDatabaseContext> action, TransactOptions options = null)
        {
            using var context = CreateContext();
            action(context);
        }

        public T Transact<T>(Func<IDatabaseContext, T> function, TransactOptions options = null)
        {
            using var context = CreateContext();
            return function(context);
        }

        public Task TransactAsync(Action<IDatabaseContext> action, TransactOptions options = null)
        {
            using var context = CreateContext();
            try
            {
                action(context);
                return Task.CompletedTask;
            }
            catch (Exception e)
            {
                return Task.FromException(e);
            }
        }

        public Task TransactAsync(Func<IDatabaseContext, Task> function, TransactOptions options = null)
        {
            using var context = CreateContext();
            return function(context);
        }

        public Task<T> TransactAsync<T>(Func<IDatabaseContext, T> function, TransactOptions options = null)
        {
            using var context = CreateContext();
            try
            {
                var obj = function(context);
                return Task<T>.FromResult(obj);
            }
            catch (Exception e)
            {
                return Task.FromException<T>(e);
            }
        }

        public Task<T> TransactAsync<T>(Func<IDatabaseContext, Task<T>> function, TransactOptions options = null)
        {
            using var context = CreateContext();
            return function(context);
        }

        public bool TryTransact(Action<IDatabaseContext> action, TransactOptions options = null)
        {
            using var context = CreateContext();
            try
            {
                action(context);
                return true;
            }
            catch (InvalidOperationException e) when (e.InnerException is AlreadyExecutingException)
            {
                throw;
            } 
            catch
            {
                return false;
            }
        }

        class AlreadyExecutingException : Exception {}

        void ThrowIfAlreadyExecuting()
        {
            if (_currentContext != null)
            {
                throw new InvalidOperationException("Already executing", new AlreadyExecutingException());
            }
        }

        DbContext CreateContext()
        {
            ThrowIfAlreadyExecuting();

            var context = new DbContext(_storage, _proxyTypeGenerator)
            {
                DisposeCallback = c => { _currentContext = null; }
            };

            _currentContext = context;
            return context;
        }
    }
}

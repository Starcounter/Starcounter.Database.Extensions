using System;
using System.Threading.Tasks;

namespace Starcounter.Database.Extensions.IntegrationTests
{
    class DbTransactor : ITransactor
    {
        readonly DbStorage _storage;
        readonly DbProxyTypeGenerator _proxyTypeGenerator;

        public DbTransactor(DbStorage storage, DbProxyTypeGenerator proxyTypeGenerator)
        {
            _storage = storage;
            _proxyTypeGenerator = proxyTypeGenerator;
        }

        public void Transact(Action<IDatabaseContext> action, TransactOptions options = null)
        {
            action(CreateContext());
        }

        public T Transact<T>(Func<IDatabaseContext, T> function, TransactOptions options = null)
        {
            return function(CreateContext());
        }

        public Task TransactAsync(Action<IDatabaseContext> action, TransactOptions options = null)
        {
            action(CreateContext());
            return Task.CompletedTask;
        }

        public Task TransactAsync(Func<IDatabaseContext, Task> function, TransactOptions options = null)
        {
            return function(CreateContext());
        }

        public Task<T> TransactAsync<T>(Func<IDatabaseContext, T> function, TransactOptions options = null)
        {
            var obj = function(CreateContext());
            return Task<T>.FromResult(default(T));
        }

        public Task<T> TransactAsync<T>(Func<IDatabaseContext, Task<T>> function, TransactOptions options = null)
        {
            return function(CreateContext());
        }

        public bool TryTransact(Action<IDatabaseContext> action, TransactOptions options = null)
        {
            try
            {
                action(CreateContext());
                return true;
            }
            catch
            {
                return false;
            }
        }

        IDatabaseContext CreateContext() => new DbContext(_storage, _proxyTypeGenerator);
    }
}

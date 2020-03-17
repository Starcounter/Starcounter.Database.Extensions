using System;
using System.Threading.Tasks;

namespace Starcounter.Database.Extensions.IntegrationTests
{
    class DbTransactor : ITransactor
    {
        readonly DbStorage _storage;

        public DbTransactor(DbStorage storage) => _storage = storage;

        public void Transact(Action<IDatabaseContext> action, TransactOptions options = null)
        {
            action(new DbContext(_storage));
        }

        public T Transact<T>(Func<IDatabaseContext, T> function, TransactOptions options = null)
        {
            return function(new DbContext(_storage));
        }

        public Task TransactAsync(Action<IDatabaseContext> action, TransactOptions options = null)
        {
            action(new DbContext(_storage));
            return Task.CompletedTask;
        }

        public Task TransactAsync(Func<IDatabaseContext, Task> function, TransactOptions options = null)
        {
            return function(new DbContext(_storage));
        }

        public Task<T> TransactAsync<T>(Func<IDatabaseContext, T> function, TransactOptions options = null)
        {
            var obj = function(new DbContext(_storage));
            return Task<T>.FromResult(default(T));
        }

        public Task<T> TransactAsync<T>(Func<IDatabaseContext, Task<T>> function, TransactOptions options = null)
        {
            return function(new DbContext(_storage));
        }

        public bool TryTransact(Action<IDatabaseContext> action, TransactOptions options = null)
        {
            try
            {
                action(new DbContext(_storage));
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}

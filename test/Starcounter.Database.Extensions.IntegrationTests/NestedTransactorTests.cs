using System;
using System.Threading.Tasks;
using System.Transactions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Starcounter.Database.Extensions.IntegrationTests
{
    public sealed class NestedTransactorTests : ServicedTests
    {
        [Database]
        public abstract class Person
        {
            public abstract string Name { get; set; }
        }

        private ITransactor _transactor;

        public NestedTransactorTests(DatabaseExtensionsIntegrationTestContext context) : base(context)
        {
            _transactor = CreateServices(s => s.Decorate<ITransactor, NestedTransactor>())
                 .GetRequiredService<ITransactor>();
        }

        [Fact]
        public void AllowNestedTransaction()
        {
            AllowNestedTransactionImpl();
        }

        [Fact]
        public void AllowNestedTransactionWithinReadOnlyTransaction()
        {
            _transactor.Transact(_ =>
            {
                AllowNestedTransactionImpl();
            }, new TransactOptions(TransactionFlags.ReadOnly));
        }

        [Fact]
        public void ShouldRenderContextThatIndicatesNesting()
        {
            ShouldRenderContextThatIndicatesNestingImpl();
        }

        [Fact]
        public void ShouldRenderContextThatIndicatesNestingWithinReadOnlyTransaction()
        {
            _transactor.Transact(_ =>
            {
                ShouldRenderContextThatIndicatesNestingImpl();
            }, new TransactOptions(TransactionFlags.ReadOnly));
        }

        [Fact]
        public void ShouldHaveOuterChangesVisibleInNestedTransaction()
        {
            ShouldHaveOuterChangesVisibleInNestedTransactionImpl();
        }

        [Fact]
        public void ShouldHaveOuterChangesVisibleInNestedTransactionWithinReadOnlyTransaction()
        {
            _transactor.Transact(_ =>
            {
                ShouldHaveOuterChangesVisibleInNestedTransactionImpl();
            }, new TransactOptions(TransactionFlags.ReadOnly));
        }

        [Fact]
        public void ProhibitSupressingExceptionIfInnerFailed()
        {
            Assert.Throws<TransactionAbortedException>(() =>
            {
                _transactor.Transact(db =>
                {
                    try
                    {
                        _transactor.Transact(_ => throw new Exception());
                    }
                    catch
                    {
                    }
                });
            });
        }

        [Fact]
        public async Task ShouldRenderContextThatIndicatesNestingWhenRunningContinuation()
        {
            // This execute as a top-level transaction, saving the context as
            // a thread local.
            var t = _transactor.TransactAsync(async db =>
            {
                // Yield to force continuation to be scheduled and
                // hence assure we test actual asynchronous execution.
                await Task.Yield();

                // Inner transactor should find original context when
                // back on original thread.
                return _transactor.Transact(db => db.IsNested());
            });

            var result = await t;
            Assert.True(result);
        }

        [Fact]
        public async Task NestedTransactAsyncFailingActionRenderFaultyTask()
        {
            Action<IDatabaseContext> action = db =>
            {
                if (db.IsNested())
                {
                    throw new Exception("Foo");
                }
            };

            var t = _transactor.TransactAsync(db => _transactor.TransactAsync(action));

            var e = await Assert.ThrowsAsync<Exception>(() => t);
            Assert.Equal("Foo", e.Message);
        }

        [Fact]
        public async Task NestedTransactAsyncFailingFuncRenderFaultyTask()
        {
            Func<IDatabaseContext, bool> func = db =>
            {
                if (db.IsNested())
                {
                    throw new Exception("Foo");
                }

                return false;
            };

            var t = _transactor.TransactAsync(db => _transactor.TransactAsync(func));

            var e = await Assert.ThrowsAsync<Exception>(() => t);
            Assert.Equal("Foo", e.Message);
        }

        [Fact]
        public void NestedTransactRestartsParentReadOnlyTransaction()
        {
            _transactor.Transact(odb =>
            {
                Person p = null;

                _transactor.Transact(idb =>
                {
                    p = idb.Insert<Person>();
                });

                Assert.NotNull(p);
                Assert.NotEqual(0UL, odb.GetOid(p));
                Assert.Null(p.Name);

                _transactor.Transact(idb =>
                {
                    p.Name = nameof(NestedTransactRestartsParentReadOnlyTransaction);
                });

                Assert.Equal(nameof(NestedTransactRestartsParentReadOnlyTransaction), p.Name);

                _transactor.Transact(idb =>
                {
                    idb.Delete(p);
                });

                Assert.Throws<DatabaseException>(() => Assert.NotNull(p.Name));
            }, new TransactOptions(TransactionFlags.ReadOnly));
        }

        private void AllowNestedTransactionImpl()
        {
            _transactor.Transact(db =>
            {
                _transactor.Transact(db => { });
            });
        }

        private void ShouldRenderContextThatIndicatesNestingImpl()
        {
            var nested = _transactor.Transact(db =>
            {
                return _transactor.Transact(db => db.IsNested());
            });

            Assert.True(nested);
        }

        private void ShouldHaveOuterChangesVisibleInNestedTransactionImpl()
        {
            var result = _transactor.Transact(db =>
            {
                var p = db.Insert<Person>();
                var id = db.GetOid(p);

                return _transactor.Transact(db => db.Get<Person>(id) != null);
            });

            Assert.True(result);
        }
    }
}

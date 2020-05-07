using System;
using System.Threading.Tasks;
using System.Transactions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Starcounter.Database.Extensions.IntegrationTests
{
    public sealed class NestedTransactorTests : ServicedTests
    {
        [Database] public class Person { }

        public NestedTransactorTests(DatabaseExtensionsIntegrationTestContext context) : base(context) { }

        [Fact]
        public void AllowNestedTransaction()
        {
            var transactor = CreateServices(
                s => s.Decorate<ITransactor, NestedTransactor>())
                .GetRequiredService<ITransactor>();

            transactor.Transact(db =>
            {
                transactor.Transact(db => { });
            });
        }

        [Fact]
        public void ShouldRenderContextThatIndicatesNesting()
        {
            var transactor = CreateServices(
                s => s.Decorate<ITransactor, NestedTransactor>())
                .GetRequiredService<ITransactor>();

            var nested = transactor.Transact(db =>
            {
                return transactor.Transact(db => db.IsNested());
            });

            Assert.True(nested);
        }

        [Fact]
        public void ShouldHaveOuterChangesVisibleInNestedTransaction()
        {
            var transactor = CreateServices(
                s => s.Decorate<ITransactor, NestedTransactor>())
                .GetRequiredService<ITransactor>();

            var result = transactor.Transact(db =>
            {
                var p = db.Insert<Person>();
                var id = db.GetOid(p);

                return transactor.Transact(db => db.Get<Person>(id) != null);
            });

            Assert.True(result);
        }

        [Fact]
        public void ProhibitSupressingExceptionIfInnerFailed()
        {
            var transactor = CreateServices(
                s => s.Decorate<ITransactor, NestedTransactor>())
                .GetRequiredService<ITransactor>();


            Assert.Throws<TransactionAbortedException>(() =>
            {

                transactor.Transact(db =>
                {
                    try
                    {
                        transactor.Transact(_ => throw new Exception());
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
            var transactor = CreateServices(
                s => s.Decorate<ITransactor, NestedTransactor>())
                .GetRequiredService<ITransactor>();

            // This execute as a top-level transaction, saving the context as
            // a thread local.
            var t = transactor.TransactAsync(async db =>
            {
                // Yield to force continuation to be scheduled and
                // hence assure we test actual asynchronous execution.
                await Task.Yield();

                // Inner transactor should find original context when
                // back on original thread.
                return transactor.Transact(db => db.IsNested());
            });

            var result = await t;
            Assert.True(result);
        }

        [Fact]
        public async Task NestedTransactAsyncFailingActionRenderFaultyTask()
        {
            var transactor = CreateServices(
                s => s.Decorate<ITransactor, NestedTransactor>())
                .GetRequiredService<ITransactor>();

            Action<IDatabaseContext> action = db =>
            {
                if (db.IsNested())
                {
                    throw new Exception("Foo");
                }
            };

            var t = transactor.TransactAsync(db => transactor.TransactAsync(action));

            var e = await Assert.ThrowsAsync<Exception>(() => t);
            Assert.Equal("Foo", e.Message);
        }

        [Fact]
        public async Task NestedTransactAsyncFailingFuncRenderFaultyTask()
        {
            var transactor = CreateServices(
                s => s.Decorate<ITransactor, NestedTransactor>())
                .GetRequiredService<ITransactor>();

            Func<IDatabaseContext, bool> func = db =>
            {
                if (db.IsNested())
                {
                    throw new Exception("Foo");
                }

                return false;
            };

            var t = transactor.TransactAsync(db => transactor.TransactAsync(func));

            var e = await Assert.ThrowsAsync<Exception>(() => t);
            Assert.Equal("Foo", e.Message);
        }
    }
}

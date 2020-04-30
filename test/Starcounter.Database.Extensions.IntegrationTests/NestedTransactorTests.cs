using System;
using System.Threading.Tasks;
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
        public void ShouldRollbackOuterTransactionWhenNestedScopeFail()
        {
            var transactor = CreateServices(
                s => s.Decorate<ITransactor, NestedTransactor>())
                .GetRequiredService<ITransactor>();

            var ex = transactor.Transact(db =>
            {
                var p = db.Insert<Person>();
                var id = db.GetOid(p);

                try
                {
                    transactor.Transact(_ => throw new Exception());
                }
                catch
                {
                    // Even if this outer transaction catches it; it should not be
                    // able to continue, since the transaction is aborted.
                    return Assert.ThrowsAny<DatabaseException>(() => db.Get<Person>(id));
                }

                return null;
            });

            Assert.NotNull(ex);
            Assert.Equal(4003u, ex.Code);
            Assert.Contains("ScErrNoTransactionAttached", ex.Message);
        }

        [Fact]
        public void ShouldRenderContextThatIndicatesNestingWhenRunningContinuation()
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

            Assert.True(t.GetAwaiter().GetResult());
        }

        [Fact]
        public void NestedTransactAsyncFailingActionRenderFaultyTask()
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

            var e = Assert.Throws<Exception>(() => t.GetAwaiter().GetResult());
            Assert.Equal("Foo", e.Message);
        }

        [Fact]
        public void NestedTransactAsyncFailingFuncRenderFaultyTask()
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

            var e = Assert.Throws<Exception>(() => t.GetAwaiter().GetResult());
            Assert.Equal("Foo", e.Message);
        }
    }
}

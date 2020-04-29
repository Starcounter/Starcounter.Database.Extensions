using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Starcounter.Database.ChangeTracking;
using Xunit;

namespace Starcounter.Database.Extensions.IntegrationTests
{
    public class DbCoreTests : ServicedTests
    {
        public DbCoreTests(DatabaseExtensionsIntegrationTestContext context) : base(context) {}

        public class Person { }

        [Fact]
        public void ServiceSetupYieldExpectedImplementations()
        {
            var transactor = CreateServices(withRealTemporaryDatabase: false).GetRequiredService<ITransactor>();
            var context = transactor.Transact(db => db);

            Assert.IsType<DbTransactor>(transactor);
            Assert.IsType<DbContext>(context);
        }

        [Fact]
        public void InsertOneReturnFirstId()
        {
            var t = CreateServices(withRealTemporaryDatabase: false).GetRequiredService<ITransactor>();

            t.Transact(db =>
            {
                var p = db.Insert<Person>();
                Assert.Equal(1UL, db.GetOid(p));
            });
        }

        [Fact]
        public void InsertOneReturnExpectedChanges()
        {
            var t = CreateServices(withRealTemporaryDatabase: false).GetRequiredService<ITransactor>();

            t.Transact(db =>
            {
                var p = db.Insert<Person>();

                var change = Assert.Single(db.ChangeTracker.Changes);
                Assert.Equal(1UL, change.Oid);
                Assert.Equal(ChangeType.Insert, change.Type);
            });
        }

        [Fact]
        public void DeleteResultInObjectBeingRemoved()
        {
            var t = CreateServices(withRealTemporaryDatabase: false).GetRequiredService<ITransactor>();

            var id = t.Transact(db =>
            {
                var p = db.Insert<Person>();
                return db.GetOid(p);
            });

            t.Transact(db =>
            {
                var p = db.Get<Person>(id);
                db.Delete(p);

                Assert.Throws<ArgumentOutOfRangeException>(() => db.Get<Person>(id));
            });
        }

        [Fact]
        public void InsertThenDeleteResultInNoChanges()
        {
            var t = CreateServices(withRealTemporaryDatabase: false).GetRequiredService<ITransactor>();

            var changes = t.Transact(db =>
            {
                var p = db.Insert<Person>();
                db.Delete(p);

                return db.ChangeTracker.Changes;
            });

            Assert.Empty(changes);
        }

        [Fact]
        public void CoreTransactorDontAllowNesting()
        {
            var t = CreateServices(withRealTemporaryDatabase: false).GetRequiredService<ITransactor>();

            Action transact = () => t.Transact(_ => t.Transact(__ => { }));
            Func<object> transactFunc = () => t.Transact(_ => t.Transact(__ => new object()));
            Func<Task> transactAsync = () => t.TransactAsync(_ => t.TransactAsync(__ => { }));
            Func<object> tryTransact = () => t.TryTransact(_ => t.TryTransact(__ => { }));

            Assert.Throws<InvalidOperationException>(transact);
            Assert.Throws<InvalidOperationException>(transactFunc);
            Assert.Throws<InvalidOperationException>(tryTransact);
            Assert.ThrowsAsync<InvalidOperationException>(transactAsync);
        }

        [Fact]
        public void TransactAsyncFailingActionRenderFaultyTask()
        {
            var transactor = CreateServices().GetRequiredService<ITransactor>();

            Action<IDatabaseContext> action = db => throw new Exception("Foo");

            var t = transactor.TransactAsync(action);

            var e = Assert.Throws<Exception>(() => t.GetAwaiter().GetResult());
            Assert.Equal("Foo", e.Message);
        }

        [Fact]
        public void TransactAsyncFailingFuncRenderFaultyTask()
        {
            var transactor = CreateServices().GetRequiredService<ITransactor>();

            Func<IDatabaseContext, bool> func = db => throw new Exception("Foo");

            var t = transactor.TransactAsync(func);

            var e = Assert.Throws<Exception>(() => t.GetAwaiter().GetResult());
            Assert.Equal("Foo", e.Message);
        }
    }
}

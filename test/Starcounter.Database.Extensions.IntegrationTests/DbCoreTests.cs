using System;
using Microsoft.Extensions.DependencyInjection;
using Starcounter.Database.ChangeTracking;
using Xunit;

namespace Starcounter.Database.Extensions.IntegrationTests
{
    public class DbCoreTests : ServicedTests
    {
        public class Person { }

        [Fact]
        public void ServiceSetupYieldExpectedImplementations()
        {
            var transactor = CreateServices().GetRequiredService<ITransactor>();
            var context = transactor.Transact(db => db);

            Assert.IsType<DbTransactor>(transactor);
            Assert.IsType<DbContext>(context);
        }

        [Fact]
        public void InsertOneReturnFirstId()
        {
            var t = CreateServices().GetRequiredService<ITransactor>();

            t.Transact(db =>
            {
                var p = db.Insert<Person>();
                Assert.Equal(1UL, db.GetOid(p));
            });
        }

        [Fact]
        public void InsertOneReturnExpectedChanges()
        {
            var t = CreateServices().GetRequiredService<ITransactor>();

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
            var t = CreateServices().GetRequiredService<ITransactor>();

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
            var t = CreateServices().GetRequiredService<ITransactor>();

            var changes = t.Transact(db =>
            {
                var p = db.Insert<Person>();
                db.Delete(p);

                return db.ChangeTracker.Changes;
            });

            Assert.Empty(changes);
        }
    }
}

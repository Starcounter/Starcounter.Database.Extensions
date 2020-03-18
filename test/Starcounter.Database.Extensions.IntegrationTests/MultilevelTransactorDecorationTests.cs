using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Starcounter.Database.ChangeTracking;
using Xunit;

namespace Starcounter.Database.Extensions.IntegrationTests
{
    public sealed class MultilevelTransactorDecorationTests : ServicedTests
    {
        public abstract class Person : IDeleteAware
        {
            public bool WasHooked { get; set; }

            public bool WasDeleted { get; set; }

            public void OnDelete(IDatabaseContext db) => WasDeleted = true;
        }

        [Fact]
        public void OnDeleteIsCalledWhenOuterTransactorIsUsed()
        {
            var transactor = CreateServices
            (
                serviceCollection => serviceCollection
                    .Configure<PreCommitOptions>(o => {})
                    .Decorate<ITransactor, OnDeleteTransactor>()
                    .Decorate<ITransactor, PreCommitTransactor>()
            )
            .GetRequiredService<ITransactor>();

            var result = transactor.Transact(db =>
            {
                var p = db.Insert<Person>();
                db.Delete(p);
                return p.WasDeleted;
            });

            Assert.IsType<PreCommitTransactor>(transactor);
            Assert.True(result);
        }

        [Fact]
        public void OnDeleteIsCalledWhenUsedAsOuterTransactor()
        {
            var transactor = CreateServices
            (
                serviceCollection => serviceCollection
                    .Configure<PreCommitOptions>(o => {})
                    .Decorate<ITransactor, PreCommitTransactor>()
                    .Decorate<ITransactor, OnDeleteTransactor>()
            )
            .GetRequiredService<ITransactor>();

            var result = transactor.Transact(db =>
            {
                var p = db.Insert<Person>();
                db.Delete(p);
                return p.WasDeleted;
            });

            Assert.IsType<OnDeleteTransactor>(transactor);
            Assert.True(result);
        }

        [Fact]
        public void HookIsCalledWhenOuterTransactorIsUsed()
        {
            var transactor = CreateServices
            (
                serviceCollection => serviceCollection
                    .Decorate<ITransactor, PreCommitTransactor>()
                    .Decorate<ITransactor, OnDeleteTransactor>()
                    .Configure<PreCommitOptions>(o =>
                    {
                        o.Hook<Person>((db, change) => db.Get<Person>(change.Oid).WasHooked = true);
                    })
            ).GetRequiredService<ITransactor>();

            (ulong Id, bool WasHooked) before = transactor.Transact(db =>
            {
                var p = db.Insert<Person>();
                return (db.GetOid(p), p.WasHooked);
            });

            var after = transactor.Transact(db => db.Get<Person>(before.Id).WasHooked);

            Assert.IsType<OnDeleteTransactor>(transactor);
            Assert.False(before.WasHooked);
            Assert.True(after);
        }

        [Fact]
        public void HookIsCalledWhenUsedAsOuterTransactor()
        {
            var transactor = CreateServices
            (
                serviceCollection => serviceCollection
                    .Decorate<ITransactor, OnDeleteTransactor>()
                    .Decorate<ITransactor, PreCommitTransactor>()
                    .Configure<PreCommitOptions>(o =>
                    {
                        o.Hook<Person>((db, change) => db.Get<Person>(change.Oid).WasHooked = true);
                    })
            ).GetRequiredService<ITransactor>();

            (ulong Id, bool WasHooked) before = transactor.Transact(db =>
            {
                var p = db.Insert<Person>();
                return (db.GetOid(p), p.WasHooked);
            });

            var after = transactor.Transact(db => db.Get<Person>(before.Id).WasHooked);

            Assert.IsType<PreCommitTransactor>(transactor);
            Assert.False(before.WasHooked);
            Assert.True(after);
        }

        [Fact]
        public void HookAndOnDeleteIsCalledWhenUsedWithOuterOnDeleteTransactor()
        {
            var recordedChanges = new Stack<(ulong Id, ChangeType Type)>();
            var expectedChanges = new Stack<(ulong Id, ChangeType Type)>();

            var transactor = CreateServices
            (
                serviceCollection => serviceCollection
                    .Decorate<ITransactor, PreCommitTransactor>()
                    .Decorate<ITransactor, OnDeleteTransactor>()
                    .Configure<PreCommitOptions>(o =>
                    {
                        o.Hook<Person>((db, change) => recordedChanges.Push((change.Oid, change.Type)));
                    })
            ).GetRequiredService<ITransactor>();

            var id = transactor.Transact(db =>
            {
                var p = db.Insert<Person>();
                return db.GetOid(p);
            });
            expectedChanges.Push((id, ChangeType.Insert));

            var wasDeleted = transactor.Transact(db => 
            {
                var p = db.Get<Person>(id);
                db.Delete(p);
                return p.WasDeleted;
            });

            Assert.IsType<OnDeleteTransactor>(transactor);
            Assert.Equal(expectedChanges, recordedChanges);
            Assert.True(wasDeleted);
        }

        [Fact]
        public void HookAndOnDeleteIsCalledWhenUsedWithOuterPreCommitTransactor()
        {
            var recordedChanges = new Stack<(ulong Id, ChangeType Type)>();
            var expectedChanges = new Stack<(ulong Id, ChangeType Type)>();

            var transactor = CreateServices
            (
                serviceCollection => serviceCollection
                    .Decorate<ITransactor, OnDeleteTransactor>()
                    .Decorate<ITransactor, PreCommitTransactor>()
                    .Configure<PreCommitOptions>(o =>
                    {
                        o.Hook<Person>((db, change) => recordedChanges.Push((change.Oid, change.Type)));
                    })
            ).GetRequiredService<ITransactor>();

            var id = transactor.Transact(db =>
            {
                var p = db.Insert<Person>();
                return db.GetOid(p);
            });
            expectedChanges.Push((id, ChangeType.Insert));

            var wasDeleted = transactor.Transact(db => 
            {
                var p = db.Get<Person>(id);
                db.Delete(p);
                return p.WasDeleted;
            });

            Assert.IsType<PreCommitTransactor>(transactor);
            Assert.Equal(expectedChanges, recordedChanges);
            Assert.True(wasDeleted);
        }
    }
}

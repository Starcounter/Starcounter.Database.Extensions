using System.Collections.Generic;
using System.Linq;
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
                    .Configure<PreCommitOptions>(o => { })
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
                    .Configure<PreCommitOptions>(o => { })
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

        [Fact]
        public void MixedTransactorDecorationSetupsPassCommonTests()
        {
            ITransactor CreateTransactorForCombination<T1, T2, T3>()
                where T1 : ITransactor
                where T2 : ITransactor
                where T3 : ITransactor
            {
                var services = CreateServices
                (
                    serviceCollection => serviceCollection
                        .Decorate<ITransactor, T1>()
                        .Decorate<ITransactor, T2>()
                        .Decorate<ITransactor, T3>()
                        .Configure<PreCommitOptions>(o =>
                        {
                            o.Hook<Person>((db, change) => db.Get<Person>(change.Oid).WasHooked = true);
                        })
                );

                return services.GetRequiredService<ITransactor>();
            }

            var transactors = new List<ITransactor>(new[]
            {
                CreateTransactorForCombination<NestedTransactor, OnDeleteTransactor, PreCommitTransactor>(),
                CreateTransactorForCombination<NestedTransactor, PreCommitTransactor, OnDeleteTransactor>(),
                CreateTransactorForCombination<OnDeleteTransactor, NestedTransactor, PreCommitTransactor>(),
                CreateTransactorForCombination<OnDeleteTransactor, PreCommitTransactor, NestedTransactor>(),
                CreateTransactorForCombination<PreCommitTransactor, OnDeleteTransactor, NestedTransactor>(),
                CreateTransactorForCombination<PreCommitTransactor, NestedTransactor, OnDeleteTransactor>()
            });

            foreach (var transactor in transactors)
            {
                (ulong Id, bool WasHooked) resultFirst = transactor.Transact(db =>
                {
                    var p = db.Insert<Person>();
                    var id = db.GetOid(p);

                    var innerId = transactor.Transact(db =>
                    {
                        var p2 = db.Insert<Person>();
                        return db.GetOid(p2);
                    });

                    var expected = new[] { id, innerId };
                    var result = db.ChangeTracker.Changes
                        .Select(c => c.Oid)
                        .OrderBy(id => id)
                        .ToArray();

                    Assert.Equal(expected, result);

                    return (id, p.WasHooked);
                });

                (bool WasDeleted, bool WasHooked) resultSecond = transactor.Transact(db =>
                {
                    var p = db.Get<Person>(resultFirst.Id);
                    db.Delete(p);
                    return (p.WasDeleted, p.WasHooked);
                });

                Assert.False(resultFirst.WasHooked);
                Assert.True(resultSecond.WasHooked);
                Assert.True(resultSecond.WasDeleted);
            }
        }
    }
}

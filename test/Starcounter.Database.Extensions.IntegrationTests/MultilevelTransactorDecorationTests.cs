using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Starcounter.Database.ChangeTracking;
using Xunit;

namespace Starcounter.Database.Extensions.IntegrationTests
{
    public sealed class MultilevelTransactorDecorationTests : ServicedTests
    {
        public MultilevelTransactorDecorationTests(DatabaseExtensionsIntegrationTestContext context) : base(context) { }

        [Database]
        public abstract class Person : IDeleteAware
        {
            [ProxyState]
            Action<Person> _whenDeleted;

            public void WhenDeleted(Action<Person> action) => _whenDeleted = action;

            public void OnDelete(IDatabaseContext db) => _whenDeleted?.Invoke(this);
        }

        [Fact]
        public void OnDeleteIsCalledWhenOuterTransactorIsUsed()
        {
            var transactor = CreateServices
            (
                serviceCollection => serviceCollection
                    .Configure<IOptions<OnCommitTransactorOptions<object>>>(o => { })
                    .Decorate<ITransactor, OnDeleteTransactor>()
                    .Decorate<ITransactor, OnCommitTransactor<object>>()
            )
            .GetRequiredService<ITransactor>();

            var result = transactor.Transact(db =>
            {
                var p = db.Insert<Person>();

                var deleted = false;
                p.WhenDeleted(p => deleted = true);

                db.Delete(p);

                return deleted;
            });

            Assert.IsType<OnCommitTransactor<object>>(transactor);
            Assert.True(result);
        }

        [Fact]
        public void OnDeleteIsCalledWhenUsedAsOuterTransactor()
        {
            var transactor = CreateServices
            (
                serviceCollection => serviceCollection
                    .Configure<OnCommitTransactor<object>>(o => { })
                    .Decorate<ITransactor, OnCommitTransactor<object>>()
                    .Decorate<ITransactor, OnDeleteTransactor>()
            )
            .GetRequiredService<ITransactor>();

            var result = transactor.Transact(db =>
            {
                var p = db.Insert<Person>();

                var deleted = false;
                p.WhenDeleted(p => deleted = true);

                db.Delete(p);

                return deleted;
            });

            Assert.IsType<OnDeleteTransactor>(transactor);
            Assert.True(result);
        }

        [Fact]
        public void HookIsCalledWhenOuterTransactorIsUsed()
        {
            var hooked = new List<ulong>();

            var transactor = CreateServices
            (
                serviceCollection => serviceCollection
                    .Decorate<ITransactor, OnCommitTransactor<object>>()
                    .Decorate<ITransactor, OnDeleteTransactor>()
                    .Configure<OnCommitTransactorOptions<object>>(o =>
                    {
                        o.OnBeforeCommit = db =>
                        {
                            foreach (var change in db.ChangeTracker.Changes)
                            {
                                hooked.Add(change.Oid);
                            }
                            return null;
                        };
                    })
            ).GetRequiredService<ITransactor>();

            (ulong Id, bool WasHooked) before = transactor.Transact(db =>
            {
                var p = db.Insert<Person>();
                var id = db.GetOid(p);
                return (id, hooked.Contains(id));
            });

            var after = transactor.Transact(db => hooked.Contains(before.Id));

            Assert.IsType<OnDeleteTransactor>(transactor);
            Assert.False(before.WasHooked);
            Assert.True(after);
        }

        [Fact]
        public void HookIsNotCalledWhenOuterTransactorIsUsedAndTransactionFail()
        {
            var hooked = new List<ulong>();

            var transactor = CreateServices
            (
                serviceCollection => serviceCollection
                    .Decorate<ITransactor, OnCommitTransactor<object>>()
                    .Decorate<ITransactor, OnDeleteTransactor>()
                    .Configure<OnCommitTransactorOptions<object>>(o =>
                    {
                        o.OnBeforeCommit = db =>
                        {
                            foreach (var change in db.ChangeTracker.Changes)
                            {
                                hooked.Add(change.Oid);
                            }
                            return null;
                        };
                    })
            ).GetRequiredService<ITransactor>();

            ulong id = 0;
            Assert.Throws<Exception>(() => transactor.Transact(db =>
            {
                var p = db.Insert<Person>();
                id = db.GetOid(p);
                throw new Exception();
            }));

            var existInDatabase = transactor.Transact(db => db.Get<Person>(id) != null);
            Assert.False(existInDatabase);
            Assert.Empty(hooked);
        }

        [Fact]
        public void HookIsCalledWhenUsedAsOuterTransactor()
        {
            var hooked = new List<ulong>();

            var transactor = CreateServices
            (
                serviceCollection => serviceCollection
                    .Decorate<ITransactor, OnDeleteTransactor>()
                    .Decorate<ITransactor, OnCommitTransactor<object>>()
                    .Configure<OnCommitTransactorOptions<object>>(o =>
                    {
                        o.OnBeforeCommit = db =>
                        {
                            foreach (var change in db.ChangeTracker.Changes)
                            {
                                hooked.Add(change.Oid);
                            }
                            return null;
                        };
                    })
            ).GetRequiredService<ITransactor>();

            (ulong Id, bool WasHooked) before = transactor.Transact(db =>
            {
                var p = db.Insert<Person>();
                var id = db.GetOid(p);
                return (id, hooked.Contains(id));
            });

            var after = transactor.Transact(db => hooked.Contains(before.Id));

            Assert.IsType<OnCommitTransactor<object>>(transactor);
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
                    .Decorate<ITransactor, OnCommitTransactor<object>>()
                    .Decorate<ITransactor, OnDeleteTransactor>()
                    .Configure<OnCommitTransactorOptions<object>>(o =>
                    {
                        o.OnBeforeCommit = db =>
                        {
                            foreach (var change in db.ChangeTracker.Changes.Where(x => x.Type != ChangeType.Delete))
                            {
                                recordedChanges.Push((change.Oid, change.Type));
                            }
                            return null;
                        };
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

                var deleted = false;
                p.WhenDeleted(p => deleted = true);

                db.Delete(p);

                return deleted;
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
                    .Decorate<ITransactor, OnCommitTransactor<object>>()
                    .Configure<OnCommitTransactorOptions<object>>(o =>
                    {
                        o.OnBeforeCommit = db =>
                        {
                            foreach (var change in db.ChangeTracker.Changes.Where(x => x.Type != ChangeType.Delete))
                            {
                                recordedChanges.Push((change.Oid, change.Type));
                            }
                            return null;
                        };
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

                var deleted = false;
                p.WhenDeleted(p => deleted = true);

                db.Delete(p);

                return deleted;
            });

            Assert.IsType<OnCommitTransactor<object>>(transactor);
            Assert.Equal(expectedChanges, recordedChanges);
            Assert.True(wasDeleted);
        }

        [Theory]
        [InlineData(typeof(OnDeleteTransactor), typeof(OnCommitTransactor<object>), typeof(NestedTransactor))]
        [InlineData(typeof(OnCommitTransactor<object>), typeof(OnDeleteTransactor), typeof(NestedTransactor))]
        [InlineData(typeof(OnCommitTransactor<object>), typeof(NestedTransactor), typeof(OnDeleteTransactor))]
        public void MixedTransactorDecorationSetupsPassCommonTests(Type transactorDecorator0, Type transactorDecorator1, Type transactorDecorator2)
        {
            var hooked = new List<ulong>();

            var services = CreateServices
            (
                serviceCollection => serviceCollection
                    .Decorate(typeof(ITransactor), transactorDecorator0)
                    .Decorate(typeof(ITransactor), transactorDecorator1)
                    .Decorate(typeof(ITransactor), transactorDecorator2)
                    .Configure<OnCommitTransactorOptions<object>>(o =>
                    {
                        o.OnBeforeCommit = db =>
                        {
                            foreach (var change in db.ChangeTracker.Changes)
                            {
                                hooked.Add(change.Oid);
                            }
                            return null;
                        };
                    })
            );

            var transactor = services.GetRequiredService<ITransactor>();

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

                return (id, hooked.Contains(id));
            });

            (bool WasDeleted, bool WasHooked) resultSecond = transactor.Transact(db =>
            {
                var p = db.Get<Person>(resultFirst.Id);

                var deleted = false;
                p.WhenDeleted(p => deleted = true);
                db.Delete(p);

                return (deleted, hooked.Contains(resultFirst.Id));
            });

            Assert.False(resultFirst.WasHooked);
            Assert.True(resultSecond.WasHooked);
            Assert.True(resultSecond.WasDeleted);
        }
    }
}

using System;
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

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void OnDeleteIsCalledWhenOuterTransactorIsUsed(bool inner)
        {
            var transactor = CreateServices
            (
                serviceCollection => serviceCollection
                    .Configure<PreCommitOptions>(o => { })
                    .Decorate<ITransactor, OnDeleteTransactor>()
                    .Decorate<ITransactor, PreCommitTransactor>()
            )
            .GetRequiredService<ITransactor>();

            var result = Transact(transactor, inner, db =>
            {
                var p = db.Insert<Person>();
                db.Delete(p);
                return p.WasDeleted;
            });

            Assert.IsType<PreCommitTransactor>(transactor);
            Assert.True(result);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void OnDeleteIsCalledWhenUsedAsOuterTransactor(bool inner)
        {
            var transactor = CreateServices
            (
                serviceCollection => serviceCollection
                    .Configure<PreCommitOptions>(o => { })
                    .Decorate<ITransactor, PreCommitTransactor>()
                    .Decorate<ITransactor, OnDeleteTransactor>()
            )
            .GetRequiredService<ITransactor>();

            var result = Transact(transactor, inner, db =>
            {
                var p = db.Insert<Person>();
                db.Delete(p);
                return p.WasDeleted;
            });

            Assert.IsType<OnDeleteTransactor>(transactor);
            Assert.True(result);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void HookIsCalledWhenOuterTransactorIsUsed(bool inner)
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

            (ulong Id, bool WasHooked) before = Transact(transactor, inner, db =>
            {
                var p = db.Insert<Person>();
                return (db.GetOid(p), p.WasHooked);
            });

            var after = Transact(transactor, inner, db => db.Get<Person>(before.Id).WasHooked);

            Assert.IsType<OnDeleteTransactor>(transactor);
            Assert.False(before.WasHooked);
            Assert.True(after);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void HookIsCalledWhenUsedAsOuterTransactor(bool inner)
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

            (ulong Id, bool WasHooked) before = Transact(transactor, inner, db =>
            {
                var p = db.Insert<Person>();
                return (db.GetOid(p), p.WasHooked);
            });

            var after = Transact(transactor, inner, db => db.Get<Person>(before.Id).WasHooked);

            Assert.IsType<PreCommitTransactor>(transactor);
            Assert.False(before.WasHooked);
            Assert.True(after);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void HookAndOnDeleteIsCalledWhenUsedWithOuterOnDeleteTransactor(bool inner)
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

            var id = Transact(transactor, inner, db =>
            {
                var p = db.Insert<Person>();
                return db.GetOid(p);
            });
            expectedChanges.Push((id, ChangeType.Insert));

            var wasDeleted = Transact(transactor, inner, db =>
            {
                var p = db.Get<Person>(id);
                db.Delete(p);
                return p.WasDeleted;
            });

            Assert.IsType<OnDeleteTransactor>(transactor);
            Assert.Equal(expectedChanges, recordedChanges);
            Assert.True(wasDeleted);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void HookAndOnDeleteIsCalledWhenUsedWithOuterPreCommitTransactor(bool inner)
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

            var id = Transact(transactor, inner, db =>
            {
                var p = db.Insert<Person>();
                return db.GetOid(p);
            });
            expectedChanges.Push((id, ChangeType.Insert));

            var wasDeleted = Transact(transactor, inner, db =>
            {
                var p = db.Get<Person>(id);
                db.Delete(p);
                return p.WasDeleted;
            });

            Assert.IsType<PreCommitTransactor>(transactor);
            Assert.Equal(expectedChanges, recordedChanges);
            Assert.True(wasDeleted);
        }

        private T Transact<T>(ITransactor transactor, bool inner, Func<IDatabaseContext, T> action)
        {
            if (inner)
            {
                T result = default;

                transactor.Transact(udb =>
                {
                    result = transactor.Transact(action, new TransactOptions(TransactionFlags.Innter));
                }, new TransactOptions(TransactionFlags.ReadOnly));

                return result;
            }
            else
            {
                return transactor.Transact(action);
            }
        }
    }
}

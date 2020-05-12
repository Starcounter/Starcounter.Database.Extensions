using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Starcounter.Database.Extensions.IntegrationTests
{
    public sealed class PreCommitTransactorTests : ServicedTests
    {
        public PreCommitTransactorTests(DatabaseExtensionsIntegrationTestContext context) : base(context) { }

        [Database]
        public abstract class Person { }

        [Fact]
        public void TriggerCallbackOnInsert()
        {
            var hooked = new List<ulong>();

            // Given
            var services = CreateServices
            (
                serviceCollection => serviceCollection
                    .Configure<PreCommitOptions>(o => o.Hook<Person>((db, change) =>
                    {
                        hooked.Add(change.Oid);
                    }))
                    .Decorate<ITransactor, PreCommitTransactor>()
            );
            var transactor = services.GetRequiredService<ITransactor>();

            // Act
            (bool WasHooked, ulong Id) before = transactor.Transact(db =>
            {
                var p = db.Insert<Person>();
                var id = db.GetOid(p);
                return (hooked.Contains(id), id);
            });

            // Assert
            var after = transactor.Transact(db => hooked.Contains(before.Id));
            Assert.False(before.WasHooked);
            Assert.True(after);
        }

        [Fact]
        public void DontInvokeHooksWhenTransactRaiseException()
        {
            var hooked = new List<ulong>();

            // Given
            var services = CreateServices
            (
                serviceCollection => serviceCollection
                    .Configure<PreCommitOptions>(o => o.Hook<Person>((db, change) =>
                    {
                        hooked.Add(change.Oid);
                    }))
                    .Decorate<ITransactor, PreCommitTransactor>()
            );
            var transactor = services.GetRequiredService<ITransactor>();

            // Act
            ulong id = 0;
            Assert.Throws<Exception>(() => transactor.Transact(db =>
            {
                var p = db.Insert<Person>();
                id = db.GetOid(p);
                throw new Exception();
            }));

            // Assert
            var existInDatabase = transactor.Transact(db => db.Get<Person>(id) != null);
            Assert.False(existInDatabase);
            Assert.Empty(hooked);
        }
    }
}

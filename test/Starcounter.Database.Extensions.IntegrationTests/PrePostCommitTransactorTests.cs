using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Starcounter.Database.Extensions.IntegrationTests
{
    public sealed class PrePostCommitTransactorTests : ServicedTests
    {
        public PrePostCommitTransactorTests(DatabaseExtensionsIntegrationTestContext context) : base(context) { }

        [Database]
        public abstract class Person { }

        [Fact]
        public void TriggerHooksOnInsert()
        {
            var preHooked = new List<ulong>();
            var postHooked = new List<ulong>();

            // Given
            var services = CreateServices
            (
                serviceCollection => serviceCollection
                    .Configure<PreCommitOptions>(o => o.Hook<Person>((db, change) =>
                    {
                        preHooked.Add(change.Oid);
                    }))
                    .Configure<PostCommitOptions>(o => o.Hook<Person>(change => 
                    {
                        postHooked.Add(change.Oid);
                    }))
                    .Decorate<ITransactor, PreCommitTransactor>()
                    .Decorate<ITransactor, PostCommitTransactor>()
            );
            var transactor = services.GetRequiredService<ITransactor>();

            // Act
            (bool WasHooked, ulong Id) before = transactor.Transact(db =>
            {
                var p = db.Insert<Person>();
                var id = db.GetOid(p);
                return (preHooked.Contains(id), id);
            });

            // Assert
            Assert.Single(preHooked);
            Assert.Contains(before.Id, preHooked);
            Assert.Equal(preHooked, postHooked);
            Assert.False(before.WasHooked);
        }

        [Fact]
        public void DontInvokeHooksWhenTransactRaiseException()
        {
            var count = 0;

            // Given
            var services = CreateServices
            (
                serviceCollection => serviceCollection
                    .Configure<PreCommitOptions>(o => o.Hook<Person>((db, change) =>
                    {
                        count++;
                    }))
                    .Configure<PostCommitOptions>(o =>
                    {
                        o.Hook<Person>(_ => count++);
                    })
                    .Decorate<ITransactor, PreCommitTransactor>()
                    .Decorate<ITransactor, PostCommitTransactor>()
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
            Assert.Equal(0, count);
        }
    }
}

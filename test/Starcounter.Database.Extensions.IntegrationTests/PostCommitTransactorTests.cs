using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Starcounter.Database.Extensions.IntegrationTests
{
    public sealed class PostCommitTransactorTests : ServicedTests
    {
        public PostCommitTransactorTests(DatabaseExtensionsIntegrationTestContext context) : base(context) { }

        [Database]
        public abstract class Person { }

        [Fact]
        public void TriggerHookOnInsert()
        {
            var hooked = new List<ulong>();

            // Given
            var services = CreateServices
            (
                serviceCollection => serviceCollection
                    .Configure<PostCommitOptions>(o => o.Hook<Person>((change) =>
                    {
                        hooked.Add(change.Oid);
                    }))
                    .Decorate<ITransactor, PostCommitTransactor>()
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
            var after = hooked.Contains(before.Id);
            Assert.False(before.WasHooked);
            Assert.True(after);
        }

        [Fact]
        public void DontInvokeHooksWhenTransactRaiseException()
        {
            var count = 0;

            // Given
            var services = CreateServices
            (
                serviceCollection => serviceCollection
                    .Configure<PostCommitOptions>(o =>
                    {
                        o.Hook<Person>(_ => count++);
                    })
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

        [Fact]
        public void DontInvokeHooksWhenTransactHasNoChanges()
        {
            var count = 0;

            // Given
            var services = CreateServices
            (
                serviceCollection => serviceCollection
                    .Configure<PostCommitOptions>(o =>
                    {
                        o.Hook<Person>(_ => count++);
                    })
                    .Decorate<ITransactor, PostCommitTransactor>()
            );
            var transactor = services.GetRequiredService<ITransactor>();

            // Act
            transactor.Transact(db =>
            {
                var p = db.Insert<Person>();
                db.Delete(p);
            });

            // Assert
            Assert.Equal(0, count);
        }
    }
}

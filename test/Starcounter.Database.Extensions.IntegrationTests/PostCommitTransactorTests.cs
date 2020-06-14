using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using Xunit;

namespace Starcounter.Database.Extensions.IntegrationTests
{
    public sealed class PostCommitTransactorTests : ServicedTests
    {
        public PostCommitTransactorTests(DatabaseExtensionsIntegrationTestContext context) : base(context) { }

        [Database]
        public abstract class Person { }

        [Fact]
        public void TriggerCallbackOnInsert()
        {
            var hooked = new List<ulong>();
            var tcs = new TaskCompletionSource<int>();

            // Given
            var services = CreateServices
            (
                serviceCollection => serviceCollection
                    .Configure<PostCommitOptions>(o => o.Hook<Person>((change) =>
                    {
                        hooked.Add(change.Oid);
                        tcs.SetResult(int.MinValue);
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

            tcs.Task.Wait();

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
                        o.Hook<Person>(_ => { });
                    })
                    .Decorate<ITransactor>((transactor, services) =>
                    {
                        var options = services.GetService<IOptions<PostCommitOptions>>();
                        var decoratorMoch = new Mock<PostCommitTransactor>(transactor, options);

                        decoratorMoch.CallBase = true;
                        decoratorMoch.Protected()
                            .Setup("LeftContext")
                            .Callback(() => count++);

                        return decoratorMoch.Object;
                    })
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

            var existInDatabase = transactor.Transact(db => db.Get<Person>(id) != null);
            Assert.False(existInDatabase);
        }
    }
}

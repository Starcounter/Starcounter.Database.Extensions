using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Moq.Protected;
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
            var tcs = new TaskCompletionSource<ulong>();

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
                        tcs.SetResult(change.Oid);
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

            tcs.Task.Wait();

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
            var schedulerMoch = new Mock<TaskScheduler>();
            schedulerMoch
                .Protected()
                .Setup("QueueTask", ItExpr.IsAny<Task>())
                .Callback(() => count++);

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
                        o.TaskScheduler = schedulerMoch.Object;
                        o.Hook<Person>(_ => { });
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

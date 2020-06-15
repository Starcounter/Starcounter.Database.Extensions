using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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

        class CountingTaskScheduler : TaskScheduler 
        {
            public int TaskCount { get; set; }

            protected override void QueueTask(Task task) 
            {
                TaskCount++;
                TryExecuteTask(task);
            }

            protected override IEnumerable<Task> GetScheduledTasks() => null;

            protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued) => true;
        }

        [Fact]
        public void DontInvokeHooksWhenTransactRaiseException()
        {
            var scheduler = new CountingTaskScheduler();

            // Given
            var services = CreateServices
            (
                serviceCollection => serviceCollection
                    .Configure<PostCommitOptions>(o => 
                    { 
                        o.TaskScheduler = scheduler;
                        o.Hook<Person>(_ => {});
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
            Assert.Equal(0, scheduler.TaskCount);
        }
    }
}

using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Starcounter.Database.Extensions.IntegrationTests
{
    public sealed class NestedTransactorTests : ServicedTests
    {
        [Fact]
        public void AllowNestedTransaction()
        {
            var transactor = CreateServices(
                s => s.Decorate<ITransactor, NestedTransactor>())
                .GetRequiredService<ITransactor>();

            transactor.Transact(db =>
            {
                transactor.Transact(db => { });
            });
        }

        [Fact]
        public void ShouldRenderContextThatIndicatesNesting()
        {
            var transactor = CreateServices(
                s => s.Decorate<ITransactor, NestedTransactor>())
                .GetRequiredService<ITransactor>();

            var nested = transactor.Transact(db =>
            {
                return transactor.Transact(db => db.IsNested());
            });

            Assert.True(nested);
        }

        [Fact]
        public void ShouldRenderContextThatIndicatesNestingWhenRunningContinuation()
        {
            var transactor = CreateServices(
                s => s.Decorate<ITransactor, NestedTransactor>())
                .GetRequiredService<ITransactor>();

            // This execute as a top-level transaction, saving the context as
            // a thread local.
            var t = transactor.Transact(async db =>
            {
                // Yield to force continuation to be scheduled and
                // hence assure we test actual asynchronous execution.
                await Task.Yield();

                // Inner transactor should find original context when
                // back on original thread.
                return transactor.Transact(db => db.IsNested());
            });

            Assert.True(t.GetAwaiter().GetResult());
        }
    }
}

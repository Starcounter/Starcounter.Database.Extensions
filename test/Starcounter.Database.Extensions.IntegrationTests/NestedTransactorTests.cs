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
    }
}

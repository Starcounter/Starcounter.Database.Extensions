using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Starcounter.Database.Extensions.IntegrationTests
{
    public sealed class OnDeleteTransactorTests
    {
        public class Person : IDeleteAware
        {
            public bool WasDeleted { get; set; }

            public void OnDelete(IDatabaseContext db) => WasDeleted = true;
        }

        [Fact]
        public void TriggerCallbackWhenDeleted()
        {
            var transactor = UnitTest1.CreateServices(
                s => s.Decorate<ITransactor, OnDeleteTransactor>())
                .GetRequiredService<ITransactor>();

            transactor.Transact(db =>
            {
                var p = db.Insert<Person>();
                db.Delete(p);
                Assert.True(p.WasDeleted);
            });
        }
    }
}

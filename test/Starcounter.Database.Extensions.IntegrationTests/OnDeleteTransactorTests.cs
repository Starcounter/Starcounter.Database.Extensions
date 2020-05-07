using System;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Starcounter.Database.Extensions.IntegrationTests
{
    public sealed class OnDeleteTransactorTests : ServicedTests
    {
        public OnDeleteTransactorTests(DatabaseExtensionsIntegrationTestContext context) : base(context) { }

        [Database]
        public abstract class Person : IDeleteAware
        {
            [ProxyState]
            Action<Person> _whenDeleted;

            public void WhenDeleted(Action<Person> action) => _whenDeleted = action;

            public void OnDelete(IDatabaseContext db) => _whenDeleted?.Invoke(this);
        }

        [Fact]
        public void TriggerCallbackWhenDeleted()
        {
            var transactor = CreateServices(
                s => s.Decorate<ITransactor, OnDeleteTransactor>())
                .GetRequiredService<ITransactor>();

            var wasDeleted = transactor.Transact(db =>
            {
                var p = db.Insert<Person>();

                var deleted = false;
                p.WhenDeleted(p => deleted = true);

                db.Delete(p);

                return deleted;
            });

            Assert.True(wasDeleted);
        }
    }
}

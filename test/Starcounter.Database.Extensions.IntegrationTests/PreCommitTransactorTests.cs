using System;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Starcounter.Database.Extensions.IntegrationTests
{
    public sealed class PreCommitTransactorTests : ServicedTests
    {
        public abstract class Person
        {
            public bool WasHooked { get; set; }
        }

        [Fact]
        public void TriggerCallbackOnInsert()
        {
            // Given
            var services = CreateServices
            (
                serviceCollection => serviceCollection
                    .Configure<PreCommitOptions>(o => o.Hook<Person>((db, change) => 
                    {
                        var p = db.Get<Person>(change.Oid);
                        p.WasHooked = true;
                    }))
                    .Decorate<ITransactor, PreCommitTransactor>()
            );
            var transactor = services.GetRequiredService<ITransactor>();

            // Act
            Tuple<bool, ulong> before = transactor.Transact(db =>
            {
                var p = db.Insert<Person>();
                return Tuple.Create(p.WasHooked, db.GetOid(p));
            });

            // Assert
            var after = transactor.Transact(db => db.Get<Person>(before.Item2).WasHooked);
            Assert.False(before.Item1);
            Assert.True(after);
        }
    }
}

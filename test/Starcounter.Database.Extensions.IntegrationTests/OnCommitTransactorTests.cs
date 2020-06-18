using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Starcounter.Database.ChangeTracking;
using Xunit;

namespace Starcounter.Database.Extensions.IntegrationTests
{
    public sealed class OnCommitTransactorTests : ServicedTests
    {
        public class RelevantChanges
        {
            public List<(ChangeType ChangeType, Type ObjectType, ulong Oid)> Changes { get; protected set; } = new List<(ChangeType ChangeType, Type ObjectType, ulong Oid)>();
        }

        public OnCommitTransactorTests(DatabaseExtensionsIntegrationTestContext context) : base(context) { }

        [Database]
        public abstract class DatabaseItem
        {
            public abstract string Name { get; set; }
        }

        [Fact]
        public void TriggerOnInsert()
        {
            var onBeforeCommit = new List<ulong>();
            var onAfterCommit = new List<ulong>();
            var expectedContext = new RelevantChanges(); 

            // Given
            var services = CreateServices
            (
                serviceCollection => serviceCollection
                    .Configure<OnCommitTransactorOptions<RelevantChanges>>(o =>
                    {
                        o.OnBeforeCommit = db =>
                        {
                            var change = db.ChangeTracker.Changes.Single();

                            expectedContext.Changes.Add((change.Type, db.GetUserDefinedType(change.Oid), change.Oid));
                            onBeforeCommit.Add(change.Oid);

                            return expectedContext;
                        };

                        o.OnAfterCommit = context =>
                        {
                            Assert.Equal(expectedContext, context);

                            var change = context.Changes.Single();
                            onAfterCommit.Add(change.Oid);
                        };
                    })
                    .Decorate<ITransactor, OnCommitTransactor<RelevantChanges>>()
            );
            var transactor = services.GetRequiredService<ITransactor>();

            // Act
            var expectedOid = transactor.Transact(db =>
            {
                var p = db.Insert<DatabaseItem>();
                return db.GetOid(p);
            });

            // Assert
            var change = expectedContext.Changes.Single();
            Assert.Equal(ChangeType.Insert, change.ChangeType);
            Assert.Equal(typeof(DatabaseItem), change.ObjectType);
            Assert.Equal(expectedOid, change.Oid);

            Assert.Single(onBeforeCommit);
            Assert.Contains(expectedOid, onBeforeCommit);
            Assert.Equal(onBeforeCommit, onAfterCommit);
        }

        [Fact]
        public void DoesntTriggerOnException()
        {
            // Given
            var services = CreateServices
            (
                serviceCollection => serviceCollection
                    .Configure<OnCommitTransactorOptions<RelevantChanges>>(o =>
                    {
                        o.OnBeforeCommit = db =>
                        {
                            throw new InvalidOperationException();
                        };

                        o.OnAfterCommit = context =>
                        {
                            throw new InvalidOperationException();
                        };
                    })
                    .Decorate<ITransactor, OnCommitTransactor<RelevantChanges>>()
            );
            var transactor = services.GetRequiredService<ITransactor>();

            // Act & assert
            Assert.Throws<Exception>(() => transactor.Transact(db =>
            {
                var p = db.Insert<DatabaseItem>();
                throw new Exception();
            }));
        }
    }
}

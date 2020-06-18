using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Starcounter.Database.Extensions.IntegrationTests
{
    public sealed class DatabaseContextExtensionsTests : ServicedTests
    {

        public DatabaseContextExtensionsTests(DatabaseExtensionsIntegrationTestContext context) : base(context) { }

        [Database]
        public abstract class DatabaseItem
        {
        }

        [Fact]
        public void GetUserDefinedTypeReturnsCorrectType()
        {
            // Given
            var services = CreateServices();
            var transactor = services.GetRequiredService<ITransactor>();

            // Act
            var oid = transactor.Transact(db =>
            {
                var i = db.Insert<DatabaseItem>();
                return db.GetOid(i);
            });

            var type = transactor.Transact(db =>
            {
                return db.GetUserDefinedType(oid);
            });

            // Assert
            Assert.Equal(typeof(DatabaseItem), type);
        }

        [Fact]
        public void GetUserDefinedTypeReturnsNullForDeletedObject()
        {
            // Given
            var services = CreateServices();
            var transactor = services.GetRequiredService<ITransactor>();

            // Act
            var type = transactor.Transact(db =>
            {
                var i = db.Insert<DatabaseItem>();
                var oid = db.GetOid(i);
                db.Delete(i);
                return db.GetUserDefinedType(oid);
            });

            // Assert
            Assert.Null(type);
        }

        [Fact]
        public void GetUserDefinedTypeReturnsNullForMissingObject()
        {
            // Given
            var services = CreateServices();
            var transactor = services.GetRequiredService<ITransactor>();

            // Act
            var type = transactor.Transact(db =>
            {
                return db.GetUserDefinedType(0);
            });

            // Assert
            Assert.Null(type);
        }
    }
}

using System;
using System.Collections.Generic;
using Starcounter.Database.TestResources;
using Xunit;

namespace Starcounter.Database.Extensions.IntegrationTests
{
    public class DatabaseExtensionsIntegrationTestContext : TemporaryDatabase
    {
        public DatabaseExtensionsIntegrationTestContext() : base() { }

        protected override IEnumerable<Type> ProvideDatabaseTypes() => typeof(DatabaseExtensionsIntegrationTestContext).Assembly.ExportedTypes;
    }

    [CollectionDefinition(nameof(DatabaseExtensionsIntegrationTestContext))]
    public class DatabaseCollection : ICollectionFixture<DatabaseExtensionsIntegrationTestContext> { }
}

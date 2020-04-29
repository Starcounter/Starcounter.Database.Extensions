using System;
using System.Collections.Generic;
using TestUtilities;
using Xunit;

namespace Starcounter.Database.Extensions.IntegrationTests
{
    public class DatabaseExtensionsIntegrationTestContext : TestAppHost
    {
        public DatabaseExtensionsIntegrationTestContext() : base() { }

        protected override IEnumerable<Type> ProvideDatabaseTypes() => typeof(DatabaseExtensionsIntegrationTestContext).Assembly.ExportedTypes;
    }

    [CollectionDefinition(nameof(DatabaseExtensionsIntegrationTestContext))]
    public class DatabaseCollection : ICollectionFixture<DatabaseExtensionsIntegrationTestContext> { }
}

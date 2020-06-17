using System;
using System.Collections.Generic;
using System.Linq;
using Starcounter.Database.TestResources;
using Xunit;

namespace Starcounter.Database.Extensions.IntegrationTests
{
    public class DatabaseExtensionsIntegrationTestContext : TemporaryDatabase
    {
        public DatabaseExtensionsIntegrationTestContext() : base() { }

        protected override IEnumerable<Type> ProvideDatabaseTypes()
        {
            return AppDomain.CurrentDomain
                .GetAssemblies()
                .Where(x => !x.IsDynamic)
                .SelectMany(x => x.ExportedTypes);
        }
    }

    [CollectionDefinition(nameof(DatabaseExtensionsIntegrationTestContext))]
    public class DatabaseCollection : ICollectionFixture<DatabaseExtensionsIntegrationTestContext> { }
}

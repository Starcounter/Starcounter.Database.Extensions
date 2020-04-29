using System;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Starcounter.Database.Extensions.IntegrationTests
{
    [Collection(nameof(DatabaseExtensionsIntegrationTestContext))]
    public abstract class ServicedTests
    {
        readonly DatabaseExtensionsIntegrationTestContext _testContext;

        public ServicedTests(DatabaseExtensionsIntegrationTestContext context) => _testContext = context;

        protected IServiceProvider CreateServices
        (
            Func<IServiceCollection, IServiceCollection> configurator = null, 
            bool withRealTemporaryDatabase = true
        )
        {
            IServiceCollection services = new ServiceCollection();
            
            if (withRealTemporaryDatabase)
            {
                services.AddSingleton<ITransactor>(sp => _testContext.Services.GetRequiredService<ITransactor>());
            }
            else
            {
                services
                    .AddSingleton<DbStorage>()
                    .AddSingleton<DbProxyTypeGenerator>()
                    .AddSingleton<ITransactor, DbTransactor>();
            }

            if (configurator != null)
            {
                services = configurator(services);
            }

            return services.BuildServiceProvider();
        }
    }
}

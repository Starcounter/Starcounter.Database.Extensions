using System;
using Microsoft.Extensions.DependencyInjection;

namespace Starcounter.Database.Extensions.IntegrationTests
{
    public abstract class ServicedTests
    {
        protected IServiceProvider CreateServices(Func<IServiceCollection, IServiceCollection> configurator = null)
        {
            var services = new ServiceCollection()
                .AddSingleton<DbStorage>()
                .AddSingleton<DbProxyTypeGenerator>()
                .AddSingleton<ITransactor, DbTransactor>();

            if (configurator != null)
            {
                services = configurator(services);
            }

            return services.BuildServiceProvider();
        }
    }
}

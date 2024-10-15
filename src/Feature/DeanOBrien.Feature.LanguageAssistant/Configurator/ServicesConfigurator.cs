using DeanOBrien.Feature.LanguageAssistant.Controllers;
using Microsoft.Extensions.DependencyInjection;
using Sitecore.DependencyInjection;

namespace DeanOBrien.Feature.LanguageAssistant.Configurator
{
    class ServicesConfigurator : IServicesConfigurator
    {
        public void Configure(IServiceCollection serviceCollection)
        {
            serviceCollection.AddTransient<LanguageAssistantController>();

        }

    }
}

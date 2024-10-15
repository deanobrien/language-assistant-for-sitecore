using Sitecore.Pipelines;
using System.Web.Mvc;
using System.Web.Routing;

namespace DeanOBrien.Feature.LanguageAssistant.Pipelines
{
    public class RegisterCustomRoute
    {
        public virtual void Process(PipelineArgs args)
        {
            Register();
        }

        public static void Register()
        {
            RouteTable.Routes.MapRoute("LanguageAssistant", "sitecore/shell/sitecore/client/applications/languageassistant/", new { controller = "LanguageAssistant", action = "LanguageAssistant" });
            RouteTable.Routes.MapRoute("LanguageAssistantById", "sitecore/shell/sitecore/client/applications/languageassistant/{id}", new { controller = "LanguageAssistant", action = "LanguageAssistant" });
        }

    }
}

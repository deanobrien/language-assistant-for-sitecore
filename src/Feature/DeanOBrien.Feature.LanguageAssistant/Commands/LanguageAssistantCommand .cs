using Sitecore.Diagnostics;
using Sitecore.Shell.Framework.Commands;
using System;
using System.Linq;
using Sitecore;
using Sitecore.Data.Items;
using System.Collections.Specialized;
using Sitecore.Web.UI.Sheer;
using System.Web;

namespace DeanOBrien.Feature.LanguageAssistant.Commands
{
    [Serializable]
    public class LanguageAssistantCommand : Command
    {
        public override void Execute(CommandContext context)
        {
            Error.AssertObject(context, "context");
            if (context.Items.Length == 1)
            {
                Item item = context.Items[0];
                var parameters = new NameValueCollection();
                parameters["id"] = item.ID.ToString();
                Context.ClientPage.Start(this, "Run", parameters);
            }
        }

        protected void Run(ClientPipelineArgs args)
        {
            string str = args.Parameters["id"].Replace("{", "").Replace("}", "");

            if (!SheerResponse.CheckModified()) return;

            if (!args.IsPostBack)
            {
                string languageAssistantURL = Sitecore.Configuration.Settings.GetSetting("LanguageAssistantURL");

                if (!string.IsNullOrWhiteSpace(languageAssistantURL))
                {
                    Sitecore.Text.UrlString url = new Sitecore.Text.UrlString(languageAssistantURL);
                    url.Append("id", HttpUtility.UrlEncode(str));

                    Sitecore.Context.ClientPage.ClientResponse.ShowModalDialog(url.ToString(),
                        "1200px", "800px",
                        "languageassistant", true);
                    args.WaitForPostBack();
                }
                else
                {
                    SheerResponse.Alert("LanguageAssistantURL is empty. Please verify your configurations",
                        true);
                }
            }

        }

        public override CommandState QueryState(CommandContext context)
        {
            Error.AssertObject(context, "context");

            if (!context.Items.Any())
            {
                return CommandState.Disabled;
            }

            return base.QueryState(context);
        }
    }
}

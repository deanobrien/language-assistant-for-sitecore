
using Azure.AI.OpenAI;
using OpenAI.Chat;
using System;
using System.ClientModel;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using Sitecore.Mvc.Controllers;
using DeanOBrien.Feature.LanguageAssistant.Models;
using Newtonsoft.Json;
using System.Text.RegularExpressions;
using System.Diagnostics;
using Sitecore.Diagnostics;
using Sitecore.Data.Fields;

namespace DeanOBrien.Feature.LanguageAssistant.Controllers
{
    public class LanguageAssistantController : SitecoreController
    {
        private const string SettingsId = "{C34468E8-0CED-4F4D-8F5D-B52F1604E145}";
        private const string DeployedModels = "{B09A35E2-B289-423D-A8CD-914E7AA58166}";
        private const string DefaultPrompts = "{D6901F93-4213-459B-B48C-F5428902B71A}";
        private string _deployedModel;
        private string _endpoint;
        private string _key;
        private string[] _ignoreList = ["__Updated by", "__Revision", "__Lock", "__Owner", "__Created by"];


        public LanguageAssistantController()
        {
        }
        public ActionResult LanguageAssistant(string id = null, int prompt = -1, string promptDetails = null, string field = null, string deployedModel = null, string customContext = null)
        {
            
            LanguageAssistantViewModel searchResponse = new LanguageAssistantViewModel();

            var timer = new Stopwatch();
            timer.Start();

            var database = Sitecore.Configuration.Factory.GetDatabase("master");
            var sitecoreItem = database.GetItem(id);
            var settingsItem = database.GetItem(SettingsId);
            if (settingsItem == null) searchResponse.ErrorMessage = "There seems to be an issue with configuration: Settings Item is missing";

            var deployedModelsItem = database.GetItem(DeployedModels);
            if (deployedModelsItem == null) searchResponse.ErrorMessage = "There seems to be an issue with configuration: Deployed Models Folder is missing";
            if (deployedModelsItem.Children.Count() == 0) searchResponse.ErrorMessage = "There seems to be an issue with configuration: No deployed models have been added";

            var deployedModels = deployedModelsItem.Children.Select(x => x.Fields["Model Name"].Value).ToList();

            var defaultPrompts = database.GetItem(DefaultPrompts);
            if (defaultPrompts == null) searchResponse.ErrorMessage = "There seems to be an issue with configuration: Default Prompts Folder is missing";
            if (defaultPrompts.Children.Count() ==0) searchResponse.ErrorMessage = "There seems to be an issue with configuration: No default prompts have been added";

            var targetField = settingsItem.Fields["Default Field"].Value;
            if (string.IsNullOrWhiteSpace(targetField) || !sitecoreItem.Fields.Where(x => x.Name == targetField).Any()) targetField = sitecoreItem.Fields.Where(x=>x.Type=="Rich Text").FirstOrDefault().Name;
            if (!string.IsNullOrWhiteSpace(field) && sitecoreItem.Fields.Where(x => x.Name == field).Any()) targetField = field;

            var fieldContent = sitecoreItem.Fields[targetField].Value;
            if (field == "custom") {
                targetField = field;
                fieldContent = customContext;
            }

            var languageModelName = settingsItem.Fields["Default Model"].Value;
            if (!string.IsNullOrWhiteSpace(deployedModel)&& deployedModels.Where(model => model == deployedModel).Any())
            {
                languageModelName = deployedModel;
            }
            else if(string.IsNullOrWhiteSpace(languageModelName) || !deployedModels.Where(model => model == languageModelName).Any())
            {
                languageModelName = deployedModels.FirstOrDefault();
            }
            var languageModelItem = deployedModelsItem.Children.Where(model => model.Fields["Model Name"].Value == languageModelName).FirstOrDefault();
            if (languageModelItem == null) searchResponse.ErrorMessage = "There seems to be an issue with configuration: Could not retrieve dpeloyed model";

            _endpoint = languageModelItem.Fields["Endpoint"].Value;
            if (string.IsNullOrWhiteSpace(_endpoint)) searchResponse.ErrorMessage = "There seems to be an issue with configuration: No Endpoint configured for the deployed model";

            _key = languageModelItem.Fields["Key"].Value;
            if (string.IsNullOrWhiteSpace(_key)) searchResponse.ErrorMessage = "There seems to be an issue with configuration: No Key configured for the deployed model";

            var defaultPrompt = settingsItem.Fields["Default Prompt"].Value;
            if (string.IsNullOrWhiteSpace(defaultPrompt))
            {
                defaultPrompt = defaultPrompts.Children.FirstOrDefault().Fields["Prompt"].Value;
            }

            var prompts = new List<Tuple<int, string>>();
            int count = 0;
            foreach (var item in defaultPrompts.Children.Select(p => p.Fields["Prompt"].Value).ToList())
            {
                count++;
                prompts.Add(new Tuple<int, string>(count, item));
            }
            var fields = new List<Tuple<string, string>>();

                foreach (Field f in sitecoreItem.Fields)
                {
                    if ((f.Type == "Rich Text" || f.Type == "Single-Line Text" || f.Type == "Multi-Line Text") && !_ignoreList.Contains(f.Name))
                    {
                        var fieldValue=f.Value;
                        if(fieldValue.Length > 500) fieldValue = TruncateAtWord(HtmlToPlainText(f.Value), 700) + "...";

                        fields.Add(new Tuple<string, string>(f.Name, fieldValue));
                    }
                }

            string mainSystemPrompt = "Please review the following text and ";
            if (prompt == -1) mainSystemPrompt = string.Empty;
            else if (prompt == -2 && !string.IsNullOrWhiteSpace(promptDetails)) mainSystemPrompt = promptDetails;
            else if (prompts.Where(x => x.Item1 == prompt).Any()) mainSystemPrompt = prompts.Where(x => x.Item1 == prompt).FirstOrDefault().Item2;
            else mainSystemPrompt += defaultPrompt;

            AzureOpenAIClient azureClient = new AzureOpenAIClient(
                new Uri(_endpoint),
                new ApiKeyCredential(_key));

            ChatClient chatClient = azureClient.GetChatClient(languageModelName);

            
            if (prompt == -1)
            {
                // Do nothing
            }
            else if (prompt == -2)
            {
                try
                {
                    var userPrompt = mainSystemPrompt;
                    if(!string.IsNullOrWhiteSpace(fieldContent)) userPrompt += " using the following text " + fieldContent;
                    ChatCompletion completion = chatClient.CompleteChat(new ChatMessage[] {
                        new SystemChatMessage("You are a helpful assistant"),
                        new SystemChatMessage("Please include html markup in your response"),
                        new SystemChatMessage("The text should use words from british english en-gb"),
                        new UserChatMessage(userPrompt)
                    });
                    searchResponse.RawJson = completion.Content[0].Text;
                    searchResponse.FullResponse = completion.Content[0].Text;
                }
                catch (Exception ex)
                {
                    searchResponse.FullResponse = "There was a problem sending your prompt: \n\r" + ex.Message;
                }
                searchResponse.OriginalText = fieldContent;
            }
            else {
                try
                {
                    var userPrompt = mainSystemPrompt + " using the following text " + fieldContent;

                    ChatCompletion completion = chatClient.CompleteChat(new ChatMessage[] {
                        new SystemChatMessage("You are a helpful assistant"),
                        new SystemChatMessage("Please include html markup in OriginalText and RevisedText"),
                        new SystemChatMessage("For each suggestion and each action, please surround the values in OriginalText and RevisedText with an html span tag with a class set to id of the action. The span tags should be added to OriginalText and RevisedText and not added to OldText or NewText. Each span should have a class but no id."),
                        new SystemChatMessage("The text should use words from british english en-gb"),
                        new SystemChatMessage("Please make a minimum of 5 suggested changes"),
                        new SystemChatMessage("Please supply a JSON response in the format {'OriginalText':'value', 'RevisedText':'value', 'Suggestions': [{'Title':'Value','Actions':['Id':'Value','Explanation':'Value','OldText':'Value','NewText':'Value']}]}"),
                        new SystemChatMessage("Please ensure the response is valid JSON"),
                        new UserChatMessage(userPrompt)

                    });
                    try
                    {
                        searchResponse = JsonConvert.DeserializeObject<LanguageAssistantViewModel>(completion.Content[0].Text);
                    }
                    catch(Exception ex)
                    {
                        Log.Info(ex.Message, this);
                        searchResponse.OriginalText = completion.Content[0].Text;
                    }
                    searchResponse.RawJson = completion.Content[0].Text;
                }
                catch (Exception ex)
                {
                    searchResponse = new LanguageAssistantViewModel() { OriginalText = ex.Message };
                }
            }
            searchResponse.ItemId = id;
            searchResponse.MainPrompt = mainSystemPrompt;
            searchResponse.Prompts = prompts;
            searchResponse.OriginalFieldValue = fieldContent;
            searchResponse.Fields= fields;
            searchResponse.DeployedModels = deployedModels;
            searchResponse.Field = targetField;
            searchResponse.DeployedModel = languageModelName;
            timer.Stop();

            TimeSpan timeTaken = timer.Elapsed;
            searchResponse.TimeTaken = "Time taken: " + timeTaken.ToString(@"m\:ss\.fff");

            return View("~/sitecore/shell/client/Applications/LanguageAssistant/Index.cshtml", searchResponse);

        }
        private static string HtmlToPlainText(string html)
        {
            const string tagWhiteSpace = @"(>|$)(\W|\n|\r)+<";//matches one or more (white space or line breaks) between '>' and '<'
            const string stripFormatting = @"<[^>]*(>|$)";//match any character between '<' and '>', even when end tag is missing
            const string lineBreak = @"<(br|BR)\s{0,1}\/{0,1}>";//matches: <br>,<br/>,<br />,<BR>,<BR/>,<BR />
            var lineBreakRegex = new Regex(lineBreak, RegexOptions.Multiline);
            var stripFormattingRegex = new Regex(stripFormatting, RegexOptions.Multiline);
            var tagWhiteSpaceRegex = new Regex(tagWhiteSpace, RegexOptions.Multiline);

            var text = html;
            //Decode html specific characters
            text = System.Net.WebUtility.HtmlDecode(text);
            //Remove tag whitespace/line breaks
            text = tagWhiteSpaceRegex.Replace(text, "><");
            //Replace <br /> with line breaks
            text = lineBreakRegex.Replace(text, Environment.NewLine);
            //Strip formatting
            text = stripFormattingRegex.Replace(text, string.Empty);

            return text;
        }
        public string TruncateAtWord(string value, int length)
        {
            if (value == null || value.Length < length || value.IndexOf(" ", length) == -1)
                return value;

            return value.Substring(0, value.IndexOf(" ", length));
        }
    }
}

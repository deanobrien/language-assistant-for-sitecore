using System;
using System.Collections.Generic;

namespace DeanOBrien.Feature.LanguageAssistant.Models
{
    public class LanguageAssistantViewModel
    {
        public string ItemId { get; set; }
        public string MainPrompt { get; set; }
        public string TimeTaken { get; set; }
        public string FullResponse { get; set; }
        public string OriginalFieldValue { get; set; }
        public string OriginalText { get; set; }
        public string RevisedText { get; set; }
        public string RawJson { get; set; }
        public string ErrorMessage { get; set; }
        public List<Tuple<int,string>> Prompts { get; set; }
        public Suggestion[] Suggestions { get; set; }
        public string Field { get; set; }
        public List<Tuple<string, string>> Fields { get; set; }
        public List<string> DeployedModels { get; set; }
        public string DeployedModel { get; set; }
        public string promptDetails { get; set; }
    }
    public class ChatGptResponse
    {
        public string OriginalText { get; set; }
        public string RevisedText { get; set; }
        public Suggestion[] Suggestions { get; set; }
    }
    public class Suggestion
    {
        public string Title { get; set; }
        public Action[] Actions { get; set; }
    }
    public class Action
    {
        public string Id { get; set; }

        public string Explanation { get; set; }
        public string OldText { get; set; }

        public string NewText { get; set; }

    }
}

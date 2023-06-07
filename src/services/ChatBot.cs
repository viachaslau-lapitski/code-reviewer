#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Azure;
using Azure.AI.OpenAI;
using Newtonsoft.Json;

namespace vsl
{
    class ChatBot
    {
        public static int InputTokens { get; } = 8192;
        private OpenAIClient _client;
        private string getPrompt(string patch) => $@"
        Please review the code patch below     
        1) Do not explain or summarize what code/change does.    
        2) Be concise.    
        3) List issues (bugs, risks) within the patch.    
        4) List code improvements if any within the patch.
        5) For every item found, give me a line index in the patch to comment on. Start with position 0 for the first line of the patch. Increment the position by 1 for each subsequent line.
        
        the expected result is json array:              
        """"""
        [{{comment = ""some text"", position = ""number""}}]              
        """"""
        
        patch:
        """"""
        {patch}
        """"""
        ";

        public ChatBot(string key)
        {
            _client = new OpenAIClient(
                new Uri("https://openai-vsl-002.openai.azure.com/"),
                new AzureKeyCredential(key));
        }

        public async Task<List<Review>> getReviews(string patch)
        {
            var prompt = getPrompt(patch);
            Response<ChatCompletions> responseWithoutStream = await _client.GetChatCompletionsAsync(
                "gpt-4",
            new ChatCompletionsOptions()
            {
                Messages =
                {
                    new ChatMessage(ChatRole.System, @"You are an AI assistant that conducts code reviews."),
                    new ChatMessage(ChatRole.User, prompt),
                },
                Temperature = (float)0.0,
                MaxTokens = 800,
                NucleusSamplingFactor = (float)0.95,
                FrequencyPenalty = 0,
                PresencePenalty = 0,
            });

            ChatCompletions completions = responseWithoutStream.Value;
            var json = completions.Choices.FirstOrDefault()?.Message.FromAssistant()?.Content;
            return json != null ? JsonConvert.DeserializeObject<List<Review>>(json) : new List<Review>();
        }
    }
}
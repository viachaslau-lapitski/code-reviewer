#nullable enable

using System;
using System.Linq;
using System.Threading.Tasks;
using Azure;
using Azure.AI.OpenAI;

namespace vsl
{
    class ChatBot
    {
        public static int InputTokens { get; } = 8192;
        private OpenAIClient _client;
        private string getPrompt(string patch) => $@"
        Please review the code patch below     
        1) do not explain or summarize what code/change does    
        2) be concise    
        3) list only issues (bugs, risks), otherwise, say lgtm    
        4) list code improvements if any, otherwise do not generate anything
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

        public async Task<string?> getReview(string patch)
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
            return completions.Choices.FirstOrDefault()?.Message.FromAssistant()?.Content;
        }
    }
}
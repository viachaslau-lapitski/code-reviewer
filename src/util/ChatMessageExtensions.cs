#nullable enable
using Azure.AI.OpenAI;

namespace vsl
{
    public static class ChatMessageExtensions
    {
        public static ChatMessage? FromAssistant(this ChatMessage? message)
        {
            if (message?.Role == "assistant")
            {
                return message;
            }

            return null;
        }
    }
}
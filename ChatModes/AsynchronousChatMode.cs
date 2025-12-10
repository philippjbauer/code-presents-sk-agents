using System.Text;
using CODE.Presents.SemanticKernel.Helpers;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Spectre.Console;

namespace CODE.Presents.SemanticKernel.ChatModes;

public static class AsynchronousChatMode
{
    public static async Task ProcessInput(IChatCompletionService chatService, ChatHistory chatHistory, string input)
    {
        chatHistory.AddUserMessage(input);

        await AnsiConsole.Live(new Panel("awaiting respone …"))
            .StartAsync(async ctx =>
            {
                StringBuilder content = new();
                await foreach (StreamingChatMessageContent message
                    in chatService.GetStreamingChatMessageContentsAsync(chatHistory))
                {
                    string chunk = message.Content ?? string.Empty;
                    content.Append(chunk);

                    ctx.UpdateTarget(ChatMessageFormatter.CreateChatMessagePanel(
                        content.ToString(),
                        message.AuthorName,
                        message.Role ?? AuthorRole.Assistant));
                }

                chatHistory.AddAssistantMessage(content.ToString());
            });
    }
}
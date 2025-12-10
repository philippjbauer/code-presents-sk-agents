using CODE.Presents.SemanticKernel.Helpers;
using Microsoft.SemanticKernel.ChatCompletion;
using Spectre.Console;

namespace CODE.Presents.SemanticKernel.ChatModes;

public static class SynchronousChatMode
{
    public static async Task ProcessInput(IChatCompletionService chatService, ChatHistory chatHistory, string input)
    {
        chatHistory.AddUserMessage(input);

        await AnsiConsole.Status()
            .StartAsync("Thinking...", async ctx =>
            {
                var response = await chatService.GetChatMessageContentAsync(chatHistory);
                chatHistory.AddAssistantMessage(response.Content ?? "No respose.");
            });

        ChatMessageFormatter.DisplayChatMessage(chatHistory.Last());
    }
}

using System.Text;
using CODE.Presents.SemanticKernel.Agents.Plugins;
using CODE.Presents.SemanticKernel.Helpers;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Spectre.Console;

namespace CODE.Presents.SemanticKernel.ChatModes;

public static class FunctionCallingChatMode
{
    public static async Task ProcessInput(IChatCompletionService chatService, Kernel kernel, ChatHistory chatHistory, string input)
    {
        // Clone the kernel to avoid polluting the main kernel with plugins
        Kernel clonedKernel = kernel.Clone();
        clonedKernel.Plugins.AddFromType<EmailPlugin>(nameof(EmailPlugin));

        OpenAIPromptExecutionSettings promptExecutionSettings = new()
        {
            FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
        };

        chatHistory.AddSystemMessage("""
        You are an assistant specialized in managing email using the EmailPlugin functions (get_emails, get_unread_emails, get_email_by_id, mark_email_as_read, count_emails, count_unread_emails).
        - Focus on the most common tasks: list inbox, list unread, show an email (full content), count total/unread, and mark an email as read.
        - Prefer calling the plugin for factual actions and data; do not invent email content or IDs.
        - For list responses: present a compact table with columns `Id`, `Sender`, `Subject`, and `ReceivedAt` (human-friendly date).
        - For showing a single email: always include `Subject`, `Sender`, `ReceivedAt`, and the full `Body` text. Don't show a list or table view when showing or "opening" a message.
        - For actions that change state (e.g., mark as read): confirm intent unless the user explicitly requested the action, then call `mark_email_as_read` and report success or failure.
        - If multiple matches are possible or an ID is required, ask one concise clarifying question.
        - Handle empty results clearly (e.g., "No unread emails") and suggest the most useful next step.
        - Keep responses concise and end with one actionable suggestion or question.
        """);

        chatHistory.AddUserMessage(input);

        int skipMessages = chatHistory.Count;

        await AnsiConsole.Live(new Panel("awaiting respone …"))
            .StartAsync(async ctx =>
            {
                StringBuilder content = new();
                await foreach (StreamingChatMessageContent message
                    in chatService.GetStreamingChatMessageContentsAsync(
                        chatHistory,
                        executionSettings: promptExecutionSettings,
                        kernel: clonedKernel))
                {
                    string chunk = message.Content ?? string.Empty;
                    content.Append(chunk);

                    List<Panel> updatingRows = chatHistory
                        .Skip(skipMessages)
                        .Select(m => ChatMessageFormatter.CreateChatMessagePanel(m))
                        .ToList();

                    updatingRows.Add(ChatMessageFormatter.CreateChatMessagePanel(
                        content.ToString(),
                        message.AuthorName,
                        message.Role ?? AuthorRole.Assistant));

                    ctx.UpdateTarget(new Rows([.. updatingRows]));
                }

                chatHistory.AddAssistantMessage(content.ToString());

                List<Panel> finalRows = chatHistory.Skip(skipMessages)
                    .Select(m => ChatMessageFormatter.CreateChatMessagePanel(m))
                    .ToList();

                ctx.UpdateTarget(new Rows([.. finalRows]));
            });
    }
}

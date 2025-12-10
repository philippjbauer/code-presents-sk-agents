using System.Net.Http.Headers;
using System.Text;
using CODE.Presents.SemanticKernel.Agents.Plugins;
using CODE.Presents.SemanticKernel.Configuration;
using CODE.Presents.SemanticKernel.Helpers;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Memory;
using Spectre.Console;

namespace CODE.Presents.SemanticKernel.ChatModes;

public static class MemoryCompletionAgentChatMode
{
    public static async Task ProcessInput(AppConfiguration appConfig, Kernel kernel, ChatHistoryAgentThread agentThread, string input)
    {
        // Start a new thread if user wants to
        if (input.Equals("/new_thread", StringComparison.OrdinalIgnoreCase))
        {
            ChatHistory chatHistory = [];
            agentThread = new ChatHistoryAgentThread(chatHistory);

            AnsiConsole.Clear();
            AnsiConsole.MarkupLine("[dim]Started a new agent thread.[/]");
            return;
        }

        // Clone the kernel to avoid polluting the main kernel with plugins
        Kernel clonedKernel = kernel.Clone();
        clonedKernel.Plugins.AddFromType<EmailPlugin>(nameof(EmailPlugin));

        // Agent using Chat Completion
        var agent = new ChatCompletionAgent()
        {
            Name = "EmailAgent",
            Description = "An agent that helps the user complete tasks using available plugins.",
            Instructions = """
                You are an AI agent designed to assist users in completing tasks efficiently and professionally using the available plugins and your memory.
                - Carefully analyze the user's request and determine if a plugin or your memory is needed to fulfill it.
                - Use plugins only when necessary, providing all required parameters accurately.
                - If the request can be answered using only your memory or chat history, do so without invoking plugins.
                - Retrieve and leverage relevant previous information about the user and chat history to provide helpful, context-aware responses.
                - Respond clearly and concisely, avoiding unnecessary questions or steps.
                - If the user's request is complete and no further action is needed, provide the final response directly.
                - Always act in the user's best interest, maintaining professionalism and privacy.
                - If you utilize thinking steps, make them concise and to the point without repeating the same thoughts.
                - When you show an email, always include the subject line, sender, date and full text of the email.
                - When you list emails, include the subject line, sender and date of each email. Format as a table for better readability.
                """,
            Kernel = clonedKernel,
            Arguments = new KernelArguments(
                new OpenAIPromptExecutionSettings
                {
                    FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
                }),
        };

        // Agent using Mem0 for memory
        using var httpClient = new HttpClient(new Mem0HttpClientHandler())
        {
            BaseAddress = new Uri("https://api.mem0.ai")
        };
        httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Token", appConfig.MemoryProvider.ApiKey);

        // Create a Mem0 provider for the current user.
        var mem0Provider = new Mem0Provider(httpClient, options: new()
        {
            UserId = "af71de18-95c0-4dde-9791-de46f5972a43",
            ContextPrompt = "## Memories\nConsider the following memories when answering user questions:",
        });

        agentThread.AIContextProviders.Add(mem0Provider);

        // Ask the user if they want to clear all stored memories for the current user.
        var confirmClearMemory = AnsiConsole.Prompt(
            new TextPrompt<bool>(
                $"{Emoji.Known.PoliceCarLight} Do you want to clear all stored memories for the current user?")
                .AddChoice(true)
                .AddChoice(false)
                .DefaultValue(false)
                .WithConverter(choice => choice ? "y" : "N"));

        if (confirmClearMemory)
        {
            await mem0Provider.ClearStoredMemoriesAsync();
            AnsiConsole.MarkupLine("[dim]All stored memories have been cleared.[/]");
        }

        int skipMessages = agentThread.ChatHistory.Count + 1;

        // var result = await agent.InvokeAsync(input, agentThread).FirstAsync();
        // agentThread.ChatHistory.Skip(skipMessages)
        //     .ToList()
        //     .ForEach(m => ChatMessageFormatter.DisplayChatMessage(m));

        await AnsiConsole.Live(new Panel("[dim]awaiting respone …[/]")
            .Border(BoxBorder.Rounded)
            .BorderStyle("dim")
            .Padding(1, 0))
            .StartAsync(async ctx =>
            {
                try
                {
                    StringBuilder content = new();
                    await foreach (StreamingChatMessageContent message
                        in agent.InvokeStreamingAsync(input, agentThread))
                    {
                        string chunk = message.Content ?? string.Empty;
                        content.Append(chunk);

                        List<Panel> updatingRows = agentThread.ChatHistory
                            .Skip(skipMessages)
                            .Select(m => ChatMessageFormatter.CreateChatMessagePanel(m))
                            .ToList();

                        updatingRows.Add(ChatMessageFormatter.CreateChatMessagePanel(
                            content.ToString(),
                            message.AuthorName,
                            message.Role ?? AuthorRole.Assistant));

                        ctx.UpdateTarget(new Rows([.. updatingRows]));
                    }

                    List<Panel> finalRows = agentThread.ChatHistory.Skip(skipMessages)
                        .Select(m => ChatMessageFormatter.CreateChatMessagePanel(m))
                        .ToList();

                    ctx.UpdateTarget(new Rows([.. finalRows]));
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                }
            });
    }
}

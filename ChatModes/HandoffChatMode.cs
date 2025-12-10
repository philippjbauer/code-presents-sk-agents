using System.Text;
using CODE.Presents.SemanticKernel.Agents.ActualBudget;
using CODE.Presents.SemanticKernel.Agents.Plugins;
using CODE.Presents.SemanticKernel.Configuration;
using CODE.Presents.SemanticKernel.Helpers;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.Orchestration;
using Microsoft.SemanticKernel.Agents.Orchestration.Handoff;
using Microsoft.SemanticKernel.Agents.Runtime.InProcess;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using ModelContextProtocol.Client;
using Spectre.Console;

namespace CODE.Presents.SemanticKernel.ChatModes;

public static class HandoffChatMode
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

        // Agent using Chat Completion
        // Clone the kernel to avoid polluting the main kernel with plugins
        Kernel emailKernel = kernel.Clone();
        emailKernel.Plugins.AddFromType<EmailPlugin>(nameof(EmailPlugin));

        var emailAgent = new ChatCompletionAgent()
        {
            Name = "EmailAgent",
            Description = "An agent that helps the user complete tasks using available plugins.",
            Instructions = $"""
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
                - Use the HandoffPlugin to transfer the conversation to another agent if you can't handle the request.

                Today's date is: {DateTime.UtcNow:yyyy-MM-dd}
                """,
            Kernel = emailKernel,
            Arguments = new KernelArguments(
                new OpenAIPromptExecutionSettings
                {
                    FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
                }),
        };

        // Finance Agent using Chat Completion
        McpClient mcpClient = await ActualBudgetAgent.CreateMcpClientAsync(appConfig.ActualBudget);
        var tools = await mcpClient.ListToolsAsync();

        Kernel financialKernel = kernel.Clone();
        financialKernel.Plugins.AddFromFunctions("budget", tools.Select(t => t.AsKernelFunction()));

        var financeAgent = new ChatCompletionAgent()
        {
            Name = "FinanceAgent",
            Description = "An agent that helps the user manage their finances using Actual Budget tools.",
            Instructions = $"""
                You are an AI agent designed to assist users in managing their finances efficiently using the Actual Budget tools and your memory.
                - Carefully analyze the user's request and determine if a tool or your memory is needed to fulfill it.
                - Use tools only when necessary, providing all required parameters accurately.
                - If the request can be answered using only your memory or chat history, do so without invoking tools.
                - Retrieve and leverage relevant previous information about the user and chat history to provide helpful, context-aware responses.
                - Respond clearly and concisely, avoiding unnecessary questions or steps.
                - If the user's request is complete and no further action is needed, provide the final response directly.
                - Always act in the user's best interest, maintaining professionalism and privacy.
                - If you utilize thinking steps, make them concise and to the point without repeating the same thoughts.
                - Use the HandoffPlugin to transfer the conversation to another agent if you can't handle the request.

                Today's date is: {DateTime.UtcNow:yyyy-MM-dd}
                """,
            Kernel = financialKernel,
            Arguments = new KernelArguments(
                new OpenAIPromptExecutionSettings
                {
                    FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
                }),
        };

        // Handoff decision agent: inspects the conversation and returns which agent should handle the request.
        Kernel handoffKernel = kernel.Clone();
        var handoffAgent = new ChatCompletionAgent()
        {
            Name = "HandoffAgent",
            Description = "Decides which specialist agent should handle the user's request (EmailAgent, FinanceAgent, or Human).",
            Instructions = """
                You are a routing assistant whose sole job is to inspect the conversation and the user's latest request
                and decide which agent should handle it. Call the HandoffPlugin to transfer to `EmailAgent`, `FinanceAgent`, or back to the human.
                - Consider both the user's latest message and relevant assistant messages from the chat history.
                - If the user clearly asks to read/send/format emails or anything email-related, choose `EmailAgent`.
                - If the user asks about budgets, transactions, payees, accounts, or other Actual Budget operations, choose `FinanceAgent`.
                - If the request is ambiguous, unsafe, or requires a human, choose `Human` and include a short reason.
                - Always use the HandoffPlugin to make your decision.
                """,
            Kernel = handoffKernel,
            Arguments = new KernelArguments(
                new OpenAIPromptExecutionSettings
                {
                    FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
                }),
        };

        // Configure the handoff orchestration
        var handoffs = OrchestrationHandoffs
            .StartWith(handoffAgent)
            .Add(handoffAgent, emailAgent, financeAgent)
            .Add(emailAgent, handoffAgent, "Transfer to this agent if the issue is no longer email-related.")
            .Add(financeAgent, handoffAgent, "Transfer to this agent if the issue is no longer finance-related.");

        static ValueTask<ChatMessageContent> interactiveCallback()
        {
            string input = AnsiConsole.Prompt(new TextPrompt<string>($"{Emoji.Known.Person} User:"));
            return new ValueTask<ChatMessageContent>(new ChatMessageContent(AuthorRole.User, input));
        }

        ValueTask responseCallback(ChatMessageContent message)
        {
            agentThread.ChatHistory.AddAssistantMessage(message.Content ?? string.Empty);
            ChatMessageFormatter.DisplayChatMessage(message);
            return new ValueTask();
        }

        HandoffOrchestration orchestration = new(
            handoffs,
            handoffAgent,
            emailAgent,
            financeAgent)
        {
            InteractiveCallback = interactiveCallback,
            ResponseCallback = responseCallback
        };

        InProcessRuntime runtime = new();
        await runtime.StartAsync();

        OrchestrationResult<string> result = await orchestration.InvokeAsync(input, runtime);
        string output = await result.GetValueAsync(timeout: TimeSpan.FromSeconds(900));

        ChatMessageFormatter.DisplayChatMessage(new ChatMessageContent(AuthorRole.Assistant, output ?? string.Empty));
    }
}

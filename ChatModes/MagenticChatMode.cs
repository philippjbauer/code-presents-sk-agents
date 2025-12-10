using CODE.Presents.SemanticKernel.Agents.ActualBudget;
using CODE.Presents.SemanticKernel.Configuration;
using CODE.Presents.SemanticKernel.Helpers;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.Magentic;
using Microsoft.SemanticKernel.Agents.Orchestration;
using Microsoft.SemanticKernel.Agents.Runtime.InProcess;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Spectre.Console;

namespace CODE.Presents.SemanticKernel.ChatModes;

public static class MagenticChatMode
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

        static ValueTask<ChatMessageContent> interactiveCallback()
        {
            string input = AnsiConsole.Prompt(new TextPrompt<string>($"{Emoji.Known.Person} User:"));
            return new ValueTask<ChatMessageContent>(new ChatMessageContent(AuthorRole.User, input));
        }

        var manager = new LoggedMagenticManager(
            kernel.GetRequiredService<IChatCompletionService>()!,
            new OpenAIPromptExecutionSettings())
        {
            MaximumInvocationCount = 25,
            InteractiveCallback = interactiveCallback
        };

        OpenAIPromptExecutionSettings promptSettings = new()
        {
            FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(),
            ChatSystemPrompt = """
            When you think through problems or plan your approach, make your thinking steps concise and to the point without repeating the same thoughts. If you have thought through something already, do not restate it.
            """
        };

        Agent[] agents = [
            await AccountAgent.CreateAsync(kernel, appConfig.ActualBudget, promptSettings),
            await TransactionAgent.CreateAsync(kernel, appConfig.ActualBudget, promptSettings),
            await CategoryAgent.CreateAsync(kernel, appConfig.ActualBudget, promptSettings),
            await PayeeAgent.CreateAsync(kernel, appConfig.ActualBudget, promptSettings)
        ];

        ValueTask responseCallback(ChatMessageContent message)
        {
            agentThread.ChatHistory.AddAssistantMessage(message.Content ?? string.Empty);
            ChatMessageFormatter.DisplayChatMessage(message);
            return new ValueTask();
        }

        MagenticOrchestration orchestration = new(manager, agents)
        {
            Name = "MagenticOrchestration",
            Description = "An orchestration that uses MagenticManager to coordinate multiple agents for complex tasks.",
            ResponseCallback = responseCallback
        };

        InProcessRuntime runtime = new();
        await runtime.StartAsync();

        OrchestrationResult<string> result = await orchestration.InvokeAsync(input, runtime);
        string output = await result.GetValueAsync(timeout: TimeSpan.FromSeconds(900));
    }
}

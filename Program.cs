using CODE.Presents.SemanticKernel.Configuration;
using CODE.Presents.SemanticKernel.ChatModes;
using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.ChatCompletion;
using Spectre.Console;

namespace CODE.Presents.SemanticKernel;

public class Program
{
    public static async Task Main(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddEnvironmentVariables()
            .AddUserSecrets<Program>(optional: true)
            .Build();

        AppConfiguration appConfig = new();
        configuration.Bind(appConfig);

        // Display loaded configuration in a Panel (for debugging purposes)
        AnsiConsole.Write(new Panel(
            $"""
            [bold]Model ID:[/] {appConfig.OpenAI.ModelId}
            [bold]API Key:[/] {string.Empty.PadRight(Math.Min(appConfig.OpenAI.ApiKey.Length - 5, 20), '*')}{appConfig.OpenAI.ApiKey[^5..]}
            [bold]Endpoint:[/] {appConfig.OpenAI.Endpoint}
            """)
            .Header($"– {Emoji.Known.Gear} Loaded Configuration -", Justify.Center)
            .Border(BoxBorder.Rounded)
            .Padding(1, 0));

        IKernelBuilder kernelBuilder = Kernel.CreateBuilder()
            .AddOpenAIChatCompletion(
                modelId: appConfig.OpenAI.ModelId,
                apiKey: appConfig.OpenAI.ApiKey,
                endpoint: new Uri(appConfig.OpenAI.Endpoint));

        Kernel kernel = kernelBuilder.Build();

        IChatCompletionService chatService = kernel.GetRequiredService<IChatCompletionService>()
            ?? throw new InvalidOperationException("Chat Completion Service is not available in the kernel.");

        ChatHistory chatHistory = [];
        ChatHistoryAgentThread agentThread = new(chatHistory);

        string input = string.Empty;
        do
        {
            input = AnsiConsole.Prompt(new TextPrompt<string>($"{Emoji.Known.Person} User:"));

            if (string.IsNullOrWhiteSpace(input)
                || input.Equals("/exit", StringComparison.OrdinalIgnoreCase))
                break;

            AnsiConsole.WriteLine();

            // Simple Chat Modes:

            // await SynchronousChatMode.ProcessInput(chatService, chatHistory, input);
            // await AsynchronousChatMode.ProcessInput(chatService, chatHistory, input);

            // Function Calling -> Agent Chat Mode:

            // await FunctionCallingChatMode.ProcessInput(chatService, kernel, chatHistory, input);
            // await CompletionAgentChatMode.ProcessInput(kernel, agentThread, input);
            // await MemoryCompletionAgentChatMode.ProcessInput(appConfig, kernel, agentThread, input);

            // Multi-Agent Orchestration Patterns:

            // | Pattern    | Description                                                                     | Typical Use Case                                                      |
            // | ---------- | ------------------------------------------------------------------------------- | --------------------------------------------------------------------- |
            // | Concurrent | Broadcasts a task to all agents, collects results independently.                | Parallel analysis, independent subtasks, ensemble decision making.    |
            // | Sequential | Passes the result from one agent to the next in a defined order.                | Step-by-step workflows, pipelines, multi-stage processing.            |
            // | Handoff    | Dynamically passes control between agents based on context or rules.            | Dynamic workflows, escalation, fallback, or expert handoff scenarios. |
            // | Group Chat | All agents participate in a group conversation, coordinated by a group manager. | Brainstorming, collaborative problem solving, consensus building.     |
            // | Magentic   | Group chat-like orchestration inspired by MagenticOne.                          | Complex, generalist multi-agent collaboration.                        |

            // await HandoffChatMode.ProcessInput(appConfig, kernel, agentThread, input);
            // await MagenticChatMode.ProcessInput(appConfig, kernel, agentThread, input);

            AnsiConsole.WriteLine();
        }
        while (!string.IsNullOrWhiteSpace(input));
    }
}

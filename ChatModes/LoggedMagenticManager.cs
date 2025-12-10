using System.Diagnostics;
using System.Text;
using System.Text.Json;
using CODE.Presents.SemanticKernel.Helpers;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents.Magentic;
using Microsoft.SemanticKernel.Agents.Magentic.Internal;
using Microsoft.SemanticKernel.ChatCompletion;
using Spectre.Console;

namespace CODE.Presents.SemanticKernel.ChatModes;

/// <summary>
/// A <see cref="MagenticManager"/> that provides orchestration logic for managing magentic agents,
/// including preparing facts, plans, ledgers, evaluating progress, and generating a final answer.
/// </summary>
public sealed class LoggedMagenticManager : MagenticManager
{
    private static readonly Kernel EmptyKernel = new();

    private readonly IChatCompletionService _service;
    private readonly PromptExecutionSettings _executionSettings;

    private string _facts = string.Empty;
    private string _plan = string.Empty;

    /// <summary>
    /// Initializes a new instance of the <see cref="LoggedMagenticManager"/> class.
    /// </summary>
    /// <param name="service">The chat completion service to use for generating responses.</param>
    /// <param name="executionSettings">The prompt execution settings to use for the chat completion service.</param>
    public LoggedMagenticManager(IChatCompletionService service, PromptExecutionSettings executionSettings)
    {
        ArgumentNullException.ThrowIfNull(service, nameof(service));
        ArgumentNullException.ThrowIfNull(executionSettings, nameof(executionSettings));

        if (!executionSettings.SupportsResponseFormat())
        {
            throw new KernelException($"Unable to proceed with {nameof(PromptExecutionSettings)} that does not support structured JSON output.");
        }

        if (executionSettings.IsFrozen)
        {
            throw new KernelException($"Unable to proceed with frozen {nameof(PromptExecutionSettings)}.");
        }

        this._service = service;
        this._executionSettings = executionSettings;
        this._executionSettings.SetResponseFormat<MagenticProgressLedger>();
    }

    /// <inheritdoc/>
    public override async ValueTask<IList<ChatMessageContent>> PlanAsync(MagenticManagerContext context, CancellationToken cancellationToken)
    {
        this._facts = await this.PrepareTaskFactsAsync(context, MagenticPrompts.NewFactsTemplate, cancellationToken).ConfigureAwait(false);
        this._plan = await this.PrepareTaskPlanAsync(context, MagenticPrompts.NewPlanTemplate, cancellationToken).ConfigureAwait(false);

        Debug.WriteLine($"\n<FACTS>:\n{this._facts}\n</FACTS>\n\n<PLAN>:\n{this._plan}\n</PLAN>\n");

        return await this.PrepareTaskLedgerAsync(context, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public override async ValueTask<IList<ChatMessageContent>> ReplanAsync(MagenticManagerContext context, CancellationToken cancellationToken)
    {
        this._facts = await this.PrepareTaskFactsAsync(context, MagenticPrompts.RefreshFactsTemplate, cancellationToken).ConfigureAwait(false);
        this._plan = await this.PrepareTaskPlanAsync(context, MagenticPrompts.RefreshPlanTemplate, cancellationToken).ConfigureAwait(false);

        Debug.WriteLine($"\n<FACTS>:\n{this._facts}\n</FACTS>\n\n<PLAN>:\n{this._plan}\n</PLAN>\n");

        return await this.PrepareTaskLedgerAsync(context, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public override async ValueTask<MagenticProgressLedger> EvaluateTaskProgressAsync(MagenticManagerContext context, CancellationToken cancellationToken = default)
    {
        ChatHistory internalChat = [.. context.History];
        KernelArguments arguments =
            new()
            {
                { MagenticPrompts.Parameters.Task, this.FormatInputTask(context.Task) },
                { MagenticPrompts.Parameters.Team, context.Team.FormatNames() },
            };
        string response = await this.GetResponseAsync(internalChat, MagenticPrompts.StatusTemplate, arguments, this._executionSettings, cancellationToken).ConfigureAwait(false);
        MagenticProgressLedger status =
            JsonSerializer.Deserialize<MagenticProgressLedger>(response) ??
            throw new InvalidDataException($"Message content does not align with requested type: {nameof(MagenticProgressLedger)}.");

        return status;
    }

    /// <inheritdoc/>
    public override async ValueTask<ChatMessageContent> PrepareFinalAnswerAsync(MagenticManagerContext context, CancellationToken cancellationToken = default)
    {
        KernelArguments arguments =
            new()
            {
                { MagenticPrompts.Parameters.Task, this.FormatInputTask(context.Task) },
            };
        string response = await this.GetResponseAsync(context.History, MagenticPrompts.AnswerTemplate, arguments, executionSettings: null, cancellationToken).ConfigureAwait(false);

        return new ChatMessageContent(AuthorRole.Assistant, response);
    }

    private async ValueTask<string> PrepareTaskFactsAsync(MagenticManagerContext context, IPromptTemplate promptTemplate, CancellationToken cancellationToken = default)
    {
        KernelArguments arguments =
            new()
            {
                { MagenticPrompts.Parameters.Task, this.FormatInputTask(context.Task) },
                { MagenticPrompts.Parameters.Facts, this._facts },
            };
        return
            await this.GetResponseAsync(
                context.History,
                promptTemplate,
                arguments,
                executionSettings: null,
                cancellationToken).ConfigureAwait(false);
    }

    private async ValueTask<string> PrepareTaskPlanAsync(MagenticManagerContext context, IPromptTemplate promptTemplate, CancellationToken cancellationToken = default)
    {
        KernelArguments arguments =
            new()
            {
                { MagenticPrompts.Parameters.Team, context.Team.FormatList() },
            };

        return
            await this.GetResponseAsync(
                context.History,
                promptTemplate,
                arguments,
                executionSettings: null,
                cancellationToken).ConfigureAwait(false);
    }

    private async ValueTask<IList<ChatMessageContent>> PrepareTaskLedgerAsync(MagenticManagerContext context, CancellationToken cancellationToken = default)
    {
        KernelArguments arguments =
            new()
            {
                { MagenticPrompts.Parameters.Task, this.FormatInputTask(context.Task) },
                { MagenticPrompts.Parameters.Team, context.Team.FormatList() },
                { MagenticPrompts.Parameters.Facts, this._facts },
                { MagenticPrompts.Parameters.Plan, this._plan },
            };
        string ledger = await this.GetMessageAsync(MagenticPrompts.LedgerTemplate, arguments).ConfigureAwait(false);

        return [new ChatMessageContent(AuthorRole.System, ledger)];
    }

    private async ValueTask<string> GetMessageAsync(IPromptTemplate template, KernelArguments arguments)
    {
        return await template.RenderAsync(EmptyKernel, arguments).ConfigureAwait(false);
    }

    private async Task<string> GetResponseAsync(
        IReadOnlyList<ChatMessageContent> internalChat,
        IPromptTemplate template,
        KernelArguments arguments,
        PromptExecutionSettings? executionSettings,
        CancellationToken cancellationToken = default)
    {
        ChatHistory history = [.. internalChat];

        string message = await this.GetMessageAsync(template, arguments).ConfigureAwait(false);
        history.Add(new ChatMessageContent(AuthorRole.User, message));
        ChatMessageFormatter.DisplayChatMessage(history.Last());

        // ChatMessageContent response = await AnsiConsole.Status()
        //     .Spinner(Spinner.Known.Dots)
        //     .StartAsync($"{Emoji.Known.Robot} Agents are generating a response...", async _ =>
        //     {
        //         return await this._service.GetChatMessageContentAsync(
        //             history, executionSettings, kernel: null,
        //             cancellationToken).ConfigureAwait(false);
        //     });

        // ChatMessageFormatter.DisplayChatMessage(response);

        ChatMessageContent response = null!;
        await AnsiConsole.Live(new Panel("awaiting respone …"))
            .StartAsync(async ctx =>
            {
                StringBuilder content = new();
                await foreach (StreamingChatMessageContent message
                    in this._service.GetStreamingChatMessageContentsAsync(
                        history,
                        executionSettings,
                        kernel: null,
                        cancellationToken))
                {
                    string chunk = message.Content ?? string.Empty;
                    content.Append(chunk);

                    ctx.UpdateTarget(ChatMessageFormatter.CreateChatMessagePanel(
                        content.ToString(),
                        message.AuthorName,
                        message.Role ?? AuthorRole.Assistant));
                }

                response = new ChatMessageContent(AuthorRole.Assistant, content.ToString());
                history.AddAssistantMessage(content.ToString());
            });

        return response.Content ?? string.Empty;
    }

    private string FormatInputTask(IReadOnlyList<ChatMessageContent> inputTask) => string.Join("\n", inputTask.Select(m => $"{m.Content}"));
}

internal sealed class MagenticPrompts
{
    private static readonly KernelPromptTemplateFactory TemplateFactory = new() { AllowDangerouslySetContent = true };

    public static readonly IPromptTemplate NewFactsTemplate = InitializePrompt(Templates.AnalyzeFacts);
    public static readonly IPromptTemplate RefreshFactsTemplate = InitializePrompt(Templates.AnalyzeFacts);
    public static readonly IPromptTemplate NewPlanTemplate = InitializePrompt(Templates.AnalyzePlan);
    public static readonly IPromptTemplate RefreshPlanTemplate = InitializePrompt(Templates.AnalyzePlan);
    public static readonly IPromptTemplate LedgerTemplate = InitializePrompt(Templates.GenerateLedger);
    public static readonly IPromptTemplate StatusTemplate = InitializePrompt(Templates.AnalyzeStatus);
    public static readonly IPromptTemplate AnswerTemplate = InitializePrompt(Templates.FinalAnswer);

    private static IPromptTemplate InitializePrompt(string template)
    {
        PromptTemplateConfig templateConfig = new() { Template = template };
        return TemplateFactory.Create(templateConfig);
    }

    public static class Parameters
    {
        public const string Task = "task";
        public const string Team = "team";
        public const string Names = "names";
        public const string Facts = "facts";
        public const string Plan = "plan";
        public const string Ledger = "ledger";
    }

    private static class Templates
    {
        public const string AnalyzeFacts =
            $$$"""
                Respond to the pre-survey in response the following user request:
                
                {{${{{Parameters.Task}}}}}

                Here is the pre-survey:

                    1. Please list any specific facts or figures that are GIVEN in the request itself. It is possible that
                       there are none.
                    2. Please list any facts that may need to be looked up, and WHERE SPECIFICALLY they might be found.
                       In some cases, authoritative sources are mentioned in the request itself.
                    3. Please list any facts that may need to be derived (e.g., via logical deduction, simulation, or computation)
                    4. Please list any facts that are recalled from memory, hunches, well-reasoned guesses, etc.

                When answering this survey, keep in mind that "facts" will typically be specific names, dates, statistics, etc.

                Your answer MUST use these headings:

                    1. GIVEN OR VERIFIED FACTS
                    2. FACTS TO LOOK UP
                    3. FACTS TO DERIVE
                    4. EDUCATED GUESSES

                DO NOT include any other headings or sections in your response. DO NOT list next steps or plans.
                """;

        public const string UpdateFacts =
            $$$"""
                As a reminder, we are working to solve the following request:

                {{${{{Parameters.Task}}}}}
                
                It's clear we aren't making as much progress as we would like, but we may have learned something new.
                Please rewrite the following fact sheet, updating it to include anything new we have learned that may be helpful.

                Example edits can include (but are not limited to) adding new guesses, moving educated guesses to verified facts
                if appropriate, etc. Updates may be made to any section of the fact sheet, and more than one section of the fact
                sheet can be edited. This is an especially good time to update educated guesses, so please at least add or update
                one educated guess or hunch, and explain your reasoning.

                Here is the old fact sheet:

                {{${{{Parameters.Facts}}}}}                
                """;

        public const string AnalyzePlan =
            $$$"""
                To address this request we have assembled the following team:

                {{${{{Parameters.Team}}}}}

                Define the plan that addresses the user request in the fewest steps possible given the tools available to the team.

                Ensure that the plan:

                - Is formatted as plan as a markdown list of sequential steps with each top-level bullet-point as: "{Agent Name}: {Actions, goals, or sub-list}".
                - Only includes the team members that are required to respond to the request.
                - Excludes extra steps that are not necessary and slow down the process.
                - Does not seek final confirmation from the user.
                - If you utilize thinking steps, make them concise and to the point without repeating the same thoughts.
                """;

        public const string UpdatePlan =
            $$$"""
                Please briefly explain what went wrong on this last run (the root
                cause of the failure), and then come up with a new plan that takes steps and/or includes hints to overcome prior
                challenges and especially avoids repeating the same mistakes. As before, the new plan should be concise, be expressed
                in bullet-point form, and consider the following team composition (do not involve any other outside people since we
                cannot contact anyone else):

                {{${{{Parameters.Team}}}}}                
                """;

        public const string GenerateLedger =
            $$$"""
                We are working to address the following user request:

                {{${{{Parameters.Task}}}}}


                To answer this request we have assembled the following team:

                {{${{{Parameters.Team}}}}}


                Here is an initial fact sheet to consider:

                {{${{{Parameters.Facts}}}}}


                Here is the plan to follow as best as possible:

                {{${{{Parameters.Plan}}}}}
                """;

        public const string AnalyzeStatus =
            $$$"""
                Recall we are working on the following request:

                {{${{{Parameters.Task}}}}}
                
                And we have assembled the following team:

                {{${{{Parameters.Team}}}}}
                
                To make progress on the request, please answer the following questions, including necessary reasoning:

                    - Is the request fully satisfied?  (True if complete, or False if the original request has yet to be SUCCESSFULLY and FULLY addressed)
                    - Are we in a loop where we are repeating the same requests and / or getting the same responses as before?
                      Loops can span multiple responses.
                    - Are we making forward progress? (True if just starting, or recent messages are adding value.
                      False if recent messages show evidence of being stuck in a loop or if there is evidence of the inability to proceed)
                    - Which team member is needed to respond next? (Select only from: {{${{{Parameters.Names}}}}}).
                      Always consider then initial plan but you may deviate from this plan as appropriate based on the conversation.
                    - Do not seek final confirmation from the user if the request is fully satisfied.
                    - What direction would you give this team member? (Always phrase in the 2nd person, speaking directly to them, and
                      include any specific information they may need)
                    - If you utilize thinking steps, make them concise and to the point without repeating the same thoughts.
                """;

        public const string FinalAnswer =
            $$$"""
                Synthesize a complete response to the user request using markdown format:

                {{${{{Parameters.Task}}}}}

                The complete response MUST:
                - Consider the entire conversation without incorporating information that changed or was corrected
                - NEVER include any new information not already present in the conversation
                - Capture verbatim content instead of summarizing
                - Directly address the request without narrating how the conversation progressed
                - Incorporate images specified in conversation responses
                - Include all citations or references
                - Be phrased to directly address the user
                - If you utilize thinking steps, make them concise and to the point without repeating the same thoughts.
                """;
    }
}

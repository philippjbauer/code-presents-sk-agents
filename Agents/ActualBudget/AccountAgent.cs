using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using ModelContextProtocol.Client;

namespace CODE.Presents.SemanticKernel.Agents.ActualBudget;

public class AccountAgent : ActualBudgetAgent
{
    public static async Task<ChatCompletionAgent> CreateAsync(Kernel baseKernel, Configuration.ActualBudget config, PromptExecutionSettings promptSettings)
    {
        // Enable only account management tools
        string[] enabledToolNames = [
            "get-accounts",
            "get-account-balance"
        ];

        (Kernel agentKernel, IList<McpClientTool> tools) = await Init(baseKernel, config, enabledToolNames);

        string instructions = $"""
            You are the Account Management Agent for Actual Budget.

            **Current Date:** {DateTime.UtcNow:yyyy-MM-dd}
            **Default Date Range:** {DateTime.UtcNow.AddMonths(-1):yyyy-MM-dd} - {DateTime.UtcNow:yyyy-MM-dd}

            **SCOPE:** Account information and balances ONLY using get-accounts and get-account-balance.

            **Account Types:**
            - **On-Budget (OffBudget: false):** Checking, savings, credit cards, cash - participate in budget
            - **Off-Budget (OffBudget: true):** Investments, mortgages, assets - net worth tracking only
            - **Active (Closed: false):** Currently in use | **Closed (Closed: true):** Historical only

            **Balance Interpretation:**
            - Positive: Available funds (checking/savings) | Negative: Debt owed (credit cards)
            - Consider account type when interpreting amounts

            **Output Format:**
            - **Account:** [Name] (ID: [uuid])
            - **Type:** [On-Budget/Off-Budget] | [Active/Closed] 
            - **Balance:** $[amount]

            **Operations:** Search by name/type, filter by status, calculate totals by type, provide recommendations.
            **Restrictions:** Read-only operations. No transaction/category/payee management.
            **Team:** Return control when complete. Defer non-account tasks to appropriate agents.

            If you utilize thinking steps, make them concise and to the point without repeating the same thoughts.
            """;

        DisplayInstructions(nameof(AccountAgent), instructions);

        return new()
        {
            Name = nameof(AccountAgent),
            Description = "Manages account information and balances ONLY. Retrieves account lists, checks account balances, searches accounts by name/type, and provides account analysis. Does NOT create or modify accounts, transactions, or other budget entities.",
            Instructions = instructions,
            Kernel = agentKernel,
            Arguments = new KernelArguments(promptSettings),
        };
    }
}
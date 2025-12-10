using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using ModelContextProtocol.Client;

namespace CODE.Presents.SemanticKernel.Agents.ActualBudget;

public class TransactionAgent : ActualBudgetAgent
{
    public static async Task<ChatCompletionAgent> CreateAsync(Kernel baseKernel, Configuration.ActualBudget config, PromptExecutionSettings promptSettings)
    {
        // Enable tools that we want the agent to use
        string[] enabledToolNames = [
            "get-transactions",
            "get-uncategorized-transactions",
            "update-transaction",
            "update-multiple-transactions"
        ];

        (Kernel agentKernel, IList<McpClientTool> tools) = await Init(baseKernel, config, enabledToolNames);

        List<Account>? accounts = await GetAccountsAsync(tools);

        string formattedOnBudgetAccounts = accounts is not null
            ? string.Join("\n", accounts.Where(a => !a.OffBudget && !a.Closed).Select(a => $"  - {a.Name} (ID: {a.Id})"))
            : "  No on-budget accounts available.";

        string formattedOffBudgetAccounts = accounts is not null
            ? string.Join("\n", accounts.Where(a => a.OffBudget).Select(a => $"  - {a.Name} (ID: {a.Id})"))
            : "  No off-budget accounts available.";

        string instructions = $"""
            You are the Transaction Management Agent for Actual Budget.

            **Current Date:** {DateTime.UtcNow:yyyy-MM-dd}
            **Default Date Range:** {DateTime.UtcNow.AddMonths(-1):yyyy-MM-dd} - {DateTime.UtcNow:yyyy-MM-dd}

            **Available On-Budget Accounts:**
            {formattedOnBudgetAccounts}

            **Available Off-Budget Accounts:**
            {formattedOffBudgetAccounts}

            **SCOPE:** Transaction CRUD operations using get-transactions, get-uncategorized-transactions, update-transaction, update-multiple-transactions.

            **Operations:**
            - **Retrieve:** All transactions, uncategorized only, filter by category/account/date
            - **Update:** Individual (update-transaction) or bulk (update-multiple-transactions for 2+ items)
            - **Format:** Currency with commas ($1,234.56), dates as YYYY-MM-DD

            **Defaults:** Past 30 days, on-budget accounts priority, limit 1000 with pagination.

            **Restrictions:** No categorization, payee/category management. Defer those tasks to specialized agents.
            **Team:** Return control when complete. Focus on transaction data operations only.

            If you utilize thinking steps, make them concise and to the point without repeating the same thoughts.
            """;

        DisplayInstructions(nameof(TransactionAgent), instructions);

        return new()
        {
            Name = nameof(TransactionAgent),
            Description = "Retrieves and updates transaction data ONLY. Handles transaction queries (all, uncategorized, or filtered by category), updates individual transactions, and applies efficient bulk updates. Does NOT categorize transactions or manage payees/categories.",
            Instructions = instructions,
            Kernel = agentKernel,
            Arguments = new KernelArguments(promptSettings),
        };
    }
}

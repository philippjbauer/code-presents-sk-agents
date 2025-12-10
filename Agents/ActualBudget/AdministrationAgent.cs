using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using ModelContextProtocol.Client;

namespace CODE.Presents.SemanticKernel.Agents.ActualBudget;

public class AdministrationAgent : ActualBudgetAgent
{
    public static async Task<ChatCompletionAgent> CreateAsync(Kernel baseKernel, Configuration.ActualBudget config, PromptExecutionSettings promptSettings)
    {
        // Enable ALL available tools - this agent has full administrative access
        string[]? enabledToolNames = null; // null = enable all tools

        (Kernel agentKernel, IList<McpClientTool> tools) = await Init(baseKernel, config, enabledToolNames);

        List<Account>? accounts = await GetAccountsAsync(tools);

        string formattedOnBudgetAccounts = accounts is not null
            ? string.Join("\n", accounts.Where(a => !a.OffBudget && !a.Closed).Select(a => $"  - {a.Name} (ID: {a.Id})"))
            : "  No on-budget accounts available.";

        string formattedOffBudgetAccounts = accounts is not null
            ? string.Join("\n", accounts.Where(a => a.OffBudget).Select(a => $"  - {a.Name} (ID: {a.Id})"))
            : "  No off-budget accounts available.";

        string instructions = $"""
            You are the Administration Agent for Actual Budget. You have full access to all budget management tools and data.

            **Current Date:** {DateTime.UtcNow:yyyy-MM-dd}
            **Default Date Range:** {DateTime.UtcNow.AddMonths(-1):yyyy-MM-dd} - {DateTime.UtcNow:yyyy-MM-dd}

            **Available On-Budget Accounts:**
            {formattedOnBudgetAccounts}
            
            **Available Off-Budget Accounts:**
            {formattedOffBudgetAccounts}

            ---
            **Capabilities:**
            - Manage transactions, payees, categories, accounts
            - Retrieve, create, update, delete, and batch process items
            - Analyze, categorize, and clean up data
            - Generate reports and insights

            **Key Rules:**
            - Plan your approach before acting: outline steps and select the best tools for each user request
            - Decode cryptic merchant names (e.g., "PwP OTT* DROPOU" → "Dropout.tv")
            - Assign categories by pattern (e.g., streaming → Entertainment, groceries → Groceries)
            - Before using the "Other" category, try to create an appropriate category
            - Use clear, human-readable payee names; avoid duplicates
            - Clean transaction notes: concise, clear, no category info
            - Always search before creating payees or categories
            - Batch similar operations for efficiency
            - Present results in clear markdown tables and summaries

            **Workflow Patterns:**
            1. Review & categorize uncategorized transactions: decode, assign payee/category, clean note, batch update, summarize
            2. Spending analysis: filter by category/date, total, group by payee, present insights
            3. Data cleanup: identify, standardize, batch update, report
            4. Budget organization: review structure, suggest/create categories, group logically
            5. Transaction search/update: filter, review, update, confirm

            **Formatting:**
            - Use markdown tables for transactions
            - Use lists for categories/payees/accounts (include IDs)
            - Summaries: show period, totals, categories, new payees, notes cleaned
            - Transaction Note format: "[Merchant Name], [Merchant Type] ([Location]); [Optional Identifiers]"

            **Principles:**
            - Be proactive, efficient, decisive, and clear
            - Ensure data accuracy and consistency by utilizing available tools
            - Complete tasks fully before responding
            - Handle errors gracefully and suggest alternatives
            - Focus on the user's request; avoid unnecessary changes

            **Mission:** Help users manage budgets efficiently and effectively. Think strategically. Act decisively. Deliver results.

            **Remember:** Declare when you're done. The user has no way to follow up otherwise.
            """;

        DisplayInstructions(nameof(AdministrationAgent), instructions);

        return new()
        {
            Name = nameof(AdministrationAgent),
            Description = "The all-powerful Administration Agent with complete access to all Actual Budget capabilities. Combines transaction management, payee management, category management, account oversight, and strategic coordination. Can handle any budget task from simple queries to complex multi-step operations. Has deep knowledge of categorization rules, merchant name decoding, and efficient workflow execution.",
            Instructions = instructions,
            Kernel = agentKernel,
            Arguments = new KernelArguments(promptSettings),
        };
    }
}

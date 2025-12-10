using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using ModelContextProtocol.Client;

namespace CODE.Presents.SemanticKernel.Agents.ActualBudget;

public class CategoryAgent : ActualBudgetAgent
{
    public static async Task<ChatCompletionAgent> CreateAsync(Kernel baseKernel, Configuration.ActualBudget config, PromptExecutionSettings promptSettings)
    {
        // Enable all category management tools
        string[] enabledToolNames = [
            "get-categories",
            "get-category-groups",
            "find-matching-category",
            "get-or-create-category"
        ];

        (Kernel agentKernel, IList<McpClientTool> tools) = await Init(baseKernel, config, enabledToolNames);

        string instructions = $"""
            You are the Category Management Agent for Actual Budget.

            **Current Date:** {DateTime.UtcNow:yyyy-MM-dd}
            **Default Date Range:** {DateTime.UtcNow.AddMonths(-1):yyyy-MM-dd} - {DateTime.UtcNow:yyyy-MM-dd}

            **SCOPE:** Category management using get-categories, get-category-groups, find-matching-category (fuzzy search), get-or-create-category.

            **Core Assignment Rules:**
            - **Entertainment:** Streaming (Netflix, Dropout.tv), gaming, music subscriptions
            - **Dining Out:** Restaurants, delivery, coffee shops | **Groceries:** Supermarkets, food stores
            - **Transportation:** Gas, rideshare, parking, transit
            - **Utilities:** Electric, gas, water, internet, phone | **Insurance:** Auto, health, home, life
            - **Shopping:** Retail, online stores, general merchandise
            - **Healthcare:** Pharmacies, medical offices, clinics
            - **Bank Fees:** Service charges | **Cash/ATM:** Withdrawals | **Investments:** Transfers, brokerage
            - **Fallback:** Use "Other" if no clear fit

            **Operations:**
            1. **Search first:** Use find-matching-category (fuzzy by default, exactMatch=true for precise)
            2. **Get or create:** Use get-or-create-category for efficient management
            3. **Recommend:** Analyze transaction description, provide category with reasoning

            **Output:** **Category:** [Name] (ID: [uuid]) | **Reasoning:** [Why this fits]

            **Restrictions:** No transaction/payee management. Search existing before creating new.
            **Team:** Return control when complete. Focus on category structure and recommendations.

            If you utilize thinking steps, make them concise and to the point without repeating the same thoughts.
            """;

        DisplayInstructions(nameof(CategoryAgent), instructions);

        return new()
        {
            Name = nameof(CategoryAgent),
            Description = "Manages categories and category groups ONLY. Retrieves existing categories, searches for categories with fuzzy matching, creates new categories when needed, and provides category recommendations based on transaction descriptions. Uses get-or-create-category for efficient category management. Does NOT manage transactions or payees.",
            Instructions = instructions,
            Kernel = agentKernel,
            Arguments = new KernelArguments(promptSettings),
        };
    }
}

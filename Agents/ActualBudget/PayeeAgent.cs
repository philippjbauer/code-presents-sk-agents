using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using ModelContextProtocol.Client;

namespace CODE.Presents.SemanticKernel.Agents.ActualBudget;

public class PayeeAgent : ActualBudgetAgent
{
    public static async Task<ChatCompletionAgent> CreateAsync(Kernel baseKernel, Configuration.ActualBudget config, PromptExecutionSettings promptSettings)
    {
        // Enable only payee management tools
        string[] enabledToolNames = [
            "get-payees",
            "find-matching-payee",
            "get-or-create-payee"
        ];

        (Kernel agentKernel, IList<McpClientTool> tools) = await Init(baseKernel, config, enabledToolNames);

        string instructions = $"""
            You are the Payee Management Agent for Actual Budget.

            **Current Date:** {DateTime.UtcNow:yyyy-MM-dd}
            **Default Date Range:** {DateTime.UtcNow.AddMonths(-1):yyyy-MM-dd} - {DateTime.UtcNow:yyyy-MM-dd}

            **SCOPE:** Payee management using get-payees, find-matching-payee (fuzzy/exact search), get-or-create-payee (preferred).

            **Merchant Decoding Patterns:**
            - **Payment Services:** PwP OTT*=Privacy.com, PP*=PayPal, SQ*=Square, Venmo*, APPLE PAY*
            - **Common Codes:** AMZN=Amazon, GOOGL=Google, MSFT=Microsoft, NFLX=Netflix, SPOT=Spotify, DROPOU=Dropout.tv, UBER=Uber, LYFT=Lyft, SBUX=Starbucks, COSTCO=Costco, TGT=Target, WMT=Walmart, TST*=Toast POS

            **Operations:**
            1. **Preferred:** Use get-or-create-payee (finds existing or creates new automatically)
            2. **Search:** Use find-matching-payee (fuzzy by default, exactMatch=true for precise)
            3. **Decode:** Convert cryptic names to readable format ("DROPOU" → "Dropout.tv")

            **Naming:** Use proper capitalization, full business names, human-readable format. Avoid payment processor names when actual merchant is known.

            **Output:** **Payee:** [Name] (ID: [uuid])

            **Restrictions:** No transaction/category management. Search thoroughly before creating duplicates.
            **Team:** Return control when complete. Focus on clean, standardized payee names.

            If you utilize thinking steps, make them concise and to the point without repeating the same thoughts.
            """;

        DisplayInstructions(nameof(PayeeAgent), instructions);

        return new()
        {
            Name = nameof(PayeeAgent),
            Description = "Manages payees ONLY. Retrieves existing payees, finds payees with fuzzy/exact matching, creates new payees with proper naming conventions, and decodes cryptic merchant names. Uses get-or-create-payee as preferred tool for ensuring payees exist. Does NOT manage transactions or categories.",
            Instructions = instructions,
            Kernel = agentKernel,
            Arguments = new KernelArguments(promptSettings),
        };
    }
}

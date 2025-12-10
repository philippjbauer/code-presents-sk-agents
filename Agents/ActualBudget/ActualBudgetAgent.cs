using System.Text.Json;
using Microsoft.SemanticKernel;
using ModelContextProtocol.Client;
using Spectre.Console;

namespace CODE.Presents.SemanticKernel.Agents.ActualBudget;

public class ActualBudgetAgent
{
    protected static bool _showDebugInformation = false;

    protected static async Task<(Kernel, IList<McpClientTool>)> Init(Kernel baseKernel, Configuration.ActualBudget config, string[]? enabledToolNames = null)
    {
        Kernel agentKernel = baseKernel.Clone();

        McpClient mcpClient = await CreateMcpClientAsync(config);
        IList<McpClientTool> tools = await mcpClient.ListToolsAsync();

        // Gather data like health status and accounts for instructions
        HealthStatusResponse? healthStatus = await GetHealthStatusAsync(tools);
        await DisplayHealthStatusAsync(healthStatus);

        // Enable tools that we want the agent to use (all tools if none specified)
        enabledToolNames ??= tools.Select(t => t.Name).ToArray();

        IList<McpClientTool> availableTools = tools
            .Where(t => enabledToolNames.Contains(t.Name))
            .ToList();

        DisplayAvailableTools(availableTools);

        agentKernel.Plugins.AddFromFunctions(
            "budget",
            availableTools.Select(tf => tf.AsKernelFunction()));

        return (agentKernel, tools);
    }

    public static async Task<McpClient> CreateMcpClientAsync(Configuration.ActualBudget config)
    {
        // Create the MCP Client for the Actual Budget API
        string actualServerUrl = Environment.GetEnvironmentVariable("ACTUAL_SERVER_URL") ?? config.ServerUrl;
        string actualServerPassword = Environment.GetEnvironmentVariable("ACTUAL_SERVER_PASSWORD") ?? config.ServerPassword;
        string actualBudgetId = Environment.GetEnvironmentVariable("ACTUAL_BUDGET_ID") ?? config.BudgetId;

        // Create temporary directory for MCP client data
        string tempDataDir = Path.Combine(Path.GetTempPath(), "actual-budget-mcp");
        Directory.CreateDirectory(tempDataDir);

        // Get the actual-budget-mcp directory path (relative to the executable)
        string appDirectory = AppContext.BaseDirectory;
        string mcpServerPath = Path.Combine(appDirectory, "actual-budget-mcp", "dist", "index.js");

        // Verify the MCP server file exists
        if (!File.Exists(mcpServerPath))
        {
            throw new FileNotFoundException($"MCP server not found at: {mcpServerPath}");
        }

        DisplayMcpInformation(actualServerUrl, actualServerPassword, actualBudgetId, tempDataDir, mcpServerPath);

        McpClient mcpClient = await McpClient.CreateAsync(
            new StdioClientTransport(new StdioClientTransportOptions()
            {
                Name = "actual-budget",
                Command = "node",
                Arguments = [mcpServerPath],
                EnvironmentVariables = new Dictionary<string, string?>
                {
                    { "ACTUAL_SERVER_URL", actualServerUrl },
                    { "ACTUAL_SERVER_PASSWORD", actualServerPassword },
                    { "ACTUAL_BUDGET_ID", actualBudgetId },
                    { "ACTUAL_DATA_DIR", tempDataDir },
                    { "NODE_TLS_REJECT_UNAUTHORIZED", "0" } // Allow self-signed certificates
                },
            }));

        return mcpClient;
    }

    private static void DisplayMcpInformation(string actualServerUrl, string actualServerPassword, string actualBudgetId, string tempDataDir, string mcpServerPath)
    {
        if (!_showDebugInformation)
            return;

        var info = $"[dim]MCP Server Path:[/] {mcpServerPath}\n" +
                   $"[dim]Server URL:[/] {actualServerUrl}\n" +
                   $"[dim]Server Password:[/] {new string('*', actualServerPassword.Length)}\n" +
                   $"[dim]Budget ID:[/] {actualBudgetId}\n" +
                   $"[dim]Data Directory:[/] {tempDataDir}";

        var panel = new Panel(info)
            .Header("Creating MCP client for Actual Budget...", Justify.Left)
            .Border(BoxBorder.Rounded)
            .BorderColor(Color.Yellow)
            .Padding(1, 0);
        AnsiConsole.Write(panel);
    }

    protected static void DisplayInstructions(string agentName, string instructions)
    {
        if (!_showDebugInformation)
            return;

        const int maxLineLength = 80;
        string instructionsHeader = $"{agentName} Instructions";
        List<string> instructionLines = [];
        foreach (var line in instructions.EscapeMarkup().Split('\n'))
        {
            if (line.Trim().Length > maxLineLength)
            {
                List<string> splitLines = [];
                string[] words = line.Split(' ');
                string currentLine = "";
                foreach (var word in words)
                {
                    if ((currentLine + " " + word).Trim().Length > maxLineLength)
                    {
                        splitLines.Add(currentLine.Trim());
                        currentLine = word;
                    }
                    else
                    {
                        currentLine += " " + word;
                    }
                }
                if (!string.IsNullOrWhiteSpace(currentLine))
                {
                    splitLines.Add(currentLine.Trim());
                }
                instructionLines.AddRange(splitLines);
            }
            else
            {
                instructionLines.Add(line.Trim());
            }
        }
        instructionLines = instructionLines.Take(20).ToList(); // Limit to first 20 lines
        if (instructionLines.Count == 20)
            instructionLines.Add("... (truncated)");

        var panelContent = string.Join("\n", instructionLines);
        var panel = new Panel(panelContent)
            .Header(instructionsHeader, Justify.Left)
            .Border(BoxBorder.Rounded)
            .BorderColor(Color.Cyan)
            .Padding(1, 0);
        AnsiConsole.Write(panel);
    }

    protected static void DisplayAvailableTools(IList<McpClientTool> tools)
    {
        if (!_showDebugInformation)
            return;

        string toolsHeader = "Available Actual Budget Tools:";
        var toolLines = tools.Select(tool => $"• [yellow]{tool.Name}[/]: {tool.Description}");
        var panelContent = string.Join("\n", toolLines);
        var panel = new Panel(panelContent)
            .Header(toolsHeader, Justify.Left)
            .Border(BoxBorder.Rounded)
            .BorderColor(Color.Green)
            .Padding(1, 0);
        AnsiConsole.Write(panel);
    }

    protected static async Task<HealthStatusResponse?> GetHealthStatusAsync(IList<McpClientTool> tools)
    {
        // Get and format health status from structuredContent if available
        string? healthResultJson = (await tools.First(t => t.Name == "health").InvokeAsync())?.ToString();
        if (healthResultJson is null)
        {
            AnsiConsole.MarkupLine("[dim]  Health check failed.[/]");
            throw new InvalidOperationException("Failed to retrieve health status from Actual Budget MCP client.");
        }

        HealthStatusResponse? healthStatus = JsonSerializer.Deserialize<HealthStatusResponse>(healthResultJson);

        if (healthStatus == null)
        {
            AnsiConsole.MarkupLine("[dim]  Health status deserialization failed.[/]");
            throw new InvalidOperationException("Failed to deserialize health status from Actual Budget MCP client.");
        }

        return healthStatus;
    }

    protected static async Task DisplayHealthStatusAsync(HealthStatusResponse? healthStatus)
    {
        if (!_showDebugInformation)
            return;

        if (healthStatus == null)
        {
            var healthPanel = new Panel("No health status to display.")
                .Header("Actual Health Status", Justify.Left)
                .Border(BoxBorder.Rounded)
                .BorderColor(Color.Red)
                .Padding(1, 0);
            AnsiConsole.Write(healthPanel);
            return;
        }

        bool initialized = healthStatus?.StructuredContent?.Initialized ?? false;
        string color = initialized ? "green" : "red";
        string[] lines =
        {
            $"Initialized: {(initialized ? "true" : "false")}",
            $"Status     : {healthStatus?.StructuredContent?.Status ?? "-"}",
            $"Message    : {healthStatus?.StructuredContent?.Message ?? "-"}"
        };
        var panelContent = string.Join("\n", lines);
        var panel = new Panel(panelContent)
            .Header("Actual Health Status", Justify.Left)
            .Border(BoxBorder.Rounded)
            .BorderColor(initialized ? Color.Green : Color.Red)
            .Padding(1, 0);
        AnsiConsole.Write(panel);
    }

    protected static async Task<List<Account>?> GetAccountsAsync(IList<McpClientTool> tools)
    {
        // Get and format accounts from structuredContent if available
        string? accountsResultJson = (await tools.First(t => t.Name == "get-accounts").InvokeAsync())?.ToString();
        if (accountsResultJson is null)
        {
            AnsiConsole.MarkupLine("[dim]  Get accounts failed.[/]");
            throw new InvalidOperationException("Failed to retrieve accounts from Actual Budget MCP client.");
        }

        GetAccountsResponse? accountsResponse = JsonSerializer.Deserialize<GetAccountsResponse>(accountsResultJson);

        if (accountsResponse?.StructuredContent == null)
        {
            AnsiConsole.MarkupLine("[dim]  Accounts deserialization failed.[/]");
            throw new InvalidOperationException("Failed to deserialize accounts from Actual Budget MCP client.");
        }

        return accountsResponse.StructuredContent.Accounts;
    }

    protected static async Task<List<Category>?> GetCategoriesAsync(IList<McpClientTool> tools)
    {
        // Get and format categories from structuredContent if available
        string? categoriesResultJson = (await tools.First(t => t.Name == "get-categories").InvokeAsync())?.ToString();
        if (categoriesResultJson is null)
        {
            AnsiConsole.MarkupLine("[dim]  Get categories failed.[/]");
            throw new InvalidOperationException("Failed to retrieve categories from Actual Budget MCP client.");
        }

        GetCategoriesResponse? categoriesResponse = JsonSerializer.Deserialize<GetCategoriesResponse>(categoriesResultJson);

        if (categoriesResponse?.StructuredContent == null)
        {
            AnsiConsole.MarkupLine("[dim]  Categories deserialization failed.[/]");
            throw new InvalidOperationException("Failed to deserialize categories from Actual Budget MCP client.");
        }

        return categoriesResponse.StructuredContent.Categories;
    }

    protected static async Task<List<CategoryGroup>?> GetCategoryGroupsAsync(IList<McpClientTool> tools)
    {
        // Get and format category groups from structuredContent if available
        string? categoryGroupsResultJson = (await tools.First(t => t.Name == "get-category-groups").InvokeAsync())?.ToString();
        if (categoryGroupsResultJson is null)
        {
            AnsiConsole.MarkupLine("[dim]  Get category groups failed.[/]");
            throw new InvalidOperationException("Failed to retrieve category groups from Actual Budget MCP client.");
        }

        GetCategoryGroupsResponse? categoryGroupsResponse = JsonSerializer.Deserialize<GetCategoryGroupsResponse>(categoryGroupsResultJson);

        if (categoryGroupsResponse?.StructuredContent == null)
        {
            AnsiConsole.MarkupLine("[dim]  Category groups deserialization failed.[/]");
            throw new InvalidOperationException("Failed to deserialize category groups from Actual Budget MCP client.");
        }

        return categoryGroupsResponse.StructuredContent.Groups;
    }
}

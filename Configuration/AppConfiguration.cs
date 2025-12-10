namespace CODE.Presents.SemanticKernel.Configuration;

public class AppConfiguration
{
    public OpenAI OpenAI { get; set; } = new();
    public MemoryProvider MemoryProvider { get; set; } = new();
    public ActualBudget ActualBudget { get; set; } = new();
}

public class OpenAI
{
    public string ModelId { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string Endpoint { get; set; } = string.Empty;
}

public class MemoryProvider
{
    public bool UseMemoryProvider { get; set; } = false;

    public string ApiKey { get; set; } = string.Empty;
}

public class ActualBudget
{
    public string ServerUrl { get; set; } = string.Empty;
    public string ServerPassword { get; set; } = string.Empty;
    public string BudgetId { get; set; } = string.Empty;
}
using System.Text.Json.Serialization;

namespace CODE.Presents.SemanticKernel.Agents.ActualBudget;

public class AccountsStructuredContent
{
    [JsonPropertyName("accounts")]
    public List<Account> Accounts { get; set; } = [];
    [JsonPropertyName("count")]
    public int Count { get; set; }
}

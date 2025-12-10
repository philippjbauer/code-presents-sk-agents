using System.Text.Json.Serialization;

namespace CODE.Presents.SemanticKernel.Agents.ActualBudget;

public class GetAccountsResponse
{
    [JsonPropertyName("structuredContent")]
    public AccountsStructuredContent? StructuredContent { get; set; }
}

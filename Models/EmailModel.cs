using System.Text.Json.Serialization;

namespace CODE.Presents.SemanticKernel.Agents.Models;

public class EmailModel
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; } = new Guid();

    [JsonPropertyName("sender")]
    public required string Sender { get; set; }

    [JsonPropertyName("subject")]
    public required string Subject { get; set; }

    [JsonPropertyName("body")]
    public required string Body { get; set; }

    [JsonPropertyName("is_read")]
    public bool? IsRead { get; set; } = false;

    [JsonPropertyName("received_at")]
    public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;
}
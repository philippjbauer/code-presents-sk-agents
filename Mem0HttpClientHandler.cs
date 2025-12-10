using System.Text.Json;
using System.Text.Json.Serialization;
using Spectre.Console;
using Spectre.Console.Json;
using Spectre.Console.Rendering;

namespace CODE.Presents.SemanticKernel;

/// <summary>
/// Logs HTTP requests and responses (for debugging purposes)
/// </summary>
internal class Mem0HttpClientHandler : DelegatingHandler
{
    public Mem0HttpClientHandler()
    {
        InnerHandler = new HttpClientHandler();
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        List<IRenderable> requestRows = [];
        requestRows.Add(new Markup($"[dim]Sending Request to [[{request.Method}]] {request.RequestUri}[/]"));
        // requestRows.Add(new Markup($"[dim]{JsonSerializer.Serialize(request, new JsonSerializerOptions() { WriteIndented = true }).EscapeMarkup()}[/]"));

        if (request.Content != null)
        {
            string requestContent = await request.Content.ReadAsStringAsync(cancellationToken);
            requestRows.Add(new JsonText(requestContent.EscapeMarkup()));
        }
        else
        {
            requestRows.Add(new Markup($"[orange3 dim]No Request Content[/]"));
        }

        AnsiConsole.Write(new Panel(new Rows([.. requestRows]))
            .Header($"– {Emoji.Known.Brain} mem0 Request -")
            .Border(BoxBorder.Rounded)
            .BorderColor(Color.Purple3)
            .BorderStyle("dim")
            .Padding(1, 0));

        var response = await base.SendAsync(request, cancellationToken);

        List<IRenderable> responseRows = [];
        responseRows.Add(new Markup($"[dim]Received Response from [[{request.Method}]] {request.RequestUri}, [[{(int)response.StatusCode}]] {response.ReasonPhrase}[/]"));
        // responseRows.Add(new Markup($"[dim]{JsonSerializer.Serialize(response, new JsonSerializerOptions() { WriteIndented = true }).EscapeMarkup()}[/]"));

        if (response.Content != null)
        {
            string responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            try
            {
                if (request.RequestUri is not null
                    && request.RequestUri.ToString().Contains("/search"))
                {
                    // Try to deserialize to MemorySearchResult array for better formatting
                    var deserialized = JsonSerializer.Deserialize<List<MemorySearchResult>>(responseContent);

                    if (deserialized != null)
                    {
                        var filtered = deserialized
                            .Select(m => new
                            {
                                m.Id,
                                Memory = m.Memory?.Length > 100 ? m.Memory[..100] + "…" : m.Memory,
                                Categories = m.Categories != null ? string.Join(", ", m.Categories) : null,
                                m.Score,
                            })
                            .ToList();

                        responseRows.Add(new JsonText(JsonSerializer.Serialize(filtered, new JsonSerializerOptions() { WriteIndented = true })));
                    }
                    else
                    {
                        throw new JsonException("Deserialization resulted in null");
                    }
                }
                else
                {
                    var deserialized = JsonSerializer.Deserialize<object>(responseContent);
                    responseRows.Add(new JsonText(JsonSerializer.Serialize(deserialized, new JsonSerializerOptions() { WriteIndented = true })));
                }
            }
            catch (Exception ex)
            {
                responseRows.Add(new Markup($"[red dim]Failed to deserialize response to MemorySearchResult: {ex.Message.EscapeMarkup()}[/]"));
                responseRows.Add(new JsonText(responseContent));
            }
        }
        else
        {
            responseRows.Add(new Markup($"[orange3 dim]No Response Content[/]"));
        }

        AnsiConsole.Write(new Panel(new Rows([.. responseRows]))
            .Header($"– {Emoji.Known.Brain} mem0 Response -")
            .Border(BoxBorder.Rounded)
            .BorderColor(Color.Purple3)
            .BorderStyle("dim")
            .Padding(1, 0));

        return response;
    }

    private class MemorySearchResult
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }
        [JsonPropertyName("memory")]
        public string? Memory { get; set; }
        [JsonPropertyName("user_id")]
        public string? UserId { get; set; }
        [JsonPropertyName("metadata")]
        public object? Metadata { get; set; }
        [JsonPropertyName("categories")]
        public List<string>? Categories { get; set; }
        [JsonPropertyName("created_at")]
        public DateTime? CreatedAt { get; set; }
        [JsonPropertyName("updated_at")]
        public DateTime? UpdatedAt { get; set; }
        [JsonPropertyName("expiration_date")]
        public DateTime? ExpirationDate { get; set; }
        [JsonPropertyName("structured_attributes")]
        public StructuredAttributes? StructuredAttributes { get; set; }
        [JsonPropertyName("score")]
        public double? Score { get; set; }
    }

    private class StructuredAttributes
    {
        [JsonPropertyName("day")]
        public int Day { get; set; }
        [JsonPropertyName("hour")]
        public int Hour { get; set; }
        [JsonPropertyName("year")]
        public int Year { get; set; }
        [JsonPropertyName("month")]
        public int Month { get; set; }
        [JsonPropertyName("minute")]
        public int Minute { get; set; }
        [JsonPropertyName("second")]
        public int Quarter { get; set; }
        [JsonPropertyName("is_weekend")]
        public bool IsWeekend { get; set; }
        [JsonPropertyName("day_of_week")]
        public string DayOfWeek { get; set; } = string.Empty;
        [JsonPropertyName("day_of_year")]
        public int DayOfYear { get; set; }
        [JsonPropertyName("week_of_year")]
        public int WeekOfYear { get; set; }
    }
}

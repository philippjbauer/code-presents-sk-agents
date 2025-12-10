using System.Text;
using System.Text.Json;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Spectre.Console;
using Spectre.Console.Json;
using Spectre.Console.Rendering;

namespace CODE.Presents.SemanticKernel.Helpers;

public static class ChatMessageFormatter
{
    private static readonly JsonSerializerOptions _jsonSerializerOptions = new()
    {
        WriteIndented = true
    };

    public static void DisplayChatMessage(ChatMessageContent message)
    {
        AnsiConsole.Write(CreateChatMessagePanel(message));
    }

    public static Panel CreateChatMessagePanel(ChatMessageContent message)
    {
        return CreateChatMessagePanel(
            message.Content,
            message.AuthorName,
            message.Role,
            message.Items.ToList());
    }

    public static Panel CreateChatMessagePanel(
        string? content,
        string? authorName,
        AuthorRole role,
        List<KernelContent>? items = null)
    {
        string roleName = GetRoleName(authorName, role);
        Color roleColor = GetRoleColor(role);

        try
        {
            List<IRenderable> rows = [];

            if (items is null || items.Count == 0)
            {
                rows.AddRange(FormatThinkingContent(content ?? "awaiting response …"));
            }
            else
            {
                foreach (KernelContent item in items)
                {
                    try
                    {
                        string kernelContentType = item.GetType().Name ?? "Unknown";

                        if (kernelContentType == nameof(TextContent))
                        {
                            var textContent = (TextContent)item;

                            try
                            {
                                if (string.IsNullOrWhiteSpace(textContent.Text))
                                    continue;

                                _ = JsonSerializer.Deserialize<object?>(textContent.Text);
                                rows.Add(new JsonText(textContent.Text.PadRight(10)));
                            }
                            catch
                            {
                                rows.AddRange(FormatThinkingContent(textContent.Text ?? "awaiting response …"));
                            }
                        }
                        else if (kernelContentType == nameof(FunctionCallContent))
                        {
                            var callContent = (FunctionCallContent)item;

                            bool hasArguments = callContent.Arguments is not null
                                && callContent.Arguments.Count > 0;

                            string title = $"[orange1]Called [bold]{callContent.PluginName}.{callContent.FunctionName}[/] {(hasArguments ? "with arguments:" : "without arguments.")}[/]";
                            rows.Add(new Markup(title));

                            if (hasArguments)
                            {
                                rows.Add(new JsonText(JsonSerializer.Serialize(callContent.Arguments, _jsonSerializerOptions)));
                            }
                        }
                        // else if (kernelContentType == nameof(FunctionResultContent))
                        // {
                        //     var resultContent = (FunctionResultContent)item;

                        //     string title = $"[yellow]Result of [bold]{resultContent.PluginName}.{resultContent.FunctionName}:[/][/]";

                        //     rows.Add(new Markup(title));

                        //     rows.Add(new JsonText(JsonSerializer.Serialize(resultContent.Result, _jsonSerializerOptions)));
                        // }
                    }
                    catch (Exception ex)
                    {
                        rows.Add(new Markup($"[red][bold]Exception:[/] {ex.Message}[/]"));
                    }
                }
            }

            if (rows.Count == 0)
            {
                rows.Add(new Markup("[dim]No content to display.[/]"));
            }

            return new Panel(new Rows([.. rows]))
                .Header($"– {roleName} –", Justify.Left)
                .Border(BoxBorder.Rounded)
                .BorderColor(roleColor)
                .Padding(1, 0);
        }
        catch
        {
            return new Panel(content ?? "")
                .Header($"– EXCEPTION: {roleName} –", Justify.Left)
                .Border(BoxBorder.Rounded)
                .BorderColor(Color.Red)
                .Padding(1, 0);
        }
    }

    private static string GetRoleName(string? authorName, AuthorRole role)
    {
        string roleEmoji = Emoji.Known.Bell;

        if (role == AuthorRole.User)
            roleEmoji = Emoji.Known.Person;
        else if (role == AuthorRole.Assistant)
            roleEmoji = Emoji.Known.Robot;
        else if (role == AuthorRole.Tool)
            roleEmoji = Emoji.Known.Gear;

        return $"{roleEmoji} {Capitalize(authorName ?? role.ToString() ?? "Unknown")}";
    }

    private static Color GetRoleColor(AuthorRole role)
    {
        Color roleColor = Color.Default;

        if (role == AuthorRole.User)
            roleColor = Color.Gray;
        else if (role == AuthorRole.Assistant)
            roleColor = Color.Blue;
        else if (role == AuthorRole.Tool)
            roleColor = Color.Orange1;

        return roleColor;
    }

    private static string Capitalize(string s)
    {
        if (string.IsNullOrEmpty(s)) return s;
        return char.ToUpper(s[0]) + s.Substring(1).ToLower();
    }

    private static List<IRenderable> FormatThinkingContent(string content, int breakAtCharLength = 120, int showThinkingLines = 10)
    {
        const string openTag = "<think>";
        const string closeTag = "</think>";

        string formattedThinkContent = string.Empty;
        string formattedAnswerContent = string.Empty;

        // Will be used to determine the output formatting
        bool foundThinkingToken = false;

        // Find the LAST occurrence of the opening tag to handle multiple <think> sections
        int startIdx = content.LastIndexOf(openTag, StringComparison.OrdinalIgnoreCase);
        if (startIdx != -1)
        {
            foundThinkingToken = true;

            int endIdx = content.IndexOf(closeTag, startIdx + openTag.Length, StringComparison.OrdinalIgnoreCase);
            int thinkContentStart = startIdx + openTag.Length;
            string thinkContent;
            if (endIdx != -1)
            {
                thinkContent = content.Substring(thinkContentStart, endIdx - thinkContentStart);
            }
            else
            {
                thinkContent = content.Substring(thinkContentStart);
            }

            formattedThinkContent = thinkContent.Trim().EscapeMarkup();
            formattedAnswerContent = FormatMarkdown(formattedThinkContent);
            formattedThinkContent = BreakLineAtCharLength(formattedThinkContent, breakAtCharLength);

            // Show only the last N lines of the thinking content
            formattedThinkContent = formattedThinkContent
                .Split("\n")
                .TakeLast(showThinkingLines)
                .Select(l => l.PadRight(breakAtCharLength))
                .Aggregate((a, b) => a + "\n" + b);

            formattedThinkContent = $"[dim]{formattedThinkContent}[/]";

            // Remove ALL <think>...</think> sections from the answer content using regex
            // This handles both closed tags and unclosed tags (everything after an unclosed tag is removed)
            formattedAnswerContent = System.Text.RegularExpressions.Regex.Replace(
                content,
                @"<think>.*?(</think>|$)",
                string.Empty,
                System.Text.RegularExpressions.RegexOptions.Singleline | System.Text.RegularExpressions.RegexOptions.IgnoreCase
            );

            formattedAnswerContent = formattedAnswerContent.Trim().EscapeMarkup();
            formattedAnswerContent = FormatMarkdown(formattedAnswerContent);
            formattedAnswerContent = BreakLineAtCharLength(formattedAnswerContent, breakAtCharLength);

            if (string.IsNullOrWhiteSpace(thinkContent))
            {
                return [new Markup(formattedAnswerContent)];
            }
        }

        if (foundThinkingToken)
        {
            return [
                new Panel(formattedThinkContent)
                    .Header($"– {Emoji.Known.ThinkingFace} Thinking … -")
                    .Border(BoxBorder.Rounded)
                    .BorderStyle("dim")
                    .Padding(1, 0),
                new Markup(formattedAnswerContent)
            ];
        }
        else
        {
            formattedAnswerContent = BreakLineAtCharLength(content.Trim().EscapeMarkup(), breakAtCharLength);
            formattedAnswerContent = FormatMarkdown(formattedAnswerContent);

            return [new Markup(formattedAnswerContent.PadRight(breakAtCharLength))];
        }
    }

    private static string FormatMarkdown(string formattedAnswerContent)
    {
        // Transform all occurrences of `**Sender:**` to `[bold]Sender:[/]`
        if (string.IsNullOrEmpty(formattedAnswerContent))
            return formattedAnswerContent;

        // Replace all occurrences of **Word:** with [bold]Word:[/]
        // Regex: \*\*(.+?):\*\*
        return System.Text.RegularExpressions.Regex.Replace(
            formattedAnswerContent,
            "\\*\\*(.+?):\\*\\*",
            match => $"[bold]{match.Groups[1].Value}:[/]");
    }

    /// <summary>
    /// Breaks a line at a specified length while preserving words and punctuation
    /// </summary>
    /// <param name="input"></param>
    /// <param name="maxCharLength"></param>
    /// <returns></returns>
    private static string BreakLineAtCharLength(string input, int maxCharLength)
    {
        if (string.IsNullOrEmpty(input) || maxCharLength <= 0)
            return input;

        var lines = input.Split('\n');
        var sb = new StringBuilder();
        foreach (var line in lines)
        {
            var words = line.Split(' ');
            int currentLineLength = 0;
            foreach (var word in words)
            {
                if (string.IsNullOrEmpty(word)) continue;
                // If adding the next word would exceed the line length, break the line
                if (currentLineLength + word.Length + (currentLineLength > 0 ? 1 : 0) > maxCharLength)
                {
                    sb.AppendLine();
                    sb.Append(word);
                    currentLineLength = word.Length;
                }
                else
                {
                    if (currentLineLength > 0)
                    {
                        sb.Append(' ');
                        currentLineLength++;
                    }
                    sb.Append(word);
                    currentLineLength += word.Length;
                }
            }
            sb.AppendLine(); // preserve original line breaks
        }
        return sb.ToString().TrimEnd('\n', '\r');
    }
}
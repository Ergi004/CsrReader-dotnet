namespace CsvReader.Models.Chat;
public class UsageDto
{
    public long PromptTokens { get; set; }
    public long CompletionTokens { get; set; }
    public int? TotalTokens { get; set; }
}
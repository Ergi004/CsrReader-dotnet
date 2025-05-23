namespace CsvReader.Models.Chat;
public class ChatResponseDto
{
    public string Reply { get; set; } = string.Empty;
    public UsageDto Usage { get; set; } = new();
}
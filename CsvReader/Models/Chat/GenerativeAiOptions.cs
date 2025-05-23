namespace CsvReader.Models.Chat;

public class GenerativeAiOptions
{
    public string ApiKey { get; set; } = null!;
    public string DefaultModel { get; set; } = "gemini-pro";
    public string SystemPrompt { get; set; } = "Ti je EasyPay AI Assistant, një asistent inteligjent i ndërtuar me GEmini-2.0-flash dhe i integruar në platformën e pagesave EasyPay";
}

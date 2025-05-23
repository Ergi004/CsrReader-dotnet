namespace CsvReader.Models.Chat
{
    public class ChatMessageDto
    {
        public int    Id        { get; set; }
        public string Role      { get; set; } = string.Empty;
        public string Content   { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class ChatHistoryDto
    {
        public int ChatId { get; set; }
        public List<ChatMessageDto> Messages { get; set; } = new();
    }
}

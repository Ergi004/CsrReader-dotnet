
using Microsoft.Extensions.Options;
using CsvReader.Data;
using GenerativeAI;
using Microsoft.EntityFrameworkCore;
using CsvReader.Models.Chat;

namespace CsvReader.Services;

public class ChatService : IChatService
{
    private readonly AppDbContext _db;
    private readonly GoogleAi _googleAi;
    private readonly GenerativeAiOptions _opts;
    private readonly ILogger<ChatService> _log;

    public ChatService(
        AppDbContext db,
        GoogleAi googleAi,
        IOptions<GenerativeAiOptions> opts,
        ILogger<ChatService> log)
    {
        _db = db;
        _googleAi = googleAi;
        _opts = opts.Value;
        _log = log;
    }

    public async Task<ChatResponseDto> SendMessageAsync(
        ChatRequestDto request,
        CancellationToken cancellationToken = default)

    {
        var chat = await _db.Chats
                            .Include(c => c.Prompts)
                            .FirstOrDefaultAsync(c => c.Id == request.ChatId, cancellationToken)
                   ?? new Chat();


        if (chat.Id == 0)
            _db.Chats.Add(chat);
        chat.Prompts.Add(new Prompt
        {
            Content = _opts.SystemPrompt,
            Role = Role.System
        });

        chat.Prompts.Add(new Prompt
        {
            Content = request.Prompt,
            Role = Role.User
        });

        await _db.SaveChangesAsync(cancellationToken);

        var historyText = string.Join("\n", chat.Prompts
            .OrderBy(p => p.CreatedAt)
            .Select(p => $"{p.Role.ToString().ToLower()}: {p.Content}"));

        _log.LogInformation("Sending full history to Gemini:\n{History}", historyText);

        var model = _googleAi.CreateGenerativeModel(_opts.DefaultModel);
        var response = await model.GenerateContentAsync(historyText, cancellationToken);

        var replyText = response.Text()
                       ?? throw new InvalidOperationException("No text in AI response");

        var usageMeta = response.UsageMetadata
                       ?? throw new InvalidOperationException("No UsageMetadata");

        chat.Prompts.Add(new Prompt
        {
            Content = replyText,
            Role = Role.Assistant
        });

        await _db.SaveChangesAsync(cancellationToken);

        return new ChatResponseDto
        {
            Reply = replyText,
            Usage = new UsageDto
            {
                PromptTokens = usageMeta.PromptTokenCount,
                CompletionTokens = usageMeta.CandidatesTokenCount,
                TotalTokens = usageMeta.TotalTokenCount
            }
        };
    }

    public async Task<ChatHistoryDto?> GetHistoryAsync(int chatId, CancellationToken cancellationToken = default)
    {
        var chat = await _db.Chats
                            .Include(c => c.Prompts)
                            .FirstOrDefaultAsync(c => c.Id == chatId, cancellationToken);

        if (chat == null) return null;

        var history = new ChatHistoryDto
        {
            ChatId = chat.Id,
            Messages = chat.Prompts
                           .OrderBy(p => p.CreatedAt)
                           .Select(p => new ChatMessageDto
                           {
                               Id = p.Id,
                               Role = p.Role.ToString().ToLower(),
                               Content = p.Content,
                               CreatedAt = p.CreatedAt
                           })
                           .ToList()
        };

        return history;
    }
}

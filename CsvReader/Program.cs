using CsvReader.Services;
using Microsoft.OpenApi.Models;
using CsvReader.Data;
using Microsoft.EntityFrameworkCore;
using GenerativeAI;
using Microsoft.Extensions.Options;
using CsvReader.Models.Chat;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseSqlServer(
      builder.Configuration.GetConnectionString("DefaultConnection"),
      sql => sql.EnableRetryOnFailure()));

builder.Services.Configure<GenerativeAiOptions>(
    builder.Configuration.GetSection("GenerativeAI"));

builder.Services.AddSingleton(sp =>
{
    var opts = sp.GetRequiredService<IOptions<GenerativeAiOptions>>().Value;
    return new GoogleAi(opts.ApiKey);
});

builder.Services.AddScoped<IChatService, ChatService>();
builder.Services.AddScoped<ChatService>();builder.Services.AddScoped<FileReaderProcessor>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title   = "Csv Reader API",
        Version = "v1",
        Description = " A simple ASP.NET Core Web API for reading CSV files and extracting data. "
    });

});



var app = builder.Build();


if (app.Environment.IsDevelopment())
{
    app.UseSwagger();   
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Gemini ChatBot API v1");
        c.RoutePrefix = string.Empty; 
    });
}




if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();


app.UseRouting();
app.UseAuthorization();
app.MapControllers();

app.Run();

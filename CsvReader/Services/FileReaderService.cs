using CsvHelper.Configuration;
using System.Globalization;
using System.Text;
using CsvReader.Models.Chat;
using CsvReader.Models.CsvFile;

namespace CsvReader.Services
{
    public class FileReaderProcessor(IChatService chatService)
    {
        private static readonly string[] ExpectedHeaders = new[]
        {
            "Date", "Description", "Reference Number","Currency",
            "Amount", "Cr/Dr",  "Balance"
        };

        private readonly IChatService _chatService = chatService;

        public async Task<CsvProcessingResult> ProcessAsync(IFormFile file)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("File is empty or null");

            if (!file.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("File must be a CSV file");

            var descriptions = new List<string>();
            var allRows = new List<string[]>();
            string[]? headers = null;
            int headerRowIndex = -1;

            using var stream = file.OpenReadStream();
            using var reader = new StreamReader(stream);
            using var csvReader = new CsvHelper.CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = false,
                TrimOptions = TrimOptions.Trim,
                IgnoreBlankLines = true
            });

            int rowIndex = 0;
            while (await csvReader.ReadAsync())
            {
                var currentRow = csvReader.Parser.Record;
                if (currentRow != null)
                {
                    allRows.Add(currentRow);

                    if (headers == null && IsHeaderRow(currentRow))
                    {
                        headers = currentRow;
                        headerRowIndex = rowIndex;
                    }
                    rowIndex++;
                }
            }

            if (headers == null)
                throw new InvalidDataException("Expected header row was not found in CSV file");

            var uniqueHeaderIndices = headers
                .Select((name, idx) => (Name: name.Trim(), Index: idx))
                .GroupBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
                .Select(g => g.First().Index)
                .OrderBy(i => i)
                .ToList();

            for (int r = 0; r < allRows.Count; r++)
            {
                var row = allRows[r];
                allRows[r] = uniqueHeaderIndices
                    .Select(colIdx => colIdx < row.Length ? row[colIdx] : string.Empty)
                    .ToArray();
                
                Console.WriteLine($"Row {r + 1}: {string.Join(", ", allRows[r])}");
            }

            headers = allRows[headerRowIndex];

            ValidateHeaders(headers);

            var dataRows = new List<string[]>();
            for (int i = headerRowIndex + 1; i < allRows.Count; i++)
            {
                var row = allRows[i];
                if (row.Length > 1)
                {
                    var description = row[1]?.Trim();
                    if (!string.IsNullOrWhiteSpace(description))
                    {
                        descriptions.Add(description);
                        dataRows.Add(row);
                    }
                }
            }

            await ProcessDescriptionsWithGemini(descriptions, dataRows);

            var updatedCsvContent = await GenerateUpdatedCsv(allRows, headerRowIndex);
            var savedFilePath = await SaveUpdatedCsv(updatedCsvContent, file.FileName);

            return new CsvProcessingResult
            {
                FileName = file.FileName,
                RowsProcessed = dataRows.Count,
                Descriptions = descriptions,
                UpdatedCsvContent = updatedCsvContent,
                SavedFilePath = savedFilePath
            };
        }

        private async Task ProcessDescriptionsWithGemini(List<string> descriptions, List<string[]> dataRows)
        {
            const string question = "Gjej emrin dhe mbiemrin e nje personi ne kete pershkrin dhe pergjigja jote duhet te jete vetem emri dhe mbiemri . Nese ne pershkrim nuk ka emer real  atehere pergjigja jote do te jete '----'.";

            for (int i = 0; i < descriptions.Count && i < dataRows.Count; i++)
            {
                try
                {
                    var chatRequest = new ChatRequestDto
                    {
                        ChatId = 0,
                        Prompt = $"{question}: {descriptions[i]}"
                    };

                    var response = await _chatService.SendMessageAsync(chatRequest);
                    var extractedName = response.Reply?.Trim();

                    if (!string.IsNullOrWhiteSpace(extractedName))
                    {
                        dataRows[i][1] = extractedName;
                        descriptions[i] = extractedName;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error processing description '{descriptions[i]}': {ex.Message}");
                }
            }
        }

        private async Task<string> SaveUpdatedCsv(string csvContent, string originalFileName)
        {
            var uploadsDirectory = Path.Combine(Directory.GetCurrentDirectory(), "uploads");
            Directory.CreateDirectory(uploadsDirectory);

            var fileNameWithoutExt = Path.GetFileNameWithoutExtension(originalFileName);
            var extension = Path.GetExtension(originalFileName);
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var newFileName = $"{fileNameWithoutExt}_processed_{timestamp}{extension}";

            var filePath = Path.Combine(uploadsDirectory, newFileName);

            await File.WriteAllTextAsync(filePath, csvContent);

            return filePath;
        }

        private Task<string> GenerateUpdatedCsv(List<string[]> allRows, int headerRowIndex)
        {
            var csvContent = new StringBuilder();

            var headerRow = allRows[headerRowIndex];
            var csvHeaderRow = string.Join(",", headerRow.Select(field =>
            {
                if (field.Contains(",") || field.Contains("\"") || field.Contains("\n"))
                {
                    return "\"" + field.Replace("\"", "\"\"") + "\"";
                }
                return field;
            }));
            csvContent.AppendLine(csvHeaderRow);

            for (int i = headerRowIndex + 1; i < allRows.Count; i++)
            {
                var row = allRows[i];
                if (row.Length > 1 && !string.IsNullOrWhiteSpace(row[1]))
                {
                    var csvRow = string.Join(",", row.Select(field =>
                    {
                        if (field.Contains(",") || field.Contains("\"") || field.Contains("\n"))
                        {
                            return "\"" + field.Replace("\"", "\"\"") + "\"";
                        }
                        return field;
                    }));
                    csvContent.AppendLine(csvRow);
                }
            }

            return Task.FromResult(csvContent.ToString());
        }

        private bool IsHeaderRow(string[] row)
        {
            if (row.Length < ExpectedHeaders.Length)
                return false;

            for (int i = 0; i < Math.Min(3, ExpectedHeaders.Length); i++)
            {
                var cell = row[i]?.Trim();
                var expectedHeader = ExpectedHeaders[i];

                if (!string.Equals(cell, expectedHeader, StringComparison.OrdinalIgnoreCase))
                    return false;
            }

            return true;
        }

        private void ValidateHeaders(string[] headers)
        {
            if (headers.Length < ExpectedHeaders.Length)
                throw new InvalidDataException($"Expected {ExpectedHeaders.Length} columns, but found {headers.Length}");

            for (int i = 0; i < ExpectedHeaders.Length; i++)
            {
                var expectedHeader = ExpectedHeaders[i];
                var actualHeader = headers[i]?.Trim();

                if (!string.Equals(expectedHeader, actualHeader, StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidDataException(
                        $"Header mismatch at position {i + 1}: Expected '{expectedHeader}', but found '{actualHeader}'");
                }
            }
        }
    }
}

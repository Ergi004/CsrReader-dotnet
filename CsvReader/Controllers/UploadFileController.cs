
using CsvReader.Models.CsvFile;
using CsvReader.Services;
using Microsoft.AspNetCore.Mvc;

namespace CsvReader.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UploadFileController : ControllerBase
    {
        private readonly FileReaderProcessor _processor;

        public UploadFileController(FileReaderProcessor processor) =>
            _processor = processor;

        [HttpPost("upload-csv")]
        public async Task<IActionResult> UploadCsvFile([FromForm] UploadFileRequest request)
        {
            try
            {
                if (request.File == null)
                {
                    return BadRequest(new { Error = "No file uploaded" });
                }

                var result = await _processor.ProcessAsync(request.File);
                
                return Ok(new 
                { 
                    FileName = result.FileName, 
                    RowsProcessed = result.RowsProcessed,
                    DescriptionCount = result.Descriptions.Count,
                    UpdatedDescriptions = result.Descriptions,
                    SavedFilePath = result.SavedFilePath,
                    Message = "File processed and saved successfully"
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
            catch (InvalidDataException ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = "An error occurred while processing the file", Details = ex.Message });
            }
        }
    }
}
using Microsoft.AspNetCore.Mvc;
using Syncfusion.DocIO;
using Syncfusion.DocIO.DLS;
using Newtonsoft.Json;
using System.IO;

namespace DocumentEditorServer.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DocumentEditorController : ControllerBase
    {
        private readonly ILogger<DocumentEditorController> _logger;

        public DocumentEditorController(ILogger<DocumentEditorController> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Import DOCX file and convert to SFDT format
        /// </summary>
        /// <returns>SFDT JSON string</returns>
        [HttpPost("Import")]
        public IActionResult Import([FromForm] IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return BadRequest(new { error = "No file provided" });
                }

                _logger.LogInformation("Importing DOCX file: {FileName}, Size: {Size} bytes",
                    file.FileName, file.Length);

                using var stream = new MemoryStream();
                file.CopyTo(stream);
                stream.Position = 0;

                // Load DOCX using Syncfusion DocIO
                using var document = new WordDocument(stream, FormatType.Docx);

                // Convert WordDocument to SFDT (JSON serialization)
                string sfdt = JsonConvert.SerializeObject(document);

                _logger.LogInformation("Successfully converted DOCX to SFDT");

                return Ok(sfdt);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to import DOCX file");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Import DOCX from base64 string
        /// </summary>
        [HttpPost("ImportBase64")]
        public IActionResult ImportBase64([FromBody] Base64Request request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.Content))
                {
                    return BadRequest(new { error = "No content provided" });
                }

                _logger.LogInformation("Importing DOCX from base64");

                var bytes = Convert.FromBase64String(request.Content);
                using var stream = new MemoryStream(bytes);

                // Load DOCX
                using var document = new WordDocument(stream, FormatType.Docx);

                // Convert WordDocument to SFDT (JSON serialization)
                string sfdt = JsonConvert.SerializeObject(document);

                _logger.LogInformation("Successfully converted base64 DOCX to SFDT");

                return Ok(sfdt);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to import base64 DOCX");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Export SFDT to DOCX format
        /// </summary>
        [HttpPost("Export")]
        public IActionResult Export([FromBody] SfdtRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.Sfdt))
                {
                    return BadRequest(new { error = "No SFDT content provided" });
                }

                _logger.LogInformation("Exporting SFDT to DOCX");

                // Deserialize SFDT (JSON) to WordDocument
                var document = JsonConvert.DeserializeObject<WordDocument>(request.Sfdt);

                if (document == null)
                {
                    return BadRequest(new { error = "Invalid SFDT format" });
                }

                // Convert to DOCX
                using var stream = new MemoryStream();
                document.Save(stream, FormatType.Docx);
                document.Close();
                stream.Position = 0;

                _logger.LogInformation("Successfully converted SFDT to DOCX");

                var fileName = request.FileName ?? "document.docx";
                return File(
                    stream.ToArray(),
                    "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                    fileName
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to export SFDT to DOCX");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Export SFDT to DOCX as base64
        /// </summary>
        [HttpPost("ExportBase64")]
        public IActionResult ExportBase64([FromBody] SfdtRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.Sfdt))
                {
                    return BadRequest(new { error = "No SFDT content provided" });
                }

                _logger.LogInformation("Exporting SFDT to base64 DOCX");

                // Deserialize SFDT (JSON) to WordDocument
                var document = JsonConvert.DeserializeObject<WordDocument>(request.Sfdt);

                if (document == null)
                {
                    return BadRequest(new { error = "Invalid SFDT format" });
                }

                // Convert to DOCX
                using var stream = new MemoryStream();
                document.Save(stream, FormatType.Docx);
                document.Close();
                stream.Position = 0;

                var base64 = Convert.ToBase64String(stream.ToArray());

                _logger.LogInformation("Successfully converted SFDT to base64 DOCX");

                return Ok(new { content = base64, fileName = request.FileName ?? "document.docx" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to export SFDT to base64 DOCX");
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }

    // Request models
    public class Base64Request
    {
        public string Content { get; set; } = string.Empty;
        public string? FileName { get; set; }
    }

    public class SfdtRequest
    {
        public string Sfdt { get; set; } = string.Empty;
        public string? FileName { get; set; }
    }
}

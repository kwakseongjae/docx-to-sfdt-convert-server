using Microsoft.AspNetCore.Mvc;
using Syncfusion.EJ2.DocumentEditor;
using WDocument = Syncfusion.DocIO.DLS.WordDocument;
using Syncfusion.DocIO;
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
        /// Reference: https://github.com/SyncfusionExamples/EJ2-Document-Editor-Web-Services
        /// </summary>
        [HttpPost("Import")]
        public IActionResult Import([FromForm] IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return BadRequest(new { error = "No file provided" });
                }

                _logger.LogInformation("Importing file: {FileName}, Size: {Size} bytes",
                    file.FileName, file.Length);

                using var stream = new MemoryStream();
                file.CopyTo(stream);
                stream.Position = 0;

                // Determine format type from file extension
                int index = file.FileName.LastIndexOf('.');
                string type = index > -1 && index < file.FileName.Length - 1 ?
                    file.FileName.Substring(index) : ".docx";

                // Load document using Syncfusion.EJ2.DocumentEditor.WordDocument
                WordDocument document = WordDocument.Load(stream, GetFormatType(type.ToLower()));

                // Serialize to SFDT JSON
                string json = JsonConvert.SerializeObject(document);
                document.Dispose();

                _logger.LogInformation("Successfully converted to SFDT");

                return Ok(json);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to import file");
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

                _logger.LogInformation("Importing from base64");

                var bytes = Convert.FromBase64String(request.Content);
                using var stream = new MemoryStream(bytes);

                // Load DOCX using Syncfusion.EJ2.DocumentEditor.WordDocument
                WordDocument document = WordDocument.Load(stream, FormatType.Docx);

                // Serialize to SFDT JSON
                string json = JsonConvert.SerializeObject(document);
                document.Dispose();

                _logger.LogInformation("Successfully converted base64 to SFDT");

                return Ok(json);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to import base64");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Export SFDT to DOCX format
        /// Reference: https://github.com/SyncfusionExamples/EJ2-Document-Editor-Web-Services
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

                // Convert SFDT JSON to WDocument using WordDocument.Save
                WDocument document = WordDocument.Save(request.Sfdt);

                if (document == null)
                {
                    return BadRequest(new { error = "Invalid SFDT format" });
                }

                // Save to stream
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
                _logger.LogError(ex, "Failed to export SFDT");
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

                // Convert SFDT JSON to WDocument using WordDocument.Save
                WDocument document = WordDocument.Save(request.Sfdt);

                if (document == null)
                {
                    return BadRequest(new { error = "Invalid SFDT format" });
                }

                // Save to stream
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
                _logger.LogError(ex, "Failed to export SFDT to base64");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Helper method to get FormatType from file extension
        /// </summary>
        private FormatType GetFormatType(string format)
        {
            if (string.IsNullOrEmpty(format))
                throw new NotSupportedException("File format not supported.");

            switch (format.ToLower())
            {
                case ".dotx":
                case ".docx":
                case ".docm":
                case ".dotm":
                    return FormatType.Docx;
                case ".dot":
                case ".doc":
                    return FormatType.Doc;
                case ".rtf":
                    return FormatType.Rtf;
                case ".txt":
                    return FormatType.Txt;
                case ".xml":
                    return FormatType.WordML;
                default:
                    throw new NotSupportedException("File format not supported.");
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

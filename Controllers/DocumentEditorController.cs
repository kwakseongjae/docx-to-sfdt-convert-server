using Microsoft.AspNetCore.Mvc;
using Syncfusion.EJ2.DocumentEditor;
using WDocument = Syncfusion.DocIO.DLS.WordDocument;
using Syncfusion.DocIO;
using Syncfusion.DocIORenderer;
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
        public string Import(IFormCollection data)
        {
            try
            {
                if (data.Files.Count == 0)
                {
                    throw new Exception("No file provided");
                }

                Stream stream = new MemoryStream();
                IFormFile file = data.Files[0];

                _logger.LogInformation("Importing file: {FileName}, Size: {Size} bytes",
                    file.FileName, file.Length);

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

                return json;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to import file");
                throw;
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
                WordDocument document = WordDocument.Load(stream, Syncfusion.EJ2.DocumentEditor.FormatType.Docx);

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
                document.Save(stream, Syncfusion.DocIO.FormatType.Docx);
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
                document.Save(stream, Syncfusion.DocIO.FormatType.Docx);
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
        /// Export SFDT to PDF format
        /// Reference: https://help.syncfusion.com/file-formats/docio/word-to-pdf
        /// </summary>
        [HttpPost("ExportPdf")]
        public IActionResult ExportPdf([FromBody] SfdtRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.Sfdt))
                {
                    return BadRequest(new { error = "No SFDT content provided" });
                }

                _logger.LogInformation("🔄 Starting PDF export from SFDT");

                // Convert SFDT JSON to WDocument using WordDocument.Save
                _logger.LogInformation("   Step 1/4: Converting SFDT to WordDocument");
                WDocument document = WordDocument.Save(request.Sfdt);

                if (document == null)
                {
                    _logger.LogError("   ❌ Failed: Invalid SFDT format");
                    return BadRequest(new { error = "Invalid SFDT format" });
                }
                _logger.LogInformation("   ✅ Step 1/4: WordDocument created");

                // Convert to PDF using DocIORenderer
                _logger.LogInformation("   Step 2/4: Creating DocIORenderer with Korean font support");
                using var renderer = new DocIORenderer();

                // Enable AutoDetectComplexScript for CJK (Korean, Chinese, Japanese) language support
                renderer.Settings.AutoDetectComplexScript = true;

                _logger.LogInformation("   Step 3/4: Converting WordDocument to PDF (this uses Syncfusion license)");
                using var pdfDocument = renderer.ConvertToPDF(document);
                using var stream = new MemoryStream();

                _logger.LogInformation("   Step 4/4: Saving PDF to stream");
                pdfDocument.Save(stream);
                document.Close();
                stream.Position = 0;

                _logger.LogInformation($"✅ Successfully converted SFDT to PDF (Size: {stream.Length} bytes)");

                var fileName = request.FileName ?? "document.pdf";
                // Remove .docx extension if present and add .pdf
                if (fileName.EndsWith(".docx", StringComparison.OrdinalIgnoreCase))
                {
                    fileName = fileName.Substring(0, fileName.Length - 5) + ".pdf";
                }
                else if (!fileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
                {
                    fileName += ".pdf";
                }

                return File(
                    stream.ToArray(),
                    "application/pdf",
                    fileName
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to export SFDT to PDF");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Helper method to get FormatType from file extension
        /// </summary>
        private Syncfusion.EJ2.DocumentEditor.FormatType GetFormatType(string format)
        {
            if (string.IsNullOrEmpty(format))
                throw new NotSupportedException("File format not supported.");

            switch (format.ToLower())
            {
                case ".dotx":
                case ".docx":
                case ".docm":
                case ".dotm":
                    return Syncfusion.EJ2.DocumentEditor.FormatType.Docx;
                case ".dot":
                case ".doc":
                    return Syncfusion.EJ2.DocumentEditor.FormatType.Doc;
                case ".rtf":
                    return Syncfusion.EJ2.DocumentEditor.FormatType.Rtf;
                case ".txt":
                    return Syncfusion.EJ2.DocumentEditor.FormatType.Txt;
                case ".xml":
                    return Syncfusion.EJ2.DocumentEditor.FormatType.WordML;
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

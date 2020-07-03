using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using pdf.Domain;
using pdf.Model;

namespace pdf.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PdfController : ControllerBase
    {
        private readonly ILogger<PdfController> _logger;
        private readonly IPdfService _pdfService;
        private static readonly FormOptions _defaultFormOptions = new FormOptions();
        private readonly string[] permittedFileExtensions = { "pdf" };
        private readonly int maxFileSize = 5242880;

        public PdfController(ILogger<PdfController> logger,
                             IPdfService pdfService)
        {
            _logger = logger;
            _pdfService = pdfService;
        }

        // GET: api/pdf
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Pdf>>> GetPdfs([FromQuery] FileOrder order)
        {
            return await _pdfService.GetPdfs(order);
        }

        // GET: api/pdf/7e7ceb51-37e0-4ebb-897c-ce801b0e37fe
        [HttpGet("{id}")]
        public async Task<ActionResult<Pdf>> GetPdf(Guid id)
        {
            var pdf = await _pdfService.GetPdf(id);

            if (pdf == null)
            {
                return NotFound();
            }

            return pdf;
        }

        // POST: api/pdf
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult> PostPdf()
        {
            var pdf = (Pdf)null;
            var tuple = await ExtractFile();

            if (tuple.Item1 != null)
            {
                pdf = new Pdf
                {
                    Content = tuple.Item1,
                    Size = tuple.Item1.Length,
                    Title = tuple.Item2,
                    UploadDate = DateTime.UtcNow
                };

                var results = new List<ValidationResult>();
                bool isValid = Validator.TryValidateObject(pdf, new ValidationContext(pdf, null, null), results, true);

                if (isValid)
                {
                    await _pdfService.InsertPdf(pdf);
                    return CreatedAtAction(nameof(GetPdf), new { id = pdf.Id }, pdf);
                }
            }

            return BadRequest(ModelState);
        }

        // DELETE: api/Pdf/5
        [HttpDelete("{id}")]
        public async Task<ActionResult<Pdf>> DeletePdf(Guid id)
        {
            var pdf = await _pdfService.DeletePdf(id);
            return pdf;
        }

        // TODO: For expediency, SOLID has been waived here - it should be in it's own Utilities class
        private async Task<Tuple<byte[], string>> ExtractFile()
        {
            var parsedContentType = MediaTypeHeaderValue.Parse(Request.ContentType);
            var boundary = HeaderUtilities.RemoveQuotes(parsedContentType.Boundary);

            if (string.IsNullOrWhiteSpace(boundary.Value))
            {
                throw new InvalidDataException("Missing content-type boundary.");
            }

            if (boundary.Length > _defaultFormOptions.MultipartBoundaryLengthLimit)
            {
                throw new InvalidDataException($"Multipart boundary length limit {_defaultFormOptions.MultipartBoundaryLengthLimit} exceeded.");
            }

            var reader = new MultipartReader(boundary.Value, HttpContext.Request.Body);
            var section = await reader.ReadNextSectionAsync();

            if (section != null)
            {
                var hasContentDispositionHeader = ContentDispositionHeaderValue.TryParse(section.ContentDisposition, out var contentDisposition);
                if (hasContentDispositionHeader)
                {
                    if (contentDisposition != null && contentDisposition.DispositionType.Equals("form-data") &&
                        (!string.IsNullOrEmpty(contentDisposition.FileName.Value) || !string.IsNullOrEmpty(contentDisposition.FileNameStar.Value)))
                    {
                        var fileName = contentDisposition.FileName.Value;
                        using var memoryStream = new MemoryStream();
                        await section.Body.CopyToAsync(memoryStream);
                        if (memoryStream.Length <= 1) // It's coming back as 1 for some bloody reason
                        {
                            ModelState.AddModelError("File", "The file is empty.");
                        }
                        else if (memoryStream.Length > maxFileSize)
                        {
                            ModelState.AddModelError("File", $"The file exceeds 5 MB.");
                        }
                        else if (!Path.GetExtension(contentDisposition.FileName.Value).ToLowerInvariant().EndsWith("pdf"))
                        {
                            ModelState.AddModelError("File", "The file type isn't permitted or the file's signature doesn't match the file's extension.");
                        }
                        else
                        {
                            var streamedFileContent = memoryStream.ToArray();
                            return new Tuple<byte[], string>(streamedFileContent, fileName);
                        }
                    }
                }
            }

            return new Tuple<byte[], string>(null, "");
        }
    }
}
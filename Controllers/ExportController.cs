using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MdReader.Services;
using System.Security.Claims;

namespace MdReader.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ExportController : ControllerBase
{
    private readonly ExportService _exportService;
    private readonly DocumentService _documentService;

    public ExportController(ExportService exportService, DocumentService documentService)
    {
        _exportService = exportService;
        _documentService = documentService;
    }

    // Get authenticated user's GitHub ID
    private string GetUserId()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            throw new UnauthorizedAccessException("User ID not found in claims");
        }
        return userId;
    }

    // Export markdown to PDF
    [HttpPost("pdf")]
    public async Task<IActionResult> ExportToPdf([FromBody] ExportRequest request)
    {
        try
        {
            string content = request.Content ?? string.Empty;
            string title = request.Title ?? "Document";

            if (!string.IsNullOrEmpty(request.DocumentId))
            {
                var userId = GetUserId();
                var document = await _documentService.GetDocumentAsync(Guid.Parse(request.DocumentId), userId);
                if (document != null)
                {
                    content = document.Content;
                    title = document.Title;
                }
            }

            var pdfBytes = _exportService.ExportToPdf(content, title);
            return File(pdfBytes, "application/pdf", $"{title}.pdf");
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Failed to export PDF: " + ex.Message });
        }
    }

    // Export markdown to Word
    [HttpPost("word")]
    public async Task<IActionResult> ExportToWord([FromBody] ExportRequest request)
    {
        string content = request.Content ?? string.Empty;
        string title = request.Title ?? "Document";

        if (!string.IsNullOrEmpty(request.DocumentId))
        {
            var userId = GetUserId();
            var document = await _documentService.GetDocumentAsync(Guid.Parse(request.DocumentId), userId);
            if (document != null)
            {
                content = document.Content;
                title = document.Title;
            }
        }

        var wordBytes = _exportService.ExportToWord(content, title);
        return File(wordBytes, "application/vnd.openxmlformats-officedocument.wordprocessingml.document", $"{title}.docx");
    }
}

public class ExportRequest
{
    public string? DocumentId { get; set; }
    public string? Content { get; set; }
    public string? Title { get; set; }
}


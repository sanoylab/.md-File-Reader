using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MdReader.Models;
using MdReader.Services;
using System.Security.Claims;

namespace MdReader.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DocumentController : ControllerBase
{
    private readonly DocumentService _documentService;
    private readonly DocumentLimitService _limitService;

    public DocumentController(DocumentService documentService, DocumentLimitService limitService)
    {
        _documentService = documentService;
        _limitService = limitService;
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

    // Get authenticated user's email
    private string GetUserEmail()
    {
        var email = User.FindFirstValue(ClaimTypes.Email) 
            ?? User.FindFirstValue("urn:github:email")
            ?? User.FindFirstValue("email");
        
        if (string.IsNullOrEmpty(email))
        {
            throw new UnauthorizedAccessException("User email not found in claims");
        }
        return email;
    }

    // Get all documents for the authenticated user
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var userId = GetUserId();
        var documents = await _documentService.GetUserDocumentsAsync(userId);
        
        // Get document count and limit for UI
        var userEmail = GetUserEmail();
        var (_, currentCount, limit) = await _limitService.CheckDocumentLimitAsync(userEmail, userId);
        
        return Ok(new 
        { 
            documents = documents,
            documentCount = currentCount,
            documentLimit = limit
        });
    }

    // Get a specific document
    [HttpGet("{id}")]
    public async Task<IActionResult> Get(Guid id)
    {
        var userId = GetUserId();
        var document = await _documentService.GetDocumentAsync(id, userId);
        
        if (document == null)
            return NotFound();

        return Ok(document);
    }

    // Save or update a document
    [HttpPost]
    public async Task<IActionResult> Save([FromBody] SaveDocumentRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Content))
            return BadRequest("Content cannot be empty");

        var userId = GetUserId();
        var userEmail = GetUserEmail();
        
        var title = string.IsNullOrWhiteSpace(request.Title) 
            ? DocumentService.ExtractTitle(request.Content) 
            : request.Title;

        try
        {
            var document = await _documentService.SaveDocumentAsync(
                userId, 
                userEmail, 
                title, 
                request.Content, 
                request.Id);

            // Get updated document count and limit
            var (_, currentCount, limit) = await _limitService.CheckDocumentLimitAsync(userEmail, userId);

            return Ok(new 
            { 
                id = document.Id, 
                title = document.Title,
                documentCount = currentCount,
                documentLimit = limit
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    // Delete a document
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var userId = GetUserId();
        var deleted = await _documentService.DeleteDocumentAsync(id, userId);
        
        if (!deleted)
            return NotFound();

        return Ok();
    }
}

public class SaveDocumentRequest
{
    public Guid? Id { get; set; }
    public string? Title { get; set; }
    public string Content { get; set; } = string.Empty;
}


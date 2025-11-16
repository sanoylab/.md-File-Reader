using Microsoft.EntityFrameworkCore;
using MdReader.Data;
using MdReader.Models;

namespace MdReader.Services;

public class DocumentService
{
    private readonly ApplicationDbContext _context;
    private readonly DocumentLimitService _limitService;

    public DocumentService(ApplicationDbContext context, DocumentLimitService limitService)
    {
        _context = context;
        _limitService = limitService;
    }

    // Get all documents for a user
    public async Task<List<Document>> GetUserDocumentsAsync(string userId)
    {
        return await _context.Documents
            .Where(d => d.UserId == userId)
            .OrderByDescending(d => d.UpdatedAt)
            .ToListAsync();
    }

    // Get a single document by ID
    public async Task<Document?> GetDocumentAsync(Guid id, string userId)
    {
        return await _context.Documents
            .FirstOrDefaultAsync(d => d.Id == id && d.UserId == userId);
    }

    // Save or update a document
    public async Task<Document> SaveDocumentAsync(string userId, string userEmail, string title, string content, Guid? documentId = null)
    {
        Document document;

        // Only update if documentId is provided and not empty
        if (documentId.HasValue && documentId.Value != Guid.Empty)
        {
            // Update existing document
            document = await _context.Documents
                .FirstOrDefaultAsync(d => d.Id == documentId.Value && d.UserId == userId) 
                ?? throw new InvalidOperationException("Document not found or access denied");

            document.Title = title;
            document.Content = content;
            document.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            // Create new document - check limit first
            var (allowed, currentCount, limit) = await _limitService.CheckDocumentLimitAsync(userEmail, userId);
            
            if (!allowed)
            {
                throw new InvalidOperationException(
                    $"Document limit reached. You have reached the maximum of {limit} documents. Please delete some documents to create new ones.");
            }

            document = new Document
            {
                UserId = userId,
                UserEmail = userEmail,
                Title = title,
                Content = content,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.Documents.Add(document);
        }

        await _context.SaveChangesAsync();
        return document;
    }

    // Delete a document
    public async Task<bool> DeleteDocumentAsync(Guid id, string userId)
    {
        var document = await _context.Documents
            .FirstOrDefaultAsync(d => d.Id == id && d.UserId == userId);

        if (document == null)
            return false;

        _context.Documents.Remove(document);
        await _context.SaveChangesAsync();
        return true;
    }

    // Extract title from markdown content (first line or first heading)
    public static string ExtractTitle(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return "Untitled Document";

        var lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        
        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            // Check for markdown heading
            if (trimmed.StartsWith("# "))
                return trimmed.Substring(2).Trim();
            if (trimmed.StartsWith("## "))
                return trimmed.Substring(3).Trim();
            // Use first non-empty line
            if (!string.IsNullOrWhiteSpace(trimmed))
                return trimmed.Length > 100 ? trimmed.Substring(0, 100) : trimmed;
        }

        return "Untitled Document";
    }
}


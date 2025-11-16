using Microsoft.EntityFrameworkCore;
using MdReader.Data;

namespace MdReader.Services;

public class DocumentLimitService
{
    private readonly ApplicationDbContext _context;
    private const int DEFAULT_LIMIT = 500;
    private const string UNLIMITED_EMAIL = "expertsanoy@gmail.com";

    public DocumentLimitService(ApplicationDbContext context)
    {
        _context = context;
    }

    // Get document count for a user
    public async Task<int> GetDocumentCountAsync(string userId)
    {
        return await _context.Documents
            .Where(d => d.UserId == userId)
            .CountAsync();
    }

    // Check if user can create more documents
    // Returns: (allowed, currentCount, limit)
    public async Task<(bool allowed, int currentCount, int limit)> CheckDocumentLimitAsync(string userEmail, string userId)
    {
        // Check if user has unlimited access
        if (string.Equals(userEmail, UNLIMITED_EMAIL, StringComparison.OrdinalIgnoreCase))
        {
            var count = await GetDocumentCountAsync(userId);
            return (true, count, -1); // -1 means unlimited
        }

        // Regular users have 500 document limit
        var currentCount = await GetDocumentCountAsync(userId);
        var limit = DEFAULT_LIMIT;
        var allowed = currentCount < limit;

        return (allowed, currentCount, limit);
    }
}


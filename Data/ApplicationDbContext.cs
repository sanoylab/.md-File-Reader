using Microsoft.EntityFrameworkCore;
using MdReader.Models;

namespace MdReader.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Document> Documents { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure PostgreSQL naming convention (lowercase with underscores)
        foreach (var entity in modelBuilder.Model.GetEntityTypes())
        {
            // Set table name to lowercase
            entity.SetTableName(entity.GetTableName()?.ToLower());

            // Set column names to lowercase
            foreach (var property in entity.GetProperties())
            {
                property.SetColumnName(property.GetColumnName()?.ToLower());
            }

            // Set index names to lowercase
            foreach (var index in entity.GetIndexes())
            {
                index.SetDatabaseName(index.GetDatabaseName()?.ToLower());
            }
        }

        modelBuilder.Entity<Document>(entity =>
        {
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.CreatedAt);
        });
    }
}


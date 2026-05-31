using HROpsBot.Domain.Entities;
using HROpsBot.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using HROpsBot.Core.Interfaces;

namespace HROpsBot.Infrastructure.Services;

public class DocService(AppDbContext dbContext) : IDocService
{
    public async Task<List<Regulation>> SearchAsync(string query)
    {
        if (string.IsNullOrWhiteSpace(query)) 
            return await dbContext.Regulations.ToListAsync();

        var q = query.ToLower();
        return await dbContext.Regulations
            .Where(r =>
                r.TitleRu.ToLower().Contains(q) ||
                r.TitleKk.ToLower().Contains(q) ||
                r.ContentRu.ToLower().Contains(q) ||
                r.ContentKk.ToLower().Contains(q) ||
                r.Tags.ToLower().Contains(q) ||
                r.Category.ToLower().Contains(q))
            .ToListAsync();
    }

    public async Task<Regulation?> GetByIdAsync(int id) =>
        await dbContext.Regulations.FirstOrDefaultAsync(r => r.Id == id);
}

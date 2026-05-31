using HROpsBot.Domain.Entities;

namespace HROpsBot.Core.Interfaces;

public interface IDocService
{
    Task<List<Regulation>> SearchAsync(string query);
    Task<Regulation?> GetByIdAsync(int id);
}

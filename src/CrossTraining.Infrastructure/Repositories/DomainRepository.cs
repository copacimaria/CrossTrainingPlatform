using CrossTraining.Domain.Entities;
using CrossTraining.Domain.Interfaces;
using CrossTraining.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CrossTraining.Infrastructure.Repositories;

public class DomainRepository : IDomainRepository
{
    private readonly AppDbContext _db;

    public DomainRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<TrainingDomain>> GetAllAsync()
        => await _db.Domains.AsNoTracking().ToListAsync();

    public async Task<List<TrainingDomain>> GetActiveAsync()
        => await _db.Domains.AsNoTracking().Where(d => d.IsActive).ToListAsync();

    public Task<TrainingDomain?> GetByIdAsync(int id)
        => _db.Domains.FirstOrDefaultAsync(d => d.DomainId == id);

    public async Task<List<TrainingDomain>> GetByIdsAsync(IEnumerable<int> ids)
    {
        var idList = ids?.ToList() ?? new List<int>();
        if (idList.Count == 0)
            return new List<TrainingDomain>();

        return await _db.Domains.AsNoTracking()
                                .Where(d => idList.Contains(d.DomainId))
                                .ToListAsync();
    }

    public async Task<int> CreateAsync(TrainingDomain domain)
    {
        _db.Domains.Add(domain);
        await _db.SaveChangesAsync();
        return domain.DomainId;
    }

    public async Task UpdateAsync(TrainingDomain domain)
    {
        var existing = await _db.Domains.FirstOrDefaultAsync(d => d.DomainId == domain.DomainId);
        if (existing == null)
            return;

        existing.DomainName = domain.DomainName;
        existing.IsActive = domain.IsActive;
        await _db.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var existing = await _db.Domains.FirstOrDefaultAsync(d => d.DomainId == id);
        if (existing == null)
            return;

        _db.Domains.Remove(existing);
        await _db.SaveChangesAsync();
    }
}

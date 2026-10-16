using CrossTraining.Application.Abstractions.Persistence;
using CrossTraining.Domain.Entities;
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

    public async Task<List<TrainingDomain>> GetAllForAdminAsync()
        => await _db.Domains.AsNoTracking()
                    .IgnoreQueryFilters()
                    .OrderBy(d => d.DomainName)
                    .ToListAsync();

    public async Task<List<TrainingDomain>> GetActiveAsync()
        => await _db.Domains.AsNoTracking()
                    .OrderBy(d => d.DomainName)
                    .ToListAsync();

    public Task<TrainingDomain?> GetForAdminAsync(int id)
        => _db.Domains.IgnoreQueryFilters().FirstOrDefaultAsync(d => d.DomainId == id);

    public async Task<List<TrainingDomain>> GetByIdsAsync(IEnumerable<int> ids)
    {
        var idList = ids?.ToList() ?? [];
        if (idList.Count == 0)
            return [];

        return await _db.Domains.AsNoTracking()
                        .Where(d => idList.Contains(d.DomainId))
                        .ToListAsync();
    }

    public void Add(TrainingDomain domain) => _db.Domains.Add(domain);
}

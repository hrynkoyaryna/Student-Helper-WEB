using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using StudentHelper.Application.Interfaces;
using StudentHelper.Application.Models;
using StudentHelper.Domain.Entities;
using StudentHelper.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace StudentHelper.Web.Services;

public class CacheableLookupServiceWeb : ICacheableLookupService
{
    private readonly IMemoryCache _cache;
    private readonly StudentHelperDbContext _context;
    private readonly CacheSettings _cacheSettings;

    public CacheableLookupServiceWeb(
        IMemoryCache cache,
        StudentHelperDbContext context,
        IOptions<ApplicationSettings> options)
    {
        _cache = cache;
        _context = context;
        _cacheSettings = options.Value.CacheSettings;
    }

    public async Task<List<Group>> GetAllGroupsAsync(CancellationToken cancellationToken = default)
    {
        return await _cache.GetOrCreateAsync("Groups_All", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(_cacheSettings.GroupsSeconds);
            return await _context.Groups.OrderBy(g => g.Name).ToListAsync(cancellationToken);
        });
    }

    public async Task<List<Subject>> GetAllSubjectsAsync(CancellationToken cancellationToken = default)
    {
        return await _cache.GetOrCreateAsync("Subjects_All", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(_cacheSettings.SubjectsSeconds);
            return await _context.Subjects.OrderBy(s => s.Title).ToListAsync(cancellationToken);
        });
    }

    public async Task<List<Teacher>> GetAllTeachersAsync(CancellationToken cancellationToken = default)
    {
        return await _cache.GetOrCreateAsync("Teachers_All", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(_cacheSettings.TeachersSeconds);
            return await _context.Teachers.OrderBy(t => t.FullName).ToListAsync(cancellationToken);
        });
    }
}

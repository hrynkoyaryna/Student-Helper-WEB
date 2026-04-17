using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StudentHelper.Application.DTOs.Groups;
using StudentHelper.Application.Interfaces;
using StudentHelper.Application.Models;
using StudentHelper.Domain.Entities;
using StudentHelper.Infrastructure.Data;

namespace StudentHelper.Infrastructure.Services;

public class GroupsService : IGroupsService
{
    private readonly StudentHelperDbContext dbContext;
    private readonly ApplicationSettings settings;
    private readonly ILogger<GroupsService> logger;

    public GroupsService(
        StudentHelperDbContext dbContext,
        IOptions<ApplicationSettings> options,
        ILogger<GroupsService> logger)
    {
        this.dbContext = dbContext;
        this.settings = options.Value;
        this.logger = logger;
    }

    public async Task<Result<List<GroupListItemDto>>> GetAllAsync(string? search = null)
    {
        this.logger.LogInformation("Отримання списку груп. Search: {Search}", search);

        var query = this.dbContext.Groups.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            if (search.Length < this.settings.MinSearchCharacters)
            {
                this.logger.LogWarning(
                    "Пошуковий запит закороткий. Length: {Length}, MinRequired: {MinRequired}",
                    search.Length,
                    this.settings.MinSearchCharacters);

                return Result<List<GroupListItemDto>>.Fail(
                    $"Пошук можливий від {this.settings.MinSearchCharacters} символів.");
            }

            query = query.Where(g => g.Name.Contains(search));
        }

        var groups = await query
            .OrderBy(g => g.Name)
            .Take(this.settings.ItemsPerPage)
            .Select(g => new GroupListItemDto
            {
                Id = g.Id,
                Name = g.Name,
            })
            .ToListAsync();

        return groups;
    }

    public async Task<Result<GroupListItemDto>> GetByIdAsync(int id)
    {
        this.logger.LogInformation("Отримання групи за Id: {GroupId}", id);

        var group = await this.dbContext.Groups
            .Where(g => g.Id == id)
            .Select(g => new GroupListItemDto
            {
                Id = g.Id,
                Name = g.Name,
            })
            .FirstOrDefaultAsync();

        if (group is null)
        {
            this.logger.LogWarning("Групу не знайдено. Id: {GroupId}", id);
            return Result<GroupListItemDto>.Fail("Групу не знайдено.");
        }

        return group;
    }

    public async Task<Result> CreateAsync(CreateGroupDto dto)
    {
        this.logger.LogInformation("Створення групи з назвою {GroupName}", dto.Name);

        var normalizedName = dto.Name.Trim();

        var exists = await this.dbContext.Groups
            .AnyAsync(g => g.Name == normalizedName);

        if (exists)
        {
            this.logger.LogWarning("Спроба створити дублікат групи {GroupName}", normalizedName);
            return Result.Fail("Група з такою назвою вже існує.");
        }

        var group = new Group
        {
            Name = normalizedName,
        };

        this.dbContext.Groups.Add(group);
        await this.dbContext.SaveChangesAsync();

        this.logger.LogInformation("Групу успішно створено. Id: {GroupId}", group.Id);
        return "Групу успішно створено.";
    }

    public async Task<Result> UpdateAsync(UpdateGroupDto dto)
    {
        this.logger.LogInformation("Оновлення групи. Id: {GroupId}", dto.Id);

        var group = await this.dbContext.Groups.FirstOrDefaultAsync(g => g.Id == dto.Id);

        if (group is null)
        {
            this.logger.LogWarning("Групу для оновлення не знайдено. Id: {GroupId}", dto.Id);
            return Result.Fail("Групу не знайдено.");
        }

        var normalizedName = dto.Name.Trim();

        var duplicate = await this.dbContext.Groups
            .AnyAsync(g => g.Id != dto.Id && g.Name == normalizedName);

        if (duplicate)
        {
            this.logger.LogWarning(
                "Спроба оновити групу на дубльовану назву {GroupName}",
                normalizedName);

            return Result.Fail("Група з такою назвою вже існує.");
        }

        group.Name = normalizedName;
        await this.dbContext.SaveChangesAsync();

        this.logger.LogInformation("Групу успішно оновлено. Id: {GroupId}", group.Id);
        return "Групу успішно оновлено.";
    }

    public async Task<Result> DeleteAsync(int id)
    {
        this.logger.LogInformation("Видалення групи. Id: {GroupId}", id);

        var group = await this.dbContext.Groups.FirstOrDefaultAsync(g => g.Id == id);

        if (group is null)
        {
            this.logger.LogWarning("Групу для видалення не знайдено. Id: {GroupId}", id);
            return Result.Fail("Групу не знайдено.");
        }

        this.dbContext.Groups.Remove(group);
        await this.dbContext.SaveChangesAsync();

        this.logger.LogInformation("Групу успішно видалено. Id: {GroupId}", id);
        return "Групу успішно видалено.";
    }
}
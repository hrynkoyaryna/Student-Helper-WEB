using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using StudentHelper.Application.DTOs.Groups;
using StudentHelper.Application.Models;
using StudentHelper.Infrastructure.Data;
using StudentHelper.Infrastructure.Services;
using Xunit;

namespace StudentHelper.Application.Tests;

public class GroupsServiceTests
{
    private static StudentHelperDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<StudentHelperDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new StudentHelperDbContext(options);
    }

    private static GroupsService CreateService(StudentHelperDbContext dbContext)
    {
        var settings = Options.Create(new ApplicationSettings
        {
            ItemsPerPage = 10,
            MinSearchCharacters = 2,
        });

        return new GroupsService(dbContext, settings, NullLogger<GroupsService>.Instance);
    }

    [Fact]
    public async Task CreateAsync_Should_Create_Group_When_Name_Is_Unique()
    {
        await using var dbContext = CreateDbContext();
        var service = CreateService(dbContext);

        var dto = new CreateGroupDto
        {
            Name = "ПМІ-33",
        };

        var result = await service.CreateAsync(dto);

        Assert.True(result.Success);
        Assert.Single(dbContext.Groups);
    }

    [Fact]
    public async Task CreateAsync_Should_Fail_When_Group_Already_Exists()
    {
        await using var dbContext = CreateDbContext();
        dbContext.Groups.Add(new StudentHelper.Domain.Entities.Group
        {
            Name = "ПМІ-33",
        });
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);

        var dto = new CreateGroupDto
        {
            Name = "ПМІ-33",
        };

        var result = await service.CreateAsync(dto);

        Assert.False(result.Success);
        Assert.Equal("Група з такою назвою вже існує.", result.Message);
    }

    [Fact]
    public async Task GetAllAsync_Should_Return_Groups()
    {
        await using var dbContext = CreateDbContext();
        dbContext.Groups.Add(new StudentHelper.Domain.Entities.Group { Name = "ПМІ-31" });
        dbContext.Groups.Add(new StudentHelper.Domain.Entities.Group { Name = "ПМІ-32" });
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);

        var result = await service.GetAllAsync();

        Assert.True(result.Success);
        Assert.Equal(2, result.Value.Count);
    }

    [Fact]
    public async Task GetAllAsync_Should_Fail_When_Search_Is_Too_Short()
    {
        await using var dbContext = CreateDbContext();
        var service = CreateService(dbContext);

        var result = await service.GetAllAsync("a");

        Assert.False(result.Success);
    }

    [Fact]
    public async Task UpdateAsync_Should_Update_Group_Name()
    {
        await using var dbContext = CreateDbContext();
        var group = new StudentHelper.Domain.Entities.Group { Name = "ПМІ-33" };
        dbContext.Groups.Add(group);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);

        var dto = new UpdateGroupDto
        {
            Id = group.Id,
            Name = "ПМІ-34",
        };

        var result = await service.UpdateAsync(dto);

        Assert.True(result.Success);
        Assert.Equal("ПМІ-34", dbContext.Groups.First().Name);
    }

    [Fact]
    public async Task DeleteAsync_Should_Delete_Group()
    {
        await using var dbContext = CreateDbContext();
        var group = new StudentHelper.Domain.Entities.Group { Name = "ПМІ-33" };
        dbContext.Groups.Add(group);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);

        var result = await service.DeleteAsync(group.Id);

        Assert.True(result.Success);
        Assert.Empty(dbContext.Groups);
    }
}
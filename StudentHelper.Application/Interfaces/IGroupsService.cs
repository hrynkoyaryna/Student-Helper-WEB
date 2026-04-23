using StudentHelper.Application.DTOs.Groups;
using StudentHelper.Application.Models;

namespace StudentHelper.Application.Interfaces;

public interface IGroupsService
{
    Task<Result<List<GroupListItemDto>>> GetAllAsync(string? search = null);

    Task<Result<GroupListItemDto>> GetByIdAsync(int id);

    Task<Result> CreateAsync(CreateGroupDto dto);

    Task<Result> UpdateAsync(UpdateGroupDto dto);

    Task<Result> DeleteAsync(int id);
}
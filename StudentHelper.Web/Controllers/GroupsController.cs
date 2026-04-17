using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudentHelper.Application.DTOs.Groups;
using StudentHelper.Application.Interfaces;

namespace StudentHelper.Web.Controllers;

[Authorize(Roles = "Admin")]
public class GroupsController : BaseController
{
    private readonly IGroupsService groupsService;

    public GroupsController(IGroupsService groupsService)
    {
        this.groupsService = groupsService;
    }

    public async Task<IActionResult> Index(string? search)
    {
        var result = await this.groupsService.GetAllAsync(search);

        if (result.Failure)
        {
            this.SetErrorMessage(result.Message);
            return this.View(new List<GroupListItemDto>());
        }

        return this.View(result.Value);
    }

    public IActionResult Create()
    {
        return this.View(new CreateGroupDto());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateGroupDto dto)
    {
        if (!this.ModelState.IsValid)
        {
            return this.View(dto);
        }

        var result = await this.groupsService.CreateAsync(dto);

        if (result.Failure)
        {
            this.SetErrorMessage(result.Message);
            return this.View(dto);
        }

        this.SetSuccessMessage(result.Message);
        return this.RedirectToAction(nameof(this.Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var result = await this.groupsService.GetByIdAsync(id);

        if (result.Failure)
        {
            this.SetErrorMessage(result.Message);
            return this.RedirectToAction(nameof(this.Index));
        }

        var model = new UpdateGroupDto
        {
            Id = result.Value.Id,
            Name = result.Value.Name,
        };

        return this.View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(UpdateGroupDto dto)
    {
        if (!this.ModelState.IsValid)
        {
            return this.View(dto);
        }

        var result = await this.groupsService.UpdateAsync(dto);

        if (result.Failure)
        {
            this.SetErrorMessage(result.Message);
            return this.View(dto);
        }

        this.SetSuccessMessage(result.Message);
        return this.RedirectToAction(nameof(this.Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await this.groupsService.DeleteAsync(id);

        if (result.Failure)
        {
            this.SetErrorMessage(result.Message);
        }
        else
        {
            this.SetSuccessMessage(result.Message);
        }

        return this.RedirectToAction(nameof(this.Index));
    }
}
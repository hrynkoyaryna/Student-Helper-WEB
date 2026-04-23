using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudentHelper.Application.Interfaces;
using StudentHelper.Application.Models;
using StudentHelper.Domain.Entities;
using StudentHelper.Web.Models.Exams;

namespace StudentHelper.Web.Controllers;

public class ExamsController : BaseController
{
    private readonly IExamsService _examsService;
    private readonly IUserService _userService;
    private readonly ILogger<ExamsController> _logger;

    public ExamsController(
        IExamsService examsService,
        IUserService userService,
        ILogger<ExamsController> logger)
    {
        _examsService = examsService;
        _userService = userService;
        _logger = logger;
    }

    private async Task<User?> GetCurrentUserAsync()
    {
        var userId = GetCurrentUserId();
        return await _userService.GetUserByIdAsync(userId);
    }

    public async Task<IActionResult> Index(string? subject = null, string time = "all", string sort = "subject_asc")
    {
        var userId = GetCurrentUserId();
        var userExams = (await _examsService.GetByUserIdAsync(userId)).ToList();
        
        // Get group exams from user's groups
        var user = await GetCurrentUserAsync();
        List<Exam> groupExams = new();
        
        if (user?.GroupId.HasValue == true)
        {
            groupExams = (await _examsService.GetByGroupIdAsync(user.GroupId.Value)).ToList();
        }

        // Combine exams
        var exams = userExams.Union(groupExams).Distinct().ToList();

        var subjects = exams.Select(e => e.Subject).Distinct().OrderBy(s => s).ToList();

        var nowUtc = DateTime.UtcNow;
        if (time == "past")
        {
            exams = exams.Where(e => e.DateTime < nowUtc).ToList();
        }
        else if (time == "upcoming")
        {
            exams = exams.Where(e => e.DateTime >= nowUtc).ToList();
        }

        if (!string.IsNullOrWhiteSpace(subject))
        {
            exams = exams.Where(e => e.Subject == subject).ToList();
        }

        exams = sort switch
        {
            "subject_desc" => exams.OrderByDescending(e => e.Subject).ToList(),
            _ => exams.OrderBy(e => e.Subject).ToList()
        };

        var model = new ExamIndexViewModel
        {
            Exams = exams,
            Subjects = subjects,
            SelectedSubject = subject,
            TimeFilter = time,
            SortOrder = sort
        };

        return View(model);
    }

    public async Task<IActionResult> Details(int id)
    {
        var exam = await _examsService.GetExamByIdAsync(id);
        if (exam == null) return NotFound();
        return View(exam);
    }

    [Authorize]
    [HttpGet]
    public IActionResult Create()
    {
        // Admins can only create group exams
        if (User.IsInRole("Admin"))
        {
            return RedirectToAction(nameof(CreateGroupExam));
        }

        var model = new ExamCreateEditViewModel 
        { 
            DateTime = DateTime.Now.AddDays(7) 
        };
        return View(model);
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ExamCreateEditViewModel model)
    {
        // Admins can only create group exams
        if (User.IsInRole("Admin"))
        {
            return RedirectToAction(nameof(CreateGroupExam));
        }

        if (!ModelState.IsValid) return View(model);

        var userId = GetCurrentUserId();

        var request = new CreateExamRequest
        {
            Subject = model.Subject,
            DateTime = model.DateTime,
            TeacherName = model.TeacherName,
            Description = model.Description, 
            UserId = userId
        };

        var result = await _examsService.CreateExamAsync(request);
        if (!result.Success)
        {
            ModelState.AddModelError("", result.Message);
            return View(model);
        }

        TempData["SuccessMessage"] = result.Message;
        return RedirectToAction(nameof(Index));
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var exam = await _examsService.GetExamByIdAsync(id);
        if (exam == null) return NotFound();

        // Admins can only edit group exams, not personal exams
        if (User.IsInRole("Admin") && exam.GroupId == null)
        {
            TempData["ErrorMessage"] = "Адміни можуть редагувати лише групові екзамени";
            return RedirectToAction(nameof(Index));
        }

        var userId = GetCurrentUserId();
        var canEdit = await _examsService.CanEditExamAsync(id, userId);
        if (!canEdit)
        {
            TempData["ErrorMessage"] = "Ви не можете редагувати цей екзамен";
            return RedirectToAction(nameof(Index));
        }

        var model = new ExamCreateEditViewModel
        {
            Id = exam.Id,
            Subject = exam.Subject,
            DateTime = DateTime.SpecifyKind(exam.DateTime, DateTimeKind.Local),
            TeacherName = exam.TeacherName,
            Description = exam.Description
        };

        return View(model);
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(ExamCreateEditViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var exam = await _examsService.GetExamByIdAsync(model.Id);
        if (exam == null) return NotFound();

        // Admins can only edit group exams, not personal exams
        if (User.IsInRole("Admin") && exam.GroupId == null)
        {
            ModelState.AddModelError("", "Адміни можуть редагувати лише групові екзамени");
            return View(model);
        }

        var userId = GetCurrentUserId();
        var canEdit = await _examsService.CanEditExamAsync(model.Id, userId);
        if (!canEdit)
        {
            ModelState.AddModelError("", "Ви не можете редагувати цей екзамен");
            return View(model);
        }

        var request = new UpdateExamRequest
        {
            Id = model.Id,
            Subject = model.Subject,
            DateTime = model.DateTime,
            TeacherName = model.TeacherName,
            Description = model.Description, 
            UserId = userId
        };

        var result = await _examsService.UpdateExamAsync(request);
        if (!result.Success)
        {
            ModelState.AddModelError("", result.Message);
            return View(model);
        }

        TempData["SuccessMessage"] = result.Message;
        return RedirectToAction(nameof(Index));
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Delete(int id)
    {
        var exam = await _examsService.GetExamByIdAsync(id);
        if (exam == null) return NotFound();

        // Admins can only delete group exams, not personal exams
        if (User.IsInRole("Admin") && exam.GroupId == null)
        {
            TempData["ErrorMessage"] = "Адміни можуть видаляти лише групові екзамени";
            return RedirectToAction(nameof(Index));
        }

        var userId = GetCurrentUserId();
        var canDelete = await _examsService.CanDeleteExamAsync(id, userId);
        if (!canDelete)
        {
            TempData["ErrorMessage"] = "Ви не можете видалити цей екзамен";
            return RedirectToAction(nameof(Index));
        }

        return View(exam);
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var exam = await _examsService.GetExamByIdAsync(id);
        if (exam == null) return NotFound();

        // Admins can only delete group exams, not personal exams
        if (User.IsInRole("Admin") && exam.GroupId == null)
        {
            TempData["ErrorMessage"] = "Адміни можуть видаляти лише групові екзамени";
            return RedirectToAction(nameof(Index));
        }

        var userId = GetCurrentUserId();
        var canDelete = await _examsService.CanDeleteExamAsync(id, userId);
        if (!canDelete)
        {
            TempData["ErrorMessage"] = "Ви не можете видалити цей екзамен";
            return RedirectToAction(nameof(Index));
        }

        var result = await _examsService.DeleteExamAsync(id, userId);
        if (!result.Success)
        {
            TempData["ErrorMessage"] = result.Message;
            return RedirectToAction(nameof(Index));
        }

        TempData["SuccessMessage"] = result.Message;
        return RedirectToAction(nameof(Index));
    }

    // Admin methods for creating group exams
    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<IActionResult> CreateGroupExam()
    {
        var groupsEnum = await _userService.GetAllGroupsAsync();
        var groups = groupsEnum.ToList();
        var model = new GroupExamCreateEditViewModel
        {
            DateTime = DateTime.Now.AddDays(7),
            Groups = groups
        };
        return View(model);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateGroupExam(GroupExamCreateEditViewModel model)
    {
        if (!ModelState.IsValid)
        {
            var groupsEnum = await _userService.GetAllGroupsAsync();
            model.Groups = groupsEnum.ToList();
            return View(model);
        }

        var adminUserId = GetCurrentUserId();

        var request = new CreateGroupExamRequest
        {
            Subject = model.Subject,
            DateTime = model.DateTime,
            TeacherName = model.TeacherName,
            Description = model.Description,
            GroupId = model.GroupId,
            AdminUserId = adminUserId
        };

        var result = await _examsService.CreateGroupExamAsync(request);
        if (!result.Success)
        {
            var groupsEnum = await _userService.GetAllGroupsAsync();
            model.Groups = groupsEnum.ToList();
            ModelState.AddModelError("", result.Message);
            return View(model);
        }

        TempData["SuccessMessage"] = result.Message;
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<IActionResult> EditGroupExam(int id)
    {
        var exam = await _examsService.GetExamByIdAsync(id);
        if (exam == null || exam.GroupId == null) return NotFound();

        var adminUserId = GetCurrentUserId();
        if (exam.UserId != adminUserId)
        {
            TempData["ErrorMessage"] = "Ви не можете редагувати цей екзамен";
            return RedirectToAction(nameof(Index));
        }

        var groupsEnum = await _userService.GetAllGroupsAsync();
        var model = new GroupExamCreateEditViewModel
        {
            Id = exam.Id,
            Subject = exam.Subject,
            DateTime = DateTime.SpecifyKind(exam.DateTime, DateTimeKind.Local),
            TeacherName = exam.TeacherName,
            Description = exam.Description,
            GroupId = exam.GroupId.Value,
            Groups = groupsEnum.ToList()
        };

        return View(model);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditGroupExam(GroupExamCreateEditViewModel model)
    {
        if (!ModelState.IsValid)
        {
            var groupsEnum = await _userService.GetAllGroupsAsync();
            model.Groups = groupsEnum.ToList();
            return View(model);
        }

        var adminUserId = GetCurrentUserId();

        var request = new UpdateGroupExamRequest
        {
            Id = model.Id,
            Subject = model.Subject,
            DateTime = model.DateTime,
            TeacherName = model.TeacherName,
            Description = model.Description,
            AdminUserId = adminUserId
        };

        var result = await _examsService.UpdateGroupExamAsync(request);
        if (!result.Success)
        {
            var groupsEnum = await _userService.GetAllGroupsAsync();
            model.Groups = groupsEnum.ToList();
            ModelState.AddModelError("", result.Message);
            return View(model);
        }

        TempData["SuccessMessage"] = result.Message;
        return RedirectToAction(nameof(Index));
    }
}
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using StudentHelper.Application.Interfaces;
using StudentHelper.Application.Models;
using StudentHelper.Domain.Entities;
using StudentHelper.Web.Models.Schedule;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace StudentHelper.Web.Controllers
{
    [Authorize]
    public class ScheduleController : BaseController
    {
        private readonly IScheduleService _scheduleService;
        private readonly IScheduleRepository _scheduleRepository;

        public ScheduleController(
            IScheduleService scheduleService,
            IScheduleRepository scheduleRepository)
        {
            _scheduleService = scheduleService;
            _scheduleRepository = scheduleRepository;
        }

        [AllowAnonymous]
        public async Task<IActionResult> Index(int? groupId)
        {
            var groups = await _scheduleRepository.GetAllGroupsAsync();
            ViewBag.Groups = new SelectList(groups, "Id", "Name", groupId);

            if (groupId.HasValue)
            {
                var schedule = await _scheduleService.GetGroupScheduleAsync(groupId.Value);
                ViewBag.SelectedGroupId = groupId.Value;
                return View(schedule.OrderBy(s => s.Date).ThenBy(s => s.StartTime));
            }

            return View(new List<ScheduleLesson>());
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create(int? groupId)
        {
            var model = await PrepareCreateViewModel(groupId);
            return View(model);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateScheduleViewModel model)
        {
            // Прибираємо валідацію для ID, бо ми заповнимо їх самі
            ModelState.Remove("SubjectId");
            ModelState.Remove("TeacherId");

            if (!ModelState.IsValid)
            {
                return View(await PrepareCreateViewModel(model.GroupId, model));
            }

            try 
            {
                // 1. ОБРОБКА ПРЕДМЕТА через ScheduleRepository
                var allSubjects = await _scheduleRepository.GetAllSubjectsAsync();
                var subject = allSubjects.FirstOrDefault(s => s.Title.Trim().Equals(model.SubjectTitle.Trim(), StringComparison.OrdinalIgnoreCase));

                if (subject == null)
                {
                    subject = new Subject { Title = model.SubjectTitle.Trim() };
                    await _scheduleRepository.CreateSubjectAsync(subject); 
                }

                // 2. ОБРОБКА ВИКЛАДАЧА через ТОЙ ЖЕ ScheduleRepository
                // Тобі потрібно додати метод GetAllTeachersAsync у IScheduleRepository
                var allTeachers = await _scheduleRepository.GetAllTeachersAsync(); 
                var teacher = allTeachers.FirstOrDefault(t => t.FullName.Trim().Equals(model.TeacherFullName.Trim(), StringComparison.OrdinalIgnoreCase));

                if (teacher == null)
                {
                    teacher = new Teacher { FullName = model.TeacherFullName.Trim() };
                    await _scheduleRepository.CreateTeacherAsync(teacher); 
                }

                var request = new CreateScheduleLessonRequest
                {
                    GroupId = model.GroupId,
                    SubjectId = subject.Id,
                    TeacherId = teacher.Id,
                    StartDate = model.StartDate,
                    EndDate = model.EndDate,
                    DayOfWeek = model.DayOfWeek,
                    StartTime = model.StartTime,
                    EndTime = model.EndTime,
                    Room = model.Room ?? "Н/Д",
                    LessonType = model.LessonType ?? "Лекція",
                    IsEvenWeek = model.IsEvenWeek
                };

                var result = await _scheduleService.CreateLessonForGroupAsync(request);

                if (result.Success)
                {
                    TempData["SuccessMessage"] = "Графік успішно створено!";
                    return RedirectToAction(nameof(Index), new { groupId = model.GroupId });
                }

                ModelState.AddModelError(string.Empty, result.Message ?? "Помилка при генерації розкладу.");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, "Помилка: " + ex.Message);
            }

            return View(await PrepareCreateViewModel(model.GroupId, model));
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id, int groupId)
        {
            await _scheduleService.DeleteLessonAsync(id);
            return RedirectToAction(nameof(Index), new { groupId = groupId });
        }

        private async Task<CreateScheduleViewModel> PrepareCreateViewModel(int? groupId, CreateScheduleViewModel? model = null)
        {
            model ??= new CreateScheduleViewModel();
            var allGroups = await _scheduleRepository.GetAllGroupsAsync();
            model.Groups = allGroups.Select(g => new SelectListItem 
            { 
                Value = g.Id.ToString(), 
                Text = g.Name,
                Selected = g.Id == groupId
            }).ToList();

            if (groupId.HasValue) model.GroupId = groupId.Value;
            return model;
        }
    }
}
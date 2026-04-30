using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using StudentHelper.Application.Interfaces;
using StudentHelper.Application.Models; 
using StudentHelper.Domain.Entities;
using System.Globalization;

namespace StudentHelper.Application.Services
{
    public class ScheduleService : IScheduleService
    {
        private readonly IScheduleRepository _scheduleRepository;

        public ScheduleService(IScheduleRepository scheduleRepository)
        {
            _scheduleRepository = scheduleRepository;
        }

        public async Task AddRangeAsync(IEnumerable<ScheduleLesson> lessons)
        {
            await _scheduleRepository.AddRangeAsync(lessons);
        }

        public async Task<Result> CreateLessonForGroupAsync(CreateScheduleLessonRequest request)
        {
            try
            {
                var lessonsToCreate = new List<ScheduleLesson>();
                DateTime current = request.StartDate;

                // Знаходимо перший потрібний день тижня
                while (current.DayOfWeek != request.DayOfWeek)
                {
                    current = current.AddDays(1);
                }

                while (current <= request.EndDate)
                {
                    bool shouldAdd = true;
                    if (request.IsEvenWeek.HasValue)
                    {
                        int weekNumber = ISOWeek.GetWeekOfYear(current);
                        bool isEven = weekNumber % 2 == 0;
                        if (request.IsEvenWeek.Value != isEven)
                        {
                            shouldAdd = false;
                        }
                    }

                    if (shouldAdd)
                    {
                        lessonsToCreate.Add(new ScheduleLesson
                        {
                            GroupId = request.GroupId,
                            SubjectId = request.SubjectId,
                            TeacherId = request.TeacherId,
                            DayOfWeek = request.DayOfWeek,
                            Date = current, 
                            StartTime = request.StartTime,
                            EndTime = request.EndTime,
                            Room = request.Room ?? "Н/Д",
                            LessonType = request.LessonType ?? "Лекція",
                            IsEvenWeek = request.IsEvenWeek
                        });
                    }
                    
                    current = current.AddDays(7); 
                }

                if (!lessonsToCreate.Any())
                {
                    return Result.Fail("Не знайдено жодної дати, що відповідає критеріям.");
                }

                await _scheduleRepository.AddRangeAsync(lessonsToCreate);
                await _scheduleRepository.SaveChangesAsync();

                return Result.Ok();
            }
            catch (Exception ex)
            {
                return Result.Fail($"Помилка генерації графіка: {ex.Message}");
            }
        }

        public async Task<IEnumerable<ScheduleLesson>> GetGroupScheduleAsync(int groupId)
        {
            return await _scheduleRepository.GetByGroupIdAsync(groupId);
        }

        // ВИПРАВЛЕНО: Додано знак ?, щоб відповідати інтерфейсу репозиторію
        public async Task<ScheduleLesson?> GetLessonByIdAsync(int lessonId)
        {
            return await _scheduleRepository.GetByIdAsync(lessonId);
        }

        public async Task<Result> DeleteLessonAsync(int lessonId)
        {
            var lesson = await _scheduleRepository.GetByIdAsync(lessonId);
            if (lesson == null)
            {
                return Result.Fail("Заняття не знайдено.");
            }

            await _scheduleRepository.DeleteAsync(lesson);
            return Result.Ok();
        }

        public async Task<Result> CreateGroupAsync(string name)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(name))
                    return Result.Fail("Назва групи не може бути порожньою.");

                var allGroups = await _scheduleRepository.GetAllGroupsAsync();
                if (allGroups.Any(g => g.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
                    return Result.Fail("Група з такою назвою вже існує.");

                var group = new Group { Name = name };
                await _scheduleRepository.CreateGroupAsync(group); 

                return Result.Ok();
            }
            catch (Exception ex)
            {
                return Result.Fail($"Помилка при створенні групи: {ex.Message}");
            }
        }
    }
}
using StudentHelper.Application.Models;
using StudentHelper.Domain.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace StudentHelper.Application.Interfaces
{
    public interface IScheduleService
    {
        // Створення занять для групи на період
        Task<Result> CreateLessonForGroupAsync(CreateScheduleLessonRequest request);
        
        // Отримання розкладу групи
        Task<IEnumerable<ScheduleLesson>> GetGroupScheduleAsync(int groupId);
        
        // Отримання одного заняття (МАЄ БУТИ ТІЛЬКИ ОДИН ТАКИЙ МЕТОД ІЗ ?)
        Task<ScheduleLesson?> GetLessonByIdAsync(int lessonId);
        
        // Видалення заняття
        Task<Result> DeleteLessonAsync(int lessonId);
        
        // Робота з групами
        Task<Result> CreateGroupAsync(string name);
        
        // Масове додавання
        Task AddRangeAsync(IEnumerable<ScheduleLesson> lessons);
    }
}
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using StudentHelper.Domain.Entities;

namespace StudentHelper.Application.Interfaces
{
    public interface IScheduleRepository
    {
        // Отримуємо заняття за ID (дозволяємо null, щоб збігалося з репозиторієм)
        Task<ScheduleLesson?> GetByIdAsync(int id);

        // Отримуємо розклад для конкретної групи
        Task<IEnumerable<ScheduleLesson>> GetByGroupIdAsync(int groupId);

        // Додавання занять
        Task AddAsync(ScheduleLesson lesson);
        Task AddRangeAsync(IEnumerable<ScheduleLesson> lessons);

        // Видалення та перевірка наявності
        Task DeleteAsync(ScheduleLesson lesson);
        Task<bool> AnyAsync(Expression<Func<ScheduleLesson, bool>> predicate);

        // Робота з групами
        Task<IEnumerable<Group>> GetAllGroupsAsync();
        Task CreateGroupAsync(Group group);

        // Робота з предметами (для ручного введення з клавіатури)
        Task<IEnumerable<Subject>> GetAllSubjectsAsync();
        Task CreateSubjectAsync(Subject subject);

        // Робота з викладачами (для ручного введення з клавіатури)
        Task<IEnumerable<Teacher>> GetAllTeachersAsync();
        Task CreateTeacherAsync(Teacher teacher);

        // Збереження змін
        Task SaveChangesAsync();
    }
}
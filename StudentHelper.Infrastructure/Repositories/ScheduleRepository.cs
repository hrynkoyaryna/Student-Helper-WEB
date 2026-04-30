using Microsoft.EntityFrameworkCore;
using StudentHelper.Application.Interfaces;
using StudentHelper.Domain.Entities;
using StudentHelper.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace StudentHelper.Infrastructure.Repositories
{
    public class ScheduleRepository : IScheduleRepository
    {
        private readonly StudentHelperDbContext _context;

        public ScheduleRepository(StudentHelperDbContext context)
        {
            _context = context;
        }

        // --- МЕТОДИ ДЛЯ РОЗКЛАДУ ---

        public async Task<ScheduleLesson?> GetByIdAsync(int id)
        {
            return await _context.ScheduleLessons
                .Include(s => s.Subject)
                .Include(s => s.Teacher)
                .Include(s => s.Group)
                .FirstOrDefaultAsync(s => s.Id == id);
        }

        public async Task<IEnumerable<ScheduleLesson>> GetByGroupIdAsync(int groupId)
        {
            return await _context.ScheduleLessons
                .Where(s => s.GroupId == groupId)
                .Include(s => s.Subject)
                .Include(s => s.Teacher)
                .OrderBy(s => s.DayOfWeek)
                .ThenBy(s => s.StartTime)
                .ToListAsync();
        }

        public async Task AddAsync(ScheduleLesson lesson)
        {
            await _context.ScheduleLessons.AddAsync(lesson);
        }

        public async Task AddRangeAsync(IEnumerable<ScheduleLesson> lessons)
        {
            await _context.ScheduleLessons.AddRangeAsync(lessons);
        }

        public async Task DeleteAsync(ScheduleLesson lesson)
        {
            _context.ScheduleLessons.Remove(lesson);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> AnyAsync(Expression<Func<ScheduleLesson, bool>> predicate)
        {
            return await _context.ScheduleLessons.AnyAsync(predicate);
        }

        // --- МЕТОДИ ДЛЯ ГРУП ---

        public async Task<IEnumerable<Group>> GetAllGroupsAsync()
        {
            return await _context.Groups.OrderBy(g => g.Name).ToListAsync();
        }

        public async Task CreateGroupAsync(Group group)
        {
            await _context.Groups.AddAsync(group);
            await _context.SaveChangesAsync();
        }

        // --- МЕТОДИ ДЛЯ ПРЕДМЕТІВ ---

        public async Task<IEnumerable<Subject>> GetAllSubjectsAsync()
        {
            return await _context.Subjects.OrderBy(s => s.Title).ToListAsync();
        }

        public async Task CreateSubjectAsync(Subject subject)
        {
            _context.Subjects.Add(subject);
            await _context.SaveChangesAsync();
        }

        // --- МЕТОДИ ДЛЯ ВИКЛАДАЧІВ ---

        public async Task<IEnumerable<Teacher>> GetAllTeachersAsync()
        {
            return await _context.Teachers.ToListAsync();
        }

        public async Task CreateTeacherAsync(Teacher teacher)
        {
            _context.Teachers.Add(teacher);
            await _context.SaveChangesAsync();
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
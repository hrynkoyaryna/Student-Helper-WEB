using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StudentHelper.Application.Interfaces;
using StudentHelper.Application.Models;
using StudentHelper.Domain.Entities;

namespace StudentHelper.Application.Services;

public class ExamsService : IExamsService
{
    private readonly IExamsRepository _repository;
    private readonly ITeacherRepository _teacherRepository;
    private readonly ILogger<ExamsService> _logger;
    private readonly IOptions<ApplicationSettings> _settings;

    public ExamsService(IExamsRepository repository, ITeacherRepository teacherRepository, ILogger<ExamsService> logger, IOptions<ApplicationSettings> settings)
    {
        _repository = repository;
        _teacherRepository = teacherRepository;
        _logger = logger;
        _settings = settings;
    }

    public async Task<List<Exam>> GetExamsAsync()
    {
        return await _repository.GetAllAsync();
    }

    public async Task<Exam?> GetExamByIdAsync(int id)
    {
        return await _repository.GetByIdAsync(id);
    }

    public async Task<List<Exam>> GetByUserIdAsync(int userId)
    {
        return (await _repository.GetAllAsync()).Where(e => e.UserId == userId).ToList();
    }

    public async Task<List<Exam>> GetByGroupIdAsync(int groupId)
    {
        return (await _repository.GetAllAsync()).Where(e => e.GroupId == groupId).ToList();
    }

    public async Task<Result> CreateExamAsync(Exam exam)
    {
        if (string.IsNullOrWhiteSpace(exam.Subject))
            return Result.Fail("Предмет не може бути порожнім");

        if (exam.DateTime == default)
            return Result.Fail("Вкажіть дату та час іспиту");

        // Normalize to UTC
        exam.DateTime = DateTime.SpecifyKind(exam.DateTime, DateTimeKind.Local).ToUniversalTime();

        await _repository.AddAsync(exam);
        await _repository.SaveChangesAsync();
        _logger.LogInformation("Exam created: {ExamId} {Subject}", exam.Id, exam.Subject);
        return "Екзамен створено";
    }

    public async Task<Result> UpdateExamAsync(Exam exam)
    {
        var existing = await _repository.GetByIdAsync(exam.Id);
        if (existing == null)
            return Result.Fail("Екзамен не знайдено");

        existing.Subject = exam.Subject;
        existing.DateTime = DateTime.SpecifyKind(exam.DateTime, DateTimeKind.Local).ToUniversalTime();
        existing.TeacherId = exam.TeacherId;

        await _repository.UpdateAsync(existing);
        await _repository.SaveChangesAsync();
        _logger.LogInformation("Exam updated: {ExamId}", exam.Id);
        return "Екзамен оновлено";
    }

    public async Task<Result> DeleteExamAsync(int id, int userId)
    {
        var existing = await _repository.GetByIdAsync(id);
        if (existing == null)
            return Result.Fail("Екзамен не знайдено");

        // Allow deletion if user is the creator (works for both personal and group exams)
        if (existing.UserId != userId)
            return Result.Fail("Ви можете видаляти лише свої екзамени");

        await _repository.DeleteAsync(existing);
        await _repository.SaveChangesAsync();
        _logger.LogInformation("Exam deleted: {ExamId}", id);
        return "Екзамен видалено";
    }

    // Permission checks
    public async Task<bool> CanEditExamAsync(int examId, int userId)
    {
        var exam = await _repository.GetByIdAsync(examId);
        if (exam == null) return false;

        // Only the creator can edit their own exam
        return exam.UserId == userId && exam.GroupId == null;
    }

    public async Task<bool> CanDeleteExamAsync(int examId, int userId)
    {
        var exam = await _repository.GetByIdAsync(examId);
        if (exam == null) return false;

        // Allow deletion if user is the creator:
        // - For personal exams: user created it (GroupId == null)
        // - For group exams: user is the admin who created it
        return exam.UserId == userId;
    }

    // New higher-level methods that encapsulate teacher lookup/creation
    public async Task<Result> CreateExamAsync(CreateExamRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Subject))
            return Result.Fail("Предмет не може бути порожнім");

        if (request.DateTime == default)
            return Result.Fail("Вкажіть дату та час іспиту");

        int teacherId;
        if (!string.IsNullOrWhiteSpace(request.TeacherName))
        {
            var existing = await _teacherRepository.GetByNameAsync(request.TeacherName!.Trim());
            if (existing != null)
            {
                teacherId = existing.Id;
            }
            else
            {
                var newTeacher = new Teacher { FullName = request.TeacherName!.Trim() };
                await _teacherRepository.AddAsync(newTeacher);
                await _teacherRepository.SaveChangesAsync();
                teacherId = newTeacher.Id;
            }
        }
        else if (request.TeacherId.HasValue)
        {
            teacherId = request.TeacherId.Value;
        }
        else
        {
            return Result.Fail("Вкажіть викладача");
        }

        var exam = new Exam
        {
            Subject = request.Subject.Trim(),
            DateTime = DateTime.SpecifyKind(request.DateTime, DateTimeKind.Local),
            TeacherId = teacherId,
            Description = request.Description,
            UserId = request.UserId
        };

        return await CreateExamAsync(exam);
    }

    public async Task<Result> UpdateExamAsync(UpdateExamRequest request)
    {
        var existing = await _repository.GetByIdAsync(request.Id);
        if (existing == null)
            return Result.Fail("Екзамен не знайдено");

        if (existing.UserId != request.UserId)
            return Result.Fail("Ви можете змінювати лише свої екзамени");

        int teacherId;
        if (!string.IsNullOrWhiteSpace(request.TeacherName))
        {
            var found = await _teacherRepository.GetByNameAsync(request.TeacherName!.Trim());
            if (found != null)
            {
                teacherId = found.Id;
            }
            else
            {
                var newTeacher = new Teacher { FullName = request.TeacherName!.Trim() };
                await _teacherRepository.AddAsync(newTeacher);
                await _teacherRepository.SaveChangesAsync();
                teacherId = newTeacher.Id;
            }
        }
        else if (request.TeacherId.HasValue)
        {
            teacherId = request.TeacherId.Value;
        }
        else
        {
            return Result.Fail("Вкажіть викладача");
        }

        existing.Subject = request.Subject.Trim();
        existing.DateTime = DateTime.SpecifyKind(request.DateTime, DateTimeKind.Local).ToUniversalTime();
        existing.TeacherId = teacherId;
        existing.Description = request.Description;
        existing.UserId = request.UserId;

        await _repository.UpdateAsync(existing);
        await _repository.SaveChangesAsync();
        _logger.LogInformation("Exam updated: {ExamId}", request.Id);
        return "Екзамен оновлено";
    }

    // Admin methods for creating exams for groups
    public async Task<Result> CreateGroupExamAsync(CreateGroupExamRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Subject))
            return Result.Fail("Предмет не може бути порожнім");

        if (request.DateTime == default)
            return Result.Fail("Вкажіть дату та час іспиту");

        if (request.GroupId <= 0)
            return Result.Fail("Виберіть групу");

        int teacherId;
        if (!string.IsNullOrWhiteSpace(request.TeacherName))
        {
            var existing = await _teacherRepository.GetByNameAsync(request.TeacherName!.Trim());
            if (existing != null)
            {
                teacherId = existing.Id;
            }
            else
            {
                var newTeacher = new Teacher { FullName = request.TeacherName!.Trim() };
                await _teacherRepository.AddAsync(newTeacher);
                await _teacherRepository.SaveChangesAsync();
                teacherId = newTeacher.Id;
            }
        }
        else if (request.TeacherId.HasValue)
        {
            teacherId = request.TeacherId.Value;
        }
        else
        {
            return Result.Fail("Вкажіть викладача");
        }

        var exam = new Exam
        {
            Subject = request.Subject.Trim(),
            DateTime = DateTime.SpecifyKind(request.DateTime, DateTimeKind.Local),
            TeacherId = teacherId,
            Description = request.Description,
            UserId = request.AdminUserId,
            GroupId = request.GroupId
        };

        await _repository.AddAsync(exam);
        await _repository.SaveChangesAsync();
        _logger.LogInformation("Group exam created: {ExamId} {Subject} for GroupId: {GroupId}", exam.Id, exam.Subject, exam.GroupId);
        return "Екзамен для групи створено";
    }

    public async Task<Result> UpdateGroupExamAsync(UpdateGroupExamRequest request)
    {
        var existing = await _repository.GetByIdAsync(request.Id);
        if (existing == null)
            return Result.Fail("Екзамен не знайдено");

        if (existing.GroupId == null)
            return Result.Fail("Це не групоевий екзамен");

        if (existing.UserId != request.AdminUserId)
            return Result.Fail("Тільки адміністратор, який створив цей екзамен, може його редагувати");

        int teacherId;
        if (!string.IsNullOrWhiteSpace(request.TeacherName))
        {
            var found = await _teacherRepository.GetByNameAsync(request.TeacherName!.Trim());
            if (found != null)
            {
                teacherId = found.Id;
            }
            else
            {
                var newTeacher = new Teacher { FullName = request.TeacherName!.Trim() };
                await _teacherRepository.AddAsync(newTeacher);
                await _teacherRepository.SaveChangesAsync();
                teacherId = newTeacher.Id;
            }
        }
        else if (request.TeacherId.HasValue)
        {
            teacherId = request.TeacherId.Value;
        }
        else
        {
            return Result.Fail("Вкажіть викладача");
        }

        existing.Subject = request.Subject.Trim();
        existing.DateTime = DateTime.SpecifyKind(request.DateTime, DateTimeKind.Local).ToUniversalTime();
        existing.TeacherId = teacherId;
        existing.Description = request.Description;

        await _repository.UpdateAsync(existing);
        await _repository.SaveChangesAsync();
        _logger.LogInformation("Group exam updated: {ExamId}", request.Id);
        return "Екзамен для групи оновлено";
    }

    // New method: expose teachers for controllers
    public Task<List<Teacher>> GetAllTeachersAsync()
    {
        return _teacherRepository.GetAllAsync();
    }
}

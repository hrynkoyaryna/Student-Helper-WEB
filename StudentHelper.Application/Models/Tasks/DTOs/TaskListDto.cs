using StudentHelper.Application.Models.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StudentHelper.Application.Models.Tasks.DTOs;

public sealed class TaskListDto
{
    public List<TaskListItemDto> Tasks { get; set; } = new();

    public string? SearchTerm { get; set; }

    public int? SubjectId { get; set; }

    public string? Status { get; set; }

    public List<TaskSubjectDto> Subjects { get; set; } = new();
}

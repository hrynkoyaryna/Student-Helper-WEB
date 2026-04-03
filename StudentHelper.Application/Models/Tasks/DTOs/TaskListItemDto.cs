using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StudentHelper.Application.Models.Tasks.DTOs;

public sealed class TaskListItemDto
{
    public Guid Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string SubjectName { get; set; } = string.Empty;

    public DateTime Deadline { get; set; }

    public string Status { get; set; } = string.Empty;
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StudentHelper.Application.Models.Tasks.DTOs;

public sealed class TaskFilterDto
{
    public string? SearchTerm { get; set; }

    public int? SubjectId { get; set; }

    public string? Status { get; set; }
}

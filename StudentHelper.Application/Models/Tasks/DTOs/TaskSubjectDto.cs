using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StudentHelper.Application.Models.Tasks.DTOs;

public sealed class TaskSubjectDto
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;
}

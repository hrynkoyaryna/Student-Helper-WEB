using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StudentHelper.Application.Common;

public sealed class Error
{
    public static readonly Error None = new(string.Empty, string.Empty);

    public Error(string code, string message)
    {
        this.Code = code;
        this.Message = message;
    }

    public string Code { get; }

    public string Message { get; }

    public static Error Validation(string message) => new("Validation", message);

    public static Error NotFound(string message) => new("NotFound", message);

    public static Error Failure(string message) => new("Failure", message);
}

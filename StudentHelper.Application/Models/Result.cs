namespace StudentHelper.Application.Models;

public class Result
{
    private Result()
    {
    }

    public bool Success { get; private set; }
    public string Message { get; private set; } = string.Empty;

    // Type conversion operators
    public static implicit operator Result(string message)
    {
        return Ok(message);
    }

    public static implicit operator Result(bool success)
    {
        return success ? Ok() : Fail("Operation failed");
    }

    public static Result Ok(string? message = null)
    {
        return new Result { Success = true, Message = message ?? string.Empty };
    }

    public static Result Fail(string message)
    {
        return new Result { Success = false, Message = message };
    }

    // Allow deconstruction: (bool success, string message) = result;
    public void Deconstruct(out bool success, out string message)
    {
        success = Success;
        message = Message;
    }
}

public class Result<T>
{
    private Result()
    {
    }

    public bool Success { get; private set; }
    public T? Value { get; private set; }
    public string Message { get; private set; } = string.Empty;

    // Type conversion operators
    public static implicit operator Result<T>(T? value)
    {
        return Ok(value);
    }

    public static Result<T> Ok(T? value, string? message = null)
    {
        return new Result<T> { Success = true, Value = value, Message = message ?? string.Empty };
    }

    public static Result<T> Fail(string message)
    {
        return new Result<T> { Success = false, Value = default, Message = message };
    }

    // Allow deconstruction: (bool success, T? value, string message) = result;
    public void Deconstruct(out bool success, out T? value, out string message)
    {
        success = Success;
        value = Value;
        message = Message;
    }
}

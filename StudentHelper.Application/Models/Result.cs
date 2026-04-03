namespace StudentHelper.Application.Models;

public class Result
{
    protected Result(bool success, string message)
    {
        this.Success = success;
        this.Message = message;
    }

    public bool Success { get; }

    public bool Failure => !this.Success;

    public string Message { get; }

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
        return new Result(true, message ?? string.Empty);
    }

    public static Result Fail(string message)
    {
        return new Result(false, message);
    }

    public void Deconstruct(out bool success, out string message)
    {
        success = this.Success;
        message = this.Message;
    }
}

public class Result<T> : Result
{
    private readonly T? value;

    private Result(T value, string? message = null)
        : base(true, message ?? string.Empty)
    {
        this.value = value;
    }

    private Result(string message)
        : base(false, message)
    {
        this.value = default;
    }

    public T Value =>
        this.Success
            ? this.value!
            : throw new InvalidOperationException("No value for failure result.");

    public static implicit operator Result<T>(T value)
    {
        return new Result<T>(value);
    }

    public static Result<T> Ok(T value, string? message = null)
    {
        return new Result<T>(value, message);
    }

    public static new Result<T> Fail(string message)
    {
        return new Result<T>(message);
    }

    public void Deconstruct(out bool success, out T? value, out string message)
    {
        success = this.Success;
        value = this.value;
        message = this.Message;
    }
}

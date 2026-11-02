namespace ZenithAssets.Application.Common.Models;

public class Result
{
    public bool Succeeded { get; }
    public string? Error { get; }
    public string[] Errors { get; }

    protected Result(bool succeeded, string? error, string[]? errors = null)
    {
        Succeeded = succeeded;
        Error = error;
        Errors = errors ?? (error != null ? [error] : []);
    }

    public static Result Success() => new(true, null);
    public static Result Failure(string error) => new(false, error);
    public static Result Failure(IEnumerable<string> errors) => new(false, errors.FirstOrDefault(), errors.ToArray());
}

public class Result<T> : Result
{
    public T? Value { get; }

    private Result(bool succeeded, T? value, string? error, string[]? errors = null)
        : base(succeeded, error, errors)
    {
        Value = value;
    }

    public static Result<T> Success(T value) => new(true, value, null);
    public new static Result<T> Failure(string error) => new(false, default, error);
    public new static Result<T> Failure(IEnumerable<string> errors) => new(false, default, errors.FirstOrDefault(), errors.ToArray());
}

public class PaginatedList<T>
{
    public IReadOnlyCollection<T> Items { get; }
    public int PageNumber { get; }
    public int TotalPages { get; }
    public int TotalCount { get; }
    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber < TotalPages;

    public PaginatedList(IReadOnlyCollection<T> items, int count, int pageNumber, int pageSize)
    {
        PageNumber = pageNumber;
        TotalPages = (int)Math.Ceiling(count / (double)pageSize);
        TotalCount = count;
        Items = items;
    }
}


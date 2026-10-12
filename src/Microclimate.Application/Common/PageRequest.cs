namespace Microclimate.Application.Common;

/// <summary>Параметри пагінації: сторінка від 1, розмір від 1 до <see cref="MaxPageSize"/>.</summary>
public sealed record PageRequest
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;

    public PageRequest(int page = 1, int pageSize = DefaultPageSize)
    {
        if (page < 1)
        {
            throw new ValidationException(nameof(page), "Номер сторінки має бути не меншим за 1.");
        }

        if (pageSize is < 1 or > MaxPageSize)
        {
            throw new ValidationException(nameof(pageSize), $"Розмір сторінки має бути від 1 до {MaxPageSize}.");
        }

        Page = page;
        PageSize = pageSize;
    }

    public int Page { get; }

    public int PageSize { get; }

    /// <summary>Кількість елементів, які треба пропустити.</summary>
    public int Skip => (Page - 1) * PageSize;
}

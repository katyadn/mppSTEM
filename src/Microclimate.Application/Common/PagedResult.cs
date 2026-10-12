namespace Microclimate.Application.Common;

/// <summary>Сторінка результатів запиту.</summary>
/// <typeparam name="T">Тип елемента.</typeparam>
public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount);

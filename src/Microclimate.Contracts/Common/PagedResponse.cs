namespace Microclimate.Contracts.Common;

/// <summary>
/// Універсальна пагінована відповідь списку сутностей.
/// </summary>
/// <typeparam name="T">Тип елемента в списку.</typeparam>
/// <param name="Items">Колекція елементів поточної сторінки.</param>
/// <param name="Page">Номер поточної сторінки (починаючи з 1).</param>
/// <param name="PageSize">Кількість елементів на сторінці.</param>
/// <param name="TotalCount">Загальна кількість елементів, що задовольняють фільтр.</param>
public sealed record PagedResponse<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalCount);

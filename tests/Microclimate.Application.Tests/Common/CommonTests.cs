using Microclimate.Application.Common;

namespace Microclimate.Application.Tests.Common;

public class CommonTests
{
    [Theory]
    [InlineData(1, 1, 0)]
    [InlineData(3, 20, 40)]
    [InlineData(1, PageRequest.MaxPageSize, 0)]
    public void PageRequest_ValidValues_ComputesSkip(int page, int pageSize, int expectedSkip)
    {
        var request = new PageRequest(page, pageSize);

        Assert.Equal(expectedSkip, request.Skip);
    }

    [Fact]
    public void PageRequest_Defaults_FirstPageOfTwenty()
    {
        var request = new PageRequest();

        Assert.Equal(1, request.Page);
        Assert.Equal(PageRequest.DefaultPageSize, request.PageSize);
    }

    [Theory]
    [InlineData(0, 20, "page")]
    [InlineData(1, 0, "pageSize")]
    [InlineData(1, PageRequest.MaxPageSize + 1, "pageSize")]
    public void PageRequest_InvalidValues_ThrowsValidationExceptionForField(int page, int pageSize, string field)
    {
        var exception = Assert.Throws<ValidationException>(() => new PageRequest(page, pageSize));

        Assert.True(exception.Errors.ContainsKey(field));
    }

    [Fact]
    public void Exceptions_StandardConstructors_PreserveMessages()
    {
        var inner = new InvalidOperationException();

        Assert.Equal("Кімната '42' не знайдено.", NotFoundException.For("Кімната", 42).Message);
        Assert.Same(inner, new NotFoundException("m", inner).InnerException);
        Assert.NotNull(new NotFoundException().Message);
        Assert.Equal("m", new ConflictException("m").Message);
        Assert.Same(inner, new ConflictException("m", inner).InnerException);
        Assert.NotNull(new ConflictException().Message);
        Assert.Empty(new ValidationException().Errors);
        Assert.Empty(new ValidationException("m").Errors);
        Assert.Empty(new ValidationException("m", inner).Errors);
    }
}

using Microclimate.Application.Common;

namespace Microclimate.Application.Tests.Common;

public class CommonTests
{
    [Fact]
    public void PageRequest_ThirdPage_SkipsTwoPages()
    {
        Assert.Equal(40, new PageRequest(3, 20).Skip);
    }

    [Theory]
    [InlineData(0, 20, "page")]
    [InlineData(1, PageRequest.MaxPageSize + 1, "pageSize")]
    public void PageRequest_InvalidValues_ThrowsForField(int page, int pageSize, string field)
    {
        var exception = Assert.Throws<ValidationException>(() => new PageRequest(page, pageSize));

        Assert.True(exception.Errors.ContainsKey(field));
    }
}

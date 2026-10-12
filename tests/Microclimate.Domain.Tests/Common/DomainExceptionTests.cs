using Microclimate.Domain.Common;

namespace Microclimate.Domain.Tests.Common;

public class DomainExceptionTests
{
    [Fact]
    public void Constructors_PreserveMessageAndInnerException()
    {
        var inner = new InvalidOperationException("inner");

        Assert.NotNull(new DomainException().Message);
        Assert.Equal("m", new DomainException("m").Message);
        Assert.Same(inner, new DomainException("m", inner).InnerException);
    }
}

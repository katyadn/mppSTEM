using Microclimate.Domain.Common;
using Microclimate.Domain.ValueObjects;

namespace Microclimate.Domain.Tests.ValueObjects;

public class ValueObjectsTests
{
    [Fact]
    public void DateRange_ValidBounds_NormalizesToUtcAndComputesDuration()
    {
        var from = new DateTimeOffset(2026, 10, 1, 15, 0, 0, TimeSpan.FromHours(3));
        var to = from.AddHours(2);

        var range = DateRange.Create(from, to);

        Assert.Equal(TimeSpan.Zero, range.From.Offset);
        Assert.Equal(new DateTimeOffset(2026, 10, 1, 12, 0, 0, TimeSpan.Zero), range.From);
        Assert.Equal(TimeSpan.FromHours(2), range.Duration);
    }

    [Fact]
    public void DateRange_FromEqualsTo_Throws()
    {
        Assert.Throws<DomainException>(() => DateRange.Create(TestData.Now, TestData.Now));
    }

    [Fact]
    public void DateRange_FromAfterTo_Throws()
    {
        Assert.Throws<DomainException>(() => DateRange.Create(TestData.Now, TestData.Now.AddSeconds(-1)));
    }

    [Fact]
    public void DateRange_ExactlyMaxDuration_IsAllowed()
    {
        var range = DateRange.Create(TestData.Now, TestData.Now.AddDays(90), TimeSpan.FromDays(90));

        Assert.Equal(TimeSpan.FromDays(90), range.Duration);
    }

    [Fact]
    public void DateRange_LongerThanMaxDuration_Throws()
    {
        Assert.Throws<DomainException>(() =>
            DateRange.Create(TestData.Now, TestData.Now.AddDays(91), TimeSpan.FromDays(90)));
    }

    [Fact]
    public void DateRange_Contains_IsHalfOpen()
    {
        var range = DateRange.Create(TestData.Now, TestData.Now.AddHours(1));

        Assert.True(range.Contains(TestData.Now));
        Assert.True(range.Contains(TestData.Now.AddMinutes(59)));
        Assert.False(range.Contains(TestData.Now.AddHours(1)));
        Assert.False(range.Contains(TestData.Now.AddTicks(-1)));
    }

    [Fact]
    public void MetricRange_Contains_IncludesBounds()
    {
        var range = new MetricRange(0, 100);

        Assert.True(range.Contains(0));
        Assert.True(range.Contains(100));
        Assert.False(range.Contains(100.01));
        Assert.False(range.Contains(-0.01));
    }

    [Fact]
    public void MetricRange_MinGreaterThanMax_Throws()
    {
        Assert.Throws<DomainException>(() => new MetricRange(10, 5));
    }

    [Fact]
    public void MetricRange_NaN_Throws()
    {
        Assert.Throws<DomainException>(() => new MetricRange(double.NaN, 5));
    }
}

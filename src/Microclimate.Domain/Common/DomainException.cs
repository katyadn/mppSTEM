namespace Microclimate.Domain.Common;

/// <summary>Порушення правила предметної області (некоректні дані або заборонена зміна стану).</summary>
public sealed class DomainException : Exception
{
    public DomainException()
    {
    }

    public DomainException(string message)
        : base(message)
    {
    }

    public DomainException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

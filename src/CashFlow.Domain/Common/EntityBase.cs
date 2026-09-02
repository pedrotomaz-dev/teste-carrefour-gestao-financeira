namespace CashFlow.Domain.Common;

public abstract class EntityBase
{
    public Guid Id { get; protected set; }
    public DateTime CreatedAt { get; protected set; }

    protected EntityBase()
    {
        Id = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
    }
}

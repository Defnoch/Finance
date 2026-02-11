namespace Finance.Domain.Entities;

public class TransactionLink
{
    public Guid TransactionLinkId { get; set; }
    public Guid TransactionId1 { get; set; }
    public Guid TransactionId2 { get; set; }
    public DateTime LinkedAt { get; set; } = DateTime.UtcNow;
}

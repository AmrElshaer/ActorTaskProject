namespace Shared.Events;

public class NewNumberAddedEvent
{
    public int Result { get; set; }
    public DateTime CreationDate { get; set; }
    public DateTimeOffset EnqueueTimestamp { get; set; }
}
namespace ServiceA.Entities;

public class Message(int number1, int number2, DateTime createdAt)
{
    public int Id { get; private set; }
    public int Number1 { get; init; } = number1;
    public int Number2 { get; init; } = number2;
    public DateTime CreatedAt { get; init; } = createdAt;
}
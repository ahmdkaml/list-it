namespace ListIt.Models;

public class Task
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public string Title { get; set; } = string.Empty;
}

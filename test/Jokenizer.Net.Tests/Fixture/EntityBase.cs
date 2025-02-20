namespace Jokenizer.Net.Tests.Fixture;

public class EntityBase<T>: IEntity<T> {
    public T Id { get; set; } = default!;
    public string? Name { get; set; }
}

namespace Jokenizer.Net.Tests.Fixture;

public interface IEntity<T> {
    T Id { get; set; }
    public string? Name { get; set; }
}

using System;

namespace Jokenizer.Net.Tests.Fixture;

public class Company {
    public int this[int index] => Id.ToString()[index];
    public Guid Id { get; set; }
    public Guid? OwnerId { get; set; }
    public string? Name { get; set; }
    public int? PostalCode { get; set; }
    public DateTime CreateDate { get; set; }
    public DateTime? UpdateDate { get; set; }

    public int Count(Func<int, int> modifier) => modifier(Name?.Length ?? 0);
}

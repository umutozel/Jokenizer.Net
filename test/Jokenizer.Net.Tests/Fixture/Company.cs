using System;

namespace Jokenizer.Net.Tests.Fixture;

public class Company {
    public Guid Id { get; set; }
    public Guid? OwnerId { get; set; }
    public string? Name { get; set; }
    public int? PostalCode { get; set; }
    public DateTime CreateDate { get; set; }
    public DateTime? UpdateDate { get; set; }
}

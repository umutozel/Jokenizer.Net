using System;
using System.Collections.Generic;

namespace Jokenizer.Net.Tests.Fixture;

public class Supplier {
    public int Id { get; set; }
    public string Name { get; set; }
    public ICollection<SupplierType> SupplierTypes { get; set; }
    public SupplierType SupplierType { get; set; }
}

[Flags]
public enum SupplierType {
    Damage = 1,
    Tyre = 2,
    Fine = 4
}

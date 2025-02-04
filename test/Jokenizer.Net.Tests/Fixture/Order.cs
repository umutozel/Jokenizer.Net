using System;
using System.Collections.Generic;

namespace Jokenizer.Net.Tests.Fixture;

public class Order {
    public int Id { get; set; }
    public string? OrderNo { get; set; }
    public DateTime OrderDate { get; set; }
    public double? Price;
    public IList<OrderLine>? Lines { get; set; }
}

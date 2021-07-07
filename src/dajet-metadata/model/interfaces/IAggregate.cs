using System.Collections.Generic;

namespace DaJet.Metadata.Model
{
    public interface IAggregate // only reference types can be aggregates
    {
        ApplicationObject Owner { get; set; }
        List<ApplicationObject> Elements { get; set; }
    }
}
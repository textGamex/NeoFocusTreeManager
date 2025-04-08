using System.Collections.Generic;
using System.Runtime.Serialization;

namespace FocusTreeManager.DataContract;

[KnownType(typeof(Focus))]
[DataContract(Name = "prerequisite", Namespace = "focusesNs", IsReference = true)]
public sealed class PrerequisitesSet(Focus focus)
{
    [DataMember(Name = "focus", Order = 0)]
    public Focus Focus { get; set; } = focus;

    [DataMember(Name = "linked_foci", Order = 1)]
    public List<Focus> FociList { get; set; } = [];
}
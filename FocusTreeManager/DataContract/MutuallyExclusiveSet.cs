using System.Runtime.Serialization;

namespace FocusTreeManager.DataContract;

[KnownType(typeof(Focus))]
[DataContract(Name = "mutually_exclusive", Namespace = "focusesNs")]
public sealed class MutuallyExclusiveSet(Focus focus1, Focus focus2)
{
    [DataMember(Name = "focus_left", Order = 0)]
    public Focus Focus1 { get; set; } = focus1;

    [DataMember(Name = "focus_right", Order = 1)]
    public Focus Focus2 { get; set; } = focus2;
}
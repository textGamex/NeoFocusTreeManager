using FocusTreeManager.CodeStructures;
using System.Runtime.Serialization;

namespace FocusTreeManager.DataContract;

[KnownType(typeof(Script))]
[DataContract(Name = "event_description")]
public class EventDescription
{
    [DataMember(Name = "script", Order = 0)]
    public Script InternalScript { get; set; }
}
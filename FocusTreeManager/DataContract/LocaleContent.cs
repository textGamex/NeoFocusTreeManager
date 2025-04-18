﻿using System.Runtime.Serialization;

namespace FocusTreeManager.DataContract;

[DataContract(Name = "locale")]
public class LocaleContent
{
    [DataMember(Name = "key", Order = 0)]
    public string Key { get; set; }

    [DataMember(Name = "value", Order = 1)]
    public string Value { get; set; }

    public LocaleContent() { }
}
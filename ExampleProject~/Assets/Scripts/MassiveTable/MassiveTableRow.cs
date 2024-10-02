using System;
using System.Collections.Generic;
using System.ComponentModel;
using Ninjadini.Neuro;
using UnityEngine;


[NeuroGlobalType(300)]
[DisplayName("MassiveTableRow[] Generate via Tools/Populate Massive Table")]
public partial class MassiveTableRow : IReferencable
{
    [Neuro(1)] public int Int;
    [Neuro(2)] public int Int2;
    [Neuro(3)] public uint Uint;
    [Neuro(4)] public uint Uint2;
    [Neuro(5)] public string Str;
    [Neuro(6)] public float Float;
    [Neuro(7)] public float Float2;
    
    [Neuro(15)] public DateTime Date;
    [Neuro(16)] public TimeSpan TimeSpan;
    
    [Header(">Objects")]
    [Neuro(20)] public MassiveTableRow Child;
    [Neuro(21)] public List<MassiveTableRow> Children = new List<MassiveTableRow>();
    [Neuro(30)] public List<string> Strings = new List<string>();
    
    [Header(">Polymorphic")]
    [Neuro(40)] public MyTestRef Poly;
    [Neuro(41)] public List<MyTestRef> PolyList = new List<MyTestRef>();
    
    [Header(">")]
    [Neuro(51)] public TestStruct Struct;
    
    public uint RefId { get; set; }
    public string RefName { get; set; }
    
    public partial struct TestStruct : IEquatable<TestStruct>
    {
        [Neuro(1)] public int Id;
        [Neuro(2)] public string Name;

        public bool Equals(TestStruct other)
        {
            return Id == other.Id && Name == other.Name;
        }
    }
}


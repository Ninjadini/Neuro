namespace Ninjadini.Neuro.Sync
{
    public enum FieldSizeType : uint
    {
        VarInt = 0,// next data is varint
        Fixed32 = 1,// 4 bytes
        Fixed64 = 2,// 8 bytes
        Length = 3,// next data is varint which determines the size
        Child = 4,// start or end of child - with tag value = start, no tag = end
        ChildWithTag = 5,// start of child but next data is class's tag (varint) to determine polymorphic type - for calls to base class, the tag value will be 0.
        Dictionary = 6 // start of dictionary, next data is the field size type of key and value (split half in a byte) followed by number of items and then the contents.
    }
}
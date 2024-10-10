namespace Ninjadini.Neuro.Sync
{
    public enum FieldSizeType : byte
    {
        VarInt = 0,// content is varint
        Fixed32 = 1,// content 4 bytes
        Fixed64 = 2,// content 8 bytes
        Fixed128 = 3,// content 16 bytes
        Length = 5,// next data is varint which determines the size
        Child = 6,// start or end of child - with tag value = start, no tag = end
        ChildWithTag = 7,// start of child but next data is class's tag (varint) to determine polymorphic type - for calls to base class, the tag value will be 0.
        EndOfChild = 8,
        List = 10,// content is a list. next data is child field size type + count. if children are not primitive type, each child has a pre item header to determine if it's null or not.
        Dictionary = 11 // content is a dictionary, next data is field size type of key + count.  if value type is not primitive type, each child has a pre item header to determine if it's null or not.
    }
}
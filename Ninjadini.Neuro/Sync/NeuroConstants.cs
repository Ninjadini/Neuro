using System;

namespace Ninjadini.Neuro.Sync
{
    public static class NeuroConstants
    {
        // skipType: [varInt, length+body, groupStart/End, typedGroupStart]
        // isRepeated: [false, true]
        
        // int =     varInt, false, 1
        // string =     length, false, 1
        // class =     groupStart, false, 1  ... ...  groupEnd, false, 0
        
        public const int HeaderShift = 4;
        public const int CollectionTypeHeaderMask = 1 | 1 << 1 | 1 << 2;
        public const int CollectionHasNullMask = 1 << 3;
        public const uint HeaderMask = 0xF;//1 | 1 << 1 | 1 << 2 | 1 << 3; // binary: 0000 1111

        public static uint RepeatedMask => throw new Exception();

        public const uint VarInt = (uint)FieldSizeType.VarInt; 
        public const uint Fixed32 = (uint)FieldSizeType.Fixed32;
        public const uint Fixed64 = (uint)FieldSizeType.Fixed64;
        public const uint Fixed128 = (uint)FieldSizeType.Fixed128;
        public const uint Length = (uint)FieldSizeType.Length;
        public const uint Child = (uint)FieldSizeType.Child;
        public const uint ChildWithType = (uint)FieldSizeType.ChildWithTag; 
        public const uint EndOfChild = (uint)FieldSizeType.EndOfChild;
        public const uint List = (uint)FieldSizeType.List; 
        public const uint Dictionary = (uint)FieldSizeType.Dictionary; 

        public static readonly DateTime TwentyTwentyTime = new DateTime(2020, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
        public static readonly long TwentyTwentyTicks = TwentyTwentyTime.Ticks;

        public const string Reference_RefId_FieldName = "RefId";
    }
}
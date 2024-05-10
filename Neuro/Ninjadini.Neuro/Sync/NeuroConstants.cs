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
        public const uint SizeTypeMask = 1 | 1 << 1 | 1 << 2;
        public const uint HeaderMask = 1 | 1 << 1 | 1 << 2 | RepeatedMask; // binary: 0000 1111
        
        public const uint RepeatedMask = 1 << 3; // binary: 0000 1000. flag to determine if it is a list, if it is, next data is the count (not size)

        public const uint VarInt = (uint)FieldSizeType.VarInt; 
        public const uint Fixed32 = (uint)FieldSizeType.Fixed32;
        public const uint Fixed64 = (uint)FieldSizeType.Fixed64;
        public const uint Length = (uint)FieldSizeType.Length;
        public const uint Child = (uint)FieldSizeType.Child; 
        public const uint ChildWithType = (uint)FieldSizeType.ChildWithTag; 

        public static readonly DateTime TwentyTwentyTime = new DateTime(2020, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
        public static readonly long TwentyTwentyTicks = TwentyTwentyTime.Ticks;

        public const string Reference_RefId_FieldName = "RefId";
    }
}
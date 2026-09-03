using System;

namespace Ninjadini.Neuro.Sync
{
    static class NeuroDefaultSyncTypes
    {
        const int OffsetBits = 11;
        const int MaxOffsetMinutes = 840; // 14 hours, the largest utc offset DateTimeOffset allows
        const long OffsetMask = (1L << OffsetBits) - 1;

        public static void Register()
        {
            NeuroSyncTypes.Register(FieldSizeType.Child, delegate(INeuroSync neuro, ref object value)
            {
                throw new System.Exception($"Invalid sync target type 'object' via {value?.GetType().FullName ?? "null"}");
            });
            NeuroSyncTypes.Register(FieldSizeType.VarInt, delegate(INeuroSync neuro, ref bool value)
            {
                neuro.Sync(ref value);
            });
            NeuroSyncTypes.Register(FieldSizeType.VarInt, delegate(INeuroSync neuro, ref int value)
            {
                neuro.Sync(ref value);
            });
            NeuroSyncTypes.Register(FieldSizeType.VarInt, delegate(INeuroSync neuro, ref uint value)
            {
                neuro.Sync(ref value);
            });
            NeuroSyncTypes.Register(FieldSizeType.VarInt, delegate(INeuroSync neuro, ref long value)
            {
                neuro.Sync(ref value);
            });
            NeuroSyncTypes.Register(FieldSizeType.VarInt, delegate(INeuroSync neuro, ref ulong value)
            {
                neuro.Sync(ref value);
            });
            NeuroSyncTypes.Register(FieldSizeType.Fixed32, delegate(INeuroSync neuro, ref float value)
            {
                neuro.Sync(ref value);
            });
            NeuroSyncTypes.Register(FieldSizeType.Fixed64, delegate(INeuroSync neuro, ref double value)
            {
                neuro.Sync(ref value);
            });
            NeuroSyncTypes.Register(FieldSizeType.Length, delegate(INeuroSync neuro, ref string value)
            {
                neuro.Sync(ref value);
            });
            NeuroSyncTypes.RegisterEqualityCheck<DateTime>((a, b) => a.Ticks == b.Ticks && a.Kind == b.Kind);
            NeuroSyncTypes.Register(FieldSizeType.VarInt, delegate(INeuroSync neuro, ref DateTime value)
            {
                var valueLong = (long)value.Kind | ((value.Ticks - NeuroConstants.TwentyTwentyTicks) / 10000L) << 2;
                neuro.Sync(ref valueLong);
                if (neuro.IsReading)
                {
                    value = new DateTime((valueLong >> 2) * 10000L + NeuroConstants.TwentyTwentyTicks, (DateTimeKind)(valueLong & 3));
                }
            });
            NeuroSyncTypes.RegisterEqualityCheck<DateTimeOffset>((a, b) => a.UtcTicks == b.UtcTicks && a.Offset == b.Offset);
            NeuroSyncTypes.Register(FieldSizeType.VarInt, delegate(INeuroSync neuro, ref DateTimeOffset value)
            {
                var offsetMinutes = (int)(value.Offset.Ticks / TimeSpan.TicksPerMinute);
                var utcMs = (value.UtcTicks - NeuroConstants.TwentyTwentyTicks) / TimeSpan.TicksPerMillisecond;
                var valueLong = (utcMs << OffsetBits) | (long)(offsetMinutes + MaxOffsetMinutes);
                neuro.Sync(ref valueLong);
                if (neuro.IsReading)
                {
                    var readOffset = new TimeSpan(((int)(valueLong & OffsetMask) - MaxOffsetMinutes) * TimeSpan.TicksPerMinute);
                    var utcTicks = (valueLong >> OffsetBits) * TimeSpan.TicksPerMillisecond + NeuroConstants.TwentyTwentyTicks;
                    value = new DateTimeOffset(utcTicks + readOffset.Ticks, readOffset);
                }
            });
            NeuroSyncTypes.Register(FieldSizeType.VarInt, delegate(INeuroSync neuro, ref TimeSpan value)
            {
                var valueLong = value.Ticks / TimeSpan.TicksPerMillisecond;
                neuro.Sync(ref valueLong);
                if (neuro.IsReading)
                {
                    value = new TimeSpan(valueLong * TimeSpan.TicksPerMillisecond);
                }
            });
            NeuroSyncTypes.Register<Guid>(FieldSizeType.Child, delegate(INeuroSync neuro, ref Guid value)
            {
                Span<byte> buffer = stackalloc byte[16];
                ulong a, b;
                if (neuro.IsWriting)
                {
                    value.TryWriteBytes(buffer);
                    a = BitConverter.ToUInt64(buffer[..8]);
                    b = BitConverter.ToUInt64(buffer[8..16]);
                }
                else
                {
                    a = 0L;
                    b = 0L;
                }
                neuro.Sync(1, "a", ref a, default);
                neuro.Sync(2, "b", ref b, default);
                if (neuro.IsReading)
                {
                    BitConverter.TryWriteBytes(buffer, a);
                    BitConverter.TryWriteBytes(buffer[8..], b);
                    value = new Guid(buffer);
                }
            });
            
            if(NeuroSyncTypes.IsEmpty<System.Drawing.Color>())
                NeuroSyncTypes.Register(FieldSizeType.Fixed32, (INeuroSync neuro, ref System.Drawing.Color value) => {
                    // ARGB
                    var num = value.ToArgb();
                    neuro.Sync(ref num);
                    if (neuro.IsReading)
                    {
                        value = System.Drawing.Color.FromArgb(num);
                    }
                });
            
        }
    }
}
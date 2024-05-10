using System;

namespace Ninjadini.Neuro.Sync
{
    static class NeuroDefaultSyncTypes
    {
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
            NeuroSyncTypes.Register(FieldSizeType.VarInt, delegate(INeuroSync neuro, ref DateTime value)
            {
                var valueLong = (long)value.Kind | ((value.Ticks - NeuroConstants.TwentyTwentyTicks) / 10000L) << 2;
                neuro.Sync(ref valueLong);
                value = new DateTime((valueLong >> 2) * 10000L + NeuroConstants.TwentyTwentyTicks, (DateTimeKind)(valueLong & 3));
            });
            NeuroSyncTypes.Register(FieldSizeType.VarInt, delegate(INeuroSync neuro, ref TimeSpan value)
            {
                var valueLong = value.Ticks / TimeSpan.TicksPerMillisecond;
                neuro.Sync(ref valueLong);
                value = new TimeSpan(valueLong * TimeSpan.TicksPerMillisecond);
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
        }
    }
}
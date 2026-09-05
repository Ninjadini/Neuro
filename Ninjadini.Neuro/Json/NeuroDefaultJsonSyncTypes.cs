using System;
using System.Globalization;
using Ninjadini.Neuro.Utils;
using Ninjadini.Neuro.Sync;

namespace Ninjadini.Neuro
{
#if UNITY_6000_5_OR_NEWER
    [Unity.Scripting.LifecycleManagement.NoAutoStaticsCleanup]
#endif
    static class NeuroDefaultJsonSyncTypes
    {
        private static bool registered;
        
        public static void Register()
        {
            if (registered)
            {
                return;
            }
            NeuroJsonSyncTypes.Register<Guid>(FieldSizeType.Length, delegate(INeuroSync neuro, ref Guid value)
            {
                if (neuro is NeuroJsonWriter jsonWriter)
                {
                    jsonWriter.CurrentStringBuilder.Append("\"").Append(value).Append("\"");
                }
                else if (neuro is NeuroJsonReader jsonReader)
                {
                    value = Guid.Parse(jsonReader.CurrentValue);
                }
                else
                {
                    throw new ArgumentException($"Not expecting {neuro} in for JSON sync of Guid");
                }
            });
            
            NeuroJsonSyncTypes.Register<DateTimeOffset>(FieldSizeType.Length, delegate(INeuroSync neuro, ref DateTimeOffset value)
            {
                // yyyy-MM-ddTHH:mm:ss:fff+HH:mm - the DateTime shape above with the utc offset appended.
                if (neuro is NeuroJsonWriter jsonWriter)
                {
                    var offset = value.Offset;
                    jsonWriter.CurrentStringBuilder
                        .Append("\"")
                        .AppendNumWithZeroPadding(value.Year, 4)
                        .Append("-")
                        .AppendNumWithZeroPadding(value.Month, 2)
                        .Append("-")
                        .AppendNumWithZeroPadding(value.Day, 2)
                        .Append("T")
                        .AppendNumWithZeroPadding(value.Hour, 2)
                        .Append(":")
                        .AppendNumWithZeroPadding(value.Minute, 2)
                        .Append(":")
                        .AppendNumWithZeroPadding(value.Second, 2)
                        .Append(":")
                        .AppendNumWithZeroPadding(value.Millisecond, 3)
                        .Append(offset.Ticks < 0 ? "-" : "+")
                        .AppendNumWithZeroPadding(Math.Abs(offset.Hours), 2)
                        .Append(":")
                        .AppendNumWithZeroPadding(Math.Abs(offset.Minutes), 2)
                        .Append("\"");
                }
                else if (neuro is NeuroJsonReader jsonReader)
                {
                    var currentValue = jsonReader.CurrentValue;
                    if (currentValue.Length != 29)
                    {
                        // not our own format, so let the framework have a go at it.
                        value = DateTimeOffset.Parse(currentValue, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
                    }
                    else
                    {
                        var offsetTicks = int.Parse(currentValue[24..26], NumberStyles.Integer, CultureInfo.InvariantCulture) * TimeSpan.TicksPerHour
                                          + int.Parse(currentValue[27..29], NumberStyles.Integer, CultureInfo.InvariantCulture) * TimeSpan.TicksPerMinute;
                        if (currentValue[23] == '-')
                        {
                            offsetTicks = -offsetTicks;
                        }
                        value = new DateTimeOffset(
                            int.Parse(currentValue[..4], NumberStyles.Integer, CultureInfo.InvariantCulture),
                            int.Parse(currentValue[5..7], NumberStyles.Integer, CultureInfo.InvariantCulture),
                            int.Parse(currentValue[8..10], NumberStyles.Integer, CultureInfo.InvariantCulture),
                            int.Parse(currentValue[11..13], NumberStyles.Integer, CultureInfo.InvariantCulture),
                            int.Parse(currentValue[14..16], NumberStyles.Integer, CultureInfo.InvariantCulture),
                            int.Parse(currentValue[17..19], NumberStyles.Integer, CultureInfo.InvariantCulture),
                            int.Parse(currentValue[20..23], NumberStyles.Integer, CultureInfo.InvariantCulture),
                            new TimeSpan(offsetTicks)
                        );
                    }
                }
                else
                {
                    throw new ArgumentException($"Not expecting {neuro} in for JSON sync of DateTimeOffset");
                }
            });
            
            NeuroJsonSyncTypes.Register<DateTime>(FieldSizeType.Length, delegate(INeuroSync neuro, ref DateTime value)
            {
                if (neuro is NeuroJsonWriter jsonWriter)
                {
                    jsonWriter.CurrentStringBuilder
                        .Append("\"")
                        .AppendNumWithZeroPadding(value.Year, 4)
                        .Append("-")
                        .AppendNumWithZeroPadding(value.Month, 2)
                        .Append("-")
                        .AppendNumWithZeroPadding(value.Day, 2)
                        .Append(value.Kind switch
                        {
                            DateTimeKind.Local => "L",
                            DateTimeKind.Utc => "U",
                            _ => "T"
                        })
                        .AppendNumWithZeroPadding(value.Hour, 2)
                        .Append(":")
                        .AppendNumWithZeroPadding(value.Minute, 2)
                        .Append(":")
                        .AppendNumWithZeroPadding(value.Second, 2)
                        .Append(":")
                        .AppendNumWithZeroPadding(value.Millisecond, 3)
                        .Append("\"");
                }
                else if(neuro is NeuroJsonReader jsonReader)
                {
                    var currentValue = jsonReader.CurrentValue;
                    if (currentValue.Length != 23)
                    {
                        // not our own format, so let the framework have a go at it.
                        value = DateTime.Parse(currentValue, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
                    }
                    else
                    {
                        var kindStr = currentValue[10];
                        value = new DateTime(
                            int.Parse(currentValue[..4]),
                            int.Parse(currentValue[5..7]),
                            int.Parse(currentValue[8..10]),
                            int.Parse(currentValue[11..13]),
                            int.Parse(currentValue[14..16]),
                            int.Parse(currentValue[17..19]),
                            int.Parse(currentValue[20..23]),
                            kindStr switch
                            {
                                'L' => DateTimeKind.Local,
                                'U' => DateTimeKind.Utc,
                                _ => DateTimeKind.Unspecified
                            }
                        );
                    }
                }
                else
                {
                    throw new ArgumentException($"Not expecting {neuro} in for JSON sync of DateTime");
                }
            });
            
            
            registered = true;
        }
    }
}
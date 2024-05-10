using System;
using Ninjadini.Neuro.Utils;
using Ninjadini.Neuro.Sync;

namespace Ninjadini.Neuro
{
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
                        DateTime.Parse(currentValue);
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
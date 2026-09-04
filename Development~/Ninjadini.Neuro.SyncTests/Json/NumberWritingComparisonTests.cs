using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using Newtonsoft.Json;
using Ninjadini.Neuro.Sync;
using Ninjadini.Neuro.Utils;
using NUnit.Framework;

namespace Ninjadini.Neuro.SyncTests
{
    /// <summary>
    /// Is the hand rolled number writing in StringBuilderExtensions actually worth keeping?
    /// This compares it against the two things it could be replaced with, on allocations, time and output size.
    ///
    /// The answer depends on the runtime, and desktop .NET is NOT representative of Unity:
    ///
    ///   desktop .NET Core 3.0+  StringBuilder.Append(float) formats straight into the builder's buffer and
    ///                           allocates nothing. Here AppendNum is a speed win only (about 2x on game data).
    ///
    ///   Unity, every version    StringBuilder.Append(float) allocates a string per call. Verified by
    ///   checked so far          disassembling the mscorlib Unity ships (ikdasm) for 2020.3, 2022.3, 6000.0,
    ///                           6000.5 and 6000.7a, on BOTH the unityaot profile (used by IL2CPP) and the
    ///                           unityjit profile (used by Mono). The scripting backend makes no difference -
    ///                           IL2CPP only changes how the IL is compiled, not which class library runs.
    ///                           2020.3: Append(float) -> Single::ToString(CurrentCulture) -> Append(string).
    ///                           6000.x: Append(float) -> AppendSpanFormattable&lt;float&gt;, which despite the
    ///                           name lifted from corefx still does IFormattable::ToString(null, CurrentCulture)
    ///                           -> Append(string). No span formatting anywhere in it.
    ///                           So on Unity the "sb.Append(ToString)" row below IS what Append(float) costs,
    ///                           and AppendNum removes that allocation outright.
    ///
    /// ReportRuntime prints what the runtime you are on actually does, so run this inside Unity rather than
    /// trusting the desktop numbers. Re-check after a Unity upgrade - the day Unity moves to a CoreCLR class
    /// library this stops being an allocation win and becomes a speed one.
    ///
    /// Caveat on the timings: every row goes through the same delegate indirection, so they are comparable to
    /// each other but each carries a few ns of overhead that a direct call would not.
    /// </summary>
    public class NumberWritingComparisonTests
    {
        const int Iterations = 200000;
        const int WarmUp = 5000;

        readonly StringBuilder stringBuilder = new StringBuilder(64);

        [Test]
        public void ReportRuntime()
        {
            // the answer is different per runtime, so always say which one produced these numbers.
            TestContext.WriteLine($"runtime : {RuntimeInformation.FrameworkDescription} / {RuntimeInformation.OSArchitecture}");
            var stringBuilderAllocates = MeasureAppendAllocation();
            TestContext.WriteLine($"StringBuilder.Append(float) allocates {stringBuilderAllocates} bytes per call on this runtime.");
            TestContext.WriteLine(stringBuilderAllocates == 0
                ? "  -> the framework formats straight into the builder, so AppendNum is a speed play here, not an allocation one."
                : "  -> the framework allocates a string per number, so AppendNum is saving that on top of any speed difference.");
        }

        long MeasureAppendAllocation()
        {
            var builder = new StringBuilder(64);
            for (var i = 0; i < WarmUp; i++)
            {
                builder.Length = 0;
                builder.Append(1.2345f);
            }
            var before = GC.GetAllocatedBytesForCurrentThread();
            for (var i = 0; i < Iterations; i++)
            {
                builder.Length = 0;
                builder.Append(1.2345f);
            }
            return (GC.GetAllocatedBytesForCurrentThread() - before) / Iterations;
        }

        struct Result
        {
            public string Name;
            public long BytesPerValue;
            public double NanosPerValue;
            public double CharsPerValue;
        }

        /// The realistic alternative to the hand rolled writer: let the runtime format, but into a stack
        /// buffer so nothing is allocated. This is what StringBuilderExtensions itself falls back to.
        static StringBuilder AppendViaTryFormat(StringBuilder stringBuilder, float num)
        {
            Span<char> buffer = stackalloc char[32];
            if (num.TryFormat(buffer, out var written, default, CultureInfo.InvariantCulture))
            {
                return stringBuilder.Append(buffer.Slice(0, written));
            }
            return stringBuilder.Append(num.ToString(CultureInfo.InvariantCulture));
        }

        static StringBuilder AppendViaTryFormat(StringBuilder stringBuilder, double num)
        {
            Span<char> buffer = stackalloc char[32];
            if (num.TryFormat(buffer, out var written, default, CultureInfo.InvariantCulture))
            {
                return stringBuilder.Append(buffer.Slice(0, written));
            }
            return stringBuilder.Append(num.ToString(CultureInfo.InvariantCulture));
        }

        Result Measure(string name, float[] values, Action<StringBuilder, float> write)
        {
            for (var i = 0; i < WarmUp; i++)
            {
                stringBuilder.Length = 0;
                write(stringBuilder, values[i % values.Length]);
            }
            long totalChars = 0;
            var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
            var stopwatch = Stopwatch.StartNew();
            for (var i = 0; i < Iterations; i++)
            {
                stringBuilder.Length = 0;
                write(stringBuilder, values[i % values.Length]);
                totalChars += stringBuilder.Length;
            }
            stopwatch.Stop();
            var allocated = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;
            return new Result
            {
                Name = name,
                BytesPerValue = allocated / Iterations,
                NanosPerValue = stopwatch.Elapsed.TotalMilliseconds * 1000000d / Iterations,
                CharsPerValue = totalChars / (double)Iterations
            };
        }

        void Report(string title, float[] values)
        {
            var results = new List<Result>
            {
                Measure("neuro AppendNum", values, (sb, f) => sb.AppendNum(f)),
                Measure("TryFormat+Append", values, (sb, f) => AppendViaTryFormat(sb, f)),
                Measure("sb.Append(float)", values, (sb, f) => sb.Append(f)),
                Measure("sb.Append(ToString)", values, (sb, f) => sb.Append(f.ToString(CultureInfo.InvariantCulture)))
            };
            TestContext.WriteLine("");
            TestContext.WriteLine(title);
            TestContext.WriteLine($"  {"approach",-22}{"bytes/value",14}{"ns/value",12}{"chars/value",14}");
            foreach (var r in results)
            {
                TestContext.WriteLine($"  {r.Name,-22}{r.BytesPerValue,14}{r.NanosPerValue,12:0.0}{r.CharsPerValue,14:0.0}");
            }
            // whatever the runtime, the hand rolled path must never be the one that allocates more.
            Assert.LessOrEqual(results[0].BytesPerValue, results[3].BytesPerValue,
                $"{title}: AppendNum allocated more per value than plain ToString");
        }

        static float[] Build(int count, Func<Random, float> generator)
        {
            var random = new Random(12345);
            var values = new float[count];
            for (var i = 0; i < count; i++)
            {
                values[i] = generator(random);
            }
            return values;
        }

        static readonly float[] TypicalGameValues =
        {
            0f, 1f, -1f, 0.5f, 2.5f, 0.1f, 100f, 9.81f, 1.5f, 60f, 0.25f, 1234.5f, 0.001f, 33.333f, -0.75f, 3.14159f
        };

        [Test]
        public void CompareTypicalGameValues() => Report("typical game values (0-4 decimal places)", TypicalGameValues);

        [Test]
        public void CompareZeroToOne() => Report("floats 0..1 (full precision)", Build(1000, r => (float)r.NextDouble()));

        [Test]
        public void CompareZeroToThousand() => Report("floats 0..1000 (full precision)", Build(1000, r => (float)(r.NextDouble() * 1000)));

        [Test]
        public void CompareQuantised() => Report("floats 0..1 rounded to 3 places", Build(1000, r => (float)Math.Round(r.NextDouble(), 3)));

        [Test]
        public void CompareRandomBitPatterns() => Report("random float bit patterns", Build(1000, r =>
        {
            var f = BitConverter.Int32BitsToSingle((int)(uint)r.NextInt64(0, uint.MaxValue));
            return float.IsFinite(f) ? f : 1f;
        }));

        [Test]
        public void CompareDoubles()
        {
            var random = new Random(12345);
            var values = new double[1000];
            for (var i = 0; i < values.Length; i++)
            {
                values[i] = random.NextDouble() * Math.Pow(10, random.Next(-6, 6));
            }
            var builder = new StringBuilder(64);
            Result MeasureDouble(string name, Action<StringBuilder, double> write)
            {
                for (var i = 0; i < WarmUp; i++)
                {
                    builder.Length = 0;
                    write(builder, values[i % values.Length]);
                }
                long totalChars = 0;
                var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
                var stopwatch = Stopwatch.StartNew();
                for (var i = 0; i < Iterations; i++)
                {
                    builder.Length = 0;
                    write(builder, values[i % values.Length]);
                    totalChars += builder.Length;
                }
                stopwatch.Stop();
                return new Result
                {
                    Name = name,
                    BytesPerValue = (GC.GetAllocatedBytesForCurrentThread() - allocatedBefore) / Iterations,
                    NanosPerValue = stopwatch.Elapsed.TotalMilliseconds * 1000000d / Iterations,
                    CharsPerValue = totalChars / (double)Iterations
                };
            }
            var results = new List<Result>
            {
                MeasureDouble("neuro AppendNum", (sb, d) => sb.AppendNum(d)),
                MeasureDouble("TryFormat+Append", (sb, d) => AppendViaTryFormat(sb, d)),
                MeasureDouble("sb.Append(double)", (sb, d) => sb.Append(d)),
                MeasureDouble("sb.Append(ToString)", (sb, d) => sb.Append(d.ToString(CultureInfo.InvariantCulture)))
            };
            TestContext.WriteLine("");
            TestContext.WriteLine("doubles 1e-6..1e6 (full precision)");
            TestContext.WriteLine($"  {"approach",-22}{"bytes/value",14}{"ns/value",12}{"chars/value",14}");
            foreach (var r in results)
            {
                TestContext.WriteLine($"  {r.Name,-22}{r.BytesPerValue,14}{r.NanosPerValue,12:0.0}{r.CharsPerValue,14:0.0}");
            }
            Assert.LessOrEqual(results[0].BytesPerValue, results[3].BytesPerValue);
        }

// ---------------------------------------------------------------------------------------------------- end to end

        public class FloatBag
        {
            public List<float> Values = new List<float>();
        }

        static bool _registered;

        static void RegisterBag()
        {
            if (_registered)
            {
                return;
            }
            _registered = true;
            NeuroSyncTypes.Register(delegate(INeuroSync neuro, ref FloatBag value)
            {
                value ??= new FloatBag();
                neuro.Sync(1, nameof(value.Values), value.Values);
            });
        }

        [Test]
        public void CompareWholeDocumentAgainstNewtonsoft()
        {
            RegisterBag();
            var random = new Random(12345);
            var bag = new FloatBag();
            for (var i = 0; i < 5000; i++)
            {
                bag.Values.Add((float)Math.Round(random.NextDouble() * 1000, 3));
            }
            const int documents = 200;

            for (var i = 0; i < 5; i++)
            {
                NeuroJsonWriter.Shared.Write(bag);
                JsonConvert.SerializeObject(bag, Formatting.Indented);
            }

            var before = GC.GetAllocatedBytesForCurrentThread();
            var stopwatch = Stopwatch.StartNew();
            var neuroLength = 0;
            for (var i = 0; i < documents; i++)
            {
                neuroLength = NeuroJsonWriter.Shared.Write(bag).Length;
            }
            stopwatch.Stop();
            var neuroBytes = GC.GetAllocatedBytesForCurrentThread() - before;
            var neuroMs = stopwatch.Elapsed.TotalMilliseconds;

            before = GC.GetAllocatedBytesForCurrentThread();
            stopwatch.Restart();
            var otherLength = 0;
            for (var i = 0; i < documents; i++)
            {
                otherLength = JsonConvert.SerializeObject(bag, Formatting.Indented).Length;
            }
            stopwatch.Stop();
            var otherBytes = GC.GetAllocatedBytesForCurrentThread() - before;
            var otherMs = stopwatch.Elapsed.TotalMilliseconds;

            TestContext.WriteLine("");
            TestContext.WriteLine($"whole document, {bag.Values.Count} floats x {documents} writes");
            TestContext.WriteLine($"  {"writer",-22}{"KB alloc/write",18}{"ms/write",12}{"output chars",14}");
            TestContext.WriteLine($"  {"neuro json",-22}{neuroBytes / 1024d / documents,18:0.0}{neuroMs / documents,12:0.00}{neuroLength,14}");
            TestContext.WriteLine($"  {"newtonsoft",-22}{otherBytes / 1024d / documents,18:0.0}{otherMs / documents,12:0.00}{otherLength,14}");
        }
    }
}

using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;

namespace Ninjadini.Neuro.Utils
{
    public static class StringBuilderExtensions
    {
        static string NegativeSign => CultureInfo.InvariantCulture.NumberFormat.NegativeSign;
        static string DecimalSeparator => CultureInfo.InvariantCulture.NumberFormat.NumberDecimalSeparator;
        static string GroupSeparator => CultureInfo.InvariantCulture.NumberFormat.NumberGroupSeparator;
        
        public static StringBuilder AppendNum(this StringBuilder stringBuilder, int num, bool group = false)
        {
            if (num < 0)
            {
                stringBuilder.Append(NegativeSign);
                num = -num;
            }
            return stringBuilder.AppendNum((uint)num, group);
        }
        
        public static StringBuilder AppendNumWithZeroPadding(this StringBuilder stringBuilder, int num, int padding)
        {
            if (num < 0)
            {
                stringBuilder.Append("-");
                return AppendNumWithZeroPadding(stringBuilder, -num, padding);
            }
            int count;
            if (num > 0)
            {
                count = 0;
                var tempNum = num;
                while (tempNum > 0)
                {
                    tempNum /= 10;
                    count++;
                }
            }
            else
            {
                count = 1;
            }
            while (count < padding)
            {
                count++;
                stringBuilder.Append("0");
            }
            return stringBuilder.AppendNum((uint)num, false);
        }
        
        public static StringBuilder AppendNum(this StringBuilder stringBuilder, uint num, bool group = false)
        {
            if (num == 0)
            {
                return stringBuilder.Append('0');
            }
            var startIndex = stringBuilder.Length;
            var count = 0;
            while (num > 0)
            {
                stringBuilder.Append((char)(num % 10 + '0')); 
                num /= 10;
                count++;
                if (group && count % 3 == 0 && num > 0)
                {
                    stringBuilder.Append(GroupSeparator);
                }
            }
            ReverseLast(stringBuilder, startIndex);
            return stringBuilder;
        }
        
        public static StringBuilder AppendNum(this StringBuilder stringBuilder, long num, bool group = false)
        {
            if (num < 0)
            {
                stringBuilder.Append(NegativeSign);
                num = -num;
            }
            return stringBuilder.AppendNum((ulong)num, group);
        }
        
        public static StringBuilder AppendNum(this StringBuilder stringBuilder, ulong num, bool group = false)
        {
            if (num == 0)
            {
                return stringBuilder.Append('0');
            }
            var startIndex = stringBuilder.Length;
            var count = 0;
            while (num > 0)
            {
                stringBuilder.Append((char)(num % 10 + '0')); 
                num /= 10;
                count++;
                if (group && count % 3 == 0 && num > 0)
                {
                    stringBuilder.Append(GroupSeparator);
                }
            }
            ReverseLast(stringBuilder, startIndex);
            return stringBuilder;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void ReverseLast(StringBuilder stringBuilder, int startIndex)
        {
            var endIndex = stringBuilder.Length - 1;
            var l = (stringBuilder.Length - startIndex) / 2;
            for (var i = 0; i < l; i++)
            {
                (stringBuilder[endIndex - i], stringBuilder[startIndex + i]) = (stringBuilder[startIndex + i], stringBuilder[endIndex - i]);
            }
        }

        /// Pass as maxDecimalPlaces to write the value exactly - as many decimal places as it takes, no more.
        public const int ExactDecimalPlaces = -1;

        // The allocation free path writes a fixed number of decimal places, so it can only be used when that is
        // enough to reproduce the value. It tries the smallest step first so everyday values stay short, and
        // widens only when they need it. Anything still out of reach - too big, too small, too precise, NaN or
        // an infinity - falls back to the framework's own formatting, which allocates but is always exact.
        static readonly int[] FloatDecimalSteps = { 5, 7, 9, 12 };
        static readonly int[] DoubleDecimalSteps = { 6, 10, 15 };
        static readonly ulong[] Pow10 =
        {
            1, 10, 100, 1000, 10000, 100000, 1000000, 10000000, 100000000, 1000000000,
            10000000000, 100000000000, 1000000000000, 10000000000000, 100000000000000, 1000000000000000
        };

        const float FloatFastPathLimit = 1E8f;
        const double DoubleFastPathLimit = 1E18d;
        const double MaxSignificantDigits = 1E15d;

        public static StringBuilder AppendNum(this StringBuilder stringBuilder, float num, int maxDecimalPlaces = ExactDecimalPlaces, int minDecimalPlaces = 0, bool group = false)
        {
            if (!(num > -FloatFastPathLimit && num < FloatFastPathLimit))
            {
                // too big to write out digit by digit. Written this way so NaN and the infinities land here too.
                return AppendExact(stringBuilder, num);
            }
            var negative = num < 0;
            double abs = negative ? -num : num;
            int decimalPlaces;
            ulong wholeNum, scaledDecimals;
            if (maxDecimalPlaces == ExactDecimalPlaces && minDecimalPlaces == 0 && !group)
            {
                if (!TrySplitExactly(abs, FloatDecimalSteps, true, out decimalPlaces, out wholeNum, out scaledDecimals))
                {
                    return AppendExact(stringBuilder, num);
                }
                maxDecimalPlaces = decimalPlaces;
            }
            else
            {
                // a particular presentation was asked for, so stick to the narrowest step.
                decimalPlaces = FloatDecimalSteps[0];
                if (maxDecimalPlaces < 0)
                {
                    maxDecimalPlaces = decimalPlaces;
                }
                Split(abs, decimalPlaces, maxDecimalPlaces, out wholeNum, out scaledDecimals);
            }
            if (negative)
            {
                stringBuilder.Append(NegativeSign);
            }
            stringBuilder.AppendNum(wholeNum, group);
            AppendDecimals(stringBuilder, scaledDecimals, decimalPlaces, maxDecimalPlaces, minDecimalPlaces);
            return stringBuilder;
        }

        public static StringBuilder AppendNum(this StringBuilder stringBuilder, double num, int maxDecimalPlaces = ExactDecimalPlaces, int minDecimalPlaces = 0, bool group = false)
        {
            if (!(num > -DoubleFastPathLimit && num < DoubleFastPathLimit))
            {
                return AppendExact(stringBuilder, num);
            }
            var negative = num < 0;
            var abs = negative ? -num : num;
            int decimalPlaces;
            ulong wholeNum, scaledDecimals;
            if (maxDecimalPlaces == ExactDecimalPlaces && minDecimalPlaces == 0 && !group)
            {
                if (!TrySplitExactly(abs, DoubleDecimalSteps, false, out decimalPlaces, out wholeNum, out scaledDecimals))
                {
                    return AppendExact(stringBuilder, num);
                }
                maxDecimalPlaces = decimalPlaces;
            }
            else
            {
                decimalPlaces = DoubleDecimalSteps[0];
                if (maxDecimalPlaces < 0)
                {
                    maxDecimalPlaces = decimalPlaces;
                }
                Split(abs, decimalPlaces, maxDecimalPlaces, out wholeNum, out scaledDecimals);
            }
            if (negative)
            {
                stringBuilder.Append(NegativeSign);
            }
            stringBuilder.AppendNum(wholeNum, group);
            AppendDecimals(stringBuilder, scaledDecimals, decimalPlaces, maxDecimalPlaces, minDecimalPlaces);
            return stringBuilder;
        }

        static bool TrySplitExactly(double num, int[] steps, bool asFloat, out int decimalPlaces, out ulong wholeNum, out ulong scaledDecimals)
        {
            decimalPlaces = 0;
            scaledDecimals = 0;
            wholeNum = (ulong)num;
            var fraction = num - wholeNum;
            if (fraction == 0)
            {
                // a whole number has no decimals to lose, however big it is.
                return true;
            }
            for (var i = 0; i < steps.Length; i++)
            {
                var places = steps[i];
                double scale = Pow10[places];
                if (num * scale > MaxSignificantDigits)
                {
                    // past here the check itself would lose digits, so stop trusting it.
                    break;
                }
                var scaled = Math.Round(fraction * scale);
                var rebuilt = wholeNum + scaled / scale;
                if (asFloat ? (float)rebuilt == (float)num : rebuilt == num)
                {
                    decimalPlaces = places;
                    scaledDecimals = (ulong)scaled;
                    return true;
                }
            }
            return false;
        }

        /// "R" is required, not cosmetic. Unity's number formatter is the pre .NET Core 3.0 one, where the
        /// default format means 7 significant digits for float with no round trip check - so ToString() and
        /// TryFormat(..., default, ...) silently lose precision there. Its "R" does the old format-at-7,
        /// reparse, retry-at-9 dance, which does round trip. On .NET Core 3.0+ "R" is the shortest round
        /// trippable form, so it is correct on both. Desktop tests cannot catch this, the default is already
        /// round trippable there.
        const string RoundTripFormat = "R";

        /// The way out for anything the digit writer above can not reproduce. Formats through the runtime, but
        /// into a stack buffer rather than a string - Single/Double.TryFormat writes straight into the span on
        /// both desktop .NET and Unity, so this stays allocation free. StringBuilder.Append(float) would not:
        /// on every Unity version checked it is value.ToString(CurrentCulture), which both allocates and would
        /// write "1,5" on a comma decimal machine.
        static StringBuilder AppendExact(StringBuilder stringBuilder, float num)
        {
            Span<char> buffer = stackalloc char[32];
            if (num.TryFormat(buffer, out var written, RoundTripFormat, CultureInfo.InvariantCulture))
            {
                return stringBuilder.Append(buffer.Slice(0, written));
            }
            return stringBuilder.Append(num.ToString(RoundTripFormat, CultureInfo.InvariantCulture));
        }

        static StringBuilder AppendExact(StringBuilder stringBuilder, double num)
        {
            Span<char> buffer = stackalloc char[32];
            if (num.TryFormat(buffer, out var written, RoundTripFormat, CultureInfo.InvariantCulture))
            {
                return stringBuilder.Append(buffer.Slice(0, written));
            }
            return stringBuilder.Append(num.ToString(RoundTripFormat, CultureInfo.InvariantCulture));
        }

        static void Split(double num, int decimalPlaces, int maxDecimalPlaces, out ulong wholeNum, out ulong scaledDecimals)
        {
            wholeNum = (ulong)num;
            var scale = Pow10[decimalPlaces];
            scaledDecimals = maxDecimalPlaces > 0 ? (ulong)Math.Round((num - wholeNum) * scale) : 0UL;
            if (scaledDecimals >= scale)
            {
                // the decimals rounded up to a whole one, carry it rather than writing a bogus ".1"
                wholeNum++;
                scaledDecimals = 0;
            }
        }

        static void AppendDecimals(StringBuilder stringBuilder, ulong scaledDecimals, int decimalPlaces, int maxDecimalPlaces, int minDecimalPlaces)
        {
            if (scaledDecimals > 0)
            {
                stringBuilder.Append(DecimalSeparator);
                Span<char> list = stackalloc char[20];
                var count = 0;
                var start = -1;
                while (scaledDecimals > 0)
                {
                    var d = (int)(scaledDecimals % 10);
                    if (start == -1 && d != 0)
                    {
                        start = count;
                    }
                    list[count++] = (char)(d + '0');
                    scaledDecimals /= 10;
                }
                for (var cc = count; cc < decimalPlaces; cc++)
                {
                    stringBuilder.Append('0');
                }
                for (var i = count - 1; i >= start && maxDecimalPlaces > 0; i--)
                {
                    maxDecimalPlaces--;
                    stringBuilder.Append(list[i]);
                }
                count -= start;
                while (count < minDecimalPlaces)
                {
                    count++;
                    stringBuilder.Append('0');
                }
            }
            else if (minDecimalPlaces > 0)
            {
                stringBuilder.Append(DecimalSeparator);
                while (minDecimalPlaces > 0)
                {
                    minDecimalPlaces--;
                    stringBuilder.Append('0');
                }
            }
        }
    }
}
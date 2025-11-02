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

        public static StringBuilder AppendNum(this StringBuilder stringBuilder, float num, int maxDecimalPlaces = 8, int minDecimalPlaces = 0, bool group = false)
        {
            if (num < 0)
            {
                stringBuilder.Append(NegativeSign);
                num = -num;
            }
            if (num > 999999899999999999999f)
            {
                // TODO need to start adding e##
                return stringBuilder.Append(num);
            }
            var wholeNum = (uint)num;
            stringBuilder.AppendNum(wholeNum, group);
            AppendDecimal(stringBuilder, num - wholeNum, maxDecimalPlaces, minDecimalPlaces);
            return stringBuilder;
        }

        static void AppendDecimal(StringBuilder stringBuilder, float decimalValue, int maxDecimalPlaces, int minDecimalPlaces)
        {
            if (maxDecimalPlaces > 0 && decimalValue >= 0.00001f)
            {
                stringBuilder.Append(DecimalSeparator);
                var remainingInt = (uint)Math.Round(decimalValue * 100000f);
                
                Span<char> list = stackalloc char[6];
                var count = 0;
                var start = -1;
                while (remainingInt > 0)
                {
                    var d = remainingInt % 10;
                    if (start == -1 && d != 0)
                    {
                        start = count;
                    }
                    list[count++] = (char)(d + '0'); 
                    remainingInt /= 10;
                }
                if (count > 0)
                {
                    if (count < 5)
                    {
                        var cc = count;
                        while (cc < 5)
                        {
                            stringBuilder.Append('0');
                            cc++;
                        }
                    }
                    for (var i = count - 1; i >= start && maxDecimalPlaces > 0; i--)
                    {
                        maxDecimalPlaces--;
                        stringBuilder.Append(list[i]);
                    }
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

        public static StringBuilder AppendNum(this StringBuilder stringBuilder, double num, int maxDecimalPlaces = 8, int minDecimalPlaces = 0, bool group = false)
        {
            if (num < 0)
            {
                stringBuilder.Append(NegativeSign);
                num = -num;
            }
            if (num > 999999899999999999999f)
            {
                // TODO need to start adding e##
                return stringBuilder.Append(num);
            }
            var wholeNum = (ulong)num;
            stringBuilder.AppendNum(wholeNum, group);
            AppendDecimal(stringBuilder, num - wholeNum, maxDecimalPlaces, minDecimalPlaces);
            return stringBuilder;
        }

        static void AppendDecimal(StringBuilder stringBuilder, double decimalValue, int maxDecimalPlaces, int minDecimalPlaces)
        {
            if (maxDecimalPlaces > 0 && decimalValue >= 0.000001)
            {
                stringBuilder.Append(DecimalSeparator);
                var remainingInt = (uint)Math.Round(decimalValue * 1000000);
                
                Span<char> list = stackalloc char[6];
                var count = 0;
                var start = -1;
                while (remainingInt > 0)
                {
                    var d = remainingInt % 10;
                    if (start == -1 && d != 0)
                    {
                        start = count;
                    }
                    list[count++] = (char)(d + '0'); 
                    remainingInt /= 10;
                }
                if (count >= 0)
                {
                    if (count < 6)
                    {
                        var cc = count;
                        while (cc < 6)
                        {
                            stringBuilder.Append('0');
                            cc++;
                        }
                    }
                    for (var i = count - 1; i >= start && maxDecimalPlaces > 0; i--)
                    {
                        maxDecimalPlaces--;
                        stringBuilder.Append(list[i]);
                    }
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
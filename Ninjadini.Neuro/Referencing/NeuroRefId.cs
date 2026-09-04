using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using Ninjadini.Neuro.Utils;

namespace Ninjadini.Neuro
{
    /// Converts RefIds between the uint they are in memory (and in the binary format) and the text form used
    /// in data file names and json. The text form is base36 - `[0-9a-z]`, the largest radix that still has no
    /// upper/lower case pairs, which matters because data file names have to survive a case insensitive file
    /// system. Base62 would fold `aB` and `Ab` onto the same file.
    ///
    /// Every id has exactly one spelling and every spelling is one id - there is no decimal form and no marker
    /// character. Data written before this (where ids were plain decimal) reads as the wrong number and has to
    /// be migrated once, via Tools > Neuro > Migrate RefIds, which is what TryParseLegacy is here for.
    public static class NeuroRefId
    {
        public const int Radix = 36;
        
        /// The range NeuroEditorDataProvider.FindNextId() picks a new RefId from - 36^3 (`1000`) to 36^4 - 1
        /// (`zzzz`). Every value in it is exactly 4 base36 chars, and under the 2^21 - 1 that fits in a 3 byte
        /// varint, so a generated id costs 4 chars in a file name and 3 bytes in the binary format.
        ///
        /// This is only a floor and ceiling on what gets *generated*. It says nothing about what the encoding
        /// can represent or what an id is allowed to be - an id set by hand is any uint from 1 up, and is
        /// written and read the same way as any other.
        public const uint GeneratedMinValue = 46656;
        public const uint GeneratedMaxValue = 1679615;
        
        /// uint.MaxValue is "1z141z3" in base36.
        const int MaxTextLength = 7;
        
        const string DigitChars = "0123456789abcdefghijklmnopqrstuvwxyz";

        /// The text form of the id, as it is written to file names and json.
        public static string ToString(uint id)
        {
            Span<char> buffer = stackalloc char[MaxTextLength];
            var written = Format(buffer, id);
            return new string(buffer.Slice(buffer.Length - written, written));
        }

        /// Not an extension method on purpose - `stringBuilder.Append(uint)` already exists as an instance
        /// method and would silently win over an extension of the same name.
        public static StringBuilder Append(StringBuilder stringBuilder, uint id)
        {
            Span<char> buffer = stackalloc char[MaxTextLength];
            var written = Format(buffer, id);
            return stringBuilder.Append(buffer.Slice(buffer.Length - written, written));
        }

        /// Writes the base36 form into the end of `buffer` and returns how many chars it wrote.
        static int Format(Span<char> buffer, uint id)
        {
            var index = buffer.Length;
            do
            {
                buffer[--index] = DigitChars[(int)(id % Radix)];
                id /= Radix;
            }
            while (id > 0);
            return buffer.Length - index;
        }

        public static uint Parse(ReadOnlySpan<char> chars)
        {
            if (!TryParse(chars, out var id))
            {
                throw new FormatException($"`{chars.ToString()}` is not a valid RefId.");
            }
            return id;
        }

        /// Accepts upper case letters even though nothing writes them - a hand typed or hand renamed `4ZBC`
        /// should find the same item as `4zbc`.
        public static bool TryParse(ReadOnlySpan<char> chars, out uint id)
        {
            return TryParse(chars, Radix, out id);
        }

        /// How the text would have been read back when RefIds were written in decimal, before base36: all digits
        /// meant a decimal number, anything with a letter in it was already base36.
        /// Only for migrating data written by that version - see NeuroRefIdMigration. Everything else uses TryParse.
        public static bool TryParseLegacy(ReadOnlySpan<char> chars, out uint id)
        {
            var allDigits = true;
            for (var i = 0; i < chars.Length; i++)
            {
                if (!IsDigit(chars[i]))
                {
                    allDigits = false;
                    break;
                }
            }
            return TryParse(chars, allDigits ? 10u : Radix, out id);
        }

        static bool TryParse(ReadOnlySpan<char> chars, uint radix, out uint id)
        {
            id = 0;
            var length = chars.Length;
            if (length == 0)
            {
                return false;
            }
            var value = 0ul;
            for (var i = 0; i < length; i++)
            {
                var digit = ValueOf(chars[i]);
                if (digit < 0 || digit >= radix)
                {
                    return false;
                }
                value = value * radix + (ulong)digit;
                if (value > uint.MaxValue)
                {
                    return false;
                }
            }
            id = (uint)value;
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool IsDigit(char c) => c >= '0' && c <= '9';

        static int ValueOf(char c)
        {
            if (IsDigit(c))
            {
                return c - '0';
            }
            if (c >= 'a' && c <= 'z')
            {
                return c - 'a' + 10;
            }
            if (c >= 'A' && c <= 'Z')
            {
                return c - 'A' + 10;
            }
            return -1;
        }
    }
}

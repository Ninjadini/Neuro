using System;
using System.Numerics;
using System.Text;
using Ninjadini.Neuro.Utils;
using Ninjadini.Neuro.Sync;

namespace Ninjadini.Neuro
{
    public class NeuroBytesDebugWalker
    {
        RawProtoReader proto = new RawProtoReader();
        StringBuilder stringBuilder = new StringBuilder();
        Options options;
        
        [Flags]
        public enum Options
        {
            PrintValues = 1,
            PrintSizes = 2
        }
        
        public string Walk(BytesChunk bytesChunk, Options options = Options.PrintSizes | Options.PrintValues)
        {
            this.options = options;
            stringBuilder.Length = 0;
            proto.Set(bytesChunk);
            ReadGroup(0);
            if (proto.Available > 0)
            {
                stringBuilder.AppendLine();
                stringBuilder.Append("ERROR: Did not reach end of stream. Remaining bytes: " + proto.GetDebugString(proto.Position, proto.Available));
            }
            return stringBuilder.Length > 1 ? stringBuilder.ToString(1, stringBuilder.Length - 1) : "";
        }
        
        public string TryWalk(BytesChunk bytesChunk, out bool errored, Options options = Options.PrintSizes | Options.PrintValues)
        {
            this.options = options;
            errored = false;
            stringBuilder.Length = 0;
            try
            {
                proto.Set(bytesChunk);
                ReadGroup(0);
                if (proto.Available > 0)
                {
                    stringBuilder.AppendLine();
                    stringBuilder.Append("ERROR: Did not reach end of stream. Remaining bytes: " + proto.GetDebugString(proto.Position, proto.Available));
                }
            }
            catch (Exception e)
            {
                errored = true;
                stringBuilder.Append("\nERROR: ");
                stringBuilder.Append(e.ToString());
            }
            
            return stringBuilder.ToString();
        }

        void ReadGroup(int indents)
        {
            var nextKey = 0u;
            while(proto.Available > 0)
            {
                var positionAtStart = proto.Position;
                var nextHeader = proto.ReadUint();
                if(nextHeader == NeuroConstants.EndOfChild)
                {
                    stringBuilder.AppendLine("");
                    AppendIndent(indents - 1);
                    stringBuilder.Append("}");
                    return; // reached end of group
                }
                var keyIncrement = (nextHeader >> NeuroConstants.HeaderShift);
                nextKey += keyIncrement;

                stringBuilder.AppendLine("");
                AppendIndent(indents);
                if (keyIncrement > 0)
                {
                    stringBuilder.AppendNum(nextKey);
                    stringBuilder.Append(": ");
                }
                PrintContent(nextHeader, indents);
                AppendSizeSince(positionAtStart);
            }
        }

        void PrintContent(uint header, int indents, uint? subClassTag = null)
        {
            var dataType = header & NeuroConstants.HeaderMask;
            if(dataType == NeuroConstants.VarInt)
            {
                PrintVarInt();
            }
            else if(dataType == NeuroConstants.Fixed32)
            {
                PrintFixed32();
            }
            else if(dataType == NeuroConstants.Fixed64)
            {
                PrintFixed64();
            }
            else if(dataType == NeuroConstants.Length)
            {
                PrintContentWithLength();
            }
            else if(dataType == NeuroConstants.List)
            {
                PrintList(indents);
            }
            else if(dataType == NeuroConstants.Dictionary)
            {
                PrintDictionary(indents);
            }
            else if(dataType == NeuroConstants.Child)
            {
                stringBuilder.Append("{");
                ReadGroup(indents + 1);
            }
            else if (dataType == NeuroConstants.ChildWithType)
            {
                if (header == NeuroConstants.ChildWithType && !subClassTag.HasValue)
                {
                    stringBuilder.Append("<{");
                    ReadGroup(indents + 1);
                    stringBuilder.Append(">");
                }
                else
                {
                    stringBuilder.Append("<");
                    stringBuilder.AppendNum(subClassTag ?? proto.ReadUint());
                    stringBuilder.Append("> {");
                    ReadGroup(indents + 1);
                }
            }
            else if(dataType == NeuroConstants.EndOfChild)
            {
                stringBuilder.Append("empty"); // empty list / dictionary
            }
            else
            {
                throw new Exception($"Unexpected sizeType: {dataType}");
            }
        }

        void PrintList(int indents)
        {
            var header = proto.ReadUint();
            NeuroBytesReader.ReadCollectionTypeAndSize(header, out var sizeType, out var count, out var containsNulls);
            header = (header & ~NeuroConstants.HeaderMask) | sizeType;
            indents++;
            stringBuilder.AppendNum(count);
            stringBuilder.AppendLine("x [");
            AppendIndent(indents);
            var countLeft = count;
            while(countLeft > 0)
            {
                countLeft--;
                if (sizeType == NeuroConstants.ChildWithType)
                {
                    var itemTypeTagOrNull = proto.ReadUint();
                    if (itemTypeTagOrNull == 0)
                    {
                        stringBuilder.Append("(0) null");
                    }
                    else
                    {
                        PrintContent(header, indents, itemTypeTagOrNull - 1);
                    }
                }
                else if (containsNulls)
                {
                    var itemHeader = proto.ReadUint();
                    if (itemHeader == 0)
                    {
                        stringBuilder.Append("(0) null");
                    }
                    else
                    {
                        PrintContent(itemHeader, indents);
                    }
                }
                else
                {
                    PrintContent(sizeType, indents);
                }
                stringBuilder.AppendLine();
                AppendIndent(countLeft > 0 ? indents : indents - 1);
            }
            stringBuilder.Append("]");
        }

        void PrintDictionary(int indents)
        {
            stringBuilder.AppendLine("{");
            var types = proto.ReadUint();
            var keyType = types & NeuroConstants.HeaderMask;
            var valueType = types >> NeuroConstants.HeaderShift;
            var count = proto.ReadUint();
            var indents1 = indents + 1;
            for (var i = 0; i < count; i++)
            {
                AppendIndent(indents1);
                PrintContent(keyType, indents1);
                stringBuilder.Append(": ");
                var header = proto.ReadUint();
                if (header == 0u)
                {
                    stringBuilder.Append("(0) null");
                }
                else if (valueType == NeuroConstants.ChildWithType)
                {
                    PrintContent(valueType, indents1, header - 1);
                }
                else
                {
                    PrintContent(valueType, indents1);
                }
                stringBuilder.AppendLine("");
            }
            AppendIndent(indents);
            stringBuilder.Append("}");
        }

        void PrintVarInt()
        {
            var u = proto.ReadULong();
            if ((options & Options.PrintValues) != 0)
            {
                stringBuilder.AppendNum(u);
                stringBuilder.Append("u");
                //stringBuilder.Append("u / ");
                //stringBuilder.AppendNum(RawProtoReader.Zag(u));
            }
            else
            {
                stringBuilder.Append("varint ");
            }
        }

        void PrintFixed32()
        {
            var u = proto.ReadInt32();
            if ((options & Options.PrintValues) != 0)
            {
                stringBuilder.AppendNum(u);
                stringBuilder.Append(" fixed32");
                //stringBuilder.Append("u / ");
                //stringBuilder.AppendNum(RawProtoReader.Zag(u));
            }
            else
            {
                stringBuilder.Append("fixed32 ");
            }
        }

        void PrintFixed64()
        {
            var u = proto.ReadInt64();
            if ((options & Options.PrintValues) != 0)
            {
                stringBuilder.AppendNum(u);
                stringBuilder.Append(" fixed64");
                //stringBuilder.Append("u / ");
                //stringBuilder.AppendNum(RawProtoReader.Zag(u));
            }
            else
            {
                stringBuilder.Append("fixed64 ");
            }
        }
        
        void PrintContentWithLength()
        {
            var l = proto.ReadUint();
            if ((options & Options.PrintValues) != 0)
            {
                try
                {
                    var chunk = proto.GetCurrentBytesChunk();
                    var str = RawProtoWriter.UTF8Encoding.GetString(chunk.Bytes, proto.Position, (int)l);
                    
                    stringBuilder.Append("(");
                    stringBuilder.AppendNum(l);
                    stringBuilder.Append(") \"");
                    stringBuilder.Append(str);
                    stringBuilder.Append("\"");
                }
                catch (Exception)
                {
                    stringBuilder.Append("(");
                    stringBuilder.AppendNum(l);
                    stringBuilder.Append(") ");
                    stringBuilder.Append(proto.GetDebugString(proto.Position, (int)l));
                }
            }
            else
            {
                stringBuilder.Append("length (");
            }

            proto.Skip((int)l);
        }

        void AppendIndent(int indents)
        {
            for (var i = 0; i < indents; i++)
            {
                stringBuilder.Append("    ");
            }
        }

        void AppendSizeSince(int previousPos)
        {
            if ((options & Options.PrintSizes) != 0)
            {
                stringBuilder.Append(" ").AppendNum(proto.Position - previousPos, true).Append("b");
            }
        }
    }
}
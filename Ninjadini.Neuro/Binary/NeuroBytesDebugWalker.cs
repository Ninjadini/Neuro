using System;
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
                var keyIncrement = (nextHeader >> NeuroConstants.HeaderShift);
                if(nextHeader == NeuroConstants.Child)
                {
                    stringBuilder.AppendLine("");
                    AppendIndent(indents - 1);
                    stringBuilder.Append("}");
                    return; // reached end of group
                }
                nextKey += keyIncrement;

                stringBuilder.AppendLine("");
                AppendIndent(indents);
                if (keyIncrement > 0)
                {
                    stringBuilder.AppendNum(nextKey);
                    stringBuilder.Append(": ");
                }
                
                var count = 1u;
                var isList = (nextHeader & NeuroConstants.RepeatedMask) != 0;
                if (isList)
                {
                    indents++;
                    count = proto.ReadUint();
                    stringBuilder.Append("(");
                    stringBuilder.AppendNum(count);
                    if (count > 1)
                    {
                        stringBuilder.AppendLine(") [");
                        AppendIndent(indents);
                    }
                    else
                    {
                        stringBuilder.Append(") [");
                    }
                }
                var sizeType = nextHeader & NeuroConstants.SizeTypeMask;
                var countLeft = count;
                while (countLeft > 0)
                {
                    countLeft--;
                    if(sizeType == NeuroConstants.VarInt)
                    {
                        PrintVarInt();
                    }
                    else if(sizeType == NeuroConstants.Fixed32)
                    {
                        PrintFixed32();
                    }
                    else if(sizeType == NeuroConstants.Fixed64)
                    {
                        PrintFixed64();
                    }
                    else if(sizeType == NeuroConstants.Length)
                    {
                        PrintFixedLength();
                    }
                    else
                    {
                        if (sizeType == NeuroConstants.ChildWithType)
                        {
                            if (keyIncrement > 0)
                            {
                                stringBuilder.Append("<");
                                stringBuilder.AppendNum(proto.ReadUint());
                                stringBuilder.Append("> {");
                                ReadGroup(indents + 1);
                            }
                            else
                            {
                                stringBuilder.Append("<{");
                                ReadGroup(indents + 1);
                                stringBuilder.Append(">");
                            }
                        }
                        else
                        {
                            stringBuilder.Append("{");
                            ReadGroup(indents + 1);
                        }
                    }

                    if (isList && countLeft > 0)
                    {
                        stringBuilder.AppendLine();
                        AppendIndent(indents);
                    }
                }
                if (isList)
                {
                    indents--;
                    if (count > 1)
                    {
                        stringBuilder.AppendLine();
                        AppendIndent(indents);
                        stringBuilder.Append("]");
                    }
                    else
                    {
                        stringBuilder.Append("]");
                    }
                }
                AppendSizeSince(positionAtStart);
            }
        }

        void PrintVarInt()
        {
            var u = proto.ReadULong();
            if ((options & Options.PrintValues) != 0)
            {
                stringBuilder.AppendNum(u);
                stringBuilder.Append("u");
                //stringBuilder.Append("u / ");
                //stringBuilder.AppendNum(ProtoReader.Zag(u));
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
                //stringBuilder.AppendNum(ProtoReader.Zag(u));
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
                //stringBuilder.AppendNum(ProtoReader.Zag(u));
            }
            else
            {
                stringBuilder.Append("fixed64 ");
            }
        }
        
        void PrintFixedLength()
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
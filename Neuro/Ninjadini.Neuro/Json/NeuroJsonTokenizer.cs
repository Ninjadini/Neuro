using System;
using System.Linq;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Ninjadini.Neuro.SyncTests")]
[assembly: InternalsVisibleTo("Ninjadini.Neuro.Unity.Editor")]
namespace Ninjadini.Neuro.Sync
{
    internal class NeuroJsonTokenizer
    {
        private VisitedNodes _nodes;

        public void EnsureCapacity(int count)
        {
            if (_nodes.Array == null)
            {
                _nodes.Array = new VisitedNode[count];
            }
            else if (_nodes.Array.Length < count)
            {
                _nodes.Array = new VisitedNode[count];
            }
        }

        public VisitedNodes Visit(string jsonStr)
        {
            var nodes = _nodes;
            if (nodes.Array == null)
            {
                nodes.Array = new VisitedNode[32];
            }
            nodes.Count = 0;
            var index = 0;
            if (index < jsonStr.Length)
            {
                SkipWhiteSpace(jsonStr, ref index);
                VisitValue(jsonStr, ref nodes, ref index, out _);
                _nodes = nodes;
            }
            while (index < jsonStr.Length)
            {
                if (!char.IsWhiteSpace(jsonStr[index]))
                {
                    throw Error(jsonStr, index, "Unexpected character");
                }
                index++;
            }
            return _nodes;
        }

        public void PrintNodes(string jsonStr)
        {
            Console.WriteLine("NODES:");
            for(var i = 0; i < _nodes.Count; i++)
            {
                PrintNode(jsonStr, _nodes.Array[i]);
            }
        }

        static StringRange VisitGroup(string jsonStr, ref VisitedNodes nodes, int index, out int childCount)
        {
            var i = index;
            var length = jsonStr.Length;
            childCount = 0;
            while (i >= 0 && i < length)
            {
                var c = jsonStr[i];
                i++;
                if (c == '"')
                {
                    childCount++;
                    var key = ReadString(jsonStr, ref i);
                    var reservedNode = nodes.Count;
                    nodes.Add();
                    SkipToValue(jsonStr, ref i);
                    var value = VisitValue(jsonStr, ref nodes, ref i, out var nodeType);
                    nodes.Array[reservedNode] = (new VisitedNode()
                    {
                        NextNode = nodes.Count,
                        Type = nodeType,
                        Key = key,
                        Value = value,
                        Parent = index
                    });
                    //PrintNode(jsonStr, _nodes[reservedNode]);
                    GoToNextItem(jsonStr, ref i);
                }
                else if (c == '}')
                {
                    return new StringRange()
                    {
                        Start = index,
                        End = i
                    };
                }
                else if (c == ',' || char.IsWhiteSpace(c))
                {
                    continue;
                }
                else
                {
                    throw Error(jsonStr, i, "Unexpected character '" + c + "'.");
                }
            }
            throw Error(jsonStr, i, "Reached end");
        }

        static StringRange VisitArray(string jsonStr, ref VisitedNodes nodes, int index, out int childCount)
        {
            var length = jsonStr.Length;
            var i = index;
            childCount = 0;
            while (i >= 0 && i < length)
            {
                var c = jsonStr[i];
                if (char.IsWhiteSpace(c) || c == ',')
                {
                    i++;
                }
                else if (c == ']')
                {
                    return new StringRange()
                    {
                        Start = index,
                        End = i + 1
                    };
                }
                else
                {
                    childCount++;
                    var reservedNode = nodes.Count;
                    nodes.Add();
                    var value = VisitValue(jsonStr, ref nodes, ref i, out var nodeType);
                    nodes.Array[reservedNode]= new VisitedNode()
                    {
                        NextNode = nodes.Count,
                        Type = nodeType,
                        Value = value,
                        Parent = index
                    };
                    //PrintNode(jsonStr, _nodes[reservedNode]);
                }
            }
            throw Error(jsonStr, i, "Reached end");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void SkipWhiteSpace(string jsonStr, ref int index)
        {
            while (index < jsonStr.Length)
            {
                if (char.IsWhiteSpace(jsonStr[index]))
                {
                    index++;
                }
                else
                {
                    return;
                }
            }
        }

        static void PrintNode(string jsonStr, in VisitedNode node)
        {
            if (node.Type == NodeType.Group)
            {
                Console.WriteLine($"{node.Parent} - \"{node.Key.GetSubstring(jsonStr)}\" : group: {node.Value.Start} count: {node.Value.End} nextIndex: {node.NextNode}");
            }
            else if (node.Type == NodeType.Array)
            {
                Console.WriteLine($"{node.Parent} - \"{node.Key.GetSubstring(jsonStr)}\" : array:{node.Value.Start} count: {node.Value.End} nextIndex: {node.NextNode}");
            }
            else
            {
                Console.WriteLine(node.Parent +" - \"" + node.Key.GetSubstring(jsonStr) + "\" : " + node.Value.GetSubstring(jsonStr));
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void SkipToValue(string jsonStr, ref int index)
        {
            while (index < jsonStr.Length)
            {
                var c = jsonStr[index];
                if (c == ':')
                {
                    index++;
                    SkipWhiteSpace(jsonStr, ref index);
                    return;
                }
                if (!char.IsWhiteSpace(c))
                {
                    throw Error(jsonStr, index, "Expecting ':' only.");
                }
                index++;
            }
            throw Error(jsonStr, index, "Reached end");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void GoToNextItem(string jsonStr, ref int index)
        {
            while (index < jsonStr.Length)
            {
                var c = jsonStr[index];
                if (c == '}')
                {
                    return;
                }
                if (c == ',')
                {
                    index++;
                    return;
                }
                else if (!char.IsWhiteSpace(c) && c != ',')
                {
                    throw Error(jsonStr, index, "Expecting ',' only.");
                }
                index++;
            }
        }

        static StringRange VisitValue(string jsonStr, ref VisitedNodes nodes, ref int index, out NodeType nodeType)
        {
            var c = jsonStr[index];
            if (c == '{')
            {
                var result = VisitGroup(jsonStr, ref nodes, index + 1, out var childCount);
                index = result.End;
                result.End = childCount;
                nodeType = NodeType.Group;
                return result;
            }
            if (c == '[')
            {
                var result = VisitArray(jsonStr, ref nodes, index + 1, out var childCount);
                index = result.End;
                result.End = childCount;
                nodeType = NodeType.Array;
                return result;
            }
            if (c == '"')
            {
                nodeType = NodeType.String;
                index++;
                return ReadString(jsonStr, ref index);
            }
            if (char.IsNumber(c) || c == '-')
            {
                nodeType = NodeType.Value;
                return ReadNumber(jsonStr, ref index);
            }
            if (c == 't')
            {
                nodeType = NodeType.Value;
                return ReadLiteral(jsonStr, "true", ref index);
            }
            if (c == 'f')
            {
                nodeType = NodeType.Value;
                return ReadLiteral(jsonStr, "false", ref index);
            }
            if (c == 'n')
            {
                nodeType = NodeType.Value;
                return ReadLiteral(jsonStr, "null", ref index);
            }
            throw Error(jsonStr, index, "Unexpected string '" + c +"'");
        }

        static StringRange ReadLiteral(string jsonStr, string str, ref int index)
        {
            var startIndex = index;
            var strLen = str.Length;
            if (startIndex + strLen > jsonStr.Length)
            {
                throw Error(jsonStr, index, "Reached end, was expecting \"{str}\"");
            }
            index++;
            for (var i = 1; i < strLen; i++)
            {
                if (jsonStr[index] != str[i])
                {
                    throw Error(jsonStr, index, $"Unexpected char '{jsonStr[index] }', was expecting char '{str[i]} of \"{str}\"");
                }
                index++;
            }
            return new StringRange()
            {
                Start = startIndex,
                End = index
            };
        }

        static StringRange ReadString(string jsonStr, ref int index)
        {
            var start = index;
            int endIndex;
            while (true)
            {
                endIndex = jsonStr.IndexOf('"', index);
                if(endIndex < 0)
                {
                    throw Error(jsonStr, index, "Expected string end '\"'");
                }
                if (jsonStr[endIndex - 1] != '\\')
                {
                    break;
                }
                else if (endIndex >= 2 && jsonStr[endIndex - 2] == '\\')
                {
                    // TODO
                    throw new Exception("TODO Double // near a string end is not working yet");
                }
                else
                {
                    index = endIndex + 1;
                }
            }
            index = endIndex + 1;
            return new StringRange()
            {
                Start = start,
                End = endIndex
            };
        }

        static StringRange ReadNumber(string jsonStr, ref int index)
        {
            var start = index;
            index++;
            while (index < jsonStr.Length)
            {
                var c = jsonStr[index];
                if (char.IsNumber(c) || c == '.' || c == 'e' || c == 'E' || c == '+' || c == '-')
                {
                    index++;
                }
                else
                {
                    return new StringRange()
                    {
                        Start = start,
                        End = index
                    };
                }
            }
            throw Error(jsonStr, index, "Reached end");
        }

        static Exception Error(string jsonStr, int index, string str)
        {
            /*
            Console.WriteLine("NODES:");
            foreach (var node in _nodes)
            {
                PrintNode(_jsonStr, node);
            }*/
            
            var start = Math.Max(0, index - 6);
            var len = Math.Min(12, jsonStr.Length - start);
            index = Math.Min(index, jsonStr.Length - 1);
            var lineNum =  jsonStr.Take(index).Count(c => c == '\n') + 1;
            return new System.Exception($"Invalid JSON: {str}. @ line {lineNum}: '{jsonStr[Math.Min(index, jsonStr.Length - 1)]}' near \"{jsonStr.Substring(start, len)}\"");
        }
        
        public enum NodeType : byte
        {
            Unknown,
            String,
            Group,
            Array,
            Value
        }

        public struct VisitedNode
        {
            public int NextNode;
            public NodeType Type;
            public int Parent;
            public StringRange Key;
            public StringRange Value; // WARNING: Value.End maybe be the number of items in group/list
        }
        
        public struct VisitedNodes
        {
            public int Count;
            public VisitedNode[] Array;

            public void Add()
            {
                Count++;
                if (Count >= Array.Length)
                {
                    var min = Math.Max(Count, Array.Length);
                    var newArray = new VisitedNode[Math.Max(min * 2, min + 32)];
                    Array.CopyTo(newArray, 0);
                    Array = newArray;
                }
            }
        }

        public struct StringRange
        {
            public int Start;
            public int End;

            public int Length => End - Start;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public ReadOnlySpan<char> AsSpan(string text)
            {
                return text.AsSpan(Start, End - Start);
            }

            public static bool Equals(in StringRange range, string jsonString, string str)
            {
                var l = range.End - range.Start;
                if (str.Length != l)
                {
                    return false;
                }
                for (var i = 0; i < l; i++)
                {
                    if (jsonString[range.Start + i] != str[i])
                    {
                        return false;
                    }
                }
                return true;
            }
            public string GetSubstring(string str) => str.Substring(Start, End - Start);
        }
    }
}
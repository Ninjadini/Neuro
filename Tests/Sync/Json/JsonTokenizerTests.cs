using Ninjadini.Neuro.Sync;
using NUnit.Framework;

namespace Ninjadini.Neuro.SyncTests
{
    public class JsonTokenizerTests
    {
        [Test]
        public void Visit1()
        {
            var jsonStr = @"  {
    ""key1""   : ""value1"",

    ""keyStr""   : ""String with \""quote\"" and some line
break "",
    ""keyBlankStar""   : """",

    ""key2"":123,
    ""key3"":true,
    ""key3"":   false   ,
    ""key3"": null ,

    ""group"":{

    ""subKey1""   : ""subValue1"",

    ""subKey2"":""subValue2"",

    ""subKey3"": {
        ""subSubKey1""   : ""subSubValue1"",
        ""subSubKey2""   : 1234,
 }
}
,
    ""group2"":{
 }
}  ";

            var token = new NeuroJsonTokenizer();
            token.Visit(jsonStr);
            token.PrintNodes(jsonStr);
        }

        [Test]
        public void VisitArray()
        {
            var jsonStr = @"{
    ""key0"":[],

    ""key1"":[ ""1""],

    ""key2"":[ ""1"", ""1""],

    ""key3"":[ ""1"", ""2"", ""3""]
}";

            var token = new NeuroJsonTokenizer();
            token.Visit(jsonStr);
            token.PrintNodes(jsonStr);
        }

        [Test]
        public void VisitArrayObj()
        {
            var jsonStr = @"[""1"", ""2"", ""3""]";

            var token = new NeuroJsonTokenizer();
            token.Visit(jsonStr);
            token.PrintNodes(jsonStr);
        }
    }
}
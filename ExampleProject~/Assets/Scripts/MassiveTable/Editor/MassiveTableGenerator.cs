using System;
using System.Linq;
using Ninjadini.Neuro.Editor;
using UnityEditor;

public static class MassiveTableGenerator
{
    [MenuItem("Tools/Populate Massive Table")]
    public static void Populate()
    {
        var dataProvider = NeuroEditorDataProvider.Shared;
        foreach (var neuroDataFile in NeuroEditorDataProvider.Shared.DataFiles.ToArray())
        {
            if (neuroDataFile.RootType == typeof(MassiveTableRow))
            {
                dataProvider.Delete(neuroDataFile);
            }
        }
        for (var i = 0; i < 5000; i++)
        {
            var item = PopulateRow(0);
            item.RefName = "item " + (i + 1);
            dataProvider.Add(item);
        }
    }

    static MassiveTableRow PopulateRow(int depth)
    {
        var random = new Random();
        var result = new MassiveTableRow();
        result.Int = random.Next(-10000000, 10000000);
        result.Int2 = random.Next(-10000000, 10000000);
        result.Uint = (uint)random.Next(0, 10000000);
        result.Uint2 = (uint)random.Next(0, 10000000);
        result.Str = GetRandomString(random);
        result.Float = (float)random.NextDouble();
        result.Float2 = (float)random.NextDouble();
        result.Date = DateTime.UtcNow.AddMilliseconds(random.Next(0, 10000000));
        result.TimeSpan = TimeSpan.FromMilliseconds(random.Next(0, 100000));
        result.Child = depth < 10 && random.NextDouble() < 0.5 ? PopulateRow(depth + 1) : null;
        if (depth < 2 && random.NextDouble() < 0.3)
        {
            for (var i = random.Next(0, 5); i >= 0; i--)
            {
                result.Children.Add(PopulateRow(1000));
            }
        }
        for (var i = random.Next(0, 5); i >= 0; i--)
        {
            result.Strings.Add(GetRandomString(random));
        }
        result.Poly = PopulatePoly(random);
        for (var i = random.Next(0, 20); i >= 0; i--)
        {
            result.PolyList.Add(PopulatePoly(random));
        }
        result.Struct = new MassiveTableRow.TestStruct()
        {
            Id = random.Next(0, 10000),
            Name = GetRandomString(random)
        };
        return result;  
    }

    static string GetRandomString(Random random)
    {
        var length = random.Next(0, 60);
        return new string(Enumerable.Repeat(Chars, length).Select(s => s[random.Next(s.Length)]).ToArray());
    }

    static MyTestRef PopulatePoly(Random random)
    {
        if (random.NextDouble() < 0.5)
        {
            return new MyTestRef()
            {
                RefId = (uint)random.Next(0, 1000000),
                Str = GetRandomString(random)
            };
        }
        else
        {
            return new MyTestSub2Ref()
            {
                RefId = (uint)random.Next(0, 1000000),
                Str = GetRandomString(random),
                someInt = random.Next(0, 1000000),
                someStr = GetRandomString(random),
            };
        }
    }
    private static string Chars = "abcdefghijklmnopqrstuvwxyz0123456789§±';\"|/.,`~?><}{][!@£$%^&*()_+-=ABCDEFGHIJKLMNOPQRSTUVWXYZ";
    // NOTE: slash "\" not working yet
}
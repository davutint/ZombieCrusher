using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine.Assertions;

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

namespace Rukhanka.Editor
{
public class ShaderConfigManager: SymbolManager
{
    string filePath;
    List<string> cfgLines = new ();
    readonly Regex defineRegex = new ("\\s*#define\\s+(.*)");
    
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public ShaderConfigManager(string pathToShaderConfigFile)
    {
        filePath = pathToShaderConfigFile;
        if (File.Exists(filePath))
        {
            var lines = File.ReadAllLines(filePath);
            cfgLines = new List<string>(lines);
        }
        
        //  Search for defines and add them to the list
        for (var k = 0; k < cfgLines.Count;)
        {
            var l = cfgLines[k];
            var defines = defineRegex.Match(l);
            if (defines.Length > 1)
            {
                Assert.IsTrue(defines.Groups.Count == 2);
                var d = defines.Groups[1];
                symbols.Add(d.Value);
                cfgLines.RemoveAt(k);
            }
            else
            {
                ++k;
            }
        }
        
        //  Remove blank lines
        cfgLines.RemoveAll(x => x.Length == 0);
    }
    
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public override void ApplyChanges()
    {
        if (!HasChanges())
            return;
        
        for (var i = 0; i < symbols.Count; ++i)
        {
            var s = symbols[i];
            cfgLines.Add($"#define {s}");
        }
        File.WriteAllLines(filePath, cfgLines);
        AssetDatabase.Refresh();
        
        base.ApplyChanges();
    }
}
}

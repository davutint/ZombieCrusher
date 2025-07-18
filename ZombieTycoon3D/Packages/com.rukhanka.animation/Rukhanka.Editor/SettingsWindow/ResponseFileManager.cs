
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

namespace Rukhanka.Editor
{
public class ResponseFileManager: SymbolManager
{
    List<string> rspFileContent = new ();
    string filePath;
    readonly Regex defineRegex = new ("-define\\s*:\\s*(.*)");
    
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public ResponseFileManager(string pathToResponseFile)
    {
        filePath = pathToResponseFile;
        if (File.Exists(pathToResponseFile))
        {
            var rspFileContentArr = File.ReadAllLines(pathToResponseFile);
            rspFileContent = new List<string>(rspFileContentArr);
        }
        
        //  Search for defines and add them to the list
        for (var k = 0; k < rspFileContent.Count;)
        {
            var l = rspFileContent[k];
            var defines = defineRegex.Match(l);
            if (defines.Length > 1)
            {
                for (var i = 1; i < defines.Groups.Count; ++i)
                {
                    var d = defines.Groups[i];
                    var symbolsArr = d.Value.Split(';');
                    symbols.AddRange(symbolsArr);
                }
                rspFileContent.RemoveAt(k);
            }
            else
            {
                ++k;
            }
        }
        
        //  Remove blank lines
        rspFileContent.RemoveAll(x => x.Length == 0);
    }
    
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    string CreateDefinesString()
    {
        if (symbols.Count == 0)
            return "";
        
        var rv = "-define:";
        for (var i = 0; i < symbols.Count; ++i)
        {
            if (i != 0)
                rv += ';';
            rv += symbols[i];
        }
        return rv;
    }

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public override void ApplyChanges()
    {
        if (!HasChanges())
            return;
        
        var definesString = CreateDefinesString();
        rspFileContent.Add(definesString);
        File.WriteAllLines(filePath, rspFileContent);
        AssetDatabase.Refresh();
        
        base.ApplyChanges();
    }
}
}

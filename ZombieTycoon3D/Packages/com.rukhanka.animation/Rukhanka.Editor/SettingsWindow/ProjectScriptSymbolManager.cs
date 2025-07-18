
using System;
using System.Collections.Generic;
using System.Drawing.Design;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Build;

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

namespace Rukhanka.Editor
{
public static class ProjectScriptSymbolManager
{
	static NamedBuildTarget GetCurrentBuildTarget()
	{
#if UNITY_SERVER
		return NamedBuildTarget.Server;
#else
		var bt = EditorUserBuildSettings.activeBuildTarget;
		var btg = BuildPipeline.GetBuildTargetGroup(bt);
		var rv = NamedBuildTarget.FromBuildTargetGroup(btg);
		return rv;
#endif
	}
	
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	static List<string> GetScriptSymbols()
	{
		var bt = GetCurrentBuildTarget();
		var definesStr = PlayerSettings.GetScriptingDefineSymbols(bt);
		var rv = new List<string>(definesStr.Split(';'));
		return rv;
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	static void SetScriptSymbols(List<string> sss)
	{
		if (sss == null || sss.Count == 0)
			return;
		
		var rv = "";
		for (var i = 0; i < sss.Count; ++i)
		{
			rv += sss[i];
			if (i != sss.Count - 1)
				rv += ';';
		}
		PlayerSettings.SetScriptingDefineSymbols(GetCurrentBuildTarget(), rv);
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	public static void ToggleScriptSymbol(string ss, bool on)
	{
		if (on)
			AddScriptSymbol(ss);
		else
			RemoveScriptSymbol(ss);
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	public static void RemoveScriptSymbol(string ss)
	{
	    var defines = GetScriptSymbols();
	    var idx = defines.FindIndex(x => x == ss);
		if (idx >= 0)
		{
			defines.RemoveAt(idx);
			SetScriptSymbols(defines);
		}
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public static void AddScriptSymbol(string ss)
    {
	    var defines = GetScriptSymbols();
		if (defines.FindIndex(x => x == ss) < 0)
		{
			defines.Add(ss);
			SetScriptSymbols(defines);
		}
    }
}
}

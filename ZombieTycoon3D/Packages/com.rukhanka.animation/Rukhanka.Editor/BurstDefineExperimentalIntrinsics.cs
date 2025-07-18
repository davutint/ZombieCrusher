using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

namespace Rukhanka.Editor
{
[InitializeOnLoad]
public class RukhankaEditorAutorun
{
	static RukhankaEditorAutorun()
	{
#if !UNITY_BURST_EXPERIMENTAL_ATOMIC_INTRINSICS
		//	Add obligatory UNITY_BURST_EXPERIMENTAL_ATOMIC_INTRINSICS script symbol
		ProjectScriptSymbolManager.AddScriptSymbol("UNITY_BURST_EXPERIMENTAL_ATOMIC_INTRINSICS");
#endif
		
#if RUKHANKA_SHADER_DEBUG
		var scc = new ShaderConfigManager(SettingsWindow.SHADER_CONFIG_PATH);
		scc.AddScriptSymbol("RUKHANKA_SHADER_DEBUG");
		scc.ApplyChanges();
#endif
	}
}
}

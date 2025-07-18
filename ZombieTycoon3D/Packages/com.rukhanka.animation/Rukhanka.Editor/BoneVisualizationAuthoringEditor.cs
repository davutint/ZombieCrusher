using Rukhanka.Hybrid;
using UnityEditor;

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

namespace Rukhanka.Editor
{
[CustomEditor(typeof(BoneVisualizationAuthoring))]
public class BoneVisualizationAuthoringEditor: UnityEditor.Editor
{
	public override void OnInspectorGUI()
	{
	#if RUKHANKA_DEBUG_INFO
		DrawDefaultInspector();
	#else
		EditorGUILayout.HelpBox("No RUKHANKA_DEBUG_INFO scripting symbol defined\n\nBone visualization is not available", MessageType.Warning);
	#endif
	}
}
}

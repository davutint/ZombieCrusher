using Unity.Entities;
using UnityEngine;

/////////////////////////////////////////////////////////////////////////////////

namespace Rukhanka.Hybrid
{
public class BoneVisualizationAuthoring: MonoBehaviour { }

/////////////////////////////////////////////////////////////////////////////////

class BoneVisualizationBaker: Baker<BoneVisualizationAuthoring>
{
	public override void Bake(BoneVisualizationAuthoring a)
	{
	#if !UNITY_SERVER
		var e = GetEntity(TransformUsageFlags.Dynamic);
		AddComponent<BoneVisualizationComponent>(e);
	#endif
		
	#if (RUKHANKA_NO_DEBUG_DRAWER && RUKHANKA_DEBUG_INFO)
		Debug.LogWarning($"'{a.name}' rig visualization was requested, but DebugDrawer is compiled out via RUKHANKA_NO_DEBUG_DRAWER script symbol. No visualization is available.");
	#endif
	}
}
}

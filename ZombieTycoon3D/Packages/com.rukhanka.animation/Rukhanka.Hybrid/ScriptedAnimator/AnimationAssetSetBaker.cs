#if UNITY_EDITOR

using Unity.Collections;
using Unity.Entities;

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

namespace Rukhanka.Hybrid
{
public class AnimationAssetSetBaker: Baker<AnimationAssetSetAuthoring>
{
	public override void Bake(AnimationAssetSetAuthoring a)
	{
		var rigDef = GetComponent<RigDefinitionAuthoring>();
		var avatar = rigDef.GetAvatar();
		
		var animationBaker = new AnimationClipBaker();
		var bakedAnimations = animationBaker.BakeAnimations(this, a.animationClips, avatar, a.gameObject);
		var e = CreateAdditionalEntity(TransformUsageFlags.None, false, a.name + "_AnimationAssets");
		var newAnimArr = AddBuffer<NewBlobAssetDatabaseRecord<AnimationClipBlob>>(e);
		
		//	Add animations
		foreach (var ba in bakedAnimations)
		{
			var newAnim = new NewBlobAssetDatabaseRecord<AnimationClipBlob>()
			{
				hash = ba.Value.hash,
				value = ba
			};
			
			newAnimArr.Add(newAnim);
		}
		
		//	Add avatar masks
		var bakedAvatarMasks = new NativeList<AvatarMaskBakingData>(Allocator.Temp);
		foreach (var am in a.avatarMasks)
		{
			var amb = new AvatarMaskBaker();
			var avatarMaskBlobAsset = amb.CreateAvatarMaskBlob(this, am, rigDef);
			var newAvatarMaskBlob = new AvatarMaskBakingData()
			{
				rigEntity = GetEntity(a, TransformUsageFlags.None),
				dataBlob = avatarMaskBlobAsset
			};
			bakedAvatarMasks.Add(newAvatarMaskBlob);
		}
		
		if (bakedAvatarMasks.Length > 0)
		{
			var buf = AddBuffer<AvatarMaskBakingData>(e);
			buf.AddRange(bakedAvatarMasks.AsArray());
		}
	}
}
}
  
#endif

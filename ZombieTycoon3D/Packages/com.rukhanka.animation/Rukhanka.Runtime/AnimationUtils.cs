using Unity.Collections;
using Unity.Entities;

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

namespace Rukhanka
{
public static class AnimationUtils
{
	public static Hash128 CalculateBoneRemapTableHash(in BlobAssetReference<SkinnedMeshInfoBlob> skinnedMesh, in BlobAssetReference<RigDefinitionBlob> rigDef)
	{
		var rv = new Hash128(skinnedMesh.Value.hash.Value.x, skinnedMesh.Value.hash.Value.y, rigDef.Value.hash.Value.x, rigDef.Value.hash.Value.y);
		return rv;
	}
	
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	public static BlobAssetReference<BoneRemapTableBlob> MakeRigToSkinnedMeshRemapTable(in AnimatedSkinnedMeshComponent sm, in RigDefinitionComponent rigDef, Allocator allocator)
	{
		var bb = new BlobBuilder(Allocator.Temp);
		ref var brt = ref bb.ConstructRoot<BoneRemapTableBlob>();

		var bba = bb.Allocate(ref brt.remapIndices, rigDef.rigBlob.Value.bones.Length);
		for (int i = 0; i < bba.Length; ++i)
		{
			bba[i] = -1;
			ref var rb = ref rigDef.rigBlob.Value.bones[i];
			var rbHash = rb.hash;
			
			for (int j = 0; j < sm.smrInfoBlob.Value.bones.Length; ++j)
			{
				ref var bn = ref sm.smrInfoBlob.Value.bones[j];
				var bnHash = bn.hash;

				if (bnHash == rbHash)
				{ 
					bba[i] = j;
				}
			}
		}
		var rv = bb.CreateBlobAssetReference<BoneRemapTableBlob>(allocator);
		return rv;
	}
	
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	public static BlobAssetReference<BoneRemapTableBlob> MakeSkinnedMeshToRigRemapTable(in AnimatedSkinnedMeshComponent sm, in RigDefinitionComponent rigDef, Allocator allocator)
	{
		var bb = new BlobBuilder(Allocator.Temp);
		ref var brt = ref bb.ConstructRoot<BoneRemapTableBlob>();

		var bba = bb.Allocate(ref brt.remapIndices, sm.smrInfoBlob.Value.bones.Length);
		for (int i = 0; i < bba.Length; ++i)
		{
			bba[i] = -1;
			ref var rb = ref sm.smrInfoBlob.Value.bones[i];
			var rbHash = rb.hash;
			
			for (int j = 0; j < rigDef.rigBlob.Value.bones.Length; ++j)
			{
				ref var bn = ref rigDef.rigBlob.Value.bones[j];
				var bnHash = bn.hash;

				if (bnHash == rbHash)
				{ 
					bba[i] = j;
				}
			}
		}
		var rv = bb.CreateBlobAssetReference<BoneRemapTableBlob>(allocator);
		return rv;
	}
}
}

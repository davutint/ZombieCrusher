
using Unity.Entities;

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

namespace Rukhanka
{
   
public interface GenericAssetBlob
{
#if RUKHANKA_DEBUG_INFO
    public string Name();
    public float BakingTime();
#endif
    public Hash128 Hash();
}
    
}

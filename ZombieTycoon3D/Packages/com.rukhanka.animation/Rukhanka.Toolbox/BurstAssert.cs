using System;
using Unity.Collections;
using UnityEngine;

//////////////////////////////////////////////////////////////////////////////////

namespace Rukhanka.Toolbox
{
public static class BurstAssert
{
    public static void IsTrue(bool c, in FixedString128Bytes errorMessage)    
    {
#if UNITY_ASSERTIONS
        if (c) return;

        Debug.LogError(errorMessage);
        throw new Exception("Assertion Failed");
#endif
    }
}
}

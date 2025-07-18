#if UNITY_EDITOR

using System.Collections.Generic;
using Unity.Collections;
using UnityEditor;
using UnityEngine;

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

namespace Rukhanka.Hybrid
{
public partial class AnimationClipBaker
{
    struct SampledCurve
    {
        public ParsedCurveBinding pb;
        public float value;
    }
    
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    List<SampledCurve> SampleAnimation(AnimationClip ac, Avatar avatar, float time)
    {
        var rv = new List<SampledCurve>();
        var bindings = AnimationUtility.GetCurveBindings(ac);
        foreach (var cb in bindings)
        {
			var ec = AnimationUtility.GetEditorCurve(ac, cb);
            var v = new SampledCurve();
            v.pb = ParseCurveBinding(ac, cb, avatar);
            v.value = ec.Evaluate(time);
            rv.Add(v);
        }
        return rv;
    }
}
}

#endif
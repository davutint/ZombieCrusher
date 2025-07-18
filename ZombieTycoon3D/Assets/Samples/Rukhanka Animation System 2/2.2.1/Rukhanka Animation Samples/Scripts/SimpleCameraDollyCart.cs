
using System;
using UnityEngine;

///////////////////////////////////////////////////////////////////////////////////////////

namespace Rukhanka.Samples
{
class SimpleCameraDollyCart: MonoBehaviour
{
    public Transform cart;
    public float speed;
    public Transform[] points;
    public Transform lookTarget;
    int nextPointIndex;
    
///////////////////////////////////////////////////////////////////////////////////////////

    void Update()
    {
        var curPos = cart.position;
        var nextPoint = points[nextPointIndex].position;
        var v = nextPoint - curPos;
        var dp = v.normalized * Time.deltaTime * speed;
        var newCurPos = curPos + dp;
        if (v.magnitude < dp.magnitude)
        {
            nextPointIndex = (nextPointIndex + 1) % points.Length;
        }
        cart.transform.LookAt(lookTarget);
        cart.position = newCurPos;
    }
    
///////////////////////////////////////////////////////////////////////////////////////////

    void OnDrawGizmosSelected()
    {
        for (var i = 0; i < points.Length; ++i)
        {
            var pa = points[i].position;
            var pb = points[(i + 1) % points.Length].position;
            Debug.DrawLine(pa, pb, Color.cyan);
        }
    }
}
}

using System;
using System.Collections.Generic;
using Unity.Rendering;
using UnityEngine;
using UnityEngine.Assertions;

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

namespace Rukhanka.Toolbox
{
public class SparseUploaderPool: IDisposable
{
    Stack<SparseUploader> freeUploaders;
    List<SparseUploader> allUploaders;
    
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public SparseUploaderPool()
    {
        freeUploaders = new ();
        allUploaders = new ();
    }

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public void Dispose()
    {
        foreach (var u in allUploaders)
        {
            u.Dispose();
        }
    }

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public void FrameCleanup()
    {
        foreach (var u in allUploaders)
        {
            u.FrameCleanup();
        }
    }

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public SparseUploader GetUploader(GraphicsBuffer gb)
    {
        SparseUploader rv;
        if (freeUploaders.Count > 0)
        {
            rv = freeUploaders.Pop();
        }
        else
        {
            rv = new SparseUploader(null);
            allUploaders.Add(rv);
            Assert.IsTrue(allUploaders.Count < 0xff, "Looks like 'PutUploader' call is forgotten somewhere! There are too much of created uploaders.");
        }
        
        rv.ReplaceBuffer(gb);
        return rv;
    }
    
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public void PutUploader(SparseUploader su)
    {
        freeUploaders.Push(su);
    } 
}
}

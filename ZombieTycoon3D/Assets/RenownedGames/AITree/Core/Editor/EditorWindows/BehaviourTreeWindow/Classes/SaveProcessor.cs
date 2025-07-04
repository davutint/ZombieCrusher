/* ================================================================
   ----------------------------------------------------------------
   Project   :   AI Tree
   Publisher :   Renowned Games
   Developer :   Zinnur Davleev, Tamerlan Shakirov
   ----------------------------------------------------------------
   Copyright 2023 Renowned Games All rights reserved.
   ================================================================ */

using RenownedGames.AITree;
using System.IO;
using UnityEditor;

namespace RenownedGames.AITreeEditor
{
    class SaveProcessor : AssetModificationProcessor
    {
        static string[] OnWillSaveAssets(string[] paths)
        {
            foreach (string path in paths)
            {
                if (Path.GetExtension(path) == ".asset")
                {
                    BehaviourTree behaviourTree = AssetDatabase.LoadAssetAtPath<BehaviourTree>(path);
                    if (behaviourTree != null)
                    {
                        foreach(TrackerWindow tracker in TrackerWindow.ActiveTrackers)
                        {
                            if (tracker is BehaviourTreeWindow window)
                            {
                                window.MarkAsSaved();
                            }
                        }
                    }
                }
            }

            return paths;
        }
    }
}
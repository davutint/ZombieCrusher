/* ================================================================
   ----------------------------------------------------------------
   Project   :   AI Tree
   Publisher :   Renowned Games
   Developer :   Tamerlan Shakirov
   ----------------------------------------------------------------
   Copyright 2024 Renowned Games All rights reserved.
   ================================================================ */

using System.Threading.Tasks;
using UnityEditor;

namespace RenownedGames.AITreeEditor
{
    public static class BehaviourTreeUtility
    {
        [MenuItem("Tools/AI Tree/Windows/Auto Layout", false, 100)]
        public static void AutoLayout()
        {
            AutoLayoutAsync();
        }

        public static void AutoLayout(BehaviourTreeWindow window)
        {
            AutoLayoutAsync(window);
        }

        internal static async void AutoLayoutAsync(BehaviourTreeWindow window = null)
        {
            if (window == null)
            {
                window = TrackerWindow.CreateTracker<BehaviourTreeWindow>();
                await Task.Delay(50);
            }

            BlackboardWindow blackboardWindow = TrackerWindow.CreateTracker<BlackboardWindow>();
            window.DockWindow(blackboardWindow, EditorWindowUtility.DockPosition.Left);
            await Task.Delay(50);

            NodeInspectorWindow nodeInspectorWindow = TrackerWindow.CreateTracker<NodeInspectorWindow>();
            window.DockWindow(nodeInspectorWindow, EditorWindowUtility.DockPosition.Right);
            await Task.Delay(50);

            BlackboardDetailsWindow blackboardDetailsWindow = TrackerWindow.CreateTracker<BlackboardDetailsWindow>();
            blackboardWindow.DockWindow(blackboardDetailsWindow, EditorWindowUtility.DockPosition.Bottom);
            await Task.Delay(50);

            window.GetGraph().FrameAll();
        }
    }
}

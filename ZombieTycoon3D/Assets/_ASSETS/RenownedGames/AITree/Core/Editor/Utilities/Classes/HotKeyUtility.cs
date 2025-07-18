#if UNITY_2022_3_OR_NEWER
/* ================================================================
   ----------------------------------------------------------------
   Project   :   AI Tree
   Publisher :   Renowned Games
   Developer :   Tamerlan Shakirov
   ----------------------------------------------------------------
   Copyright 2022-2023 Renowned Games All rights reserved.
   ================================================================ */

using UnityEditor;
using UnityEngine;

namespace RenownedGames.AITreeEditor
{
    public static class HotKeyUtility
    {
        private static Event CurrentEvent;

        /// <summary>
        /// Try get current GUI event.
        /// <br>Can be called outside of GUI calls.</br>
        /// </summary>
        /// <param name="window">Window reference where called this method.</param>
        /// <param name="evt">Current GUI event of one of window.</param>
        /// <returns></returns>
        public static bool TryGetEvent(EditorWindow window, out Event evt)
        {
            if(CurrentEvent == null || EditorWindow.focusedWindow == null || EditorWindow.focusedWindow != window)
            {
                evt = null;
                return false;
            }

            evt = CurrentEvent;
            return true;
        }

        internal static void SetEvent(Event evt)
        {
            CurrentEvent = evt;
        }
    }
}
#endif
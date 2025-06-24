/* ================================================================
   ----------------------------------------------------------------
   Project   :   AI Tree
   Publisher :   Renowned Games
   Developer :   Tamerlan Shakirov
   ----------------------------------------------------------------
   Copyright 2024 Renowned Games All rights reserved.
   ================================================================ */

using UnityEngine;

namespace RenownedGames.AITreeEditor
{
    public interface ITrackerWindow<T> where T : Object
    {
        public void TrackEditor(T target);
    }
}
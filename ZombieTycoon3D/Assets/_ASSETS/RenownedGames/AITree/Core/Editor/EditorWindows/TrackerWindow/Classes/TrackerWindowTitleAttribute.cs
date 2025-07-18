/* ================================================================
   ----------------------------------------------------------------
   Project   :   AI Tree
   Publisher :   Renowned Games
   Developer :   Tamerlan Shakirov
   ----------------------------------------------------------------
   Copyright 2024 Renowned Games All rights reserved.
   ================================================================ */

using System;

namespace RenownedGames.AITreeEditor
{
    [AttributeUsage(AttributeTargets.Class)]
    public class TrackerWindowTitleAttribute : Attribute
    {
        public readonly string name;

        /// <summary>
        /// Automaticaly apply title to the tracker window.
        /// </summary>
        /// <param name="name">Name of the window.</param>
        public TrackerWindowTitleAttribute(string name)
        {
            this.name = name;
            IconPath = string.Empty;
        }

        #region [Optional]
        /// <summary>
        /// Icon path of the window, imported in EditorResources.
        /// </summary>
        public string IconPath { get; set; }
        #endregion
    }
}

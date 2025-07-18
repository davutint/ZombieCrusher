/* ================================================================
   ----------------------------------------------------------------
   Project   :   AI Tree
   Publisher :   Renowned Games
   Developer :   Tamerlan Shakirov
   ----------------------------------------------------------------
   Copyright 2024 Renowned Games All rights reserved.
   ================================================================ */

using RenownedGames.ExLibEditor;
using RenownedGames.ExLibEditor.Windows;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace RenownedGames.AITreeEditor
{
    public abstract class TrackerWindow : EditorWindow, IHasCustomMenu
    {
        static class Styles
        {
            public static readonly GUIStyle lockButtonStyle;

            static Styles()
            {
                lockButtonStyle = new GUIStyle("IN LockButton");
            }
        }

        private static List<TrackerWindow> Trackers;

        static TrackerWindow()
        {
            Trackers = new List<TrackerWindow>();
        }

        [SerializeField]
        private bool isLocked;

        [SerializeField]
        private Object target;

        /// <summary>
        /// This function is called when the window is loaded.
        /// </summary>
        protected virtual void OnEnable()
        {
            Trackers.Add(this);
            Trackers.TrimExcess();

            LoadTitle();
        }

        /// <summary>
        /// This function is called when the window is closed.
        /// </summary>
        protected virtual void OnDestroy()
        {
            Trackers.Remove(this);
            Trackers.TrimExcess();
        }

        /// <summary>
        /// Called when this window start tracking specified target reference.
        /// </summary>
        /// <param name="target">Target reference.</param>
        protected abstract void OnTrackEditor(Object target);

        /// <summary>
        /// Start tracking specified target reference.
        /// </summary>
        /// <param name="target">Target reference.</param>
        public void TrackEditor(Object target)
        {
            if (!isLocked || (this.target == null && target != null))
            {
                if (IsValidTarget(target))
                {
                    this.target = target;
                    OnTrackEditor(target);
                }
            }
        }

        /// <summary>
        /// Check if this tracker has a target.
        /// <br><i>Shortcut of checking on null and using the target.</i></br>
        /// </summary>
        /// <param name="target">If it exists, pass in out the target reference, otherwise null.</param>
        /// <returns>If the target exists return true, otherwise false.</returns>
        public bool TryGetTarget(out Object target)
        {
            target = GetTarget();
            return target != null;
        }

        /// <summary>
        /// Check if this tracker has a <typeparamref name="T"/> target.
        /// <br><i>Shortcut of checking on null and using the target.</i></br>
        /// </summary>
        /// <param name="target">If it exists, pass in out the <typeparamref name="T"/> target reference, otherwise null.</param>
        /// <returns>If the <typeparamref name="T"/> target exists return true, otherwise false.</returns>
        public bool TryGetTarget<T>(out T target) where T : Object
        {
            target = GetTarget() as T;
            return target != null;
        }

        /// <summary>
        /// Check if tracker has target.
        /// </summary>
        /// <returns>If the target exists return true, otherwise false.</returns>
        public bool HasTarget()
        {
            return target != null;
        }

        /// <summary>
        /// Lock this window.
        /// </summary>
        public void LockWindow()
        {
            isLocked = true;
        }

        /// <summary>
        /// Called to render button in toolbar area of editor window.
        /// </summary>
        /// <param name="position">Rectangle position.</param>
        protected virtual void ShowButton(Rect position)
        {
            isLocked = GUI.Toggle(position, isLocked, GUIContent.none, Styles.lockButtonStyle);
        }

        /// <summary>
        /// Check if passed target references is valid.
        /// </summary>
        /// <param name="target">Target reference.</param>
        /// <returns>True if valid, otherwise false.</returns>
        protected virtual bool IsValidTarget(Object target)
        {
            return true;
        }

        /// <summary>
        /// Load title TrackerWindowTitle attribute.
        /// </summary>
        private void LoadTitle()
        {
            string name = GetType().Name;

            const string WINDOW_SUFFIX = "Window";
            if (name.EndsWith(WINDOW_SUFFIX, System.StringComparison.OrdinalIgnoreCase))
            {
                name = name.Remove(name.Length - WINDOW_SUFFIX.Length, WINDOW_SUFFIX.Length);
            }

            titleContent = new GUIContent(ObjectNames.NicifyVariableName(name));

            TrackerWindowTitleAttribute titleAttribute = GetType().GetCustomAttribute<TrackerWindowTitleAttribute>();
            if (titleAttribute != null)
            {
                if(!string.IsNullOrWhiteSpace(titleAttribute.IconPath))
                {
                    titleContent.image = EditorResources.Load<Texture2D>(titleAttribute.IconPath);
                }
                titleContent.text = titleAttribute.name;
            }

            System.ObsoleteAttribute obsoleteAttribute = GetType().GetCustomAttribute<System.ObsoleteAttribute>();
            if(obsoleteAttribute != null)
            {
                titleContent.text += " (Obsolete)";
            }
        }

        #region [IHasCustomMenu Implementation]
        /// <summary>
        /// Adds custom menu items to an Editor Window.
        /// </summary>
        public virtual void AddItemsToMenu(GenericMenu menu)
        {
            menu.AddItem(new GUIContent("Locked"), isLocked, () => isLocked = !isLocked);
        }
        #endregion

        #region [Static Methods / Properties]
        /// <summary>
        /// Check if open any of tracker.
        /// </summary>
        /// <returns></returns>
        public static bool HasOpenTrackers()
        {
            return Trackers.Count > 0;
        }

        /// <summary>
        /// Check if open any of tracker <typeparamref name="T"/>.
        /// </summary>
        /// <returns></returns>
        public static bool HasOpenTrackers<T>() where T : TrackerWindow
        {
            foreach (TrackerWindow tracker in ActiveTrackers)
            {
                if(tracker is T)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Get all open this window type instances.
        /// </summary>
        /// <returns>Array of open instances.</returns>
        public static TrackerWindow[] GetTrackers()
        {
            return Trackers.ToArray();
        }

        /// <summary>
        /// Notify all tracker windows, to track specified object.
        /// </summary>
        /// <param name="target">Target reference.</param>
        public static void NotifyTrackEditor(Object target)
        {
            foreach (TrackerWindow tracker in Trackers)
            {
                tracker.TrackEditor(target);
            }
        }

        /// <summary>
        /// Notify all instance of <typeparamref name="T"/> windows, to track specified object.
        /// </summary>
        /// <param name="target">Target reference.</param>
        public static void NotifyTrackEditor<T>(Object target) where T : TrackerWindow
        {
            foreach (TrackerWindow tracker in Trackers)
            {
                if(tracker is T window)
                {
                    window.TrackEditor(target);
                }
            }
        }

        /// <summary>
        /// Notify all instance of <typeparamref name="T"/> windows, to track specified object. 
        /// If there is no active trackers of this type, then create new to track specified object.
        /// </summary>
        /// <param name="target">Target reference.</param>
        public static void NotifyOrCreateTrackEditor<T>(Object target) where T : TrackerWindow
        {
            bool hasTracker = false;
            foreach (TrackerWindow tracker in Trackers)
            {
                if (tracker is T window)
                {
                    window.TrackEditor(target);
                    hasTracker = true;
                }
            }

            if (!hasTracker)
            {
                T tracker = CreateTracker<T>();
                tracker.TrackEditor(target);
            }
        }

        /// <summary>
        /// Create new instance of window.
        /// </summary>
        /// <returns>Instance of window.</returns>
        public static T CreateTracker<T>() where T : TrackerWindow
        {
            try
            {
                T window = CreateInstance<T>();
                window.MoveToCenter();
                window.Show();
                return window;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed created {typeof(T).Name} window, due raised exception: {ex}");
                return null;
            }
        }

        /// <summary>
        /// Return first unlocked window or create new one.
        /// </summary>
        /// <returns>Instance of window.</returns>
        public static T GetOrCreateTracker<T>() where T: TrackerWindow
        {
            if (HasOpenTrackers())
            {
                foreach (TrackerWindow tracker in ActiveTrackers)
                {
                    if(tracker is T window && !window.IsLocked())
                    {
                        return window;
                    }
                }
            }

            return CreateTracker<T>();
        }

        /// <summary>
        /// Open tracker window.
        /// </summary>
        /// <typeparam name="T">Type of tracker window.</typeparam>
        protected static void Open<T>() where T : TrackerWindow
        {
            GetOrCreateTracker<T>().Focus();
        }

        /// <summary>
        /// Iterate through active trackers.
        /// </summary>
        public static IEnumerable<TrackerWindow> ActiveTrackers
        {
            get
            {
                return Trackers;
            }
        }
        #endregion

        #region [Getter / Setter]
        /// <summary>
        /// Check if this window is locked.
        /// </summary>
        /// <returns>Window locked state.</returns>
        public bool IsLocked()
        {
            return isLocked;
        }

        /// <summary>
        /// Current tracked reference.
        /// </summary>
        /// <returns>If exists, target reference. Otherwise null.</returns>
        public Object GetTarget()
        {
            return target;
        }
        #endregion
    }
}

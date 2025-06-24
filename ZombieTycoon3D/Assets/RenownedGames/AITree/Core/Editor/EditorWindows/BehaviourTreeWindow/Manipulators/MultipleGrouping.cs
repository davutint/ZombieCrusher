/* ================================================================
   ----------------------------------------------------------------
   Project   :   AI Tree
   Publisher :   Renowned Games
   Developer :   Tamerlan Shakirov, Zinnur Davleev
   ----------------------------------------------------------------
   Copyright 2022-2023 Renowned Games All rights reserved.
   ================================================================ */


#if UNITY_2022_3_OR_NEWER && !UNITY_6000_0_OR_NEWER
#define HOT_KEY_LISTENER
#endif

using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
#if HOT_KEY_LISTENER
using UnityEditor;
#endif

namespace RenownedGames.AITreeEditor
{
    public class MultipleGrouping : Manipulator
    {
#if HOT_KEY_LISTENER
        private EditorWindow window;
#endif

        /// <summary>
        /// Called to register event callbacks on the target element.
        /// </summary>
        protected override void RegisterCallbacksOnTarget()
        {
#if HOT_KEY_LISTENER
            EditorApplication.update -= HotKeyListener;
            EditorApplication.update += HotKeyListener;
#else
            target.RegisterCallback<KeyDownEvent>(OnKeyDownEvent);
#endif
        }

        /// <summary>
        /// Called to unregister event callbacks from the target element.
        /// </summary>
        protected override void UnregisterCallbacksFromTarget()
        {
#if HOT_KEY_LISTENER
            EditorApplication.update -= HotKeyListener;
#endif
            target.UnregisterCallback<KeyDownEvent>(OnKeyDownEvent);
        }

        /// <summary>
        /// Create group of selected elements.
        /// </summary>
        private void CreateGroup()
        {
            BehaviourTreeGraph graph = target as BehaviourTreeGraph;
            if (graph == null)
            {
                return;
            }

            List<WrapView> selectedViews = graph.selection.OfType<WrapView>().ToList();
            if (selectedViews.Count == 0)
            {
                return;
            }

            HashSet<GroupView> affectedGroups = new HashSet<GroupView>();
            foreach (WrapView wrapView in selectedViews)
            {
                GroupView groupView = wrapView.GetGroup();
                if (groupView != null)
                {
                    affectedGroups.Add(groupView);
                }
            }

            if (affectedGroups.Count == 1)
            {
                GroupView groupView = affectedGroups.First();
                List<GraphElement> groupElements = groupView.containedElements.ToList();

                if (groupElements.Count == selectedViews.Count &&
                    selectedViews.All(s => groupElements.Contains(s)))
                {
                    return;
                }
            }
            else
            {
                GroupView groupView = graph.CreateGroup(Vector2.zero);
                groupView.AddElements(selectedViews);

                if (affectedGroups.Count > 0)
                {
                    List<GroupView> groupsToDelete = new List<GroupView>();
                    foreach (GroupView affectedGroup in affectedGroups)
                    {
                        if (affectedGroup.containedElements.Count() == 0)
                        {
                            groupsToDelete.Add(affectedGroup);
                        }
                    }
                    graph.DeleteElements(groupsToDelete);
                }
            }
        }

        /// <summary>
        /// Built-in Unity key down callback.
        /// </summary>
        /// <param name="evt">Event reference.</param>
        private void OnKeyDownEvent(KeyDownEvent evt)
        {
            if (target == null)
            {
                return;
            }

            if (evt.keyCode == KeyCode.G && (evt.ctrlKey || evt.commandKey))
            {
                CreateGroup();
            }
        }

#if HOT_KEY_LISTENER
        /// <summary>
        /// Hot key callback listener.
        /// </summary>
        private void HotKeyListener()
        {
            if(window == null)
            {
                BehaviourTreeGraph graph = target as BehaviourTreeGraph;
                window = graph.GetWindow();
            }

            if (HotKeyUtility.TryGetEvent(window, out Event evt)
                && evt.type == EventType.KeyDown 
                && evt.keyCode == KeyCode.G
                && (evt.control || evt.command))
            {
                CreateGroup();
            }
        }
#endif
    }
}
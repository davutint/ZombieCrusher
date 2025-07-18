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

using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

#if HOT_KEY_LISTENER
using UnityEditor;
#endif

namespace RenownedGames.AITreeEditor
{
    public class AllSelector : Manipulator
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
        /// Select all graph elements.
        /// </summary>
        private void SelectAll()
        {
            BehaviourTreeGraph graph = target as BehaviourTreeGraph;
            if (graph == null)
            {
                return;
            }

            graph.ClearSelection();
            foreach (GraphElement element in graph.graphElements)
            {
                if (element is ISelectable selectable && element is not Edge)
                {
                    graph.AddToSelection(selectable);
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

            if ((evt.ctrlKey || evt.commandKey) && evt.keyCode == KeyCode.A)
            {
                SelectAll();
            }
        }

#if HOT_KEY_LISTENER
        /// <summary>
        /// Hot key callback listener.
        /// </summary>
        private void HotKeyListener()
        {
            if (window == null)
            {
                BehaviourTreeGraph graph = target as BehaviourTreeGraph;
                window = graph.GetWindow();
            }

            if (HotKeyUtility.TryGetEvent(window, out Event evt)
                && evt.type == EventType.Used
                && evt.keyCode == KeyCode.A
                && (evt.control || evt.command))
            {
                SelectAll();
            }
        }
#endif
    }
}
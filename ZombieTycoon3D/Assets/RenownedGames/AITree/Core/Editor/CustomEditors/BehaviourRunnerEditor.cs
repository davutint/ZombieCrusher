/* ================================================================
   ----------------------------------------------------------------
   Project   :   AI Tree
   Company   :   Renowned Games
   Developer :   Tamerlan Shakirov
   ----------------------------------------------------------------
   Copyright 2022-2023 Renowned Games All rights reserved.
   ================================================================ */

using RenownedGames.AITree;
using RenownedGames.ApexEditor;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace RenownedGames.AITreeEditor
{
    [CustomEditor(typeof(BehaviourRunner))]
    sealed class BehaviourRunnerEditor : Editor
    {
        private BehaviourRunner behaviourRunner;
        private SerializedField serializedTree;

        /// <summary>
        /// Called when the object becomes enabled and active.
        /// </summary>
        private void OnEnable()
        {
            behaviourRunner = target as BehaviourRunner;
            serializedTree = new SerializedField(serializedObject, "sharedBehaviourTree");
            serializedTree.SetLabel(new GUIContent("Behaviour Tree"));
        }

        /// <summary>
        /// Implement this function to make a custom inspector.
        /// </summary>
        public override void OnInspectorGUI() 
        {
            serializedObject.Update();

            Rect rect = EditorGUILayout.GetControlRect(true);
            Event evt = Event.current;
            if(evt.type == EventType.MouseDown
                && evt.button == 0
                && evt.clickCount == 2
                && rect.Contains(evt.mousePosition))
            {
                if (behaviourRunner != null)
                {
                    BehaviourTree treeRef = EditorApplication.isPlaying ? behaviourRunner.GetBehaviourTree() : behaviourRunner.GetSharedBehaviourTree();
                    if (treeRef != null)
                    {
                        TrackerWindow.NotifyOrCreateTrackEditor<BehaviourTreeWindow>(treeRef);
                        evt.Use();
                        GUIUtility.ExitGUI();
                    }
                }
            }

            serializedTree.OnGUI(rect);

            BehaviourTree behaviourTree = behaviourRunner.GetSharedBehaviourTree();
            if (behaviourTree != null)
            {
                HashSet<string> hash = new HashSet<string>();

                behaviourTree.GetRootNode().Traverse(n => 
                {
                    IEnumerable<RequireComponent> requireComponents = n.GetType().GetCustomAttributes<RequireComponent>(true);

                    foreach (RequireComponent requireComponent in requireComponents)
                    {
                        if (requireComponent.m_Type0 != null && behaviourRunner.GetComponent(requireComponent.m_Type0) == null)
                        {
                            hash.Add(requireComponent.m_Type0.Name);
                        }

                        if (requireComponent.m_Type1 != null && behaviourRunner.GetComponent(requireComponent.m_Type1) == null)
                        {
                            hash.Add(requireComponent.m_Type1.Name);
                        }

                        if (requireComponent.m_Type2 != null && behaviourRunner.GetComponent(requireComponent.m_Type2) == null)
                        {
                            hash.Add(requireComponent.m_Type2.Name);
                        }
                    }
                });

                if (hash.Count > 0)
                {
                    string notification = "Add required components:\n";

                    foreach (string component in hash)
                    {
                        notification += $" - {component}\n";
                    }

                    EditorGUILayout.HelpBox(notification, MessageType.Warning);
                }
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
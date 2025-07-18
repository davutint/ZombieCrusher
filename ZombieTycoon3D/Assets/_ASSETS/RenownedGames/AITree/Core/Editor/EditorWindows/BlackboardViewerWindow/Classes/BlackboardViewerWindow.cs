/* ================================================================
   ----------------------------------------------------------------
   Project   :   AI Tree
   Publisher :   Renowned Games
   Developer :   Tamerlan Shakirov
   ----------------------------------------------------------------
   Copyright 2024 Renowned Games All rights reserved.
   ================================================================ */

using RenownedGames.AITree;
using RenownedGames.ApexEditor;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RenownedGames.AITreeEditor
{
    [TrackerWindowTitle("Blackboard Viewer", IconPath = "Images/Icons/Window/BlackboardIcon.png")]
    public sealed class BlackboardViewerWindow : TrackerWindow
    {
        static class Styles
        {
            public static readonly Texture2D EntryTexture;
            public static readonly GUIStyle EntryStyle;
            public static readonly GUIStyle PlaceholderStyle;

            static Styles()
            {
                EntryTexture = CreateTexture(new Color32(64, 64, 64, 255), Color.black);

                PlaceholderStyle = new GUIStyle(GUI.skin.label)
                {
                    fontStyle = FontStyle.Italic,
                    fontSize = 11,
                    normal = { textColor = Color.gray },
                    active = { textColor = Color.gray },
                    focused = { textColor = Color.gray },
                    hover = { textColor = Color.gray }
                };

                Color32 textColor = EditorGUIUtility.isProSkin ? new Color32(200, 200, 200, 255) : new Color32(3, 3, 3, 255);

                EntryStyle = new GUIStyle
                {
                    fontSize = 12,
                    fontStyle = FontStyle.Normal,
                    alignment = TextAnchor.MiddleLeft,
                    border = new RectOffset(2, 2, 2, 2),
                    padding = new RectOffset(10, 0, 0, 0),
                    normal = { 
                        textColor = textColor,
                        background = EntryTexture,
                        scaledBackgrounds = new Texture2D[1] { EntryTexture }
                    },
                    active = {
                        textColor = textColor,
                        background = EntryTexture,
                        scaledBackgrounds = new Texture2D[1] { EntryTexture }
                    },
                    focused = {
                        textColor = textColor,
                        background = EntryTexture,
                        scaledBackgrounds = new Texture2D[1] { EntryTexture }
                    },
                    hover = {
                        textColor = textColor,
                        background = EntryTexture,
                        scaledBackgrounds = new Texture2D[1] { EntryTexture }
                    },
                };
            }

            /// <summary>
            /// Create new square texture with border.
            /// </summary>
            public static Texture2D CreateTexture(Color mainColor, Color borderColor)
            {
                Texture2D texture = new Texture2D(8, 8);

                Color[] colors = new Color[texture.width * texture.height];
                for (int i = 0; i < colors.Length; i++)
                {
                    colors[i] = mainColor;
                }
                texture.SetPixels(colors);

                for (int x = 0; x < texture.width; x++)
                {
                    texture.SetPixel(x, 0, borderColor);
                    texture.SetPixel(x, texture.height - 1, borderColor);
                }

                for (int y = 0; y < texture.height; y++)
                {
                    texture.SetPixel(0, y, borderColor);
                    texture.SetPixel(texture.width - 1, y, borderColor);
                }

                texture.filterMode = FilterMode.Point;
                texture.Apply();

                return texture;
            }
        }

        private BehaviourTree behaviourTree;
        private BehaviourRunner runner;
        private Blackboard blackboard;
        private Blackboard lastBlackboard;
        private string searchText;
        private SearchField searchField;
        private Vector2 scrollPos;
        private List<SerializedField> fields;
        private float viewHeight;

        /// <summary>
        /// Called when the object becomes enabled and active.
        /// </summary>
        protected override void OnEnable()
        {
            base.OnEnable();
            searchText = string.Empty;
            searchField = new SearchField();
            fields = new List<SerializedField>();

            if (!TryGetTarget(out Object target))
            {
                target = Selection.activeObject;
            }

            if (target != null)
            {
                TrackEditor(target);
            }

            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        /// <summary>
        /// Called for rendering and handling GUI events.
        /// </summary>
        private void OnGUI()
        {
            bool hierarchyMode = EditorGUIUtility.hierarchyMode;
            EditorGUIUtility.hierarchyMode = true;
            DrawToolbar();
            DrawContent();
            DrawFooter();
            EditorGUIUtility.hierarchyMode = hierarchyMode;
        }

        /// <summary>
        /// Called at 10 frames per second to give the inspector a chance to update.
        /// </summary>
        private void OnInspectorUpdate()
        {
            if (!HasTarget() && blackboard != null)
            {
                TrackEditor(null);
            }

            if (runner != null)
            {
                blackboard = runner.GetBlackboard();
                if (blackboard == null)
                {
                    blackboard = runner.GetSharedBlackboard();
                }
            }
            else if (behaviourTree != null)
            {
                blackboard = behaviourTree.GetBlackboard();
            }

            if (blackboard != lastBlackboard)
            {
                UpdateFields();
                lastBlackboard = blackboard;
            }

            Repaint();
        }

        /// <summary>
        /// Called when the selection changes.
        /// </summary>
        private void OnSelectionChange()
        {
            TrackEditor(Selection.activeObject);
        }

        /// <summary>
        /// Called when close the window.
        /// </summary>
        protected override void OnDestroy()
        {
            base.OnDestroy();
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
        }

        /// <summary>
        /// Draw toolbar of window.
        /// </summary>
        private void DrawToolbar()
        {
            Rect rect = new Rect(-1, -1, position.width + 2, 22);
            GUI.Box(rect, GUIContent.none, Styles.EntryStyle);

            Rect searchRect = new Rect(rect.xMax - 249, rect.y + 3, 246, 20);
            searchText = searchField.OnToolbarGUI(searchRect, searchText);
        }

        /// <summary>
        /// Draw content of blackboard.
        /// </summary>
        private void DrawContent()
        {
            if(viewHeight == 0)
            {
                viewHeight = fields.Count * 25;
            }

            Rect rect = new Rect(0, 20, position.width, position.height - 20);
            Rect viewRect = new Rect(0, 0, position.width, viewHeight + rect.y);

            if(rect.height < viewRect.height)
            {
                viewRect.width -= 13;
                rect.height -= 20;
                viewRect.height -= 18;
            }

            scrollPos = GUI.BeginScrollView(rect, scrollPos, viewRect);
            {
                Rect entryRect = new Rect(0, 0, viewRect.width, 25);
                GUI.Box(entryRect, GUIContent.none, ApexStyles.BoxEntryEven);

                Rect fieldRect = new Rect(entryRect.x + 4, entryRect.y + 3, entryRect.width - 8, EditorGUIUtility.singleLineHeight);
                EditorGUI.BeginChangeCheck();
                blackboard = (Blackboard)EditorGUI.ObjectField(fieldRect, "Blackboard", blackboard, typeof(Blackboard), true);
                entryRect.y = entryRect.yMax - 1;
                if (EditorGUI.EndChangeCheck())
                {
                    TrackEditor(blackboard);
                }


                if (fields.Count > 0)
                {
                    int index = 0;
                    for (int i = 0; i < fields.Count; i++)
                    {
                        SerializedField field = fields[i];
                        try
                        {
                            field.GetSerializedObject().targetObject.GetType();
                        }
                        catch
                        {
                            fields.RemoveAt(i);
                            i--;
                            continue;
                        }

                        if (string.IsNullOrEmpty(searchText) || field.GetLabel().text.Contains(searchText, StringComparison.OrdinalIgnoreCase))
                        {
                            float height = field.GetHeight();
                            if (height > 25)
                            {
                                entryRect.height = height + 7;
                            }
                            else
                            {
                                entryRect.height = 25;
                            }

                            GUI.Box(entryRect, GUIContent.none, index % 2 == 0 ? ApexStyles.BoxEntryOdd : ApexStyles.BoxEntryEven);

                            fieldRect.y = entryRect.y + 3;
                            fieldRect.height = height;
                            field.GetSerializedObject().Update();
                            field.OnGUI(fieldRect);

                            entryRect.y = entryRect.yMax - 1;
                            index++;
                        }
                    }
                }
                viewHeight = entryRect.y;
            }
            GUI.EndScrollView();
        }

        /// <summary>
        /// Draw footer of window.
        /// </summary>
        private void DrawFooter()
        {
            Rect rect = new Rect(-1, position.height - 22, position.width + 2, 22);

            GUI.Box(rect, GUIContent.none, Styles.EntryStyle);

            rect.x += 4;
            rect.width -= 4;
            GUIContent content = new GUIContent(GetTargetInfo());
            GUI.Label(rect, content, Styles.PlaceholderStyle);

            if (blackboard != null && position.width - Styles.PlaceholderStyle.CalcSize(content).x > 64)
            {
                rect.x = rect.xMax - 64;
                rect.width = 62;
                GUI.Label(rect, "Read-only", Styles.PlaceholderStyle);
            }
        }

        /// <summary>
        /// Called when editor changed play mode state.
        /// </summary>
        /// <param name="mode">New play mode state.</param>
        private void OnPlayModeChanged(PlayModeStateChange mode)
        {
            TrackEditor(GetTarget());
        }

        /// <summary>
        /// Called when this window start tracking specified target reference.
        /// </summary>
        /// <param name="target">Target reference.</param>
        protected override void OnTrackEditor(Object target)
        {
            TryGetBlackboard(out blackboard);
            UpdateFields();
            Repaint();
        }

        /// <summary>
        /// Check if passed target references is valid.
        /// </summary>
        /// <param name="target">Target reference.</param>
        /// <returns>True if valid, otherwise false.</returns>
        protected override bool IsValidTarget(Object target)
        {
            return target is Blackboard || target is BehaviourTree || target is BehaviourRunner || (target is GameObject go && go.GetComponent<BehaviourRunner>());
        }

        /// <summary>
        /// Try get blackboard reference from target.
        /// </summary>
        /// <param name="blackboard">Blackboard reference.</param>
        /// <returns>True if blackboard is exists. Otherwise false.</returns>
        public bool TryGetBlackboard(out Blackboard blackboard)
        {
            blackboard = null;
            if (TryGetTarget(out Object target))
            {
                if (target is Blackboard)
                {
                    blackboard = target as Blackboard;
                }
                else if (target is BehaviourTree behaviourTree && behaviourTree.GetBlackboard() != null)
                {
                    blackboard = behaviourTree.GetBlackboard();
                }
                else if (target is BehaviourRunner runner ||
                    target is GameObject go && go.TryGetComponent(out runner))
                {
                    blackboard = EditorApplication.isPlaying ? runner.GetBlackboard() : runner.GetSharedBlackboard();
                }
            }
            return blackboard != null;
        }

        /// <summary>
        /// Current information in string representation of tracked target.
        /// </summary>
        /// <returns></returns>
        public string GetTargetInfo()
        {
            StringBuilder sb = new StringBuilder();
            if (blackboard != null)
            {
                sb.Append(blackboard.name);

                bool isShared = AssetDatabase.IsNativeAsset(blackboard);
                sb.Append(isShared ? " (Shared)" : " (Associated)");

                if (runner != null)
                {
                    sb.AppendFormat(" of {0} object", runner.name);
                }
                else if (behaviourTree != null)
                {
                    sb.AppendFormat(" of {0} asset", behaviourTree.name);
                }
            }
            else
            {
                sb.Append("Select blackboard...");
            }

            return sb.ToString();
        }

        /// <summary>
        /// Collect fields of current blackboard.
        /// </summary>
        private void UpdateFields()
        {
            fields.Clear();
            if (blackboard != null)
            {
                foreach (Key key in blackboard.Keys)
                {
                    SerializedObject serialziedObject = new SerializedObject(key);
                    SerializedField field = new SerializedField(serialziedObject, "value");
                    field.SetLabel(new GUIContent(key.name));
                    fields.Add(field);
                }
            }
            fields.TrimExcess();
        }

        #region [Static]
        /// <summary>
        /// Open Blackboard Viewer window.
        /// </summary>
        [MenuItem("Tools/AI Tree/Windows/Blackboard Viewer", false, 25)]
        public static void Open()
        {
            Open<BlackboardViewerWindow>();
        }
        #endregion
    }
}
/* ================================================================
   ----------------------------------------------------------------
   Project   :   AI Tree
   Publisher :   Renowned Games
   Developer :   Tamerlan Shakirov
   ----------------------------------------------------------------
   Copyright 2022-2023 Renowned Games All rights reserved.
   ================================================================ */

using RenownedGames.AITree;
using UnityEditor;
using UnityEngine;

namespace RenownedGames.AITreeEditor
{
    [TrackerWindowTitle("Node Inspector", IconPath = "Images/Icons/Window/NodeInspectorIcon.png")]
    public class NodeInspectorWindow : TrackerWindow
    {
        static class Styles
        {
            public static readonly GUIStyle ToolbarStyle;
            public static readonly GUIStyle ToolbarLabelStyle;

            static Styles()
            {
                ToolbarStyle = new GUIStyle("preToolbar")
                {
                    fontSize = 15,
                    alignment = TextAnchor.MiddleLeft,
                    padding = new RectOffset(10, 0, 10, 10),
                    stretchWidth = true,
                    stretchHeight = true,
                    fixedWidth = 0,
                    fixedHeight = 0
                };

                ToolbarLabelStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 13,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleLeft,
                    padding = new RectOffset(0, 0, 0, 0),
                    wordWrap = true,
                    stretchWidth = true,
                    stretchHeight = true,
                    fixedWidth = 0,
                    fixedHeight = 0
                };
            }
        }

        private Editor editor;
        private MonoScript monoScript;
        private Vector2 scrollPos;
        private GUIContent headerContent;

        /// <summary>
        /// Called when open window.
        /// </summary>
        protected override void OnEnable()
        {
            base.OnEnable();
            Undo.undoRedoPerformed += RepaintForce;
        }

        /// <summary>
        /// Called for rendering and handling window GUI.
        /// </summary>
        protected virtual void OnGUI()
        {
            if (GetTarget() != null && editor != null)
            {
                DrawHeader(headerContent);
                scrollPos = GUILayout.BeginScrollView(scrollPos);
                {
                    GUILayout.BeginHorizontal();
                    {
                        GUILayout.Space(18);
                        GUILayout.BeginVertical();
                        {
                            GUILayout.Space(4);
                            bool hierarchyMode = EditorGUIUtility.hierarchyMode;
                            EditorGUIUtility.hierarchyMode = true;
                            editor.OnInspectorGUI();
                            EditorGUIUtility.hierarchyMode = hierarchyMode;
                        }
                        GUILayout.EndVertical();
                        GUILayout.Space(4);
                    }
                    GUILayout.EndHorizontal();
                }
                GUILayout.EndScrollView();
            }
        }

        /// <summary>
        /// Called when close the window.
        /// </summary>
        protected override void OnDestroy() 
        {
            base.OnDestroy();
            Undo.undoRedoPerformed -= RepaintForce;
        }

        /// <summary>
        /// Called at 10 frames per second to give the inspector a chance to update.
        /// </summary>
        protected virtual void OnInspectorUpdate()
        {
            Repaint();
        }

        /// <summary>
        /// Draw header on node inspector window.
        /// </summary>
        /// <param name="title">Header title.</param>
        /// <param name="icon">Header icon.</param>
        protected virtual void DrawHeader(GUIContent content)
        {
            const float HEIGHT = 52;

            Rect headerRect = GUILayoutUtility.GetRect(0, HEIGHT);
            headerRect.y -= 1;
            headerRect.height += 1;

            GUI.Box(headerRect, GUIContent.none, Styles.ToolbarStyle);

            if(content != null)
            {
                float iconSize = 0;

                headerRect.x = headerRect.xMin + 10;
                if (content.image != null)
                {
                    iconSize = 35;
                    headerRect.y += 10;
                    headerRect.width = iconSize;
                    headerRect.height = iconSize;

                    GUI.DrawTexture(headerRect, content.image, ScaleMode.ScaleToFit);

                    headerRect.x = headerRect.xMax + 7;
                    headerRect.y -= 10;
                }

                headerRect.width = position.width - iconSize;
                headerRect.height = HEIGHT;
                GUI.Label(headerRect, content.text, Styles.ToolbarLabelStyle);
            }
        }

        /// <summary>
        /// Called when this window start tracking specified target reference.
        /// </summary>
        /// <param name="target">Target reference.</param>
        protected override void OnTrackEditor(Object target)
        {
            if (target != null)
            {
                editor = Editor.CreateEditor(target);
                monoScript = GetMonoScript(target);
                headerContent = GetHeaderContent(target, monoScript);
            }
            else
            {
                editor = null;
                monoScript = null;
                headerContent = null;
            }
        }

        /// <summary>
        /// Check if passed target references is valid.
        /// </summary>
        /// <param name="target">Target reference.</param>
        /// <returns>True if valid, otherwise false.</returns>
        protected override bool IsValidTarget(Object target)
        {
            return target is MonoBehaviour || target is ScriptableObject;
        }

        /// <summary>
        /// Force repaint tracked target editor.
        /// </summary>
        public void RepaintForce()
        {
            if (TryGetTarget(out Object target))
            {
                editor = Editor.CreateEditor(target);
            }
        }

        #region [IHasCustomMenu Implementation]
        public override void AddItemsToMenu(GenericMenu menu)
        {
            if(TryGetTarget(out Object target))
            {
                menu.AddItem(new GUIContent("Ping"), false, () => EditorGUIUtility.PingObject(target));
            }
            else
            {
                menu.AddDisabledItem(new GUIContent("Ping"));
            }

            if(monoScript != null)
            {
                menu.AddItem(new GUIContent("Edit Script"), false, () => AssetDatabase.OpenAsset(monoScript));
            }
            else
            {
                menu.AddDisabledItem(new GUIContent("Edit Script"));
            }
            base.AddItemsToMenu(menu);
        }
        #endregion

        #region [Static]
        [MenuItem("Tools/AI Tree/Windows/Node Inspector", false, 21)]
        public static void Open()
        {
            Open<NodeInspectorWindow>();
        }

        /// <summary>
        /// Get mono script for target.
        /// </summary>
        internal static MonoScript GetMonoScript(Object target)
        {
            if (target is MonoBehaviour monoBehaviour)
            {
                return MonoScript.FromMonoBehaviour(monoBehaviour);
            }
            else if (target is ScriptableObject scriptableObject)
            {
                return MonoScript.FromScriptableObject(scriptableObject);
            }
            throw new System.Exception("Unsupported type! Target type must be MonoBehaviour or ScriptableObject.");
        }

        /// <summary>
        /// Get header content for target.
        /// </summary>
        /// <returns></returns>
        internal static GUIContent GetHeaderContent(Object target, MonoScript monoScript)
        {
            if (target != null && monoScript != null)
            {
                if (target is MonoBehaviour monoBehaviour)
                {
                    return LoadObjectContent(target, monoScript);
                }
                else if (target is ScriptableObject scriptableObject)
                {
                    if (scriptableObject is Node node)
                    {
                        return LoadNodeContent(node, monoScript);
                    }
                    else
                    {
                        return LoadObjectContent(target, monoScript);
                    }
                }
            }
            return GUIContent.none;
        }

        /// <summary>
        /// Load node content for specified target and monoScript.
        /// </summary>
        internal static GUIContent LoadNodeContent(Node node, MonoScript monoScript)
        {
            NodeTypeCache.NodeCollection nodeInfos = NodeTypeCache.GetNodesInfo();
            for (int i = 0; i < nodeInfos.Count; i++)
            {
                NodeTypeCache.NodeInfo nodeInfo = nodeInfos[i];
                if (nodeInfo.type == node.GetType())
                {
                    GUIContent content = new GUIContent();
                    string name = "Undefined";
                    if (nodeInfo.contentAttribute != null)
                    {
                        if (!string.IsNullOrWhiteSpace(nodeInfo.contentAttribute.name))
                        {
                            name = nodeInfo.contentAttribute.name;
                        }
                        else if (!string.IsNullOrWhiteSpace(nodeInfo.contentAttribute.path))
                        {
                            name = System.IO.Path.GetFileName(nodeInfo.contentAttribute.path);
                        }
                    }

                    content.text = $"{name} ({ObjectNames.NicifyVariableName(monoScript.name)})";
                    content.image = nodeInfo.icon;
                    return content;
                }
            }
            return null;
        }

        /// <summary>
        /// Load node content for specified target and monoScript.
        /// </summary>
        internal static GUIContent LoadNodeContent(Node node)
        {
            NodeTypeCache.NodeCollection nodeInfos = NodeTypeCache.GetNodesInfo();
            for (int i = 0; i < nodeInfos.Count; i++)
            {
                NodeTypeCache.NodeInfo nodeInfo = nodeInfos[i];
                if (nodeInfo.type == node.GetType())
                {
                    GUIContent content = new GUIContent();
                    string name = "Undefined";
                    if (nodeInfo.contentAttribute != null)
                    {
                        if (!string.IsNullOrWhiteSpace(nodeInfo.contentAttribute.name))
                        {
                            name = nodeInfo.contentAttribute.name;
                        }
                        else if (!string.IsNullOrWhiteSpace(nodeInfo.contentAttribute.path))
                        {
                            name = System.IO.Path.GetFileName(nodeInfo.contentAttribute.path);
                        }
                    }

                    content.text = name;
                    content.image = nodeInfo.icon;
                    return content;
                }
            }
            return null;
        }

        /// <summary>
        /// Load object content for specified target and monoScript.
        /// </summary>
        internal static GUIContent LoadObjectContent(Object target, MonoScript monoScript)
        {
            GUIContent content = new GUIContent();
            content.text = $"{target.name} ({ObjectNames.NicifyVariableName(monoScript.name)})";
            content.image = AssetPreview.GetMiniThumbnail(target);
            return content;
        }
        #endregion

        #region [Getter / Setter]
        public Editor GetEditor()
        {
            return editor;
        }

        public GUIContent GetHeaderContent()
        {
            return headerContent;
        }
        #endregion
    }
}

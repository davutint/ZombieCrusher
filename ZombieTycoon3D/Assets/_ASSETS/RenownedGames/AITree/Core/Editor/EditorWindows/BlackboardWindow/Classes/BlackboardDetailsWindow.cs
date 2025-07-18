/* ================================================================
   ----------------------------------------------------------------
   Project   :   AI Tree
   Publisher :   Renowned Games
   Developer :   Zinnur Davleev
   ----------------------------------------------------------------
   Copyright 2022-2023 Renowned Games All rights reserved.
   ================================================================ */

using RenownedGames.AITree;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace RenownedGames.AITreeEditor
{
    [TrackerWindowTitle("Blackboard Details", IconPath = "Images/Icons/Window/BlackboardIcon.png")]
    public sealed class BlackboardDetailsWindow : TrackerWindow
    {
        [SerializeField]
        private Key key;

        [SerializeField]
        private Blackboard blackboard;

        private SerializedObject serializedKey;
        private Label blackboardName;
        private Label typeField;
        private IMGUIContainer preContainer;
        private IMGUIContainer postContainer;
        private VisualElement typeColor;
        private IMGUIContainer parentContainer;
        private ScrollView scrollView;

        /// <summary>
        /// Called when the object becomes enabled and active.
        /// </summary>
        protected override void OnEnable()
        {
            base.OnEnable();

            LoadVisualElements();

            if (key != null)
            {
                serializedKey = new SerializedObject(key);
            }
            else 
            {
                if (!TryGetTarget(out Object target))
                {
                    target = Selection.activeObject;
                }

                if (target != null)
                {
                    TrackEditor(target);
                }
            }

            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        /// <summary>
        /// Called when the window gets keyboard focus.
        /// </summary>
        private void OnFocus()
        {
            if (HasUnloadedVisualElements())
            {
                LoadVisualElements();
            }
        }

        /// <summary>
        /// Called for rendering and handling GUI events.
        /// </summary>
        private void OnGUI()
        {
            scrollView.style.display = key != null ? DisplayStyle.Flex : DisplayStyle.None;
            if (key != null)
            {
                typeColor.style.backgroundColor = new StyleColor(key.GetColor());
                typeField.text = key.GetValueType().Name;

                bool hierarchyMode = EditorGUIUtility.hierarchyMode;
                EditorGUIUtility.hierarchyMode = true;
                typeColor.style.left = EditorGUIUtility.labelWidth;

                const float SPACE = 21;
                typeField.style.left = EditorGUIUtility.labelWidth + SPACE;
                EditorGUIUtility.hierarchyMode = hierarchyMode;
            }
            Repaint();
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
        /// Called whenever the selection has changed.
        /// </summary>
        private void OnSelectionChange()
        {
            TrackEditor(Selection.activeObject);
        }

        /// <summary>
        /// Called when this window start tracking specified target reference.
        /// </summary>
        /// <param name="target">Target reference.</param>
        protected override void OnTrackEditor(Object target)
        {
            if (HasUnloadedVisualElements())
            {
                LoadVisualElements();
            }

            if (TryGetKey(out Key key, out Blackboard blackboard))
            {
                this.key = key;
                this.blackboard = blackboard;

                serializedKey = new SerializedObject(key);
                blackboardName.text = ObjectNames.NicifyVariableName(blackboard.name);
            }
            else
            {
                this.key = null;
                this.blackboard = null;
                serializedKey = null;

                const string DEFAULT_NAME = "BLACKBOARD";
                blackboardName.text = DEFAULT_NAME;
            }
            Repaint();
        }

        /// <summary>
        /// Check if passed target references is valid.
        /// </summary>
        /// <param name="target">Target reference.</param>
        /// <returns>True if valid, otherwise false.</returns>
        protected override bool IsValidTarget(Object target)
        {
            return target is Key || target is Blackboard || target is BehaviourTree || target is BehaviourRunner || (target is GameObject go && go.GetComponent<BehaviourRunner>());
        }

        /// <summary>
        /// Try get key reference from target.
        /// </summary>
        /// <param name="key">Key reference.</param>
        /// <returns>True if key is exists. Otherwise false.</returns>
        public bool TryGetKey(out Key key, out Blackboard blackboard)
        {
            key = null;
            blackboard = null;
            if(TryGetTarget(out Object target))
            {
                if (target is Key)
                {
                    key = target as Key;
                    string path = AssetDatabase.GetAssetPath(key.GetInstanceID());
                    Object asset = AssetDatabase.LoadAssetAtPath<Object>(path);
                    if (asset is Blackboard)
                    {
                        blackboard = (Blackboard)asset;
                        blackboardName.text = ObjectNames.NicifyVariableName(blackboard.name);
                    }
                    else
                    {
                        int result = EditorUtility.DisplayDialogComplex("AI Tree", "It seems that this key is broken or incorrectly imported. Contact support for more information.", "Skip", "Delete Key", "Contact Support");
                        if (result == 2)
                        {
                            if (EditorUtility.DisplayDialog("AI Tree", $"Are you sure you want to delete {key.name}? This item will be deleted immediately. You can't undo this action.", "Yes", "No"))
                            {
                                if (!AssetDatabase.DeleteAsset(path))
                                {
                                    Destroy(key);
                                }
                                AssetDatabase.Refresh();
                            }
                        }
                    }
                }
                else if (target is Blackboard)
                {
                    blackboard = (Blackboard)target;
                    blackboard.TryFindKey("Self", out key);
                }
                else if(target is BehaviourTree behaviourTree)
                {
                    blackboard = behaviourTree.GetBlackboard();
                    if(blackboard != null)
                    {
                        blackboard.TryFindKey("Self", out key);
                    }
                }
                else if(target is BehaviourRunner runner ||
                    target is GameObject go && go.TryGetComponent(out runner))
                {
                    blackboard = EditorApplication.isPlaying ? runner.GetBlackboard() : runner.GetSharedBlackboard();
                    if(blackboard != null)
                    {
                        blackboard.TryFindKey("Self", out key);
                    }
                }
            }
            return key != null;
        }

        /// <summary>
        /// Called when on pre-container IMGUI rendering.
        /// </summary>
        private void OnPreGUI()
        {
            if (IsSerialziedKeyValid())
            {
                serializedKey.Update();

                bool repaint = false;
                bool hierarchyMode = EditorGUIUtility.hierarchyMode;

                EditorGUIUtility.hierarchyMode = true;
                EditorGUI.BeginChangeCheck();
                EditorGUI.BeginDisabledGroup(key is SelfKey);
                SerializedProperty name = serializedKey.FindProperty("m_Name");
                Rect nameRect = EditorGUILayout.GetControlRect(true);
                string newName = EditorGUI.DelayedTextField(nameRect, "Name", name.stringValue);
                EditorGUI.EndDisabledGroup();
                if (EditorGUI.EndChangeCheck())
                {
                    newName = newName.Trim();
                    if (newName != "Self")
                    {
                        if (blackboard.Contains(newName))
                        {
                            EditorUtility.DisplayDialog("AI Tree", "A key with this name already exists.", "Ok");
                        }
                        else
                        {
                            name.stringValue = newName;
                            repaint = true;
                        }
                    }
                    else
                    {
                        EditorUtility.DisplayDialog("AI Tree", "Self key name is reserved by the system.", "Ok");
                    }
                }

                EditorGUI.BeginChangeCheck();
                SerializedProperty category = serializedKey.FindProperty("category");
                Rect categoryRect = EditorGUILayout.GetControlRect(true);
                category.stringValue = EditorGUI.DelayedTextField(categoryRect, "Category", category.stringValue);
                if (EditorGUI.EndChangeCheck())
                {
                    repaint = true;
                }

                SerializedProperty description = serializedKey.FindProperty("description");
                Rect textAreaRect = EditorGUILayout.GetControlRect(true, 50);
                textAreaRect = EditorGUI.PrefixLabel(textAreaRect, new GUIContent("Description"));

                GUIStyle style = new GUIStyle(GUI.skin.textField);
                style.wordWrap = true;
                description.stringValue = EditorGUI.TextArea(textAreaRect, description.stringValue, style);

                if (serializedKey.hasModifiedProperties)
                {
                    serializedKey.ApplyModifiedProperties();
                }

                if (repaint && HasOpenInstances<BlackboardWindow>())
                {
                    GetWindow<BlackboardWindow>().RefreshKeys();
                }
                EditorGUIUtility.hierarchyMode = hierarchyMode;
            }
        }

        /// <summary>
        /// Called when on post-container IMGUI rendering.
        /// </summary>
        private void OnPostGUI()
        {
            if (IsSerialziedKeyValid())
            {
                bool hierarchyMode = EditorGUIUtility.hierarchyMode;
                EditorGUIUtility.hierarchyMode = true;
                EditorGUI.BeginDisabledGroup(key is SelfKey);
                EditorGUI.BeginChangeCheck();
                SerializedProperty sync = serializedKey.FindProperty("sync");
                Rect syncRect = EditorGUILayout.GetControlRect(true);
                sync.boolValue = EditorGUI.Toggle(syncRect, "Sync", sync.boolValue);
                if (EditorGUI.EndChangeCheck())
                {
                    serializedKey.ApplyModifiedProperties();
                }
                EditorGUI.EndDisabledGroup();
                EditorGUIUtility.hierarchyMode = hierarchyMode;
            }
        }

        /// <summary>
        /// Called when on parent container IMGUI rendering.
        /// </summary>
        private void OnParentGUI()
        {
            if (blackboard != null)
            {
                bool hierarchyMode = EditorGUIUtility.hierarchyMode;
                EditorGUIUtility.hierarchyMode = true;
                Blackboard parent = (Blackboard)EditorGUILayout.ObjectField(new GUIContent("Parent"), blackboard.GetParent(), typeof(Blackboard), false);
                EditorGUIUtility.hierarchyMode = hierarchyMode;

                if (parent != blackboard.GetParent() && (parent == null || !parent.IsNested(blackboard)))
                {
                    blackboard.SetParent(parent);
                    GetWindow<BlackboardWindow>().TrackEditor(blackboard);
                }
            }
        }

        /// <summary>
        /// Load all required Visual Elements.
        /// </summary>
        private void LoadVisualElements()
        {
            AITreeSettings settings = AITreeSettings.instance;

            rootVisualElement.Clear();

            VisualTreeAsset visualTree = settings.GetBlackboardDetailsUXML();
            visualTree.CloneTree(rootVisualElement);

            rootVisualElement.styleSheets.Add(settings.GetBlackboardDetailsUSS());

            preContainer = rootVisualElement.Q<IMGUIContainer>("pre-container");
            postContainer = rootVisualElement.Q<IMGUIContainer>("post-container");
            blackboardName = rootVisualElement.Q<Label>("blackboard-name");
            typeField = rootVisualElement.Q<Label>("type-field");
            typeColor = rootVisualElement.Q<VisualElement>("type-color");
            parentContainer = rootVisualElement.Q<IMGUIContainer>("parent-container");
            scrollView = rootVisualElement.Q<ScrollView>("scrollview");

            scrollView.horizontalScrollerVisibility = ScrollerVisibility.Hidden;

            preContainer.onGUIHandler = OnPreGUI;
            postContainer.onGUIHandler = OnPostGUI;
            parentContainer.onGUIHandler = OnParentGUI;
        }

        /// <summary>
        /// Check if window has new or unloaded visual elements.
        /// </summary>
        /// <returns>True if has new or unloaded visual elements, otherwise false.</returns>
        private bool HasUnloadedVisualElements()
        {
            return preContainer == null
                || postContainer == null
                || typeField == null
                || typeColor == null
                || parentContainer == null
                || scrollView == null;
        }

        /// <summary>
        /// Check if serialized key is valid to use.
        /// </summary>
        private bool IsSerialziedKeyValid()
        {
            try
            {
                if (serializedKey != null && serializedKey.targetObject != null)
                {
                    return true;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        #region [Callbacks]
        /// <summary>
        /// Called when play mode state change.
        /// </summary>
        /// <param name="state">Enumeration specifying a change in the Editor's play mode state. See Also: PauseState, EditorApplication.playModeStateChanged, EditorApplication.isPlaying.</param>
        private void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                TrackEditor(GetTarget());
            }
            else if (state == PlayModeStateChange.EnteredEditMode)
            {
                TrackEditor(GetTarget());
            }
        }
        #endregion

        #region [Static Methods]
        [MenuItem("Tools/AI Tree/Windows/Blackboard Details", false, 23)]
        public static void Open()
        {
            Open<BlackboardDetailsWindow>();
        }
        #endregion
    }
}
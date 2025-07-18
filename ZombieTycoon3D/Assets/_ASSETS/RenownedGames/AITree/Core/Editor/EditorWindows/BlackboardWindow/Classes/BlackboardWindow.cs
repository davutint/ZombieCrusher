/* ================================================================
   ----------------------------------------------------------------
   Project   :   AI Tree
   Publisher :   Renowned Games
   Developer :   Zinnur Davleev
   ----------------------------------------------------------------
   Copyright 2022-2023 Renowned Games All rights reserved.
   ================================================================ */

using RenownedGames.AITree;
using RenownedGames.AITreeEditor.UIElements;
using RenownedGames.ExLibEditor;
using RenownedGames.ExLibEditor.Windows;
using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace RenownedGames.AITreeEditor
{
    [TrackerWindowTitle("Blackboard", IconPath = "Images/Icons/Window/BlackboardIcon.png")]
    public sealed class BlackboardWindow : TrackerWindow
    {
        private string searchText;
        private ToolbarButton toolbarNewKey;
        private Label blackboardName;
        private TextField searchInput;
        private VisualElement searchPlaceholder;
        private ListElement inheritedKeyList;
        private ListElement keyList;
        private Foldout inheritKeysFoldout;
        private Event keyboardEvent;
        private Key selectedKey;
        private VisualElement selectedElement;

        /// <summary>
        /// Called when the object becomes enabled and active.
        /// </summary>
        protected override void OnEnable()
        {
            base.OnEnable();

            LoadVisualElements();
            RefreshKeys();

            if (!TryGetTarget(out Object target))
            {
                target = Selection.activeObject;
            }

            if (target != null)
            {
                TrackEditor(target);
            }

            EditorApplication.update -= CheckDeleteCallback;
            EditorApplication.update += CheckDeleteCallback;

            Undo.undoRedoPerformed -= OnUndoRedo;
            Undo.undoRedoPerformed += OnUndoRedo;

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
            keyboardEvent = Event.current;

            if(TryGetBlackboard(out Blackboard blackboard))
            {
                searchPlaceholder.style.display = string.IsNullOrEmpty(searchInput.text) ? DisplayStyle.Flex : DisplayStyle.None;
                inheritedKeyList.style.display = blackboard != null ? DisplayStyle.Flex : DisplayStyle.None;
                keyList.style.display = blackboard != null ? DisplayStyle.Flex : DisplayStyle.None;

                if (searchText != searchInput.text)
                {
                    searchText = searchInput.text;
                    RefreshKeys();
                }
            }
        }

        /// <summary>
        /// Called whenever the selection has changed.
        /// </summary>
        private void OnSelectionChange()
        {
            TrackEditor(Selection.activeObject);
        }

        /// <summary>
        /// This function is called when the behaviour becomes disabled.
        /// </summary>
        protected override void OnDestroy()
        {
            base.OnDestroy();
            EditorApplication.update -= CheckDeleteCallback;
        }

        /// <summary>
        /// Start tracking specified blackboard.
        /// </summary>
        /// <param name="target">Blackboard reference.</param>
        protected override void OnTrackEditor(Object target)
        {
            if (HasUnloadedVisualElements())
            {
                LoadVisualElements();
            }

            if (TryGetBlackboard(out Blackboard blackboard))
            {
                blackboardName.text = ObjectNames.NicifyVariableName(blackboard.name);
                blackboard.InitializeSelfKey();
                RefreshKeys();
            }
            else
            {
                const string DEFAULT_NAME = "BLACKBOARD";
                blackboardName.text = DEFAULT_NAME;
                RefreshKeys();
            }
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
            if(TryGetTarget(out Object target))
            {
                if(target is Blackboard)
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
        /// Check if target is blackboard or has blackboard reference.
        /// </summary>
        /// <returns>True if target is blackboard or has blackboard reference. Otherwise false.</returns>
        public bool HasBlackboard()
        {
            return TryGetBlackboard(out _);
        }

        /// <summary>
        /// Refresh all key instance.
        /// </summary>
        public void RefreshKeys()
        {
            inheritKeysFoldout.style.display = DisplayStyle.None;
            keyList.style.display = DisplayStyle.None;
            inheritedKeyList.ClearItems();
            keyList.ClearItems();

            if (HasBlackboard())
            {
                ClearNullKeys();
                InitializeInheritKeys();
                InitializeKeys();
            }

            Repaint();
        }

        /// <summary>
        /// Show new key creation search window.
        /// </summary>
        public void ShowKeyCreationWindow()
        {
            if (!HasBlackboard())
            {
                if (EditorUtility.DisplayDialog("AI Tree", "Blackboard is not selected, select Blackboard or create a new one.", "Create", "Cancel"))
                {
                    Texture2D icon = EditorResources.LoadExact<Texture2D>("RenownedGames/AITree", "Images/Icons/ScriptableObject/BlackboardIcon.png");
                    ProjectWindowUtility.CreateScriptableObject<Blackboard>("NewBlackboard", icon);
                }
                return;
            }

            ExSearchWindow searchWindow = ExSearchWindow.Create("Nodes");

            foreach (Type type in TypeCache.GetTypesDerivedFrom<Key>())
            {
                if (type.IsAbstract || type.IsGenericType || type == typeof(SelfKey)) continue;

                string name = type.Name;

                const string KEY_SUFFIX = "Key";
                if (name.EndsWith(KEY_SUFFIX, StringComparison.OrdinalIgnoreCase))
                {
                    int index = name.LastIndexOf(KEY_SUFFIX);
                    name = name.Remove(index, KEY_SUFFIX.Length);
                }

                if (type.GetCustomAttribute<ObsoleteAttribute>() != null)
                {
                    name = $"Deprecated/{name}";
                }

                searchWindow.AddEntry(new GUIContent(name), () =>
                {
                    if(TryGetBlackboard(out Blackboard blackboard))
                    {
                        BlackboardUtility.AddKey(blackboard, type);

                        foreach (TrackerWindow tracker in ActiveTrackers)
                        {
                            if (tracker is BlackboardWindow window && window.TryGetBlackboard(out Blackboard bb) && bb == blackboard)
                            {
                                window.RefreshKeys();
                            }
                        }
                    }
                });
            }

            Rect buttonRect = toolbarNewKey.contentRect;
            buttonRect.x += 88;
            buttonRect.y += 2 + buttonRect.height;
            searchWindow.Open(buttonRect);
        }

        /// <summary>
        /// Initialize inherit blackboard keys.
        /// </summary>
        private void InitializeInheritKeys()
        {
            if(!TryGetBlackboard(out Blackboard blackboard))
            {
                return;
            }

            inheritKeysFoldout.style.display = DisplayStyle.None;
            inheritedKeyList.ClearItems();

            if (blackboard.GetParent() != null)
            {
                inheritKeysFoldout.style.display = DisplayStyle.Flex;

                if (blackboard.GetParent().GetKeys().Count > 0)
                {
                    inheritedKeyList.style.display = DisplayStyle.Flex;

                    foreach (Key key in blackboard.GetParent().GetKeys())
                    {
                        if (key.name.ToLower().Contains(searchInput.text.ToLower()))
                        {
                            string name = key.name;
                            if (key.GetType().GetCustomAttribute<ObsoleteAttribute>() != null)
                            {
                                name += " (Deprecated)";
                            }

                            ListItem item = new ListItem(name, key.GetCategory(), key.GetDescription(), key.GetColor(), 
                                
                            (element) =>
                            {
                                OnSelectKey(key, element);
                                NotifyOrCreateTrackEditor<BlackboardDetailsWindow>(key);
#pragma warning disable CS0618 // Key Details window is obsolete
                                NotifyTrackEditor<KeyDetailsWindow>(key);
#pragma warning restore CS0618 // Key Details window is obsolete
                            }, 
                            
                            (element) =>
                            {
                                if (key is not SelfKey)
                                {
                                    if (BlackboardUtility.DeleteKey(blackboard, key))
                                    {
                                        selectedKey = null;
                                        selectedElement = null;

                                        RefreshKeys();
                                    }
                                }
                                else
                                {
                                    EditorUtility.DisplayDialog("AI Tree", "Self is a system key and cannot be deleted.", "Ok");
                                }
                            },

                            (element) =>
                            {
                                if (key is not SelfKey)
                                {
                                    BlackboardUtility.DuplicateKey(blackboard, key);
                                    selectedKey = null;
                                    selectedElement = null;
                                    RefreshKeys();
                                }
                                else
                                {
                                    EditorUtility.DisplayDialog("AI Tree", "Self is a system key and cannot be duplicated.", "Ok");
                                }
                            });

                            inheritedKeyList.AddItem(item);
                        }
                    }
                }
            }

            inheritedKeyList.Initialize();
        }

        /// <summary>
        /// Initialize blackboard keys.
        /// </summary>
        private void InitializeKeys()
        {
            if (!TryGetBlackboard(out Blackboard blackboard))
            {
                return;
            }

            keyList.style.display = DisplayStyle.None;
            keyList.ClearItems();

            if (blackboard.GetKeys().Count > 0)
            {
                keyList.style.display = DisplayStyle.Flex;

                foreach (Key key in blackboard.GetKeys())
                {
                    if (key.name.ToLower().Contains(searchInput.text.ToLower()))
                    {
                        string name = key.name;
                        if (key.GetType().GetCustomAttribute<ObsoleteAttribute>() != null)
                        {
                            name += " (Deprecated)";
                        }

                        ListItem item = new ListItem(name, key.GetCategory(), key.GetDescription(), key.GetColor(), (element) =>
                        {
                            OnSelectKey(key, element);
                            NotifyOrCreateTrackEditor<BlackboardDetailsWindow>(key);
#pragma warning disable CS0618 // Key Details window is obsolete
                            NotifyTrackEditor<KeyDetailsWindow>(key);
#pragma warning restore CS0618 // Key Details window is obsolete
                        },
                        (element) =>
                        {
                            if (key is not SelfKey)
                            {
                                if (BlackboardUtility.DeleteKey(blackboard, key))
                                {
                                    selectedKey = null;
                                    selectedElement = null;

                                    RefreshKeys();
                                }
                            }
                            else
                            {
                                EditorUtility.DisplayDialog("AI Tree", "Self is a system key and cannot be deleted.", "Ok");
                            }
                        },

                        (element) =>
                        {
                            if (key is not SelfKey)
                            {
                                BlackboardUtility.DuplicateKey(blackboard, key);
                                selectedKey = null;
                                selectedElement = null;
                                RefreshKeys();
                            }
                            else
                            {
                                EditorUtility.DisplayDialog("AI Tree", "Self is a system key and cannot be duplicated.", "Ok");
                            }
                        });

                        keyList.AddItem(item);
                    }
                }
            }

            keyList.Initialize();
        }

        /// <summary>
        /// Clear null blackboard asset keys.
        /// </summary>
        private void ClearNullKeys()
        {
            if(!TryGetBlackboard(out Blackboard blackboard))
            {
                return;
            }

            bool hasNull = false;

            SerializedObject serializedObject = new SerializedObject(blackboard);
            SerializedProperty keys = serializedObject.FindProperty("keys");
            for (int i = 0; i < keys.arraySize; i++)
            {
                SerializedProperty key = keys.GetArrayElementAtIndex(i);
                if(key.objectReferenceValue == null)
                {
                    keys.DeleteArrayElementAtIndex(i);
                    i--;
                    hasNull = true;
                }
            }

            if (hasNull)
            {
                serializedObject.ApplyModifiedProperties();
                EditorApplication.delayCall += () =>
                {
                    EditorUtility.DisplayDialog("AI Tree Blackboard", 
                        "Some types of keys that were contained in this blackboard were removed from the project or renamed.\n\n" +
                        "Undefined keys were automatically deleted, but references to their objects remained in the blackboard asset file.\n\n" +
                        "Expand the blackboard asset (the arrow to the right of the asset file) and manually delete the extra key assets.",
                        "Ok");
                };
            }
        }

        /// <summary>
        /// Called when you click on the list item.
        /// </summary>
        /// <param name="key">Key.</param>
        /// <param name="visualElement">VisualElement.</param>
        private void OnSelectKey(Key key, VisualElement visualElement)
        {
            if (selectedElement != null)
            {
                selectedElement.RemoveFromClassList("selected");
            }
            selectedElement = visualElement;
            selectedElement.AddToClassList("selected");

            selectedKey = key;
        }

        /// <summary>
        /// Load all required Visual Elements.
        /// </summary>
        private void LoadVisualElements()
        {
            AITreeSettings settings = AITreeSettings.instance;

            rootVisualElement.Clear();

            VisualTreeAsset visualTree = settings.GetBlackboardUXML();
            visualTree.CloneTree(rootVisualElement);
            rootVisualElement.styleSheets.Add(settings.GetBlackboardUSS());

            toolbarNewKey = rootVisualElement.Q<ToolbarButton>("toolbar-new-key");
            blackboardName = rootVisualElement.Q<Label>("blackboard-name");
            searchInput = rootVisualElement.Q<TextField>("search-input");
            searchPlaceholder = rootVisualElement.Q<VisualElement>("search-placeholder");
            inheritedKeyList = rootVisualElement.Q<ListElement>("inherited-keys-list");
            keyList = rootVisualElement.Q<ListElement>("keys-list");
            inheritKeysFoldout = rootVisualElement.Q<Foldout>("inherited-keys-foldout");

            toolbarNewKey.clicked -= ShowKeyCreationWindow;
            toolbarNewKey.clicked += ShowKeyCreationWindow;
        }

        /// <summary>
        /// Check if window has new or unloaded visual elements.
        /// </summary>
        /// <returns>True if has new or unloaded visual elements, otherwise false.</returns>
        private bool HasUnloadedVisualElements()
        {
            return toolbarNewKey == null
                || blackboardName == null
                || searchInput == null
                || searchPlaceholder == null
                || inheritedKeyList == null
                || keyList == null
                || inheritKeysFoldout == null;
        }

        #region [Callbacks]
        /// <summary>
        /// Called when undo/redo.
        /// </summary>
        private void OnUndoRedo()
        {
            EditorApplication.delayCall += () =>
            {
                RefreshKeys();
                if (!EditorApplication.isPlaying && TryGetBlackboard(out Blackboard blackboard))
                {
                    AssetDatabase.SaveAssetIfDirty(blackboard);
                }
            };
        }

        /// <summary>
        /// Check delete callback.
        /// </summary>
        private void CheckDeleteCallback()
        {
            if(selectedKey == null)
            {
                return;
            }

            if (keyboardEvent == null)
            {
                return;
            }

            if (focusedWindow == null)
            {
                return;
            }

            Type focusedWindowType = focusedWindow.GetType();
#pragma warning disable CS0618 // Key Details window is obsolete
            if (focusedWindowType == typeof(BlackboardWindow) || focusedWindowType == typeof(BlackboardWindow) || focusedWindowType == typeof(KeyDetailsWindow))
#pragma warning restore CS0618 // Key Details window is obsolete
            {
                if (keyboardEvent.keyCode == KeyCode.Delete
                    || (keyboardEvent.keyCode == KeyCode.Backspace && (keyboardEvent.control || keyboardEvent.command)))
                {
                    if (selectedKey is not SelfKey && TryGetBlackboard(out Blackboard blackboard))
                    {
                        if (BlackboardUtility.DeleteKey(blackboard, selectedKey))
                        {
                            selectedKey = null;
                            selectedElement = null;

                            foreach (TrackerWindow tracker in ActiveTrackers)
                            {
                                if(tracker is BlackboardWindow window && window.TryGetBlackboard(out Blackboard bb) && bb == blackboard)
                                {
                                    window.RefreshKeys();
                                }
                            }
                        }
                    }
                    else
                    {
                        EditorUtility.DisplayDialog("AI Tree", "Self is a system key and cannot be deleted.", "Ok");
                    }
                }
                else if((keyboardEvent.control || keyboardEvent.command) && keyboardEvent.keyCode == KeyCode.D)
                {
                    if (selectedKey is not SelfKey && TryGetBlackboard(out Blackboard blackboard))
                    {
                        BlackboardUtility.DuplicateKey(blackboard, selectedKey);
                        selectedKey = null;
                        selectedElement = null;

                        foreach (TrackerWindow tracker in ActiveTrackers)
                        {
                            if (tracker is BlackboardWindow window && window.TryGetBlackboard(out Blackboard bb) && bb == blackboard)
                            {
                                window.RefreshKeys();
                            }
                        }
                    }
                    else
                    {
                        EditorUtility.DisplayDialog("AI Tree", "Self is a system key and cannot be duplicated.", "Ok");
                    }
                }
            }
        }

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
        [MenuItem("Tools/AI Tree/Windows/Blackboard", false, 22)]
        public static void Open()
        {
            Open<BlackboardWindow>();
        }

        [OnOpenAsset]
        private static bool OnOpenAsset(int instanceId, int line)
        {
            Object asset = EditorUtility.InstanceIDToObject(instanceId);
            if (asset is Blackboard)
            {
                BlackboardWindow window = GetOrCreateTracker<BlackboardWindow>();
                window.TrackEditor(asset);
                return true;
            }
            return false;
        }
        #endregion

        #region [Getter / Setter]
        public string GetSearchText()
        {
            return searchText;
        }

        public void SetSearchText(string value)
        {
            searchText = value;
        }
        #endregion
    }
}
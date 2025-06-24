/* ================================================================
   ----------------------------------------------------------------
   Project   :   AI Tree
   Company   :   Renowned Games
   Developer :   Tamerlan Shakirov, Zinnur Davleev
   ----------------------------------------------------------------
   Copyright 2022-2023 Renowned Games All rights reserved.
   ================================================================ */

using RenownedGames.ExLibEditor;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace RenownedGames.AITreeEditor.UIElements
{
#if UNITY_6000_0_OR_NEWER
    [UxmlElement]
    public sealed partial class ListElement : VisualElement, IListElement
#else
    public sealed class ListElement : VisualElement, IListElement
#endif
    {

#if !UNITY_6000_0_OR_NEWER
        public new class UxmlFactory : UxmlFactory<ListElement, UxmlTraits> { }
#endif

        private List<ListItem> list;
        private ScrollView container;

        private VisualElement selectedElement;

        public ListElement()
        {
            AddToClassList("list-element");

            list = new List<ListItem>();
            StyleSheet styleSheet = EditorResources.Load<StyleSheet>("Styles/ListElement.uss");
            styleSheets.Add(styleSheet);
        }

        /// <summary>
        /// Initializing the list elements.
        /// </summary>
        public void Initialize()
        {
            Clear();

            container = new ScrollView(ScrollViewMode.Vertical);
            Add(container);

            list.Sort(ItemCompare);

            Foldout lastFoldout = null;
            string lastCategory = string.Empty;

            for (int i = 0; i < list.Count; i++)
            {
                ListItem item = list[i];

                if (!string.IsNullOrEmpty(item.GetCategory()))
                {
                    if (lastCategory != item.GetCategory())
                    {
                        lastCategory = item.GetCategory();

                        lastFoldout = new Foldout();
                        lastFoldout.text = lastCategory;
                        lastFoldout.AddToClassList("list-element-foldout");

                        container.Add(lastFoldout);
                    }
                }
                else if (lastFoldout != null)
                {
                    lastFoldout = null;
                }

                Label element = new Label(item.GetName());
                element.AddToClassList("list-element-item");
                element.tooltip = item.GetTooltip();
                element.userData = i;
                element.focusable = true;
                element.RegisterCallback<ClickEvent>(OnClickEvent);
                element.RegisterCallback<ContextClickEvent>(OnContextClickEvent);

                VisualElement icon = new VisualElement();
                icon.AddToClassList("list-element-item-icon");
                icon.style.backgroundColor = new StyleColor(item.GetColor());

                element.Add(icon);

                if (lastFoldout != null)
                {
                    lastFoldout.Add(element);
                }
                else
                {
                    container.Add(element);
                }
            }
        }

        /// <summary>
        /// Add new item to list.
        /// </summary>
        public void AddItem(ListItem item)
        {
            list.Add(item);
        }

        /// <summary>
        /// Clears the list of items.
        /// </summary>
        public void ClearItems()
        {
            list.Clear();
        }

        /// <summary>
        /// Called when you click on an item.
        /// </summary>
        private void OnClickEvent(ClickEvent evt)
        {
            VisualElement element = evt.target as VisualElement;
            int index = (int)element.userData;

            list[index].OnClick(element);
        }

        /// <summary>
        /// Called when you click on an item with right button.
        /// </summary>
        private void OnContextClickEvent(ContextClickEvent evt)
        {
            VisualElement element = evt.target as VisualElement;
            int index = (int)element.userData;

            GenericMenu menu = new GenericMenu();

            ListItem item = list[index];
            GUIContent deleteContent = new GUIContent("Delete", "Delete this item.");
            GUIContent duplicateContent = new GUIContent("Duplicate", "Duplicate this item.");

            const string SELF_KEY = "Self";
            if (item.GetName() == SELF_KEY)
            {
                menu.AddDisabledItem(deleteContent);
                menu.AddDisabledItem(duplicateContent);
            }
            else
            {
                menu.AddItem(deleteContent, false, () => item.OnDelete(element));
                menu.AddItem(duplicateContent, false, () => item.OnDuplicate(element));
            }
            menu.ShowAsContext();
        }

        /// <summary>
        /// The logic of sorting items.
        /// </summary>
        private int ItemCompare(ListItem lhs, ListItem rhs)
        {
            int compare = string.IsNullOrEmpty(lhs.GetCategory()).CompareTo(string.IsNullOrEmpty(rhs.GetCategory()));
            if (compare == 0)
            {
                compare = lhs.GetCategory().CompareTo(rhs.GetCategory());
                if (compare == 0)
                {
                    compare = lhs.GetName().CompareTo(rhs.GetName());
                }
            }
            return compare;
        }
    }
}
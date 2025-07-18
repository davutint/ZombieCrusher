/* ================================================================
   ----------------------------------------------------------------
   Project   :   AI Tree
   Company   :   Renowned Games
   Developer :   Zinnur Davleev
   ----------------------------------------------------------------
   Copyright 2022-2023 Renowned Games All rights reserved.
   ================================================================ */

using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace RenownedGames.AITreeEditor.UIElements
{
    public struct ListItem
    {
        private string name;
        private string category;
        private string tooltip;
        private Color color;
        private Action<VisualElement> onClick;
        private Action<VisualElement> onDelete;
        private Action<VisualElement> onDuplicate;

        /// <summary>
        /// ListItem constructor.
        /// </summary>
        /// <param name="name">Name of item.</param>
        /// <param name="onClick">Called when item is clicked.</param>
        public ListItem(string name, Action<VisualElement> onClick, Action<VisualElement> onDelete, Action<VisualElement> onDuplicate)
        {
            this.name = name;
            this.onClick = onClick;
            this.onDelete = onDelete;
            this.onDuplicate = onDuplicate;
            category = string.Empty;
            tooltip = string.Empty;
            color = Color.white;
        }

        /// <summary>
        /// ListItem constructor.
        /// </summary>
        /// <param name="name">Name of item.</param>
        /// <param name="category">Category of item.</param>
        /// <param name="onClick">Called when item is clicked.</param>
        public ListItem(string name, string category, Action<VisualElement> onClick, Action<VisualElement> onDelete, Action<VisualElement> onDuplicate) : this(name, onClick, onDelete, onDuplicate)
        {
            if (!string.IsNullOrEmpty(category))
            {
                this.category = category.Trim();
            }
        }

        /// <summary>
        /// ListItem constructor.
        /// </summary>
        /// <param name="name">Name of item.</param>
        /// <param name="category">Category of item.</param>
        /// <param name="tooltip">Tooltip of item, displayed when mouse over item.</param>
        /// <param name="onClick">Called when item is clicked.</param>
        public ListItem(string name, string category, string tooltip, Action<VisualElement> onClick, Action<VisualElement> onDelete, Action<VisualElement> onDuplicate) : this(name, category, onClick, onDelete, onDuplicate)
        {
            if (!string.IsNullOrEmpty(tooltip))
            {
                this.tooltip = tooltip.Trim();
            }
        }

        /// <summary>
        /// ListItem constructor.
        /// </summary>
        /// <param name="name">Name of item.</param>
        /// <param name="category">Category of item.</param>
        /// <param name="tooltip">Tooltip of item, displayed when mouse over item.</param>
        /// <param name="color">Color of item, by default is white.</param>
        /// <param name="onClick">Called when item is clicked.</param>
        public ListItem(string name, string category, string tooltip, Color color, Action<VisualElement> onClick, Action<VisualElement> onDelete, Action<VisualElement> onDuplicate) : this(name, category, tooltip, onClick, onDelete, onDuplicate)
        {
            this.color = color;
        }

        public void OnClick(VisualElement value)
        {
            try
            { 
                onClick?.Invoke(value);
            }
            catch { }
        }

        public void OnDelete(VisualElement value)
        {
            try
            {
                onDelete?.Invoke(value);
            }
            catch { }
        }

        public void OnDuplicate(VisualElement value)
        {
            try
            {
                onDuplicate?.Invoke(value);
            }
            catch { };
        }

        #region [Getter / Setter]
        public string GetName()
        {
            return name;
        }

        public void SetName(string value)
        {
            name = value;
        }

        public string GetCategory()
        {
            return category;
        }

        public void SetCategory(string value)
        {
            category = value;
        }

        public string GetTooltip()
        {
            return tooltip;
        }

        public void SetTooltip(string value)
        {
            tooltip = value;
        }

        public Color GetColor()
        {
            return color;
        }

        public void SetColor(Color value)
        {
            color = value;
        }
        #endregion
    }
}

/* ================================================================
   ----------------------------------------------------------------
   Project   :   AI Tree
   Publisher :   Renowned Games
   Developer :   Tamerlan Shakirov, Zinnur Davleev
   ----------------------------------------------------------------
   Copyright 2022-2023 Renowned Games All rights reserved.
   ================================================================ */

using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;

namespace RenownedGames.AITreeEditor
{
#if UNITY_6000_0_OR_NEWER
    [UxmlElement]
    internal partial class EmptyDropTarget : VisualElement, IDropTarget
#else
    internal class EmptyDropTarget : VisualElement, IDropTarget
#endif
    {

#if !UNITY_6000_0_OR_NEWER
        public new class UxmlFactory : UxmlFactory<EmptyDropTarget, UxmlTraits> { }
#endif

        public bool CanAcceptDrop(List<ISelectable> selection)
        {
            return false;
        }

        public bool DragEnter(DragEnterEvent evt, IEnumerable<ISelectable> selection, IDropTarget enteredTarget, ISelection dragSource)
        {
            return false;
        }

        public bool DragExited()
        {
            return false;
        }

        public bool DragLeave(DragLeaveEvent evt, IEnumerable<ISelectable> selection, IDropTarget leftTarget, ISelection dragSource)
        {
            return false;
        }

        public bool DragPerform(DragPerformEvent evt, IEnumerable<ISelectable> selection, IDropTarget dropTarget, ISelection dragSource)
        {
            return false;
        }

        public bool DragUpdated(DragUpdatedEvent evt, IEnumerable<ISelectable> selection, IDropTarget dropTarget, ISelection dragSource)
        {
            return false;
        }
    }
}
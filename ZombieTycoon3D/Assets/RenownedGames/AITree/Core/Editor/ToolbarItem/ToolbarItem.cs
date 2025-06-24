using System;

namespace RenownedGames.AITreeEditor
{
    internal sealed class ToolbarItem
    {
        public readonly Action callback;
        public readonly ToolbarItemAttribute attribute;

        public ToolbarItem(Action callback, ToolbarItemAttribute attribute)
        {
            this.callback = callback;
            this.attribute = attribute;
        }
    }
}

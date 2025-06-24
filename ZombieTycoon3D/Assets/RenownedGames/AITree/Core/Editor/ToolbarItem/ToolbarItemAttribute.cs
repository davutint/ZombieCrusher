using System;

namespace RenownedGames.AITreeEditor
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public sealed class ToolbarItemAttribute : Attribute
    {
        public readonly string name;
        public readonly Type windowType;
        public readonly ToolbarItemLayout layout;

        public ToolbarItemAttribute(string name, Type windowType, ToolbarItemLayout layout)
        {
            this.name = name;
            this.windowType = windowType;
            this.layout = layout;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace RenownedGames.AITreeEditor
{
    internal static class ToolbarItemUtility
    {
        private static readonly List<ToolbarItem> Items;

        static ToolbarItemUtility()
        {
            Items = new List<ToolbarItem>();

            TypeCache.MethodCollection methods = TypeCache.GetMethodsWithAttribute<ToolbarItemAttribute>();
            for (int i = 0; i < methods.Count; i++)
            {
                MethodInfo method = methods[i];
                if (!method.IsStatic)
                {
                    Debug.LogError($"Method ({method.Name}) with [ToolbarButton] attribute must be static.");
                    continue;
                }

                if (method.ReturnType != typeof(void))
                {
                    Debug.LogError($"Method ({method.Name}) with [ToolbarButton] must be with void return type.");
                    continue;
                }

                if (method.GetParameters().Length > 0)
                {
                    Debug.LogError($"Method ({method.Name}) with [ToolbarButton] cannot have parameters.");
                    continue;
                }

                try
                {
                    Action callback = (Action)Delegate.CreateDelegate(typeof(Action), method);
                    ToolbarItemAttribute attribute = method.GetCustomAttribute<ToolbarItemAttribute>();
                    Items.Add(new ToolbarItem(callback, attribute));
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }
            }
        }

        public static void Foreach<T>(Action<ToolbarItem> callback) where T : EditorWindow
        {
            if (callback == null)
            {
                throw new ArgumentNullException("Callback cannot be null!");
            }

            Type type = typeof(T);
            for (int i = 0; i < Items.Count; i++)
            {
                ToolbarItem item = Items[i];
                if (type.IsAssignableFrom(item.attribute.windowType))
                {
                    callback(item);
                }
            }
        }
    }
}

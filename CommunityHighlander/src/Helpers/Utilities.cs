using System;
using System.Reflection;
using System.Collections.Generic;

using UnityEngine;
using Utility;

namespace CommunityHighlander.Helpers
{
    // private helper methods for use by highlander only
    // public helper methods are in NCH_Utilities
    internal class Utilities
    {
        public static string GetHighlanderVersion()
        {
            return PluginInfo.PLUGIN_VERSION;
        }

        public static string FormatRemoteVersionTag(string playerName, string remoteVersion)
        {
            string finalText;
            if (remoteVersion != null && remoteVersion.Length > 0)
            {
                if (remoteVersion == GetHighlanderVersion())
                {
                    finalText = "<color=" + GameColors.GreenTextColor + ">[NCH v" + remoteVersion + "]</color> " + playerName;
                }
                else
                {
                    finalText = "<color=" + GameColors.YellowTextColor + ">[NCH v" + remoteVersion + "]</color> " + playerName;
                }
            }
            else
            {
                finalText = "[NCH v0.0.0] " + playerName;
            }

            return finalText;
        }

        public static string FormatLocalVersionTag(string playerName, string remoteVersion)
        {
            string finalText;
            string localVersion = GetHighlanderVersion();

            if (remoteVersion == localVersion)
            {
                finalText = "<color=" + GameColors.GreenTextColor + ">[NCH v" + localVersion + "]</color> " + playerName;
            }
            else
            {
                finalText = "<color=" + GameColors.YellowTextColor + ">[NCH v" + localVersion + "]</color> " + playerName;
            }

            return finalText;
        }

        public static string FormatMissingVersionTag(string playerName)
        {
            if (!playerName.Contains("[NCH v"))
            {
                return "<color=" + GameColors.RedTextColor + ">[NCH v0.0.0]</color> " + playerName;
            }
            else
            {
                return playerName;
            }
        }

        public static object GetPrivateValue(object instance, string fieldName)
        {
            Type type = instance.GetType();
            FieldInfo field = type.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            return field.GetValue(instance);
        }

        public static void SetPrivateValue(object instance, string fieldName, object value)
        {
            Type type = instance.GetType();
            FieldInfo field = type.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            field.SetValue(instance, value);
        }

        public static void CallPrivateMethod(object instance, string methodName, object[] parameters, bool baseClass = false)
        {
            Type type = instance.GetType();
            if (baseClass) type = type.BaseType;

            MethodInfo method = type.GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance);
            method.Invoke(instance, parameters);
        }

        public static void ListPrivateMethods(object instance)
        {
            Type type = instance.GetType();
            MethodInfo[] methods = type.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance);

            Debug.Log("Methods of " + type.Name + ":");

            foreach (MethodInfo method in methods)
            {
                Debug.Log(" - " + method.Name);
            }
        }

        public static object GetPrivateProperty(object instance, string fieldName)
        {
            Type type = instance.GetType();
            PropertyInfo property = type.GetProperty(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            return property.GetValue(instance);
        }

        public static void CopyPrefabRecursive(ref GameObject acceptor, ref GameObject donator, GameObject parent = null)
        {
            if (parent != null) acceptor.transform.parent = parent.transform;

            CopyAllPrefabScripts(ref acceptor, ref donator);

            for (int i = 0; i < donator.transform.childCount; i++)
            {
                GameObject dChild = donator.transform.GetChild(i).gameObject;
                GameObject aChild = new GameObject(dChild.name);
                CopyPrefabRecursive(ref aChild, ref dChild, acceptor);
            }
        }

        public static void CopyAllPrefabScripts(ref GameObject acceptor, ref GameObject donator)
        {
            if (Plugin.logOperations) Debug.Log($"Copying {donator.name}({donator.GetComponents<Component>().Length}) to {acceptor.name}({acceptor.GetComponents<Component>().Length})");

            foreach (Component component in donator.GetComponents<Component>())
            {
                CopyScriptToPrefab(component, acceptor);
            }

            if (Plugin.logOperations) Debug.Log($"Copied {donator.name}({donator.GetComponents<Component>().Length}) to {acceptor.name}({acceptor.GetComponents<Component>().Length})");
        }

        public static void CopyScriptToPrefab(Component component, GameObject prefab)
        {
            Type type = component.GetType();
            if (prefab.GetComponent(type) == null)
            {
                CopyAllScriptValues(type, prefab.AddComponent(type), component);
            }
            else
            {
                if (Plugin.logOperations) Debug.Log($"{prefab.name} already has component {type.Name}");
            }
        }

        public static void CopyAllScriptValues(Type type, UnityEngine.Object acceptor, UnityEngine.Object donator, Dictionary<string, object> specifiedFields = null, List<string> exemptFields = null)
        {
            FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            if (Plugin.logOperations) Debug.Log($"Copying all {fields.Length} fields from {donator.GetType()}");

            if (specifiedFields != null && specifiedFields.Count > 0)
            {
                if (Plugin.logOperations) Debug.Log($" with {specifiedFields.Count} specified");
            }

            if (exemptFields != null && exemptFields.Count > 0)
            {
                if (Plugin.logOperations) Debug.Log($" with {exemptFields.Count} exempt");
            }

            foreach (FieldInfo field in fields)
            {
                if (exemptFields != null && exemptFields.Count > 0 && exemptFields.Contains(field.Name))
                {
                    exemptFields.Remove(field.Name);
                    continue;
                }

                if (Plugin.logOperations) Debug.Log($"Copying {field.Name} with value {field.GetValue(donator)}");

                field.SetValue(acceptor, field.GetValue(donator));
            }

            if (specifiedFields != null && specifiedFields.Count > 0)
            {
                CopySpecificScriptValues(acceptor.GetType(), acceptor, specifiedFields, false);
            }

            if (type.BaseType != null)
            {
                CopyAllScriptValues(type.BaseType, acceptor, donator, specifiedFields, exemptFields);
            }
        }

        public static void CopySpecificScriptValues(Type type, UnityEngine.Object acceptor, Dictionary<string, object> fieldsToCopy, bool typeRecursion = false)
        {
            if (!type.IsAssignableFrom(acceptor.GetType()))
            {
                return;
            }

            FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            if (Plugin.logOperations) Debug.Log($"Copying {fieldsToCopy.Count} fields in {type}");

            foreach (FieldInfo field in fields)
            {
                if (fieldsToCopy != null)
                {
                    fieldsToCopy.TryGetValue(field.Name, out object value);

                    object oldValue = field.GetValue(acceptor);

                    if (value != null)
                    {
                        if (Plugin.logOperations) Debug.Log($"Setting {field.Name} with {value} from {oldValue}");
                        field.SetValue(acceptor, value);
                        fieldsToCopy.Remove(field.Name);
                    }
                }
            }

            if (typeRecursion && type.BaseType != null && fieldsToCopy.Count > 0)
            {
                CopySpecificScriptValues(type.BaseType, acceptor, fieldsToCopy, true);
            }
        }

        public static bool ScalePrefabLODsRecursive(GameObject prefab, Vector3 lodScale)
        {
            if (prefab.transform.parent == null)
            {
                if (Plugin.logOperations) Debug.Log($"SCALING ROOT: {prefab.name}");
                prefab.transform.localScale = lodScale;
            }

            bool isLOD = prefab.name.Contains("LOD");
            bool hasLOD = false;

            for (int i = 0; i < prefab.transform.childCount; i++)
            {
                GameObject child = prefab.transform.GetChild(i).gameObject;
                hasLOD = ScalePrefabLODsRecursive(child, lodScale) || hasLOD;
            }

            if (!isLOD && !hasLOD)
            {
                Vector3 inverseScale = new Vector3(1 / lodScale.x, 1 / lodScale.y, 1 / lodScale.z);
                if (Plugin.logOperations) Debug.Log($"INVERSELY SCALING: {prefab.name}");
                prefab.transform.localScale = inverseScale;

                for (int i = 0; i < prefab.transform.childCount; i++)
                {
                    if (Plugin.logOperations) Debug.Log($"RESET SCALING: {prefab.name}");
                    GameObject child = prefab.transform.GetChild(i).gameObject;
                    child.transform.localScale = Vector3.one;
                }
            }

            return isLOD || hasLOD;
        }

        public static void PrintAllScriptValues(Type type, UnityEngine.Object obj)
        {
            FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            Debug.Log($"Printing out {fields.Length} fields of {type.Name}");

            foreach (FieldInfo field in fields)
            {
                object value = field.GetValue(obj);

                if (value is not null)
                {
                    Debug.Log($"   {field.Name} = {value}");
                }
                else
                {
                    Debug.Log($"   {field.Name} = NULL");
                }
            }

            if (type.BaseType != null)
            {
                PrintAllScriptValues(type.BaseType, obj);
            }

            if (type == obj.GetType())
            {
                Debug.Log($"--- PRINTING COMPLETE ---");
            }
        }
    }
}

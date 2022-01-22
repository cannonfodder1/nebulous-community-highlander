using System;
using System.Reflection;
using System.Collections.Generic;

using UnityEngine;

using Utility;
using Munitions;

namespace CommunityHighlander.Helpers
{
    // useful methods for use by advanced modders and the NCH itself
    public class Utilities
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

        public static object GetPrivateValue(object instance, string fieldName, bool baseClass = false)
        {
            Type type = instance.GetType();
            if (baseClass) type = type.BaseType;

            FieldInfo field = type.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            return field.GetValue(instance);
        }

        public static void SetPrivateValue(object instance, string fieldName, object value, bool baseClass = false)
        {
            Type type = instance.GetType();
            if (baseClass) type = type.BaseType;

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

        public static void ListPrivateMethods(object instance, bool baseClass = false)
        {
            Type type = instance.GetType();
            if (baseClass) type = type.BaseType;

            MethodInfo[] methods = type.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance);

            Debug.Log("Methods of " + type.Name + ":");

            foreach (MethodInfo method in methods)
            {
                Debug.Log(" - " + method.Name);
            }
        }

        public static object GetPrivateProperty(object instance, string fieldName, bool baseClass = false)
        {
            Type type = instance.GetType();
            if (baseClass) type = type.BaseType;

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

        public static ValueType EditStructFields(object structure, Dictionary<string, object> values)
        {
            ValueType workaround = (ValueType)structure;
            Type type = structure.GetType();

            foreach (KeyValuePair<string, object> value in values)
            {
                FieldInfo field = type.GetField(value.Key, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                field.SetValue(workaround, value.Value);
            }

            return workaround;
        }

        public static GameObject CopyNebulousGameObject(GameObject donatorPrefab, string prefabName)
        {
            donatorPrefab.SetActive(false);

            GameObject acceptorPrefab = UnityEngine.Object.Instantiate(donatorPrefab);
            UnityEngine.Object.DontDestroyOnLoad(acceptorPrefab);
            acceptorPrefab.name = prefabName;

            return acceptorPrefab;
        }

        public static ScriptableObject CopyNebulousScriptableObject(ScriptableObject donatorObject, string objectName, List<string> exemptFields = null)
        {
            ScriptableObject acceptorObject = ScriptableObject.CreateInstance(donatorObject.GetType());

            Utilities.CopyAllScriptValues(acceptorObject.GetType(), acceptorObject, donatorObject, null, exemptFields);

            UnityEngine.Object.DontDestroyOnLoad(acceptorObject);
            acceptorObject.name = objectName;

            return acceptorObject;
        }

        public static void EditMunitionGradient(ScriptableObject munition, GradientColorKey[] colourKeys)
        {
            LightweightKineticShell munitionScript = (LightweightKineticShell)munition;

            if (munitionScript is null) return;

            StandbyVisualEffect visualEffect = munitionScript.TracerEffect;
            ValueType wrapperObject = visualEffect;

            Type type = visualEffect.GetType();
            FieldInfo gradientField = type.GetField("_gradients", BindingFlags.NonPublic | BindingFlags.Instance);

            StandbyVisualEffect.GradientProperty[] gradients = (StandbyVisualEffect.GradientProperty[])gradientField.GetValue(wrapperObject);

            if (gradients.Length < 1) return;

            Gradient gradient = new()
            {
                colorKeys = colourKeys,
                alphaKeys = gradients[0].Value.alphaKeys,
                mode = gradients[0].Value.mode
            };

            StandbyVisualEffect.GradientProperty property = new()
            {
                Name = gradients[0].Name,
                Value = gradient
            };

            gradientField.SetValue(wrapperObject, new StandbyVisualEffect.GradientProperty[] { property });
            visualEffect = (StandbyVisualEffect)wrapperObject;

            FieldInfo effectField = typeof(LightweightKineticShell).GetField("_tracerEffect", BindingFlags.NonPublic | BindingFlags.Instance);
            effectField.SetValue(munition, visualEffect);
        }
    }
}

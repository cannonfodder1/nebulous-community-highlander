using System;
using System.Reflection;
using System.Collections.Generic;

using UnityEngine;
using Modding;
using Munitions;
using Ships;
using Utility;

using CommunityHighlander.Overrides;

namespace CommunityHighlander.Helpers
{
    // public helper methods open for use by other mods
    // private helper methods are in Utilities
    public class Helpers
    {
        public static GameObject CreateNebulousHullPart(
            ModRecord modRecord,
            string partNetworkID,
            string partName,
            string saveToPath,
            string copyFromPath,
            Dictionary<string, object> valueChanges)
        {
            if (NCH_BundleManager.Instance.GetHullComponent(saveToPath) != null) return null;

            GameObject createdPrefab = CopyNebulousHullPart(partName, copyFromPath);

            FinalizeNebulousHullPart(modRecord, partNetworkID, saveToPath, createdPrefab, valueChanges);

            return createdPrefab;
        }

        private static GameObject CopyNebulousHullPart(string partName, string copyFromPath)
        {
            HullComponent donatorComponent = NCH_BundleManager.Instance.GetHullComponent(copyFromPath);
            GameObject donatorPrefab = donatorComponent.gameObject;
            donatorPrefab.SetActive(false);

            GameObject acceptorPrefab = UnityEngine.Object.Instantiate(donatorPrefab);
            UnityEngine.Object.DontDestroyOnLoad(acceptorPrefab);
            acceptorPrefab.name = partName;

            return acceptorPrefab;
        }

        private static void FinalizeNebulousHullPart(
            ModRecord modRecord,
            string partNetworkID,
            string saveToPath,
            GameObject createdPrefab,
            Dictionary<string, object> valueChanges)
        {
            HullComponent mainScript = createdPrefab.GetComponent<HullComponent>();

            valueChanges.TryGetValue("CH_ScaleLODs", out object scale);
            if (scale != null)
            {
                Utilities.ScalePrefabLODsRecursive(createdPrefab, (Vector3)scale);
                valueChanges.Remove("CH_ScaleLODs");
            }

            valueChanges.Add("_modId", modRecord.Info.UniqueIdentifier);
            valueChanges.Add("_partKey", partNetworkID);
            valueChanges.Add("_saveKey", saveToPath);

            Utilities.CopySpecificScriptValues(mainScript.GetType(), mainScript, valueChanges, true);

            NCH_BundleManager.Instance.AddHullComponent(saveToPath, mainScript);
        }

        public static ScriptableObject CreateNebulousAmmoType(
            ModRecord modRecord,
            string ammoName,
            string saveToPath,
            string copyFromPath,
            Dictionary<string, object> valueChanges)
        {
            if (NCH_BundleManager.Instance.GetMunition(saveToPath) != null) return null;

            ScriptableObject createdObject = CopyNebulousAmmoType(ammoName, copyFromPath);

            FinalizeNebulousAmmoType(modRecord, ammoName, saveToPath, createdObject, valueChanges);

            return createdObject;
        }

        private static ScriptableObject CopyNebulousAmmoType(string ammoName, string copyFromPath)
        {
            ScriptableObject donatorObject = (ScriptableObject)NCH_BundleManager.Instance.GetMunition(copyFromPath);
            ScriptableObject acceptorObject = ScriptableObject.CreateInstance(donatorObject.GetType());

            List<string> exemptFields = new();
            exemptFields.Add("__parsedKey");
            exemptFields.Add("_munitionKey");
            exemptFields.Add("m_CachedPtr");

            Utilities.CopyAllScriptValues(acceptorObject.GetType(), acceptorObject, donatorObject, null, exemptFields);

            UnityEngine.Object.DontDestroyOnLoad(acceptorObject);
            acceptorObject.name = ammoName;

            return acceptorObject;
        }

        private static void FinalizeNebulousAmmoType(
            ModRecord modRecord,
            string ammoName,
            string saveToPath,
            ScriptableObject createdObject,
            Dictionary<string, object> valueChanges)
        {
            valueChanges.TryGetValue("CH_ChangeGradient", out object colourKeys);
            if (colourKeys != null)
            {
                EditMunitionGradient(createdObject, (GradientColorKey[])colourKeys);
                valueChanges.Remove("CH_ChangeGradient");
            }

            valueChanges.Add("_modId", modRecord.Info.UniqueIdentifier);
            valueChanges.Add("_saveKey", saveToPath);
            valueChanges.Add("_munitionName", ammoName);

            Utilities.CopySpecificScriptValues(createdObject.GetType(), createdObject, valueChanges, true);

            NCH_BundleManager.Instance.AddMunition(saveToPath, (IMunition)createdObject);
        }

        private static void CopyMunitionGradient(LightweightKineticShell target, LightweightKineticShell source)
        {
            StandbyVisualEffect visualEffect = source.TracerEffect;
            FieldInfo effectField = typeof(LightweightKineticShell).GetField("_tracerEffect", BindingFlags.NonPublic | BindingFlags.Instance);
            effectField.SetValue(target, visualEffect);
        }

        private static void EditMunitionGradient(ScriptableObject munition, GradientColorKey[] colourKeys)
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

        private static void PrintMunitionGradient(ScriptableObject munition)
        {
            LightweightKineticShell munitionScript = (LightweightKineticShell)munition;

            StandbyVisualEffect visualEffect = munitionScript.TracerEffect;
            ValueType wrapperObject = visualEffect;

            Type type = visualEffect.GetType();
            FieldInfo gradientField = type.GetField("_gradients", BindingFlags.NonPublic | BindingFlags.Instance);

            StandbyVisualEffect.GradientProperty[] gradients = (StandbyVisualEffect.GradientProperty[])gradientField.GetValue(wrapperObject);

            if (gradients != null)
            {
                Debug.Log($"{gradients.Length} Gradients:");
                foreach (StandbyVisualEffect.GradientProperty gradient in gradients)
                {
                    Debug.Log($" - {gradient.Name}");

                    foreach (GradientColorKey key in gradient.Value.colorKeys)
                    {
                        Debug.Log($"    - {key.color} : {key.time}");
                    }
                }
            }
        }

        public static ResourceModifier CreateNebulousResourceModifier(string resourceName, int resourceAmount, bool perCubicMeter = false)
        {
            ResourceModifier resource = new();

            Dictionary<string, object> values = new();
            values.Add("_resourceName", resourceName);
            values.Add("_amount", resourceAmount);
            values.Add("_perUnit", perCubicMeter);

            resource = (ResourceModifier)EditStructFields(resource, values);

            return resource;
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
    }
}

using System.Collections.Generic;

using UnityEngine;
using Modding;
using Munitions;
using Ships;
using Bundles;

using CommunityHighlander.Overrides;

namespace CommunityHighlander.Helpers
{
    // complete packaged workflows for use by other mods
    public class Workflows
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

            valueChanges.Add("_modId", modRecord.Info.UniqueIdentifier);
            valueChanges.Add("_partKey", partNetworkID);
            valueChanges.Add("_saveKey", saveToPath);

            HullComponent donatorComponent = NCH_BundleManager.Instance.GetHullComponent(copyFromPath);
            GameObject createdPrefab = Utilities.CopyNebulousGameObject(donatorComponent.gameObject, partName);
            HullComponent acceptorComponent = createdPrefab.GetComponent<HullComponent>();
            Utilities.CopySpecificScriptValues(acceptorComponent.GetType(), acceptorComponent, valueChanges, true);
            NCH_BundleManager.Instance.AddHullComponent(saveToPath, acceptorComponent);

            valueChanges.TryGetValue("CH_ScaleLODs", out object scale);
            if (scale != null)
            {
                Utilities.ScalePrefabLODsRecursive(createdPrefab, (Vector3)scale);
                valueChanges.Remove("CH_ScaleLODs");
            }

            return createdPrefab;
        }

        public static GameObject CreateNebulousMissileType(
            ModRecord modRecord,
            string missileName,
            string saveToPath,
            string copyFromPath,
            Dictionary<string, object> valueChanges)
        {
            if (NCH_BundleManager.Instance.GetMunition(saveToPath) != null) return null;

            valueChanges.Add("_modId", modRecord.Info.UniqueIdentifier);
            valueChanges.Add("_saveKey", saveToPath);
            valueChanges.Add("_munitionName", missileName);

            Missile donatorMissile = (Missile)NCH_BundleManager.Instance.GetMunition(copyFromPath);
            GameObject createdPrefab = Utilities.CopyNebulousGameObject(donatorMissile.gameObject, missileName);
            Missile acceptorMissile = createdPrefab.GetComponent<Missile>();
            Utilities.CopySpecificScriptValues(acceptorMissile.GetType(), acceptorMissile, valueChanges, true);
            NCH_BundleManager.Instance.AddMunition(saveToPath, acceptorMissile);

            return createdPrefab;
        }

        public static ScriptableObject CreateNebulousAmmoType(
            ModRecord modRecord,
            string ammoName,
            string saveToPath,
            string copyFromPath,
            Dictionary<string, object> valueChanges)
        {
            if (NCH_BundleManager.Instance.GetMunition(saveToPath) != null) return null;

            valueChanges.Add("_modId", modRecord.Info.UniqueIdentifier);
            valueChanges.Add("_saveKey", saveToPath);
            valueChanges.Add("_munitionName", ammoName);

            List<string> exemptFields = new();
            exemptFields.Add("__parsedKey");
            exemptFields.Add("_munitionKey");
            exemptFields.Add("m_CachedPtr");

            ScriptableObject donatorMunition = (ScriptableObject)NCH_BundleManager.Instance.GetMunition(copyFromPath);
            ScriptableObject acceptorMunition = Utilities.CopyNebulousScriptableObject(donatorMunition, ammoName, exemptFields);
            Utilities.CopySpecificScriptValues(acceptorMunition.GetType(), acceptorMunition, valueChanges, true);
            NCH_BundleManager.Instance.AddMunition(saveToPath, (IMunition)acceptorMunition);

            valueChanges.TryGetValue("CH_ChangeGradient", out object colourKeys);
            if (colourKeys != null)
            {
                Utilities.EditMunitionGradient(acceptorMunition, (GradientColorKey[])colourKeys);
                valueChanges.Remove("CH_ChangeGradient");
            }

            return acceptorMunition;
        }

        public static ResourceType CreateNebulousResourceType(
            string resourceName,
            string resourceUnit, 
            ResourceType.Schedule resourceSchedule = ResourceType.Schedule.Ticked)
        {
            Dictionary<string, ResourceType> resourceDatabase = (Dictionary<string, ResourceType>)Utilities.GetPrivateValue(ResourceDefinitions.Instance, "_resources");

            if (resourceDatabase.ContainsKey(resourceName))
            {
                return null;
            }

            ResourceType resource = new();
            resource.Name = resourceName;
            resource.Unit = resourceUnit;
            resource.ScheduleMode = resourceSchedule;

            resourceDatabase.Add(resource.Name, resource);

            return resource;
        }

        public static ResourceModifier CreateNebulousResourceModifier(
            string resourceName, 
            int resourceAmount, 
            bool perCubicMeter = false)
        {
            ResourceModifier resource = new();

            Dictionary<string, object> values = new();
            values.Add("_resourceName", resourceName);
            values.Add("_amount", resourceAmount);
            values.Add("_perUnit", perCubicMeter);

            resource = (ResourceModifier)Utilities.EditStructFields(resource, values);

            return resource;
        }

        public static void HideNebulousFleetEditorItem(ModRecord mod, string key)
        {
            NCH_BundleManager.Instance.HideItem(key, mod);
        }
    }
}

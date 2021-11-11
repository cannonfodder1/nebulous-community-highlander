using System;
using System.Reflection;
using System.Collections.Generic;

using UnityEngine;
using Modding;
using Ships;

namespace CommunityHighlander
{
    public class CH_Helpers
	{
		public static GameObject CreateNebulousHullPart(
			ModRecord modRecord,
			string partNetworkID,
			string partName,
			string saveToPath,
			string copyFromPath,
			Dictionary<String, object> valueChanges)
		{
			if (CH_BundleManager.Instance.GetHullComponent(saveToPath) != null) return null;

			GameObject createdPrefab = CopyNebulousHullPart(partName, copyFromPath);

			FinalizeNebulousHullPart(modRecord, partNetworkID, saveToPath, createdPrefab, valueChanges);

			return createdPrefab;
		}

		private static GameObject CopyNebulousHullPart(string partName, string copyFromPath)
		{
			HullComponent donatorComponent = CH_BundleManager.Instance.GetHullComponent(copyFromPath);
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
			Dictionary<String, object> valueChanges)
		{
			HullComponent mainScript = createdPrefab.GetComponent<HullComponent>();

			valueChanges.TryGetValue("CH_ScaleLODs", out object scale);
			if (scale != null)
			{
				CH_Utilities.ScalePrefabLODsRecursive(createdPrefab, (Vector3)scale);
				valueChanges.Remove("CH_ScaleLODs");
			}

			valueChanges.Add("_modId", modRecord.Info.UniqueIdentifier);
			valueChanges.Add("_partKey", partNetworkID);
			valueChanges.Add("_saveKey", saveToPath);

			CH_Utilities.CopySpecificScriptValues(mainScript.GetType(), mainScript, valueChanges, true);

			CH_BundleManager.Instance.AddHullComponent(saveToPath, mainScript);
		}

		public static ResourceModifier CreateNebulousResourceModifier(string resourceName, int resourceAmount, bool perCubicMeter = false)
        {
			ResourceModifier resource = new();
			ValueType workaround = resource;

			Type type = resource.GetType();

			FieldInfo nameField = type.GetField("_resourceName", BindingFlags.NonPublic | BindingFlags.Instance);
			FieldInfo amountField = type.GetField("_amount", BindingFlags.NonPublic | BindingFlags.Instance);
			FieldInfo unitField = type.GetField("_perUnit", BindingFlags.NonPublic | BindingFlags.Instance);

			nameField.SetValue(workaround, resourceName);
			amountField.SetValue(workaround, resourceAmount);
			unitField.SetValue(workaround, perCubicMeter);

			resource = (ResourceModifier)workaround;

			return resource;
		}
	}
}

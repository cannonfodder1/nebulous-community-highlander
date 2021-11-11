using System;
using System.Reflection;
using System.Collections.Generic;

using UnityEngine;
using Modding;

namespace CommunityHighlander
{
    internal class CH_Utilities
	{
		public static void ActivateHighlanderEventHook(ModRecord modRecord, string eventHookName, bool log = false)
		{
			Type modType = modRecord.GetType();
			FieldInfo field = modType.GetField("_loadedAssemblies", BindingFlags.Instance | BindingFlags.NonPublic);
			List<Assembly> assemblies = (List<Assembly>)field.GetValue(modRecord);

			if (log) Debug.Log($"Activating {eventHookName}...");
			if (log) Debug.Log($"Mod {modRecord.Info.ModName} Assemblies:");
			foreach (Assembly assembly in assemblies)
			{
				if (log) Debug.Log($"- {assembly.GetName().Name}");
				foreach (Type assemblyType in assembly.GetTypes())
				{
					if (assemblyType.BaseType == typeof(CH_EventHookTemplate))
					{
						if (log) Debug.Log($"--- {assemblyType.Name} inherits from CH_EventHookTemplate");
						CH_EventHookTemplate eventHook = (CH_EventHookTemplate)Activator.CreateInstance(assemblyType, new object[] { modRecord });

						if (eventHook != null)
						{
							if (log) Debug.Log($"--- {assemblyType.Name} instantiated");
							MethodInfo eventHookMethod = assemblyType.GetMethod(eventHookName, BindingFlags.Instance | BindingFlags.Public);

							if (eventHookMethod != null)
							{
								if (log) Debug.Log($"--- {assemblyType.Name} activated");
								eventHookMethod.Invoke(eventHook, new object[] { });
							}
						}
					}
				}
			}
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
				CopyAllScriptValues(prefab.AddComponent(type), component);
			}
			else
			{
				if (Plugin.logOperations) Debug.Log($"{prefab.name} already has component {type.Name}");
			}
		}

		public static Component CopyAllScriptValues(Component acceptor, Component donator, Dictionary<string, object> specifiedFields = null)
		{
			Type type = donator.GetType();
			FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

			if (specifiedFields != null)
			{
				if (Plugin.logOperations) Debug.Log($"Copying all {fields.Length} fields from {donator.GetType()} with {specifiedFields.Count} specified");
			}
			else
			{
				if (Plugin.logOperations) Debug.Log($"Copying all {fields.Length} fields from {donator.GetType()}");
			}

			foreach (FieldInfo field in fields)
			{
				if (specifiedFields != null)
				{
					specifiedFields.TryGetValue(field.Name, out object value);

					if (Plugin.logOperations) Debug.Log($"Attempting to set value {value}");

					if (value != null)
					{
						if (Plugin.logOperations) Debug.Log($"Setting {field.Name} with value {value}");
						field.SetValue(acceptor, value);
						continue;
					}
				}

				if (Plugin.logOperations) Debug.Log($"Copying {field.Name} with value {field.GetValue(donator)}");
				field.SetValue(acceptor, field.GetValue(donator));
			}

			return acceptor;
		}

		public static void CopySpecificScriptValues(Type type, Component acceptor, Dictionary<string, object> fieldsToCopy, bool typeRecursion = false)
		{
			if (!type.IsAssignableFrom(acceptor.GetType()))
			{
				return;
			}

			FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

			if (Plugin.logOperations) Debug.Log($"Searching {fieldsToCopy.Count} fields in {type}");

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
	}
}

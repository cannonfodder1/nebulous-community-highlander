﻿using System;
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

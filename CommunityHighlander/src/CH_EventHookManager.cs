using System;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using Modding;
using System.Runtime.Remoting;

namespace CommunityHighlander
{
    public class CH_EventHookManager
    {
        private struct HighlanderVersion
        {
            public HighlanderVersion(string str)
            {
                string[] sections = str.Split('.');
                major = int.Parse(sections[0]);
                minor = int.Parse(sections[1]);
                patch = int.Parse(sections[2]);
            }

            public int major;
            public int minor;
            public int patch;

            public int Get()
            {
                return (major * 10000) + (minor * 100) + patch;
            }
        }

        private static CH_EventHookManager _instance = null;
        private Dictionary<ulong, CH_EventHookTemplate> _eventHooks;

        private CH_EventHookManager()
        {
            _eventHooks = new();
        }

        static CH_EventHookManager()
        {
        }

        public static CH_EventHookManager Instance
        {
            get
            {
                if (!IsInitialized)
                {
                    _instance = new CH_EventHookManager();
                }

                return _instance;
            }
        }

        public static bool IsInitialized
        {
            get
            {
                return _instance != null;
            }
        }

        public void RegisterEventHook(ModRecord modRecord, bool log = false)
        {
            string matchVersion = CH_Utilities.GetHighlanderVersion();

            Type modType = modRecord.GetType();
            FieldInfo modField = modType.GetField("_loadedAssemblies", BindingFlags.Instance | BindingFlags.NonPublic);
            List<Assembly> assemblies = (List<Assembly>)modField.GetValue(modRecord);

            if (log) Debug.Log($"Registering event hooks from {modRecord.Info.ModName} for version {matchVersion}");

            foreach (Assembly assembly in assemblies)
            {
                foreach (Type hookType in assembly.GetTypes())
                {
                    if (log) Debug.Log($"Checking if class {hookType.Name} is an event hook template");

                    if (hookType.BaseType == typeof(CH_EventHookTemplate))
                    {
                        CH_EventHookTemplate eventHook = (CH_EventHookTemplate)Activator.CreateInstance(hookType, new object[] { modRecord });
                        
                        string hookMinStr = eventHook.HighlanderVersionMinimum;
                        string hookMaxStr = eventHook.HighlanderVersionMaximum;

                        if (log) Debug.Log($"{modRecord.Info.ModName} class {hookType.Name} requires versions {hookMinStr} through {hookMaxStr}");

                        HighlanderVersion version = new(matchVersion);
                        HighlanderVersion hookMin = new(hookMinStr);
                        HighlanderVersion hookMax = new(hookMaxStr);

                        if (hookMin.Get() <= version.Get() || version.Get() <= hookMax.Get())
                        {
                            if (!_eventHooks.ContainsKey(modRecord.Info.UniqueIdentifier))
                            {
                                _eventHooks.Add(modRecord.Info.UniqueIdentifier, eventHook);
                                return;
                            }
                        }
                        else
                        {
                            if (log) Debug.Log($"VERSION MISMATCHES {version.Get()}");
                        }
                    }
                }
            }
        }

        public void TriggerEvent(string eventName, List<ulong> modIDs, bool log = false)
        {
            if (log) Debug.Log($"{eventName} activating within {modIDs.Count} mods");

            foreach (ulong modID in modIDs)
            {
                _eventHooks.TryGetValue(modID, out CH_EventHookTemplate template);

                if (template != null)
                {
                    Type type = template.GetType();
                    MethodInfo method = type.GetMethod(eventName, BindingFlags.Instance | BindingFlags.Public);

                    if (log) Debug.Log($"--- {type.Name} retrieved");

                    if (method != null)
                    {
                        if (log) Debug.Log($"--- {type.Name} activated");
                        method.Invoke(template, new object[] { });
                    }
                    else
                    {
                        if (log) Debug.Log($"--- {type.Name} has no method for this event");
                    }
                }
            }
        }
    }
}

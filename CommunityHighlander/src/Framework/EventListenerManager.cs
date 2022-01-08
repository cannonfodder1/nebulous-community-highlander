using System;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using Modding;
using CommunityHighlander.Helpers;

namespace CommunityHighlander.Framework
{
    public class EventListenerManager
    {
        public enum ListenerRegisterResult
        {
            VersionMismatch,
            ListenerRegistered,
            NoListenerFound,
            ListenerAlreadyExists
        };

        public struct ListenerRegisterReport
        {
            public ListenerRegisterReport(ListenerRegisterResult result, string modName)
            {
                this.result = result;
                this.modName = modName;
                this.minimum = string.Empty;
                this.maximum = string.Empty;
            }

            public ListenerRegisterReport(ListenerRegisterResult result, string modName, string minimum, string maximum)
            {
                this.result = result;
                this.modName = modName;
                this.minimum = minimum;
                this.maximum = maximum;
            }

            public ListenerRegisterResult result;

            public string modName;
            public string minimum;
            public string maximum;
        }

        public struct HighlanderVersion
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
                return major * 10000 + minor * 100 + patch;
            }
        }

        private static EventListenerManager _instance = null;
        private Dictionary<ulong, EventListenerTemplate> _eventListeners;

        private EventListenerManager()
        {
            _eventListeners = new();
        }

        static EventListenerManager()
        {
        }

        public static EventListenerManager Instance
        {
            get
            {
                if (!IsInitialized)
                {
                    _instance = new EventListenerManager();
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

        public ListenerRegisterReport RegisterEventListener(ModRecord modRecord, bool log = false)
        {
            if (_eventListeners.ContainsKey(modRecord.Info.UniqueIdentifier))
            {
                if (log) Debug.Log($"Event listener from {modRecord.Info.ModName} already exists");

                return new ListenerRegisterReport(ListenerRegisterResult.ListenerAlreadyExists, modRecord.Info.ModName);
            }

            string matchVersion = Utilities.GetHighlanderVersion();

            Type modType = modRecord.GetType();
            FieldInfo modField = modType.GetField("_loadedAssemblies", BindingFlags.Instance | BindingFlags.NonPublic);
            List<Assembly> assemblies = (List<Assembly>)modField.GetValue(modRecord);

            if (log) Debug.Log($"Searching for event listener from {modRecord.Info.ModName} for version {matchVersion}");

            foreach (Assembly assembly in assemblies)
            {
                foreach (Type listenerType in assembly.GetTypes())
                {
                    if (log) Debug.Log($"Searching for event listener in class {listenerType.Name}");

                    if (listenerType.BaseType == typeof(EventListenerTemplate))
                    {
                        EventListenerTemplate eventListener = (EventListenerTemplate)Activator.CreateInstance(listenerType, new object[] { modRecord });

                        string listenerMinStr = eventListener.HighlanderVersionMinimum();
                        string listenerMaxStr = eventListener.HighlanderVersionMaximum();

                        if (log) Debug.Log($"{modRecord.Info.ModName} class {listenerType.Name} requires versions {listenerMinStr} through {listenerMaxStr}");

                        HighlanderVersion version = new(matchVersion);
                        HighlanderVersion listenerMin = new(listenerMinStr);
                        HighlanderVersion listenerMax = new(listenerMaxStr);

                        if (log) Debug.Log($"{listenerMin.Get()} < {version.Get()} < {listenerMax.Get()}");

                        if (listenerMin.Get() <= version.Get() && version.Get() <= listenerMax.Get())
                        {
                            _eventListeners.Add(modRecord.Info.UniqueIdentifier, eventListener);

                            return new ListenerRegisterReport(ListenerRegisterResult.ListenerRegistered, modRecord.Info.ModName, listenerMinStr, listenerMaxStr);
                        }
                        else
                        {
                            if (log) Debug.Log($"VERSION MISMATCHES {version.Get()}");

                            _eventListeners.Add(modRecord.Info.UniqueIdentifier, eventListener);

                            return new ListenerRegisterReport(ListenerRegisterResult.VersionMismatch, modRecord.Info.ModName, listenerMinStr, listenerMaxStr);
                        }
                    }
                }
            }

            return new ListenerRegisterReport(ListenerRegisterResult.NoListenerFound, modRecord.Info.ModName);
        }

        public void TriggerEventHook(string eventName, List<ulong> modIDs, bool log = false)
        {
            if (log) Debug.Log($"Event hook {eventName} activating within {modIDs.Count} mods");

            foreach (ulong modID in modIDs)
            {
                _eventListeners.TryGetValue(modID, out EventListenerTemplate template);

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

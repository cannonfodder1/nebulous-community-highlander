using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using UnityEngine;
using Bundles;
using Game;
using Game.Map;
using Missions;
using Modding;
using Munitions;
using Ships;
using Sound;
using Utility;

namespace CommunityHighlander
{
    public class CH_BundleManager : BundleManager
	{
		///////////////////////
		/// ALL-NEW METHODS ///
		///////////////////////

		public void AddHullComponent(string key, HullComponent component)
		{
			_components.Add(key, component);
		}



		////////////////////////
		/// MODIFIED METHODS ///
		////////////////////////

		private static CH_BundleManager _instance = null;

		public CH_BundleManager()
		{
		}

		static CH_BundleManager()
		{
		}
		
		public new static CH_BundleManager Instance
		{
			get
			{
				if (CH_BundleManager._instance == null)
				{
					if (Plugin.logMiscellaneous) UnityEngine.Debug.Log("CREATING NEW BUNDLEMANAGER INSTANCE");
					CH_BundleManager._instance = new CH_BundleManager();
				}
				return CH_BundleManager._instance;
			}
		}

		public new static bool IsInitialized
		{
			get
			{
				return CH_BundleManager._instance != null;
			}
		}

		public static void WipeInstance()
		{
			if (Plugin.logMiscellaneous) UnityEngine.Debug.Log("WIPING EXISTING BUNDLEMANAGER INSTANCE");
			CH_BundleManager._instance = null;
		}

		public new void ProcessAssetBundle(AssetBundle bundle, ModInfo fromMod)
		{
			if (Plugin.logMiscellaneous) UnityEngine.Debug.Log("Community Highlander Processing Bundle: " + bundle.name);

			TextAsset manifestText = this.LoadAsset<TextAsset>(bundle, "manifest.xml");
			if (manifestText == null)
			{
				throw new FileNotFoundException("Manifest not found for asset bundle: " + bundle.name);
			}

			BundleManifest manifest = BundleManifest.LoadManifest(manifestText);
			if (!string.IsNullOrEmpty(manifest.ResourceFile))
			{
				TextAsset resources = this.LoadAsset<TextAsset>(bundle, manifest.FullPath(manifest.ResourceFile));
				if (resources != null)
				{
					ResourceDefinitions.Instance.LoadResources(resources);
				}
			}

			this.LoadBasicEntries<FactionDescription>(manifest, manifest.Factions, bundle, this._factions, fromMod);
			this.LoadComponentEntries<Hull>(bundle, manifest, manifest.Hulls, this._hulls, null, fromMod);
			this.LoadComponentEntries<HullComponent>(bundle, manifest, manifest.Components, this._components, null, fromMod);
			this.LoadMunitionEntries(manifest, bundle, fromMod);
			this.LoadDebuffs(manifest, bundle);
			this.LoadMapEntries(manifest, bundle, fromMod);
			this.LoadBasicEntries<VoiceCallbackSet>(manifest, manifest.Voices, bundle, this._voices, fromMod);
			this.LoadBasicEntries<MissionSet>(manifest, manifest.MissionSets, bundle, this._missionSets, fromMod);
			this.LoadBasicEntries<ScenarioGraph>(manifest, manifest.Scenarios, bundle, this._scenarios, fromMod);
			this.LoadTipLists(manifest, bundle);
		}



		/////////////////////////
		/// UNTOUCHED METHODS ///
		/////////////////////////

		// Token: 0x0600288B RID: 10379 RVA: 0x000992F4 File Offset: 0x000974F4
		public new FactionDescription GetFaction(string key)
		{
			return this._factions.FirstOrDefault((FactionDescription x) => x.SaveKey == key);
		}

		// Token: 0x0600288C RID: 10380 RVA: 0x0009932C File Offset: 0x0009752C
		public new IEnumerable<Hull> GetFactionHulls(FactionDescription faction)
		{
			return from x in this._hulls.Values
				   where x.UseableByFaction(faction)
				   select x;
		}

		// Token: 0x0600288D RID: 10381 RVA: 0x00099368 File Offset: 0x00097568
		public new Hull GetHull(string key)
		{
			bool flag = string.IsNullOrEmpty(key);
			Hull result;
			if (flag)
			{
				result = null;
			}
			else
			{
				Hull hull;
				this._hulls.TryGetValue(key, out hull);
				result = hull;
			}
			return result;
		}

		// Token: 0x0600288E RID: 10382 RVA: 0x00099398 File Offset: 0x00097598
		public new IEnumerable<HullComponent> GetFactionComponents(FactionDescription faction)
		{
			return from x in this._components.Values
				   where x.UseableByFaction(faction)
				   select x;
		}

		// Token: 0x0600288F RID: 10383 RVA: 0x000993D4 File Offset: 0x000975D4
		public new HullComponent GetHullComponent(string key)
		{
			bool flag = string.IsNullOrEmpty(key);
			HullComponent result;
			if (flag)
			{
				result = null;
			}
			else
			{
				HullComponent component;
				this._components.TryGetValue(key, out component);
				result = component;
			}
			return result;
		}

		// Token: 0x06002890 RID: 10384 RVA: 0x00099404 File Offset: 0x00097604
		public new IMunition GetMunition(string key)
		{
			bool flag = string.IsNullOrEmpty(key);
			IMunition result;
			if (flag)
			{
				result = null;
			}
			else
			{
				IMunition munition;
				this._munitions.TryGetValue(key, out munition);
				result = munition;
			}
			return result;
		}

		// Token: 0x06002891 RID: 10385 RVA: 0x00099434 File Offset: 0x00097634
		public new IMunition GetMunition(Guid munitionKey)
		{
			IMunition result = this._munitions.Values.FirstOrDefault((IMunition x) => x.MunitionKey == munitionKey);
			return result;
		}

		// Token: 0x06002892 RID: 10386 RVA: 0x00099470 File Offset: 0x00097670
		public new Battlespace GetMap(string key)
		{
			bool flag = string.IsNullOrEmpty(key);
			Battlespace result;
			if (flag)
			{
				result = null;
			}
			else
			{
				Battlespace map;
				this._maps.TryGetValue(key, out map);
				result = map;
			}
			return result;
		}

		// Token: 0x06002893 RID: 10387 RVA: 0x000994A0 File Offset: 0x000976A0
		public new VoiceCallbackSet GetRandomVoice()
		{
			return this._voices[UnityEngine.Random.Range(0, this._voices.Count)];
		}

		// Token: 0x06002894 RID: 10388 RVA: 0x000994D0 File Offset: 0x000976D0
		public new string GetRandomTip(out int tipNumber)
		{
			bool flag = this._tips == null || this._tips.Count == 0;
			string result;
			if (flag)
			{
				tipNumber = 0;
				result = null;
			}
			else
			{
				tipNumber = UnityEngine.Random.Range(0, this._tips.Count);
				result = this._tips[tipNumber];
			}
			return result;
		}

		// Token: 0x06002895 RID: 10389 RVA: 0x00099528 File Offset: 0x00097728
		public new List<ComponentDebuff> GetDebuffsForComponentHierarchy(Type componentType)
		{
			List<ComponentDebuff> allDebuffs = new List<ComponentDebuff>();
			foreach (Type type in ReflectionHelpers.GetClassHierarchy(componentType, typeof(HullPart)))
			{
				List<ComponentDebuff> theseDebuffs = this.GetDebuffsForClass(type.Name, false);
				bool flag = theseDebuffs != null;
				if (flag)
				{
					allDebuffs.AddRange(theseDebuffs);
				}
			}
			return allDebuffs;
		}

		// Token: 0x06002897 RID: 10391 RVA: 0x000996FC File Offset: 0x000978FC
		private void LoadBasicEntries<TAsset>(BundleManifest manifest, List<BundleManifest.Entry> entries, AssetBundle bundle, List<TAsset> loadInto, ModInfo fromMod) where TAsset : UnityEngine.Object
		{
			int successful = 0;
			int failed = 0;
			foreach (BundleManifest.Entry entry in entries)
			{
				string path = manifest.FullPath(entry.Address);
				TAsset asset = this.LoadAsset<TAsset>(bundle, path);
				bool flag = asset == null;
				if (flag)
				{
					UnityEngine.Debug.LogError("Could not load asset " + path);
					failed++;
				}
				else
				{
					IBundleKeyed keyedAsset = asset as IBundleKeyed;
					bool flag2 = keyedAsset != null;
					if (flag2)
					{
						keyedAsset.SetKey(manifest.QualifiedName(entry));
					}
					IModSource modded = null;
					bool flag3;
					if (fromMod != null)
					{
						modded = (asset as IModSource);
						flag3 = (modded != null);
					}
					else
					{
						flag3 = false;
					}
					bool flag4 = flag3;
					if (flag4)
					{
						modded.SourceModId = new ulong?(fromMod.UniqueIdentifier);
					}
					loadInto.Add(asset);
					successful++;
				}
			}
			bool flag5 = entries.Count > 0;
			if (flag5)
			{
				UnityEngine.Debug.Log(string.Format("Loaded {0} {1} assets ({2} failed).", successful, typeof(TAsset), failed));
			}
		}

		// Token: 0x06002898 RID: 10392 RVA: 0x00099840 File Offset: 0x00097A40
		private void LoadComponentEntries<TComponent>(AssetBundle bundle, BundleManifest manifest, IReadOnlyList<BundleManifest.Entry> entries, Dictionary<string, TComponent> loadInto, Func<TComponent, bool> verifyAsset, ModInfo fromMod) where TComponent : MonoBehaviour, IBundleKeyed
		{
			int successful = 0;
			int failed = 0;
			foreach (BundleManifest.Entry entry in entries)
			{
				string path = manifest.FullPath(entry.Address);
				GameObject loaded = this.LoadAsset<GameObject>(bundle, path);
				bool flag = loaded == null;
				if (flag)
				{
					UnityEngine.Debug.LogError("Failed to load asset at address " + entry.Address);
					failed++;
				}
				else
				{
					TComponent saveAs = loaded.GetComponent<TComponent>();
					bool flag2 = saveAs == null;
					if (flag2)
					{
						UnityEngine.Debug.LogError(string.Format("Asset at address {0} does not have a component of type {1}", entry.Address, typeof(TComponent)));
						UnityEngine.Object.Instantiate<GameObject>(loaded);
						failed++;
					}
					else
					{
						bool flag3 = verifyAsset == null || verifyAsset(saveAs);
						if (flag3)
						{
							IBundleKeyed keyed = saveAs;
							bool flag4 = keyed != null;
							if (flag4)
							{
								keyed.SetKey(manifest.QualifiedName(entry));
							}
							IModSource modded = null;
							bool flag5;
							if (fromMod != null)
							{
								modded = (saveAs as IModSource);
								flag5 = (modded != null);
							}
							else
							{
								flag5 = false;
							}
							bool flag6 = flag5;
							if (flag6)
							{
								modded.SourceModId = new ulong?(fromMod.UniqueIdentifier);
							}
							loadInto.AddWithOverwrite(saveAs.SaveKey, saveAs);
							successful++;
						}
						else
						{
							UnityEngine.Debug.LogError("Asset at address " + entry.Address + " failed validation.");
							failed++;
						}
					}
				}
			}
			bool flag7 = entries.Count > 0;
			if (flag7)
			{
				UnityEngine.Debug.Log(string.Format("Loaded {0} {1} assets ({2} failed).", successful, typeof(TComponent), failed));
			}
		}

		// Token: 0x06002899 RID: 10393 RVA: 0x00099A1C File Offset: 0x00097C1C
		private T LoadAsset<T>(AssetBundle bundle, string name) where T : UnityEngine.Object
		{
			T asset = bundle.LoadAsset<T>(name);
			bool flag = asset == null;
			T result;
			if (flag)
			{
				string[] array = new string[6];
				array[0] = "No asset of type ";
				int num = 1;
				Type typeFromHandle = typeof(T);
				array[num] = ((typeFromHandle != null) ? typeFromHandle.ToString() : null);
				array[2] = " with name ";
				array[3] = name;
				array[4] = " found in bundle ";
				array[5] = bundle.name;
				UnityEngine.Debug.LogError(string.Concat(array));
				result = default(T);
			}
			else
			{
				result = asset;
			}
			return result;
		}

		// Token: 0x0600289A RID: 10394 RVA: 0x00099AA4 File Offset: 0x00097CA4
		private void LoadMunitionEntries(BundleManifest manifest, AssetBundle bundle, ModInfo fromMod)
		{
			int count = 0;
			foreach (BundleManifest.Entry entry in manifest.Munitions)
			{
				string path = manifest.FullPath(entry.Address);
				UnityEngine.Object munitionObj = this.LoadAsset<UnityEngine.Object>(bundle, path);
				bool flag = munitionObj == null;
				if (flag)
				{
					UnityEngine.Debug.LogError("Could not load munition " + path);
				}
				else
				{
					bool flag2 = munitionObj is IMunition;
					IMunition munition;
					if (flag2)
					{
						munition = (munitionObj as IMunition);
					}
					else
					{
						munition = (munitionObj as GameObject).GetComponent<IMunition>();
					}
					bool flag3 = munition == null;
					if (flag3)
					{
						UnityEngine.Debug.LogError("Munition " + entry.Name + " does not have an IMunition script");
					}
					else
					{
						bool flag4 = munition.MunitionKey == Guid.Empty;
						if (flag4)
						{
							UnityEngine.Debug.LogError("Munition " + entry.Name + " has an invalid copied asset Guid");
						}
						else
						{
							bool flag5 = fromMod != null;
							if (flag5)
							{
								munition.SourceModId = new ulong?(fromMod.UniqueIdentifier);
							}
							munition.SetKeys(entry.Name, manifest.QualifiedName(entry));
							this._munitions.AddWithOverwrite(munition.SaveKey, munition);
							count++;
						}
					}
				}
			}
			bool flag6 = manifest.Munitions.Count > 0;
			if (flag6)
			{
				UnityEngine.Debug.Log(string.Format("Loaded {0} munitions", count));
			}
		}

		// Token: 0x0600289B RID: 10395 RVA: 0x00099C4C File Offset: 0x00097E4C
		private void LoadMapEntries(BundleManifest manifest, AssetBundle bundle, ModInfo fromMod)
		{
			int count = 0;
			foreach (BundleManifest.Entry entry in manifest.Maps)
			{
				string path = manifest.FullPath(entry.Address);
				GameObject map = this.LoadAsset<GameObject>(bundle, path);
				bool flag = map == null;
				if (flag)
				{
					UnityEngine.Debug.LogError("Could not load map " + path);
				}
				else
				{
					Battlespace battlespace = map.GetComponent<Battlespace>();
					bool flag2 = battlespace == null;
					if (flag2)
					{
						UnityEngine.Debug.LogError("Map " + entry.Name + " does not have a Battlespace component on the root");
					}
					else
					{
						bool flag3 = fromMod != null;
						if (flag3)
						{
							battlespace.SourceModId = new ulong?(fromMod.UniqueIdentifier);
						}
						battlespace.MapName = entry.Name;
						this._maps.Add(battlespace.MapKey, battlespace);
						count++;
					}
				}
			}
			bool flag4 = manifest.Maps.Count > 0;
			if (flag4)
			{
				UnityEngine.Debug.Log(string.Format("Loaded {0} maps", count));
			}
		}

		// Token: 0x0600289C RID: 10396 RVA: 0x00099D84 File Offset: 0x00097F84
		private void LoadDebuffs(BundleManifest manifest, AssetBundle bundle)
		{
			int count = 0;
			foreach (BundleManifest.Entry entry in manifest.Debuffs)
			{
				string path = manifest.FullPath(entry.Address);
				ComponentDebuff debuff = this.LoadAsset<ComponentDebuff>(bundle, path);
				bool flag = debuff == null;
				if (flag)
				{
					UnityEngine.Debug.LogError("Could not load debuff " + path);
				}
				else
				{
					List<ComponentDebuff> classDebuffs = this.GetDebuffsForClass(debuff.AffectedClass, true);
					bool flag2 = classDebuffs.Any((ComponentDebuff x) => x.Key == debuff.Key);
					if (flag2)
					{
						UnityEngine.Debug.LogError("A debuff with the key " + debuff.Key + " already exists for class type " + debuff.AffectedClass);
					}
					else
					{
						classDebuffs.Add(debuff);
						count++;
					}
				}
			}
			bool flag3 = manifest.Debuffs.Count > 0;
			if (flag3)
			{
				UnityEngine.Debug.Log(string.Format("Loaded {0} debuffs", count));
			}
		}

		// Token: 0x0600289D RID: 10397 RVA: 0x00099EC0 File Offset: 0x000980C0
		private void LoadTipLists(BundleManifest manifest, AssetBundle bundle)
		{
			int fileCount = 0;
			int tipCount = 0;
			foreach (BundleManifest.Entry entry in manifest.TipLists)
			{
				string path = manifest.FullPath(entry.Address);
				TextAsset text = this.LoadAsset<TextAsset>(bundle, path);
				bool flag = text == null;
				if (flag)
				{
					UnityEngine.Debug.LogError("Could not load tip list " + path);
				}
				else
				{
					int previous = this._tips.Count;
					this._tips.AddRange(from x in text.text.Split(new char[]
					{
						'\n'
					})
										where !string.IsNullOrWhiteSpace(x)
										select x);
					tipCount += this._tips.Count - previous;
					fileCount++;
				}
			}
			bool flag2 = tipCount > 0;
			if (flag2)
			{
				UnityEngine.Debug.Log(string.Format("Loaded {0} files containing {1} tips", fileCount, tipCount));
			}
		}

		// Token: 0x0600289E RID: 10398 RVA: 0x00099FE8 File Offset: 0x000981E8
		private List<ComponentDebuff> GetDebuffsForClass(string className, bool create = false)
		{
			List<ComponentDebuff> classDebuffs;
			bool flag = !this._debuffs.TryGetValue(className, out classDebuffs) && create;
			if (flag)
			{
				classDebuffs = new List<ComponentDebuff>();
				this._debuffs.Add(className, classDebuffs);
			}
			return classDebuffs;
		}

		// Token: 0x040019AC RID: 6572
		private const string _bundlesDirectory = "Assets/AssetBundles/";

		// Token: 0x040019AD RID: 6573
		private const string _stockBundleName = "stock";

		// Token: 0x040019AF RID: 6575
		private List<FactionDescription> _factions = new List<FactionDescription>();

		// Token: 0x040019B0 RID: 6576
		private Dictionary<string, Hull> _hulls = new Dictionary<string, Hull>();

		// Token: 0x040019B1 RID: 6577
		private Dictionary<string, HullComponent> _components = new Dictionary<string, HullComponent>();

		// Token: 0x040019B2 RID: 6578
		private Dictionary<string, IMunition> _munitions = new Dictionary<string, IMunition>();

		// Token: 0x040019B3 RID: 6579
		private Dictionary<string, Battlespace> _maps = new Dictionary<string, Battlespace>();

		// Token: 0x040019B4 RID: 6580
		private Dictionary<string, List<ComponentDebuff>> _debuffs = new Dictionary<string, List<ComponentDebuff>>();

		// Token: 0x040019B5 RID: 6581
		private List<VoiceCallbackSet> _voices = new List<VoiceCallbackSet>();

		// Token: 0x040019B6 RID: 6582
		private List<MissionSet> _missionSets = new List<MissionSet>();

		// Token: 0x040019B7 RID: 6583
		private List<ScenarioGraph> _scenarios = new List<ScenarioGraph>();

		// Token: 0x040019B8 RID: 6584
		private List<string> _tips = new List<string>();

		// Token: 0x170009B3 RID: 2483
		// (get) Token: 0x06002881 RID: 10369 RVA: 0x000991F9 File Offset: 0x000973F9
		public new IReadOnlyCollection<FactionDescription> AllFactions
		{
			get
			{
				return this._factions;
			}
		}

		// Token: 0x170009B4 RID: 2484
		// (get) Token: 0x06002882 RID: 10370 RVA: 0x00099201 File Offset: 0x00097401
		public new FactionDescription DefaultFaction
		{
			get
			{
				return this._factions.FirstOrDefault<FactionDescription>();
			}
		}

		// Token: 0x170009B5 RID: 2485
		// (get) Token: 0x06002883 RID: 10371 RVA: 0x0009920E File Offset: 0x0009740E
		public new IReadOnlyCollection<Hull> AllHulls
		{
			get
			{
				return this._hulls.Values;
			}
		}

		// Token: 0x170009B6 RID: 2486
		// (get) Token: 0x06002884 RID: 10372 RVA: 0x0009921B File Offset: 0x0009741B
		public new IReadOnlyCollection<HullComponent> AllComponents
		{
			get
			{
				return this._components.Values;
			}
		}

		// Token: 0x170009B7 RID: 2487
		// (get) Token: 0x06002885 RID: 10373 RVA: 0x00099228 File Offset: 0x00097428
		public new IReadOnlyCollection<IMunition> AllMunitions
		{
			get
			{
				return this._munitions.Values;
			}
		}

		// Token: 0x170009B8 RID: 2488
		// (get) Token: 0x06002886 RID: 10374 RVA: 0x00099235 File Offset: 0x00097435
		public new IReadOnlyCollection<Battlespace> AllMaps
		{
			get
			{
				return this._maps.Values;
			}
		}

		// Token: 0x170009B9 RID: 2489
		// (get) Token: 0x06002887 RID: 10375 RVA: 0x00099242 File Offset: 0x00097442
		public new IReadOnlyCollection<MissionSet> MissionSets
		{
			get
			{
				return this._missionSets;
			}
		}

		// Token: 0x170009BA RID: 2490
		// (get) Token: 0x06002888 RID: 10376 RVA: 0x0009924A File Offset: 0x0009744A
		public new IReadOnlyCollection<ScenarioGraph> AllScenarios
		{
			get
			{
				return this._scenarios;
			}
		}
	}
}

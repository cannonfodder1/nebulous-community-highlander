// system references
using System;
using System.Collections.Generic;

// unity references
using UnityEngine;

// nebulous references
using Modding;
using Ships;

// highlander references
using CommunityHighlander;

// your namespaces can be named anything
namespace HighlanderExample
{
    // your classes can be named anything, but one class needs to inherit from CH_EventHookTemplate
    class CH_Example : CH_EventHookTemplate
    {
        // all event hook classes must implement a constructor that takes one ModRecord as a parameter
        // the mod record passed in will be the game's record of your own mod
        public CH_Example(ModRecord modRecord) : base(modRecord) { }

        // this is an event hook method, which will be called by the highlander at certain points in the game
        public override void OnModLoadedAtStartup()
        {
            // from here you can execute whatever kind of code you want
            // in this case, we want to create our new weapon when the mod is loaded
            CreateAutocannon(modRecord);
            CreateSmallBeam(modRecord);
        }

        // this is an event hook method, which will be called by the highlander at certain points in the game
        public override void OnModLoadedInLobby()
        {
            // from here you can execute whatever kind of code you want
            // in this case, we want to create our new weapon when the mod is loaded
            CreateAutocannon(modRecord);
            CreateSmallBeam(modRecord);
        }

        // this method doesn't necessarily have to be static, nor have this parameter, it could even be in another file
        public static void CreateAutocannon(ModRecord modRecord)
        {
            // the asset paths consist of an AssetBundle name followed by a Prefab name
            // if your mod does not create its own AssetBundle, you should probably save to the Stock bundle
            string copyFromPath = "Stock/Mk62 Cannon";
            string saveToPath = "Stock/Mk67 Autocannon";

            // the part name should exactly match the Prefab name in the asset path you're saving to
            // this is what shows up to the player in the game itself
            string partName = "Mk67 Autocannon";

            // the network ID should be a long string of random letters and numbers, so mash your keyboard
            // this string cannot be randomly generated each time this method is called, it must be consistent
            string networkID = "9fgh32vbi73wer89ofcq2";

            // you can use value changes to specify how you want your new module to differ from the one you're copying
            Dictionary<String, object> valueChanges = new();

            // some research of the game's assembly with dnSpy may be required to find the correct variable names and types
            valueChanges.Add("_size", new Vector3Int(6, 3, 6));
            valueChanges.Add("_pointCost", 75);
            valueChanges.Add("_magazineSize", 24);
            valueChanges.Add("_timeBetweenMuzzles", 0.2f);

            // this one is special, the highlander will change the part's visual size if you send this value change in
            valueChanges.Add("CH_ScaleLODs", new Vector3(2, 2, 2));

            // this is a Unity log statement, it can print out information to make debugging easier
            Debug.Log($"Creating {partName}");

            // finally, we call the Highlander's helper method with the information we constructed above
            CH_Helpers.CreateNebulousHullPart(
                modRecord,
                networkID,
                partName,
                saveToPath,
                copyFromPath,
                valueChanges);
        }

        public static void CreateSmallBeam(ModRecord modRecord)
        {
            string copyFromPath = "Stock/Mk610 Beam Turret";
            string saveToPath = "Stock/Mk601 Beam Turret";
            string partName = "Mk601 Beam Turret";
            string networkID = "f2w3h894097gfwq8ieaq";

            Dictionary<String, object> valueChanges = new();
            valueChanges.Add("_size", new Vector3Int(3, 1, 5));
            valueChanges.Add("_pointCost", 50);
            valueChanges.Add("_cooldownTime", 80);
            valueChanges.Add("ResourcesRequired", new ResourceModifier[] { CH_Helpers.CreateNebulousResourceModifier("Power", 1750) });
            valueChanges.Add("CH_ScaleLODs", new Vector3(0.5f, 0.5f, 0.5f));

            Debug.Log($"Creating {partName}");

            CH_Helpers.CreateNebulousHullPart(
                modRecord,
                networkID,
                partName,
                saveToPath,
                copyFromPath,
                valueChanges);
        }
    }
}

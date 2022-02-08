// system references
using System;
using System.Collections.Generic;

// unity references
using UnityEngine;

// nebulous references
using Modding;
using Ships;

// highlander references
using CommunityHighlander.Framework;
using CommunityHighlander.Helpers;

// your namespaces can be named anything
namespace HighlanderExample
{
    // your classes can be named anything, but one class needs to inherit from EventListenerTemplate
    class EventListener_Example : EventListenerTemplate
    {
        // all event listeners must implement a constructor that takes one ModRecord as a parameter.
        // the mod record passed in will contain information about your own mod,
        // and some helper methods will ask for it as proof of identity.
        public EventListener_Example(ModRecord modRecord) : base(modRecord) { }

        // all event listeners must override this method, which tells the highlander
        // the oldest highlander version that this mod can operate on
        public override string HighlanderVersionMinimum()
        {
            return "1.2.1";
        }

        // all event listeners must override this method, which tells the highlander
        // the latest highlander version that this mod can operate on
        public override string HighlanderVersionMaximum()
        {
            return "1.2.5";
        }

        // this is an event hook method, which will be called by the highlander at certain points in the game
        public override void OnModLoadedAtStartup()
        {
            // from here you can execute whatever kind of code you want
            // in this case, we want to create our new weapon when the mod is loaded
            CreateSmallBeam(modRecord);
        }

        // this is an event hook method, which will be called by the highlander at certain points in the game
        public override void OnModLoadedInLobby()
        {
            // from here you can execute whatever kind of code you want
            // in this case, we want to create our new weapon when the mod is loaded
            CreateSmallBeam(modRecord);
        }

        // this method doesn't necessarily have to be static, nor have this parameter,
        // it could even be in another file or have a different name, whatever works for you
        public static void CreateSmallBeam(ModRecord modRecord)
        {
            // the asset paths consist of an AssetBundle name followed by a Prefab name
            // if your mod does not create its own AssetBundle, you should save to the Stock bundle
            string copyFromPath = "Stock/Mk610 Beam Turret";
            string saveToPath = "Stock/Mk601 Beam Turret";

            // the part name should exactly match the Prefab name in the asset path you're saving to
            // this is what shows up to the player in the game itself
            string partName = "Mk601 Beam Turret";

            // the network ID should be a long string of random letters and numbers, so mash your keyboard
            // this string cannot be randomly generated each time this method is called, it must be hardcoded
            string networkID = "f2w3h894097gfwq8ieaq";

            // you can use value changes to specify how you want your new module to differ from the one you're copying
            // some research of the game's Nebulous.dll with dnSpy may be required to find the correct variable names and types
            Dictionary<String, object> valueChanges = new();
            valueChanges.Add("_size", new Vector3Int(3, 1, 5));
            valueChanges.Add("_pointCost", 50);
            valueChanges.Add("_crewRequired", 5);
            valueChanges.Add("_burstDuration", 5.0f);
            valueChanges.Add("_cooldownTime", 40);
            valueChanges.Add("_traverseRate", 10.0f);
            valueChanges.Add("_elevationRate", 15.0f);

            // with this value change, we use one of the highlander's helper methods to construct a replacement value
            valueChanges.Add("ResourcesRequired", new ResourceModifier[] { Workflows.CreateNebulousResourceModifier("Power", 1750) });

            // this one is special, the highlander will change the part's visual size if you make a value change with "CH_ScaleLODs"
            valueChanges.Add("CH_ScaleLODs", new Vector3(0.5f, 0.5f, 0.5f));

            // these aren't anything special, but I placed them away from the others because they don't affect the weapon's functionality
            valueChanges.Add("_flavorText", "");
            valueChanges.Add("_shortDescription", "Class 3 Particle Beam");
            valueChanges.Add("_longDescription",
                "The Mk601 Particle Beam is the lighter counterpart to the Mk610 - a weapon designed for point-blank range, " +
                "inheriting all the strengths and weaknesses of it's bigger brother. There are a few differences; the Mk601 " +
                "cannot sustain a beam burst for as long as the larger turret, but the smaller housing allows for much " +
                "faster rotation and elevation speeds."
            );

            // this is a Unity log statement, it prints info to your player.log file to make debugging easier
            Debug.Log($"Creating {partName}");

            // finally, we call the Highlander's helper method with the information we constructed above
            Workflows.CreateNebulousHullPart(
                modRecord,
                networkID,
                partName,
                saveToPath,
                copyFromPath,
                valueChanges);

            // if we wanted to, we could order the highlander to hide the old beam turret from the Fleet Editor
            //NCH_BundleManager.Instance.HideItem(copyFromPath, modRecord);
        }
    }
}

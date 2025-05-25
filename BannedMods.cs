using Reptile;
using BepInEx;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScoreAttack
{
    //[BepInPlugin("YourPluginID", "Your Plugin Name", "1.0.0")]
    public class BannedMods : BaseUnityPlugin
    {
        void Awake()
        {
            // Check if the specific mod you want to prevent is loaded
            if (IsAdvantageousModLoaded())
            {
                // Execute your desired action
                Core.Instance.UIManager.ShowNotification("The score attack app is not compatible with TrickGod!");
            }
        }

        // Method to check if the specific mod is loaded
        public static bool IsAdvantageousModLoaded()
        {
            // Check if TrickGod is used
            return BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("TrickGod");
        }
    }
}

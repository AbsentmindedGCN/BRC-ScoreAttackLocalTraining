using BepInEx;
using HarmonyLib;
using System.IO;

namespace ScoreAttack
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    [BepInDependency(CommonAPIGUID, BepInDependency.DependencyFlags.HardDependency)]
    public class ScoreAttackPlugin : BaseUnityPlugin
    {
        private const string CommonAPIGUID = "CommonAPI";
        public static ScoreAttackPlugin Instance { get; private set; }
        public string Directory => Path.GetDirectoryName(Info.Location);
        private void Awake()
        {
            Instance = this;
            // Create the singleton for our custom save data.
            new ScoreAttackSaveData();
            AppScoreAttackOffline.Initialize();
            AppScoreAttackStageSelect.Initialize();
            AppScoreAttack.Initialize();
            AppFPSLimit.Initialize();
            ScoreAttackManager.Initialize();
            AppMoveStyle.Initialize();
            AppGrindDebt.Initialize();
            AppExtras.Initialize();
            //AppPolice.Initialize(); - No longer needed

            // Patch Player, Phone, and NPCs so cops can spawn during battles, taxi appears, and more!
            var harmony = new Harmony(PluginInfo.PLUGIN_GUID);
            harmony.PatchAll();

            // Plugin startup logic
            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
        }
    }
}

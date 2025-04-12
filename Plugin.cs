using BepInEx;
using BepInEx.Logging;

using HarmonyLib;

namespace Ardot.REPO.CommandLine;

[BepInPlugin(PluginGUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
#pragma warning disable BepInEx002 // Classes with BepInPlugin attribute must inherit from BaseUnityPlugin
public class Plugin : BaseUnityPlugin
#pragma warning restore BepInEx002 // Classes with BepInPlugin attribute must inherit from BaseUnityPlugin
{
    public const string PluginGUID = "Ardot.REPO.CommandLine";
    internal static new ManualLogSource Logger;

    private void Awake()
    {
        Logger = base.Logger;
        Harmony harmony = new (PluginGUID);
        harmony.Patch(AccessTools.Method(typeof(ChatManager), "MessageSend"), prefix: new HarmonyMethod(typeof(Patches), "MessageSendPrefix"));
        harmony.Patch(AccessTools.Method(typeof(MainMenuOpen), "Start"), postfix: new HarmonyMethod(typeof(Patches), "StartPostfix"));
        harmony.Patch(AccessTools.Method(typeof(ChatManager), "Update"), prefix: new HarmonyMethod(typeof(Patches), "ChatManagerUpdatePrefix"), postfix: new HarmonyMethod(typeof(Patches), "ChatManagerUpdatePostfix"));

        Utils.InitUtils();
    }
}

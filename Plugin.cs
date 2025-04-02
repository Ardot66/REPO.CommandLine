using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.Mono;
using System.Reflection;

using HarmonyLib;

namespace Ardot.REPO.CommandLine;

[BepInPlugin(PluginGUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
    public const string PluginGUID = "Ardot.REPO.CommandLine";
    internal static new ManualLogSource Logger;

    private void Awake()
    {
        Logger = base.Logger;
        Harmony harmony = new (PluginGUID);
        harmony.Patch(AccessTools.Method(typeof(ChatManager), "MessageSend"), prefix: new HarmonyMethod(typeof(Patches), "MessageSendPrefix"));
    }
}

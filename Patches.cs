using System;
using System.Collections.Generic;

namespace Ardot.REPO.CommandLine;

public static class Patches
{    
    public static bool GameStarted = false;

    public static bool MessageSendPrefix(List<string> ___chatHistory, string ___chatMessage, bool _possessed = false)
    {
        string[] messageComponents = ___chatMessage.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        for(int x = 0; x < messageComponents.Length; x++)
            messageComponents[x] = messageComponents[x].ToLower();  

        if(string.IsNullOrEmpty(messageComponents[0]) || messageComponents[0][0] != Commands.CommandChar)
            return SemiFunc.IsMultiplayer();

        CommandInfo command = Utils.FindCommand(messageComponents[0]);

        if(command == null)
            return SemiFunc.IsMultiplayer();

        int prevIndex = ___chatHistory.IndexOf(___chatMessage);

        if(prevIndex != -1)
            ___chatHistory.RemoveAt(prevIndex);
        else if(___chatHistory.Count > 20)
            ___chatHistory.RemoveAt(0);

        ___chatHistory.Add(___chatMessage);

        string[] messageArguments = new string[messageComponents.Length - 1];
        Array.ConstrainedCopy(messageComponents, 1, messageArguments, 0, messageArguments.Length);

        string result = "";

        try { result = command.Command(messageArguments); }
        catch (Exception exception) { result = $"Exception ocurred while executing command: {exception}"; }

        Plugin.Logger.LogInfo(result);

        return false;
    }

    public static void StartPostfix()
    {
        if(GameStarted)
            return;

        GameStarted = true;

        string[] commandArgs = Environment.GetCommandLineArgs();
        string arg;

        if(Utils.TryGetArg(commandArgs, 0, out arg, "-d"))
        {
            arg = arg.ToLower();

            GameManager.instance.localTest = false;
            RunManager.instance.ResetProgress();
            RunManager.instance.Set("waitToChangeScene", true);
            MainMenuOpen.instance.NetworkConnect();

            RunManager.ChangeLevelType levelType;
            if(arg == "shop")
            {
                levelType = RunManager.ChangeLevelType.Shop;
                SteamManager.instance.LockLobby();
                DataDirector.instance.RunsPlayedAdd();
            }
            else if(arg == "lobby")
                levelType = RunManager.ChangeLevelType.LobbyMenu;
            else
            {
                levelType = RunManager.ChangeLevelType.RunLevel;
                SteamManager.instance.LockLobby();
                DataDirector.instance.RunsPlayedAdd();
            }

            RunManager.instance.ChangeLevel(true, false, levelType);
        }

        if(Utils.TryGetArg(commandArgs, 0, out arg, "-l"))
        {
            if(!int.TryParse(arg, out int levelIndex))
                levelIndex = 0;

            RunManager.instance.levelsCompleted = levelIndex;
            StatsManager.instance.runStats["level"] = levelIndex;
        }

        if(Utils.TryGetArg(commandArgs, 0, out arg, "-lc"))
        {
            arg = arg.ToLower();

            RunManager.instance.levelCurrent = arg switch
            {
                "manor" => RunManager.instance.levels[0],
                "academy" => RunManager.instance.levels[1],
                "station" => RunManager.instance.levels[2],
                _ => RunManager.instance.levels[0]
            };
        }
    }

    public static bool ChatManagerUpdatePrefix(out int __state)
    {
        __state = GameManager.instance.gameMode;
        GameManager.instance.SetGameMode(1);
        return true;
    }

    public static void ChatManagerUpdatePostfix(int __state)
    {
        GameManager.instance.SetGameMode(__state);
    }
}
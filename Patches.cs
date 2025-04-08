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

        if(!Commands.CommandList.TryGetValue(messageComponents[0], out CommandInfo command))
            return true;

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
        int debugIndex = Array.IndexOf(commandArgs, "-d");

        if(debugIndex != -1)
        {
            string arg = "";
            if(commandArgs.Length > debugIndex + 1)
                arg = commandArgs[debugIndex + 1];

            GameManager.instance.localTest = false;
            RunManager.instance.ResetProgress();
            SemiFunc.SaveFileCreate();
            RunManager.instance.Set("waitToChangeScene", true);
            MainMenuOpen.instance.NetworkConnect();

            RunManager.ChangeLevelType levelType;
            if(arg == "Shop")
            {
                levelType = RunManager.ChangeLevelType.Shop;
                SteamManager.instance.LockLobby();
                DataDirector.instance.RunsPlayedAdd();
            }
            else if(arg == "Lobby")
                levelType = RunManager.ChangeLevelType.LobbyMenu;
            else
            {
                levelType = RunManager.ChangeLevelType.RunLevel;
                SteamManager.instance.LockLobby();
                DataDirector.instance.RunsPlayedAdd();
            }

            RunManager.instance.ChangeLevel(true, false, levelType);
        }

    }
}
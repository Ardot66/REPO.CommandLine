using System;
using System.Collections.Generic;

namespace Ardot.REPO.CommandLine;

public static class Patches
{    
    public static bool MessageSendPrefix(List<string> ___chatHistory, string ___chatMessage, bool _possessed = false)
    {
        string[] messageComponents = ___chatMessage.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        for(int x = 0; x < messageComponents.Length; x++)
            messageComponents[x] = messageComponents[x].ToLower();  

        if(!Commands.CommandList.TryGetValue(messageComponents[0], out CommandInfo command))
            return true;

        int prevIndex = ___chatHistory.IndexOf(messageComponents[0]);

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
}
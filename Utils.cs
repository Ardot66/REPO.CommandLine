using UnityEngine;
using HarmonyLib;
using System.Reflection;
using System.Collections.Generic;
using System;

namespace Ardot.REPO.CommandLine;
public static class Utils
{
    public static FieldInfo PlayerNameFieldInfo;

    private record struct FieldKey(Type Type, string Field);

    private static Dictionary<FieldKey, FieldInfo> Fields = new ();

    public static void InitUtils()
    {
        PlayerNameFieldInfo = AccessTools.Field(typeof(PlayerAvatar), "playerName");
    }

    public static Dictionary<string, GameObject> GetEnemies()
    {
        List<EnemySetup> enemyList = new ();
        enemyList.AddRange(EnemyDirector.instance.enemiesDifficulty1);
        enemyList.AddRange(EnemyDirector.instance.enemiesDifficulty2);
        enemyList.AddRange(EnemyDirector.instance.enemiesDifficulty3);

        Dictionary<string, GameObject> enemyObjects = new ();

        for(int x = 0; x < enemyList.Count; x++)
        {
            List<GameObject> spawnObjects = enemyList[x].spawnObjects;
            for (int y = 0; y < spawnObjects.Count; y++)
            {
                GameObject enemyObject = spawnObjects[y];

                if(enemyObject.GetComponent<EnemyParent>() is not EnemyParent enemyParent)
                    continue;

                string enemyName = enemyParent.enemyName.ToLower().Replace(' ', '-');

                if(enemyObjects.ContainsKey(enemyName))
                    continue;

                enemyObjects.Add(enemyName, enemyObject);
            }
        }

        return enemyObjects;
    }

    public static string PlayerName(PlayerAvatar player)
    {
        return ((string)PlayerNameFieldInfo.GetValue(player)).Replace(' ','-');
    }

    public static PlayerAvatar GetPlayer(string name)
    {
        name = name.ToLower();

        PlayerAvatar mostCharsMatchedPlayer = null;
        int mostCharsMatched = 0;

        for(int x = 0; x < GameDirector.instance.PlayerList.Count; x++)
        {
            PlayerAvatar playerAvatar = GameDirector.instance.PlayerList[x];
            int charsMatched = 0;
            string playerName = PlayerName(playerAvatar).ToLower();

            for(int y = 0; y < name.Length && y < playerName.Length; y++)
            {
                if(name[y] == playerName[y])
                    charsMatched++;
                else
                    break;
            }

            if(charsMatched > mostCharsMatched)
            {
                mostCharsMatched = charsMatched;
                mostCharsMatchedPlayer = playerAvatar;
            }
        }

        return mostCharsMatchedPlayer;
    }

    public static object Get<O>(this O obj, string field)
    {
        return GetField<O>(field).GetValue(obj);
    }

    public static T Get<T, O>(this O obj, string field)
    {
        return (T)Get(obj, field);
    }

    public static void Set<T>(this T obj, string field, object value)
    {
        GetField<T>(field).SetValue(obj, value);
    }

    public static FieldInfo GetField<T>(string field)
    {
        FieldKey fieldKey = new (typeof(T), field);

        if(!Fields.TryGetValue(fieldKey, out FieldInfo fieldInfo))
        {
            fieldInfo = AccessTools.Field(typeof(T), field);
            Fields.Add(fieldKey, fieldInfo);
        }

        return fieldInfo;
    }

    public static bool TryGetArg(string[] args, int index, out string arg, params string[] aliases)
    {
        int found = 0;
        int foundIndex = -1;
        for(int x = 0; x < args.Length; x++)
        {
            string curArg = args[x];

            for(int y = 0; y < aliases.Length; y++)
            {
                if(aliases[y] != curArg)
                    continue;

                found++;
                if(found > index)
                {
                    foundIndex = x;
                    goto BreakLoops;
                }
            }   
        }

        BreakLoops:

        if(foundIndex != -1)
        {
            arg = foundIndex + 1 < args.Length ? args[foundIndex + 1] : "";
            return true;
        }

        arg = "";
        return false;
    }

    public static CommandInfo FindCommand(string command)
    {
        CommandInfo bestCommand = null;
        int mostCharsMatching = 0;
        int mostCharsMatchingLateness = 0;

        for(int x = 0; x < Commands.CommandList.Count; x++)
        {
            CommandInfo commandInfo = Commands.CommandList[x];

            for(int y = 0; y < commandInfo.CommandAliases.Length; y++)
            {
                string commandAlias = commandInfo.CommandAliases[y];
                int aliasLateness = 1;
                int aliasCharsMatching = 1;

                for(int z = 0; z < commandAlias.Length && aliasCharsMatching < command.Length; z++)
                {
                    if(commandAlias[z] == command[aliasCharsMatching])
                    {
                        aliasCharsMatching++;
                        aliasLateness *= z + 1;
                    }
                }

                if(aliasCharsMatching > mostCharsMatching || aliasCharsMatching == mostCharsMatching && aliasLateness < mostCharsMatchingLateness)
                {
                    mostCharsMatching = aliasCharsMatching;
                    mostCharsMatchingLateness = aliasLateness;
                    bestCommand = commandInfo;
                }
            }
        }

        return bestCommand;
    }
}
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

        for(int x = 0; x < GameDirector.instance.PlayerList.Count; x++)
        {
            PlayerAvatar playerAvatar = GameDirector.instance.PlayerList[x];

            if(name == PlayerName(playerAvatar).ToLower())
                return playerAvatar;
        }

        return null;
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
}
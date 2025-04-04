using UnityEngine;
using HarmonyLib;
using System.Reflection;
using System.Collections.Generic;

namespace Ardot.REPO.CommandLine;
public static class Utils
{
    public static FieldInfo PlayerNameFieldInfo;

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
}
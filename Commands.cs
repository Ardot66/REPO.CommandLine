using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.Mono;

using Photon.Pun;
using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using System.Collections.Generic;

namespace Ardot.REPO.CommandLine;

public class CommandInfo(Func<string[], string> command, string summary = "", string help = "")
{
    public Func<string[], string> Command = command;
    public string Summary = summary;
    public string Help = help;
}

public static class Commands
{
    public static Dictionary<string, CommandInfo> CommandList = new ()
    {
        {"/spawnenemy", new CommandInfo(Spawn, "[name of enemy] [amount] Spawns a number of enemies around the map")},
        {"/tpenemy", new CommandInfo(TPEnemy, "[name of enemy] [number of enemies] Teleports enemies directly in front of you")},
        {"/listenemy", new CommandInfo(ListEnemy, "Lists all enemies currently spawned and despawned")},
        {"/godmode", new CommandInfo(Godmode, "[true/false] Sets the player to be invincible or not invincible")},
        {"/sethealth", new CommandInfo(SetHealth, "[player] [health] [max health] Sets the health and max health of the player")},
        {"/help", new CommandInfo(Help, "Prints this information")},
        {"/listplayers", new CommandInfo(ListPlayers, "Lists the names of all players")}
    };

    public const string ErrTooFewArgs = "Error: Too few arguments";


    public static string Help(string[] args)
    {
        List<char> message = ['\n'];

        if(args.Length > 0)
        {
            for(int x = 0; x < args.Length; x++)
            {
                string commandName = args[x];
                KeyValuePair<string, CommandInfo> bestMatch = default;

                foreach(KeyValuePair<string, CommandInfo> command in CommandList)
                    if(command.Key.Contains(commandName) && command.Key.Length < bestMatch.Key.Length)
                        bestMatch = command;
                        
                if(bestMatch.Value == null)
                    message.AddRange($"Could not find command: {commandName}\n");
                else
                    PrintCommand(bestMatch.Key, bestMatch.Value, true);
            }
        }
        else
            foreach(KeyValuePair<string, CommandInfo> command in CommandList)
                PrintCommand(command.Key, command.Value, false);

        void PrintCommand(string name, CommandInfo info, bool detailed)
        {
            message.AddRange(name);
            for(int x = 0; x < 15 - name.Length; x++);
                message.Add(' ');
            message.AddRange(info.Summary);
            message.Add('\n');

            if(detailed)
            {
                message.AddRange(info.Help);
                message.Add('\n');
            }
        }

        return new string([.. message]);
    }

    public static string ListPlayers(string[] args)
    {
        List<char> message = ['\n'];

        for(int x = 0; x < GameDirector.instance.PlayerList.Count; x++)
            message.AddRange($"{Utils.PlayerName(GameDirector.instance.PlayerList[x])}");

        return new string([.. message]);
    }

    public static string SetHealth(string[] args)
    {
        if(args.Length < 2)
            return ErrTooFewArgs;

        PlayerAvatar player = Utils.GetPlayer(args[0]);

        if(player == null)
            return $"Player {args[0]} not found";

        int.TryParse(args[1], out int health);
        int maxHealth = 100;
        PhotonView photonView = (PhotonView)AccessTools.Field(typeof(PlayerHealth), "photonView").GetValue(player.playerHealth);

        if(args.Length > 2)
            int.TryParse(args[2], out maxHealth);
        else
            maxHealth = (int)AccessTools.Field(typeof(PlayerHealth), "maxHealth").GetValue(player.playerHealth);

        photonView.RPC("UpdateHealthRPC", RpcTarget.All, health, maxHealth, true);
        return $"Successfully updated {args[0]}'s health to {health} / {maxHealth}";
    }

    public static string Spawn(string [] args)
    {
        if(!PhotonNetwork.LocalPlayer.IsMasterClient)
            return "Cannot currently spawn enemies as client";

        if(args.Length < 1)
            return ErrTooFewArgs;

        string enemyName = args[0];
        int count = 1;

        if(args.Length > 1)
            int.TryParse(args[1], out count);
        
        Dictionary<string, GameObject> enemies = Utils.GetEnemies();
        if(!enemies.TryGetValue(enemyName, out GameObject enemy))
            return "Enemy name not valid";

        Vector3 position = Vector3.zero;

        for(int x = 0; x < count; x++)
        {
            GameObject enemyInstance;
            if (GameManager.instance.gameMode == 0)
                enemyInstance = UnityEngine.Object.Instantiate(enemy, position, Quaternion.identity);
            else
                enemyInstance = PhotonNetwork.InstantiateRoomObject("Enemies" + "/" + enemy.name, position, Quaternion.identity, 0, null);

            EnemyParent enemyParent = enemyInstance.GetComponent<EnemyParent>();
            if (enemyParent)
            {
                AccessTools.Field(typeof(EnemyParent), "SetupDone").SetValue(enemyParent, true);
                enemyInstance.GetComponentInChildren<Enemy>().EnemyTeleported(position);

                FieldInfo enemiesSpawnTargetFI = AccessTools.Field(typeof(LevelGenerator), "EnemiesSpawnTarget");
                enemiesSpawnTargetFI.SetValue(LevelGenerator.Instance, ((int)enemiesSpawnTargetFI.GetValue(LevelGenerator.Instance)) + 1);
        }
        }

        return $"Successfully spawned {count} {enemyName}";
    }
    
    public static string TPEnemy(string[] args)
    {
        if(args.Length < 1)
            return ErrTooFewArgs;

        string enemyName = args[0].ToLower();
        int count = 1;

        if(args.Length > 1)
            int.TryParse(args[1], out count);

        FieldInfo enemyFI = AccessTools.Field(typeof(EnemyParent), "Enemy");
        List<EnemyParent> enemiesSpawned = EnemyDirector.instance.enemiesSpawned;
        int countRemaining = count;
        for(int x = 0; x < enemiesSpawned.Count && countRemaining > 0; x++)
        {
            EnemyParent enemy = enemiesSpawned[x];
            if(enemy.EnableObject.activeSelf && enemy.enemyName.ToLower() == enemyName)
            {
                ((Enemy)enemyFI.GetValue(enemy)).EnemyTeleported(PlayerAvatar.instance.transform.position + PlayerAvatar.instance.transform.forward * 3);
                countRemaining--;
            }
        }

        return $"Successfully teleported {count - countRemaining} {enemyName} to you";
    }

    public static string ListEnemy(string[] args)
    {
        Dictionary<string, ValueTuple<int, int>> enemies = new ();

        List<EnemyParent> enemiesSpawned = EnemyDirector.instance.enemiesSpawned;
        for(int x = 0; x < enemiesSpawned.Count; x++)
        {
            EnemyParent enemy = enemiesSpawned[x];
            if(!enemies.ContainsKey(enemy.enemyName))
                enemies.Add(enemy.enemyName, (0, 0));

            ValueTuple<int, int> enemyAmounts = enemies[enemy.enemyName];

            if(enemy.EnableObject.activeSelf)
                enemyAmounts.Item1 += 1;
            enemyAmounts.Item2 += 1;

            enemies[enemy.enemyName] = enemyAmounts;
        }

        foreach(string enemyName in enemies.Keys)
            Plugin.Logger.LogInfo($"{enemyName} : {enemies[enemyName].Item1} / {enemies[enemyName].Item2}");

        return "Enemies listed successfully";
    }

    public static string Godmode(string[] args)
    {
        if(args.Length < 1)
            return ErrTooFewArgs;

        PlayerAvatar player = Utils.GetPlayer(args[0]);     

        if(player == null)
            return $"Player {args[0]} not found"; 

        FieldInfo godModeFI = AccessTools.Field(typeof(PlayerHealth), "godMode");
        bool godmode = false;
        
        if(args.Length > 1)
            bool.TryParse(args[0], out godmode);
        else
            godmode = !(bool)godModeFI.GetValue(player.playerHealth); 

        godModeFI.SetValue(player.playerHealth, godmode); 

        return godmode ? "Godmode activated" : "Godmode deactivated";
    }
}


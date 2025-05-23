using Photon.Pun;
using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using System.Collections.Generic;

namespace Ardot.REPO.CommandLine;

public class CommandInfo(string[] commandAliases, Func<string[], string> command, string summary = "", string help = "")
{
    public string[] CommandAliases = commandAliases;
    public Func<string[], string> Command = command;
    public string Summary = summary;
    public string Help = help;
}

public static class Commands
{
    public static char CommandChar = '/';

    public static List<CommandInfo> CommandList = new ()
    {
        { new CommandInfo(["spawnenemy"], Spawn, "[name of enemy] [amount] Spawns a number of enemies around the map")},
        { new CommandInfo(["spawnitem"], SpawnItem, "[name of item] [amount] Spawns a number of items at your position")},
        { new CommandInfo(["tpenemy"], TPEnemy, "[name of enemy] [number of enemies] Teleports enemies directly in front of you")},
        { new CommandInfo(["listenemy"], ListEnemy, "Lists all enemies currently spawned and despawned")},
        { new CommandInfo(["listitem"], ListItem, "Lists the names of all tool items")},
        { new CommandInfo(["godmode"], Godmode, "[true/false] Sets the player to be invincible or not invincible")},
        { new CommandInfo(["sethealth"], SetHealth, "[player] [health] [max health] Sets the health and max health of the player")},
        { new CommandInfo(["help"], Help, "Prints this information")},
        { new CommandInfo(["listplayers"], ListPlayers, "Lists the names of all players")}
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
                CommandInfo command = Utils.FindCommand(args[0]);
                        
                if(command == null)
                    message.AddRange($"Could not find command: {commandName}\n");
                else
                    PrintCommand(command, true);
            }
        }
        else
            for(int x = 0; x < CommandList.Count; x++)
                PrintCommand(CommandList[x], false);

        void PrintCommand(CommandInfo info, bool detailed)
        {
            int aliasesLength = 0;
            for(int x = 0; x < info.CommandAliases.Length; x++)
            {
                string alias = info.CommandAliases[x];
                message.AddRange(alias);
                aliasesLength += alias.Length;
            }

            for(int x = 0; x < 20 - aliasesLength; x++);
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
            message.AddRange($"{Utils.PlayerName(GameDirector.instance.PlayerList[x])}\n");

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

    public static string SpawnItem(string[] args)
    {
        if(args.Length < 1)
            return ErrTooFewArgs;

        List<char> itemName = [.. args[0].ToLower()];
        
        itemName[0] = char.ToUpper(itemName[0]);
        for(int x = 0; x < itemName.Count; x++)
        {
            if(itemName[x] == '-')
            {
                itemName[x] = ' ';

                if(x + 1 < itemName.Count)
                    itemName[x + 1] = char.ToUpper(itemName[x + 1]);
            }
        }

        Item item = StatsManager.instance.itemDictionary[new string([.. itemName])];

        if(args.Length < 2 || !int.TryParse(args[1], out int spawnCount))
            spawnCount = 1;

        for(int x = 0; x < spawnCount; x++)
        {
            if(SemiFunc.IsMasterClient())
                PhotonNetwork.InstantiateRoomObject("Items/" + item.prefab.name, PlayerAvatar.instance.transform.position, Quaternion.identity, 0, null);
            else if(!SemiFunc.IsMultiplayer())
                GameObject.Instantiate(item.prefab, PlayerAvatar.instance.transform);
        }

        return $"Sucessfully instantiated {spawnCount} {args[0]}";
    }

    public static string ListItem(string[] args)
    {
        List<char> message = ['\n'];

        foreach(string itemName in StatsManager.instance.itemDictionary.Keys)
        {
            message.AddRange(itemName.Replace(' ', '-'));
            message.Add('\n');
        }

        return new string([.. message]);
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


using BepInEx;
using GameNetcodeStuff;
using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;

namespace ToxicOmega_Tools.Patches
{
    [HarmonyPatch(typeof(PlayerControllerB))]
    internal class PlayerControllerB_Patch : MonoBehaviour
    {
        [HarmonyPatch(nameof(PlayerControllerB.KillPlayer))]
        [HarmonyPostfix]
        static void DeadPlayerEnableHUD(PlayerControllerB __instance)
        {
            if (Plugin.CheckPlayerIsHost(__instance))
            {
                HUDManager HUD = HUDManager.Instance;
                HUD.HideHUD(false);
                HUD.ToggleHUD(true);
            }
        }

        [HarmonyPatch(nameof(PlayerControllerB.AllowPlayerDeath))]
        [HarmonyPrefix]
        static bool OverrideDeath(PlayerControllerB __instance)
        {
            if (!Plugin.CheckPlayerIsHost(__instance))
                return true;
            return !Plugin.godmode;
        }

        [HarmonyPatch(nameof(PlayerControllerB.Update))]
        [HarmonyPostfix]
        static void Update(PlayerControllerB __instance)
        {
            if (CustomGUI.nearbyVisible || CustomGUI.fullListVisible)
            {
                Vector3 localPosition = (__instance.isPlayerDead && __instance.spectatedPlayerScript != null) ? __instance.spectatedPlayerScript.transform.position : __instance.transform.position;
                CustomGUI.posLabelText = $"Time: {(RoundManager.Instance.timeScript.hour + 6 > 12 ? RoundManager.Instance.timeScript.hour - 6 : RoundManager.Instance.timeScript.hour + 6)}{(RoundManager.Instance.timeScript.hour + 6 < 12 ? "am" : "pm")}\n";
                CustomGUI.posLabelText += $"GodMode: {(Plugin.godmode ? "Enabled" : "Disabled")}\n";
                CustomGUI.posLabelText += $"X: {Math.Round(localPosition.x, 1)}\nY: {Math.Round(localPosition.y, 1)}\nZ: {Math.Round(localPosition.z, 1)}";
            }

            NoClipHandler();
            DefogHandler();

            // If you're the host, you get hotkey priveleges :)
            if (Plugin.CheckPlayerIsHost(__instance))
            {
                // Hotkey presses were registering multiple times, so. Basic debounce.
                if (Plugin.keydebounce <= 0)
                {
                    CheckHotKey();
                }
                else
                {
                    Plugin.keydebounce--;
                }
            }
        }

        private static void NoClipHandler()
        {
            if (SceneManager.GetActiveScene().name != "SampleSceneRelay")
                return;
            var player = GameNetworkManager.Instance.localPlayerController;
            if (player == null)
                return;
            var camera = player.gameplayCamera.transform;
            if (camera == null)
                return;
            var collider = player.GetComponent<CharacterController>() as Collider;
            if (collider == null)
                return;

            if (Plugin.noclip)
            {
                collider.enabled = false;
                var dir = new Vector3();

                // Horizontal
                if (UnityInput.Current.GetKey(KeyCode.W))
                    dir += camera.forward;
                if (UnityInput.Current.GetKey(KeyCode.S))
                    dir += camera.forward * -1;
                if (UnityInput.Current.GetKey(KeyCode.D))
                    dir += camera.right;
                if (UnityInput.Current.GetKey(KeyCode.A))
                    dir += camera.right * -1;

                // Vertical
                if (UnityInput.Current.GetKey(KeyCode.Space))
                    dir.y += camera.up.y;
                if (UnityInput.Current.GetKey(KeyCode.C))
                    dir.y += camera.up.y * -1;

                var prevPos = player.transform.localPosition;
                if (prevPos.Equals(Vector3.zero))
                    return;
                if (!player.isTypingChat)
                {
                    var newPos = prevPos + dir * ((UnityInput.Current.GetKey(KeyCode.LeftShift) ? 15f : 5f) * Time.deltaTime);
                    if (newPos.y < -100f && !player.isInsideFactory)
                    {
                        Plugin.PlayerTeleportEffects(player.playerClientId, true, false);
                    }
                    else if (newPos.y >= -100f && player.isInsideFactory)
                    {
                        Plugin.PlayerTeleportEffects(player.playerClientId, false, false);
                    }
                    player.transform.localPosition = newPos;
                }
            }
            else
            {
                collider.enabled = true;
            }
        }

        private static void DefogHandler()
        {
            GameObject.Find("Systems")?.transform.Find("Rendering")?.Find("VolumeMain")?.gameObject.SetActive(!Plugin.defog);
            GameObject.Find("Environment")?.transform.Find("Lighting")?.Find("GroundFog")?.gameObject.SetActive(!Plugin.defog);
            GameObject.Find("Environment")?.transform.Find("Lighting")?.Find("BrightDay")?.Find("Sun")?.Find("SunAnimContainer")?.Find("StormVolume")?.gameObject.SetActive(!Plugin.defog);
            //GameObject.Find("Environment")?.transform.Find("Lighting")?.Find("BrightDay")?.Find("Local Volumetric Fog")?.gameObject.SetActive(!Plugin.defog);
            //GameObject.Find("Environment")?.transform.Find("Lighting")?.Find("BrightDay")?.Find("Sun")?.Find("BlizzardSunAnimContainer")?.Find("Sky and Fog Global Volume")?.gameObject.SetActive(!Plugin.defog);
            //RenderSettings.fog = !Plugin.defog;
        }

        private static void CheckHotKey()
        {
            // Spawn low level
            if (UnityInput.Current.GetKeyUp(KeyCode.F5))
            {
                Plugin.keydebounce = 30;
                SpawnRandomEnemy(false);
                Plugin.LogMessage("Spawned a regular beast.");
            }

            // Spawn high level
            if (UnityInput.Current.GetKeyUp(KeyCode.F6))
            {
                Plugin.keydebounce = 30;
                SpawnRandomEnemy(true);
                Plugin.LogMessage("Spawned a dangerous beast.");
            }

            // Dead Money
            if (UnityInput.Current.GetKeyUp(KeyCode.F7))
            {
                Plugin.keydebounce = 30;
                DeadMoney();
                Plugin.LogMessage("Dead Money.");
            }

            // Lights on/lights off
            if (UnityInput.Current.GetKeyUp(KeyCode.F8))
            {
                Plugin.keydebounce = 30;
                ToggleBreaker();
            }

            // Party time!
            if (UnityInput.Current.GetKeyUp(KeyCode.F9))
            {
                Plugin.keydebounce = 30;
                GatherAllPlayers();
                Plugin.LogMessage("Party!");
            }

            // Chaos Reigns
            if (UnityInput.Current.GetKeyUp(KeyCode.F10))
            {
                Plugin.keydebounce = 30;
                
                for (int i = 0; i < 10; i++) {
                    SpawnRandomEnemy(true);
                }

                ReviveAllPlayers();
            }

            // Care package
            if (UnityInput.Current.GetKeyUp(KeyCode.F11))
            {
                Plugin.keydebounce = 30;
                CarePackage();
                Plugin.LogMessage("Care Package.");
            }
        }

        private static void ToggleBreaker()
        {
            BreakerBox breaker = FindObjectOfType<BreakerBox>();
            if (breaker != null)
            {
                breaker.SwitchBreaker(!breaker.isPowerOn);
                Plugin.LogMessage($"Turned breaker {(breaker.isPowerOn ? "on" : "off")}.");
            }
            else
            {
                Plugin.LogMessage("BreakerBox not found!", true);
            }
        }

        private static void RandomTeleport()
        {

        }

        private static void TeleportPlayer(PlayerControllerB playerToTeleport)
        {

        }

        private static void GatherAllPlayers()
        {
            List<Item> allItemsList = StartOfRound.Instance.allItemsList.itemsList;
            StartOfRound round = StartOfRound.Instance;

            // Random.Range(0, (round.allPlayerScripts.Length - 1));
            // Select a player at random
            // 
            // Grab the first player.
            PlayerControllerB target = round.allPlayerScripts[0];

            Vector3 destination = Plugin.GetPositionFromCommand("#0", 3);
            for (int i = 1; i < round.allPlayerScripts.Length; i++)
            {
                if (StartOfRound.Instance.allPlayerScripts[i].isActiveAndEnabled)
                {
                    Networking.TPPlayerClientRpc((ulong)i, destination);
                }
            }

            SearchableGameObject boombox = Plugin.allSpawnablesList.FirstOrDefault(obj => obj.Name.ToLower().StartsWith("boombox"));
            SearchableGameObject bottles = Plugin.allSpawnablesList.FirstOrDefault(obj => obj.Name.ToLower().StartsWith("bottles"));
            SearchableGameObject flashbang = Plugin.allSpawnablesList.FirstOrDefault(obj => obj.Name.ToLower().StartsWith("homemade flashbang"));

            Plugin.SpawnItem(boombox, 1, allItemsList[boombox.Id].maxValue, "#0");
            Plugin.SpawnItem(bottles, 1, allItemsList[bottles.Id].maxValue, "#0");
            Plugin.SpawnItem(flashbang, 4, allItemsList[flashbang.Id].maxValue, "#0");
        }

        private static void GiveItemToPlayer()
        {

        }

        private static void SpawnRandomEnemy(bool highHazard)
        {
            Plugin.SpawnEnemy(SelectLevelledEnemy(highHazard), 1, "$");
        }

        private static void ReviveAllPlayers()
        {
            for (int i = 0; i < StartOfRound.Instance.allPlayerScripts.Length; i++)
            {
                //Plugin.RevivePlayer(StartOfRound.Instance.allPlayerScripts[i].playerClientId);
                RevivePlayer(StartOfRound.Instance.allPlayerScripts[i]);
            }
        }

        private static void CarePackage()
        {
            List<Item> allItemsList = StartOfRound.Instance.allItemsList.itemsList;
            for (int i = 0; i < StartOfRound.Instance.allPlayerScripts.Length; i++)
            {
                if (StartOfRound.Instance.allPlayerScripts[i].isActiveAndEnabled)
                {
                    SearchableGameObject itemToSpawn = SelectRandomItem();
                    Plugin.SpawnItem(itemToSpawn, 1, allItemsList[itemToSpawn.Id].maxValue, "#"+i);
                }
            }
        }

        private static void DeadMoney()
        {
            SearchableGameObject prefabFromString = Plugin.allSpawnablesList.FirstOrDefault(obj => obj.Name.ToLower().StartsWith("gold bar"));
            Plugin.SpawnItem(prefabFromString, 4, 210, "#0", true);
        }

        private static SearchableGameObject SelectRandomItem()
        {
            List<SearchableGameObject> allItems = Plugin.allSpawnablesList.Where(item => item.IsItem && (item.Name.ToLower() != "utility belt" && item.Name.ToLower() != "nokia 3310")).ToList();
            int itemId = Random.Range(0, allItems.Count);
            return allItems[itemId];
        }

        private static string[] lowLevelEnemies = new[] { "centipede", "bunker spider", "hoarding bug", "blob", "boomba", "immortalsnail" };
        private static string[] highLevelEnemies = new[] { "nutcracker", "flowerman", "masked", "spring", "jester", "earth leviathan", "radmech" };

        private static SearchableGameObject SelectLevelledEnemy(bool highHazard)
        {
            string[] enemyList = highHazard ? highLevelEnemies : lowLevelEnemies;
            int enemyIndex = Random.Range(0, enemyList.Length);

            return Plugin.allSpawnablesList.FirstOrDefault(obj => obj.Name.ToLower() == enemyList[enemyIndex]);
        }

        private static SearchableGameObject SelectRandomEnemy()
        {
            List<SearchableGameObject> allItems = Plugin.allSpawnablesList.Where(item => item.IsEnemy).ToList();
            int itemId = Random.Range(0, allItems.Count);
            return allItems[itemId];
        }

        private static ulong GetRandomLivePlayerId()
        {
            List<PlayerControllerB> livePlayers = GetAllPlayers(false);
            int playerId = Random.Range(0, livePlayers.Count);
            return livePlayers[playerId].playerClientId;
        }

        // Need to work out how to target individual players for revive. Maybe just
        // clone Instance.ReviveDeadPlayers targeting a single player?
        // Check HealPlayerClientRpc - that should do it.
        private static void RevivePlayer(PlayerControllerB playerToRevive) {
            Networking.HealPlayerClientRpc(playerToRevive.playerClientId);
        }
        private static List<PlayerControllerB> GetAllPlayers(bool includeDead)
        {
            List<PlayerControllerB> allPlayers = StartOfRound.Instance.allPlayerScripts.ToList();
            if (!includeDead)
            {
                List<PlayerControllerB> alivePlayers = new List<PlayerControllerB>();
                foreach (PlayerControllerB player in allPlayers)
                {
                    if (!player.isPlayerDead)
                    {
                        alivePlayers.Add(player);
                    }
                }
                allPlayers = alivePlayers;
            }
            return allPlayers;
        }
    }
}

/**
 $5 - Toggle breaker (Lights go on, lights go off) [done, tested]
$10 - Spawn low hazard enemy at random on current player (Spider, baboon hawk, ghost girl, lootbug) [done, needs tested]
$25 - Care Package (Every living player gets a random tool) [done, needs tested]
$40 - Spawn a high hazard enemy at random on current player (Earth Leviathan, Nutcracker, Jester, Bracken, Butler, Giant, robot) [done, needs tested]
$50 - DEAD MONEY (Spawn 4 gold bars that weigh 5x what they normally do) [done, weight is weird]
$75 - PARTY TIME (Teleport all crewmates to one living crewmate selected at random. Summon a boombox and a crate of bottles) [done, test]
$100 - CHAOS REIGNS (Revive all dead players, set weather to eclipsed, spawn four high hazard enemies) [partial]
 */
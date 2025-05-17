using MelonLoader;
using System.Reflection;
using HarmonyLib;

namespace CustomRushMode
{
    internal class RushModeManager
    { 
        public static bool IsInitialized { get; private set; } = false;
        public static bool IsRushModePatched { get; private set; } = false;

        private static readonly Dictionary<RushModes, PlayerCardData> rushModeCardMap = [];
        private static readonly List<PlayerCardData> availableRandomCards = [];

        public static class ActiveRush
        {
            private static RushModes? rushMode = null;

            public static void SetMode(RushModes rushMode) => ActiveRush.rushMode = rushMode;

            public static PlayerCardData GetCard()
            {
                // returns PlayerCardData based on the current rush mode

                if (!IsInitialized || !rushMode.HasValue)
                    return null;

                if (rushMode.Value == RushModes.Random)
                {
                    if (availableRandomCards.Count == 0) 
                        return null;
                    int index = UnityEngine.Random.Range(0, availableRandomCards.Count);
                    return availableRandomCards[index];
                }
                else
                {
                    if (rushModeCardMap.TryGetValue(rushMode.Value, out PlayerCardData card))
                        return card;
                    return null;
                }
            }
        }

        // CardPickup.Spawn
        private static readonly MethodInfo CardPickup_Spawn_Original = 
            AccessTools.Method(typeof(CardPickup), "Spawn");
        private static readonly HarmonyMethod CardPickup_Spawn_Patch =
            new(AccessTools.Method(typeof(RushModeManager), nameof(CardPickup_Spawn_Prefix)));

        // CardPickup.SetCard 
        private static readonly MethodInfo CardPickup_SetCard_Original =
            AccessTools.Method(typeof(CardPickup), "SetCard");
        private static readonly HarmonyMethod CardPickup_SetCard_Patch = 
            new(AccessTools.Method(typeof(RushModeManager), nameof(CardPickup_SetCard_Prefix)));

        // CardVendor.SpawnPickupVendor
        private static readonly MethodInfo CardVendor_SpawnPickupVendor_Original =
            AccessTools.Method(typeof(CardVendor), "SpawnPickupVendor");
        private static readonly HarmonyMethod CardVendor_SpawnPickupVendor_Patch =
            new(AccessTools.Method(typeof(RushModeManager), nameof(CardVendor_SpawnPickupVendor_Prefix)));

        public static void Initialize()
        {
            GameData gameData = Core.Game.GetGameData();
            if (gameData == null) {
                MelonLogger.Error("Failed to cache card data: GameData is null.");
                return;
            }

            var cardMappings = new Dictionary<RushModes, string>
            {
                { RushModes.Purify, "MACHINEGUN" },
                { RushModes.Elevate, "PISTOL" },
                { RushModes.Godspeed, "RIFLE" },
                { RushModes.Stomp, "UZI" },
                { RushModes.Fireball, "SHOTGUN" },
                { RushModes.Dominion, "ROCKETLAUNCHER" }
            };

            foreach (var kvp in cardMappings)
            {
                var card = gameData.GetCard(kvp.Value);
                rushModeCardMap.Add(kvp.Key, card);
                availableRandomCards.Add(card);
            }

            IsInitialized = true;
        }

        public static void ToggleRushModePatch(bool apply)
        {
            if (IsRushModePatched == apply) return;
            if (apply) PatchRushMode();
            else UnpatchRushMode();
        }

        private static void PatchRushMode()
        {
            Core.Harmony.Patch(CardPickup_Spawn_Original, prefix: CardPickup_Spawn_Patch);
            Core.Harmony.Patch(CardPickup_SetCard_Original, prefix: CardPickup_SetCard_Patch);
            Core.Harmony.Patch(CardVendor_SpawnPickupVendor_Original, prefix: CardVendor_SpawnPickupVendor_Patch);
            IsRushModePatched = true;
        }

        private static void UnpatchRushMode()
        {
            Core.Harmony.Unpatch(CardPickup_Spawn_Original, CardPickup_Spawn_Patch.method);
            Core.Harmony.Unpatch(CardPickup_SetCard_Original, CardPickup_SetCard_Patch.method);
            Core.Harmony.Unpatch(CardVendor_SpawnPickupVendor_Original, CardVendor_SpawnPickupVendor_Patch.method);
            IsRushModePatched = false;
        }

        private static bool CanReplaceCard(PlayerCardData card)
        {
            return card.cardID != "RAPTURE" &&
                   card.consumableType != PlayerCardData.ConsumableType.Tutorial &&
                   card.consumableType != PlayerCardData.ConsumableType.GiftCollectible &&
                   card.consumableType != PlayerCardData.ConsumableType.LoreCollectible &&
                   card.cardType != PlayerCardData.Type.SpecialConsumableAutomatic &&
                   NeonLite.Modules.Anticheat.Active;
        }

        private static bool CardPickup_Spawn_Prefix(ref PlayerCardData card, bool autoPickup = false)
        {
            if (autoPickup && CanReplaceCard(card) && ActiveRush.GetCard() is PlayerCardData newCard)
                card = newCard;
            return true;
        }

        private static bool CardPickup_SetCard_Prefix(ref PlayerCardData card)
        {
            if (CanReplaceCard(card) && ActiveRush.GetCard() is PlayerCardData newCard)
                card = newCard;
            return true;
        }

        private static bool CardVendor_SpawnPickupVendor_Prefix(ref PlayerCardData card)
        {
            if (CanReplaceCard(card) && ActiveRush.GetCard() is PlayerCardData newCard)
                card = newCard;
            return true;
        }
    }
}

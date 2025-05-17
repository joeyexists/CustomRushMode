using MelonLoader;

[assembly: MelonInfo(typeof(CustomRushMode.Core), "CustomRushMode", "1.0.0", "joeyexists", null)]
[assembly: MelonGame("Little Flag Software, LLC", "Neon White")]

namespace CustomRushMode
{
    public class Core : MelonMod
    {
        internal static Game Game { get; private set; }
        internal static new HarmonyLib.Harmony Harmony { get; private set; }
        public static bool IsModEnabled { get; private set; } = false;

        public override void OnLateInitializeMelon()
        {
            Game = Singleton<Game>.Instance;
            Harmony = new HarmonyLib.Harmony("joeyexists.CustomRushMode");

            Settings.Register(this);

            Game.OnInitializationComplete += () =>
            {
                RushModeManager.Initialize();
                RushModeManager.ActiveRush.SetMode(Settings.rushModeEntry.Value);
                if (Settings.modEnabledEntry.Value)
                    EnableMod();
            };
        }

        public static class Settings
        {
            public static MelonPreferences_Category category;

            public static MelonPreferences_Entry<bool> modEnabledEntry;
            public static MelonPreferences_Entry<RushModes> rushModeEntry;

            public static void Register(Core modInstance)
            {
                category = MelonPreferences.CreateCategory("Custom Rush Mode");

                modEnabledEntry = category.CreateEntry("Enabled", false,
                    description: "Enables the mod.\n\nTriggers anti-cheat. To reset it, return to the hub.");

                rushModeEntry = category.CreateEntry("Rush Mode", RushModes.Purify);

                modEnabledEntry.OnEntryValueChanged.Subscribe((_, enable) =>
                {
                    if (IsModEnabled == enable) return;
                    if (enable) modInstance.EnableMod();
                    else modInstance.DisableMod();
                });

                rushModeEntry.OnEntryValueChanged.Subscribe((_, rushMode) =>
                {
                    RushModeManager.ActiveRush.SetMode(rushMode);
                });
            }
        }

        private void EnableMod()
        {
            RushModeManager.ToggleRushModePatch(true);
            RegisterAntiCheat();
            IsModEnabled = true;
        }

        private void DisableMod()
        {
            RushModeManager.ToggleRushModePatch(false);
            IsModEnabled = false;
        }

        private void RegisterAntiCheat() => NeonLite.Modules.Anticheat.Register(MelonAssembly);
        private void UnregisterAntiCheat() => NeonLite.Modules.Anticheat.Unregister(MelonAssembly);

        public override void OnSceneWasLoaded(int buildindex, string sceneName)
        {
            if (sceneName.Equals("HUB_HEAVEN")
                && IsModEnabled == false
                && RushModeManager.IsRushModePatched == false)
            {
                UnregisterAntiCheat();
            }
        }
    }
}
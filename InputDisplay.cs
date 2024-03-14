using InputDisplay.Objects;
using MelonLoader;
using UnityEngine;

namespace InputDisplay
{
    public class InputDisplay : MelonMod
    {
        public static DisplayObject Display { get; set; }
        public static new HarmonyLib.Harmony Harmony { get; private set; }

        public override void OnLateInitializeMelon()
        {
            Harmony = new("InputDisplay");

            DisplayObject.Setup();
            Settings.Register();
            Singleton<Game>.Instance.OnLevelLoadComplete += OnLevelLoadComplete;
        }

        private void OnLevelLoadComplete()
        {
            Display = null;
            if (Singleton<Game>.Instance.GetCurrentLevelType() != LevelData.LevelType.Hub && Settings.Enabled.Value)
                DisplayObject.Initialize();
        }


        public static class Settings
        {
            public static MelonPreferences_Category Category;

            public static MelonPreferences_Entry<bool> Enabled;
            public static MelonPreferences_Entry<Color> SelectedColor;
            public static MelonPreferences_Entry<bool> Invert;

            public static void Register()
            {
                Category = MelonPreferences.CreateCategory("Input Display");

                Enabled = Category.CreateEntry("Enabled", true);
                SelectedColor = Category.CreateEntry("Color", Color.clear, description: "The color to make the input display.\nSet alpha to 0 to have it match the current card.");
                Invert = Category.CreateEntry("Invert Input Display", false, description: "Whether or not to invert the colors of the input display.\n(Buttons *icons* are display color when pressed)");
            }
        }
    }
}

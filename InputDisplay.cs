using System;
using InputDisplay.Objects;
using UnityEngine;
using MelonLoader;
using MelonLoader.Preferences;

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

        [Flags]
        public enum DisplayModes
        {
            None = 0,
            BlackBackground = 1 << 0,
            Borderless = 1 << 1,
            ColoredOff = 1 << 2,
        }


        public static class Settings
        {
            public static MelonPreferences_Category Category;

            public static MelonPreferences_Entry<bool> Enabled;
            public static MelonPreferences_Entry<Color> SelectedColor;

            public static MelonPreferences_Entry<bool> AlwaysColor;
            public static MelonPreferences_Entry<bool> FaustasMode;
            public static MelonPreferences_Entry<float> FaustasSpeed;

            public static MelonPreferences_Entry<bool> BlackBG;
            public static MelonPreferences_Entry<bool> Borderless;
            public static MelonPreferences_Entry<bool> ColoredOff;
            public static MelonPreferences_Entry<bool> SeperateScroll;

            public static MelonPreferences_Entry<bool> InvertPressed;

            public static DisplayModes DisplayMode {
                get
                {
                    return (BlackBG.Value ? DisplayModes.BlackBackground : DisplayModes.None) |
                            (Borderless.Value ? DisplayModes.Borderless : DisplayModes.None) |
                            (ColoredOff.Value ? DisplayModes.ColoredOff : DisplayModes.None);
                }
            }
            
            public static void Register()
            {
                Category = MelonPreferences.CreateCategory("Input Display");

                Enabled = Category.CreateEntry("Enabled", true);
                SelectedColor = Category.CreateEntry("Color", Color.clear, description: "The color to make the input display.\nSet alpha to 0 to have it match the current card.");

                BlackBG = Category.CreateEntry("Black Background", false, oldIdentifier: "Invert Input Display", description: "Whether or not to make the background black on press.\nThis can be stacked with the other style options!");
                Borderless = Category.CreateEntry("Borderless", false, description: "Whether or not to make the keys borderless.\nThis can be stacked with the other style options!");
                ColoredOff = Category.CreateEntry("Colored on Off", false, description: "Whether or not to make the keys colored when off instead of on.\nThis can be stacked with the other style options!");
                SeperateScroll = Category.CreateEntry("Seperate Scrollwheel", false, description: "Whether or not to have a seperated scroll wheel instead of the scroll being inbuilt into the space bar.\nThis can be stacked with the other style options!");
                InvertPressed = Category.CreateEntry("Invert Pressed", false, description: "Completely inverts the on/off states for the buttons.\n(I recommend combining this with separated scrollwheel)");

                AlwaysColor = Category.CreateEntry("Always Colored when Pressed", false, description: "If Colored on Off is set, this enables the color to be applied even when the button is pressed.");
                FaustasMode = Category.CreateEntry("Rainbow Colors", false, description: "Turning this on sets the color to change in a rainbow-like gradient!");
                FaustasSpeed = Category.CreateEntry("Rainbow Speed", 10f, validator: new ValueRange<float>(0, 180));

            }
        }
    }
}

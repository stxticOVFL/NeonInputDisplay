using InputDisplay.Objects;
using MelonLoader;
using MelonLoader.Preferences;
using MelonLoader.Utils;
using NeonLite;
using NeonLite.Modules;
using System;
using System.IO;
using System.IO.Compression;
using UnityEngine;

namespace InputDisplay
{
    public class InputDisplay : MelonMod, IModule
    {
        internal static Texture2D Blank { get; private set; }

        internal static MelonLogger.Instance Logger { get; private set; }

        public override void OnInitializeMelon()
        {
            NeonLite.NeonLite.LoadModules(MelonAssembly);
            Logger = LoggerInstance;

            if (!Directory.Exists(Folder))
            {
                // extract the defasults
                Directory.CreateDirectory(Folder);
                var p = Path.Combine(Folder, "defaults.zip");
                File.WriteAllBytes(p, Resources.Resources.defaults);
                ZipFile.ExtractToDirectory(p, MelonEnvironment.ModsDirectory);
                File.Delete(p);
            }
        }

        public override void OnLateInitializeMelon()
        {
            Blank = new Texture2D(1, 1, TextureFormat.Alpha8, false);
            Blank.SetPixel(0, 0, Color.clear);
            Blank.Apply();
            Blank.name = "BLANK_TEXTURE";

            Settings.Register();
        }

#pragma warning disable CS0414
        const bool priority = true;
        static bool active = true;

        static void Activate(bool activate)
        {
            Patching.TogglePatch(activate, typeof(PlayerUICardHUD), "UpdateHUD", PreUpdateHUD, Patching.PatchTarget.Prefix);
            active = activate;
        }

        private static bool OnLevelLoad(LevelData level)
        {
            if (level && level.type != LevelData.LevelType.Hub)
            {
                DisplayObject.Initialize();
                return DisplayObject.initialized;
            }
            return true;
        }

        private static void PreUpdateHUD(PlayerCard card)
        {
            if (DisplayObject.current)
                DisplayObject.current.SetCardColor(card);
        }


        public enum ColorMode
        {
            Pressed = 0,
            Unpressed = 1,
            Always = 2
        }

        internal static string Folder => Path.Combine(MelonEnvironment.ModsDirectory, Settings.h);

        public static class Settings
        {
            // kept for old
            [Flags]
            enum DisplayModes
            {
                None = 0,
                BlackBackground = 1 << 0,
                Borderless = 1 << 1,
                ColoredOff = 1 << 2,
            }

            internal const string h = "InputDisplay";

            public static MelonPreferences_Category Category;

            public static MelonPreferences_Entry<bool> enabled;
            public static MelonPreferences_Entry<string> offTexture;
            public static MelonPreferences_Entry<string> onTexture;
            public static MelonPreferences_Entry<string> layout;

            public static MelonPreferences_Entry<Color> selectedColor;
            public static MelonPreferences_Entry<ColorMode> colorMode;
            public static MelonPreferences_Entry<bool> AlwaysColor;
            public static MelonPreferences_Entry<bool> faustasMode;
            public static MelonPreferences_Entry<float> faustasSpeed;

            public static void Register()
            {

                NeonLite.Settings.AddHolder("InputDisplay");

                enabled = NeonLite.Settings.Add(h, "", "enabled", "Enabled", null, true);
                enabled.SetupForModule(Activate, static (_, after) => after);

                onTexture = NeonLite.Settings.Add(h, "", "onTexture", "Pressed Texture Folder", "Which folder to use for textures where buttons are pressed.", "Pressed/Default");
                offTexture = NeonLite.Settings.Add(h, "", "offTexture", "Unpressed Texture Folder", "Which folder to use for textures where buttons aren't pressed.", "Unpressed/Default");
                layout = NeonLite.Settings.Add(h, "", "layout", "Layout", "The name of the layout JSON in the Layouts folder to use.", "Default");

                onTexture.OnEntryValueChanged.Subscribe((_, _) => DisplayObject.Setup());
                offTexture.OnEntryValueChanged.Subscribe((_, _) => DisplayObject.Setup());
                layout.OnEntryValueChanged.Subscribe((_, _) => DisplayObject.Setup());

                selectedColor = NeonLite.Settings.Add(h, "", "color", "Color", "The color to make the input display.\nSet alpha to 0 to have it match the current card.", Color.clear);
                colorMode = NeonLite.Settings.Add(h, "", "colorMode", "Color Mode", "When to color the buttons.\n(White is replaced with the color.)", ColorMode.Pressed);
                faustasMode = NeonLite.Settings.Add(h, "", "faustasMode", "Rainbow Colors", "Turning this on sets the color to change in a rainbow-like gradient!", false);
                faustasSpeed = NeonLite.Settings.Add(h, "", "faustasSpeed", "Rainbow Speed", null, 10f, new ValueRange<float>(0, 180));

                var category = MelonPreferences.CreateCategory("Input Display");
                var migrated = category.CreateEntry("MIGRATED", false, is_hidden: true).Value;
                if (!migrated)
                {
                    category.GetEntry<bool>("MIGRATED").Value = true;

                    T CreateOrFind<T>(string name, T defaultVal)
                    {
                        try
                        {
                            var e = category.CreateEntry(name, defaultVal, dont_save_default: true);
                            T val = e.Value;
                            category.DeleteEntry(name);
                            return val;
                        }
                        catch
                        {
                            var e = category.GetEntry<T>(name);
                            T val = e.Value;
                            category.DeleteEntry(name);
                            return val;
                        }
                    }

                    var entry = CreateOrFind("Enabled", false);
                    enabled.Value = entry;
                    if (entry)
                    {

                        selectedColor.Value = CreateOrFind("Color", Color.clear);
                        faustasMode.Value = CreateOrFind("Rainbow Colors", false);
                        faustasSpeed.Value = CreateOrFind("Rainbow speed", 10f);

                        var displayMode = CreateOrFind("Black Background", false) ? DisplayModes.BlackBackground : DisplayModes.None;
                        displayMode |= CreateOrFind("Borderless", false) ? DisplayModes.Borderless : DisplayModes.None;
                        var coloredOff = CreateOrFind("Colored on Off", false);
                        displayMode |= coloredOff ? DisplayModes.ColoredOff : DisplayModes.None;

                        string on = displayMode switch
                        {
                            _ when displayMode.HasFlag(DisplayModes.BlackBackground) => "Pressed/Black",
                            _ when displayMode.HasFlag(DisplayModes.Borderless) => "Pressed/Borderless",
                            _ => "Pressed/Default"
                        };

                        string off = displayMode switch
                        {
                            _ when displayMode.HasFlag(DisplayModes.ColoredOff) => displayMode switch
                            {
                                _ when displayMode.HasFlag(DisplayModes.Borderless) => "Unpressed/White Borderless",
                                _ => "Unpressed/White"
                            },
                            _ when displayMode.HasFlag(DisplayModes.Borderless) => "Unpressed/Borderless",
                            _ => "Unpressed/Default"
                        };

                        if (coloredOff)
                        {
                            var always = CreateOrFind("Always Colored when Pressed", false);
                            if (always)
                                colorMode.Value = ColorMode.Always;
                            else
                                colorMode.Value = ColorMode.Unpressed;
                        }
                        else
                            colorMode.Value = ColorMode.Pressed;

                            var seperateScroll = CreateOrFind("Seperate Scrollwheel", false);
                        if (seperateScroll)
                            layout.Value = "Seperate Scroll";

                        var invertPressed = CreateOrFind("Invert Pressed", false);
                        if (invertPressed)
                            (on, off) = (off, on);

                        onTexture.Value = on;
                        offTexture.Value = off;

                    }
                    MelonPreferences.Save();
                }
            }
        }
    }
}

# NeonInputDisplay
A customizable basic input display for Neon White, based on the style of the OBS input display previously developed by Glint.

![image](https://github.com/stxticOVFL/NeonInputDisplay/assets/29069561/016e6ce7-2a48-454e-870f-e682cc2fcf41)

## Features
- Basic input display that meshes in with the rest of the UI
- Automatic color by current card type
- Support for all forms of input including scroll-wheel coyote jumping as a special case
- Customizable by color and inversion of the colored spaces
  - (e.g. The icons become colored while the rest becomes black) 

## Installation

1. Download [MelonLoader](https://github.com/LavaGang/MelonLoader/releases/latest) and install it onto your `Neon White.exe`.
2. Run the game once. This will create required folders.
3. Download the **Mono** version of [Melon Preferences Manager](https://github.com/Bluscream/MelonPreferencesManager/releases/latest), and put the .DLLs from that zip into the `Mods` folder of your Neon White install.
    - The preferences manager is required to customize the input display, using F5 (by default).
4. Download the **Mono** version of [UniverseLib](https://github.com/sinai-dev/UniverseLib) and put it in the `Mods` folder.
    - The preferences manager **requires UniverseLib.** 
5. Download `InputDisplay.dll` from the [Releases page](https://github.com/stxticOVFL/NeonInputDisplay/releases/latest) and drop it in the `Mods` folder.

## Building & Contributing
This project uses Visual Studio 2022 as its project manager. When opening the Visual Studio solution, ensure your references are corrected by right clicking and selecting `Add Reference...` as shown below. 
Most will be in `Neon White_data/Managed`. Some will be in `MelonLoader/net35`, **not** `net6`. Select the `MelonPrefManager` mod for that reference. 
If you get any weird errors, try deleting the references and re-adding them manually.

![image](https://github.com/stxticOVFL/NeonInputDisplay/assets/29069561/4ee86163-03cf-4e8d-a623-a5698c14436f)

Once your references are correct, build using the keybind or like the picture below.

![image](https://github.com/stxticOVFL/EventTracker/assets/29069561/40a50e46-5fc2-4acc-a3c9-4d4edb8c7d83)

Make any edits as needed, and make a PR for review. PRs are very appreciated.

### Additional Notes
It's recommended to add `--melonloader.hideconsole` to your game launch properties (Steam -> Right click Neon White -> Properties -> Launch Options) to hide the console that MelonLoader spawns.

![image](https://github.com/stxticOVFL/EventTracker/assets/29069561/9c037da5-7323-435f-9e55-80904f799ae0)
![image](https://github.com/stxticOVFL/EventTracker/assets/29069561/4a4fa519-15b4-486f-a354-6ff7d0672df4)

using MelonLoader.TinyJSON;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;
using static UILayoutGroup;
using static UnityEngine.InputSystem.HID.HID;
using Resource = InputDisplay.Resources.Resources;
using Settings = InputDisplay.InputDisplay.Settings;


namespace InputDisplay.Objects
{
    public class DisplayObject : MonoBehaviour
    {
        const float GRID_OFF = 0.66666666f;

        static private DisplayObject copy;
        static internal DisplayObject current;

        static Texture2D defaultFakeAA;

        static GameInput gameInput;

        GameObject baseCopy;
        DisplayButton[] buttons = [];

        static float scrollTimer = 0;

        public static Color currentColor;
        private float faustasCycle = 0;

        internal static bool initialized = false;

        static readonly Dictionary<Texture2D, (string, DateTime)> textures = [];
        static readonly Dictionary<string, DateTime> layouts = [];

        private static Texture2D LoadTexture(string path, string name)
        {
            Texture2D tex = new(0, 0, TextureFormat.RGBA32, false)
            {
                name = name
            };
            tex.LoadImage(File.ReadAllBytes(path));
            textures.Add(tex, (path, File.GetLastWriteTimeUtc(path)));
            return tex;
        }

        public static void Setup()
        {
            var cardUI = RM.ui.cardHUDUI;

            if (!copy)
            {
                copy = new GameObject("Input Display", typeof(DisplayObject)).GetComponent<DisplayObject>();
                copy.transform.localPosition = new Vector3(0, -0.666f, 0) + cardUI.abilityIconHolder.transform.localPosition;
                copy.transform.localRotation = Quaternion.identity;
                copy.gameObject.SetActive(false);

                var holder = Instantiate(cardUI.abilityIconHolder, copy.transform);
                holder.transform.localPosition = Vector3.zero;
                foreach (Transform t in holder.transform)
                {
                    // eliminate all but the first one.
                    if (t.GetSiblingIndex() == 0)
                        continue;
                    Destroy(t.gameObject);
                }
                // the sole survivor will be the base
                copy.baseCopy = holder.transform.GetChild(0).gameObject;
                copy.baseCopy.SetActive(false);

                defaultFakeAA = (Texture2D)copy.baseCopy.transform.GetChild(0).GetComponent<MeshRenderer>().material.mainTexture;

                DontDestroyOnLoad(copy.gameObject);
            }

            gameInput = Singleton<GameInput>.Instance;
            copy.Construct();

            if (current)
            {
                Destroy(current);
                copy.StartCoroutine(WaitForInit());
            }
        }

        static IEnumerator WaitForInit()
        {
            yield return null;
            Initialize();
        }

        static ProxyObject LoadJSON(string path, bool missing = false)
        {
            try
            {
                var r = (ProxyObject)JSON.Load(File.ReadAllText(path));
                layouts.Add(path, File.GetLastWriteTimeUtc(path));
                return r;
            }
            catch (Exception e)
            {
                if (e is not FileNotFoundException || missing)
                    InputDisplay.Logger.Error("Failed to load JSON: " + e);
                layouts.Add(path, DateTime.MinValue);

                return null;
            }
        }

        void Construct()
        {
            // only to be used by copy

            initialized = false;
            textures.Clear();
            layouts.Clear();
            var pathbuf = Path.Combine(InputDisplay.Folder, "Layouts", Settings.layout.Value) + ".json";
            var layout = LoadJSON(pathbuf, true);
            if (layout == null)
                return;

            pathbuf = Path.Combine(InputDisplay.Folder, "Textures", Settings.onTexture.Value, "override.json");
            var onOverride = LoadJSON(pathbuf);
            pathbuf = Path.Combine(InputDisplay.Folder, "Textures", Settings.offTexture.Value, "override.json");
            var offOverride = LoadJSON(pathbuf);

            static void CheckOverride(ProxyObject ov, DisplayProperties props, string name)
            {
                if (ov == null)
                    return;
                if (ov.Keys.Contains("_ALL"))
                    JsonUtility.FromJsonOverwrite(ov["_ALL"].ToJSON(), props);
                if (ov.Keys.Contains(name))
                    JsonUtility.FromJsonOverwrite(ov[name].ToJSON(), props);
            }

            List<DisplayButton> newb = [];
            foreach (var kv in layout)
            {
                var button = DisplayButton.Create(baseCopy, kv.Key, (ProxyObject)kv.Value);

                CheckOverride(onOverride, button.pressed, kv.Key);
                CheckOverride(offOverride, button.unpressed, kv.Key);

                button.FetchTextures();
                newb.Add(button);
            }

            foreach (var b in buttons)
            {
                Destroy(b.pressed);
                Destroy(b.unpressed);
                Destroy(b.gameObject);
            }
            buttons = newb.ToArray();
        }

        void BuildList() => buttons = GetComponentsInChildren<DisplayButton>(false);

        static bool AnyOutdated()
        {
            foreach (var kv in layouts)
            {
                var lastWrite = DateTime.MinValue;
                if (File.Exists(kv.Key))
                    lastWrite = File.GetLastWriteTimeUtc(kv.Key);
                if (lastWrite != kv.Value)
                    return true;
            }
            return false;
        }

        internal static void Initialize()
        {
            if (!copy || AnyOutdated())
            {
                Setup();
                copy.StartCoroutine(WaitForInit());

                return;
            }
            else
            {
                // just check textures 
                foreach (var kv in textures)
                {
                    if (File.Exists(kv.Value.Item1) && File.GetLastWriteTimeUtc(kv.Value.Item1) != kv.Value.Item2)
                        kv.Key.LoadImage(File.ReadAllBytes(kv.Value.Item1));
                }
            }
            initialized = true;
            scrollTimer = 0;

            var cardUI = RM.ui.cardHUDUI;
            current = Instantiate(copy.gameObject, cardUI.transform).GetComponent<DisplayObject>();

            cardUI.abilityIconHolder.transform.GetChild(1).localPosition = new Vector3(GRID_OFF * 1, 0, 0);
            cardUI.abilityIconHolder.transform.GetChild(2).localPosition = new Vector3(GRID_OFF * 2, 0, 0);

            current.BuildList();
            current.gameObject.SetActive(true);
        }

        void Update()
        {
            var j = gameInput.Controls.Gameplay.Jump;
            if (j.activeControl != null && j.activeControl.valueType == typeof(Vector2))
                scrollTimer = j.ReadValue<Vector2>().y > 0 ? 0.1f : -0.1f;
            else if (scrollTimer != 0)
            {
                bool negative = scrollTimer < 0;
                if (negative)
                {
                    scrollTimer += Time.deltaTime;
                    if (scrollTimer > 0)
                        scrollTimer = 0;
                }
                else
                {
                    scrollTimer -= Time.deltaTime;
                    if (scrollTimer < 0)
                        scrollTimer = 0;
                }
            }

            if (Settings.faustasMode.Value)
            {
                faustasCycle = (faustasCycle + Time.deltaTime * Settings.faustasSpeed.Value) % 360;
                currentColor = Color.HSVToRGB(faustasCycle / 360f, 0.8f, 0.8f);

                foreach (var button in buttons)
                    button.SetColor();
            }

            else
            {
                var sel = Settings.selectedColor.Value;
                if (sel.a != 0)
                {
                    if (currentColor != sel.Alpha(1))
                    {
                        currentColor = sel.Alpha(1);
                        foreach (var button in buttons)
                            button.SetColor();
                    }
                }
            }
        }

        internal void SetCardColor(PlayerCard card)
        {
            if (Settings.faustasMode.Value || Settings.selectedColor.Value.a > 0)
                return;

            var pre = currentColor;
            if (card.data.discardAbility == PlayerCardData.DiscardAbility.None) // katana/fist
                currentColor = new Color(0.5f, 0.5f, 0.5f, 1f);
            else
                currentColor = card.data.cardColor.Alpha(1f);

            if (pre != currentColor)
            {
                foreach (var button in buttons)
                {
                    if (button.material)
                        button.SetColor();
                }
            }
        }

        [Serializable]
        class DisplayProperties : ScriptableObject
        {
            public float x = 0;
            public float y = 0;
            public float scale = 1;
            public int layer = 0;
            public string fakeAA = null;
            public bool forceInvisible = false;

            [NonSerialized]
            internal Texture2D _fakeAA;

            [NonSerialized]
            internal Texture2D _tex;

            internal void FetchTextures()
            {
                _fakeAA = defaultFakeAA;

                if (fakeAA == "")
                    _fakeAA = InputDisplay.Blank;
                else if (fakeAA != null)
                {
                    var fakeP = Path.Combine(InputDisplay.Folder, "Textures", $"{fakeAA}.png");
                    if (File.Exists(fakeP))
                        _fakeAA = LoadTexture(fakeP, fakeAA);
                }
            }
        }

        class DisplayButton : MonoBehaviour
        {
            public DisplayProperties pressed;
            public DisplayProperties unpressed;

            public int _axis = 0;
            public GameInput.GameActions _gameAction = GameInput.GameActions.DialogueAdvance;
            public KeyCode _keycode = KeyCode.None;

            internal Material _fakeAA;
            internal Material material;

            static readonly int tintColor = Shader.PropertyToID("_TintColor");
            static readonly int mainTex = Shader.PropertyToID("_MainTex");

            Func<bool> _downFunc;
            bool _lastState = false;

            event Action<DisplayProperties, DisplayProperties> SwapChain;

            internal static DisplayButton Create(GameObject prefab, string name, ProxyObject props)
            {
                var obj = Instantiate(prefab, prefab.transform.parent);
                var button = obj.AddComponent<DisplayButton>();

                if (props.TryGetValue("gameAction", out var variantBuf))
                    Enum.TryParse(variantBuf.ToString(), out button._gameAction);
                if (props.TryGetValue("axis", out variantBuf))
                {
                    var str = variantBuf.ToString();
                    if (str == "-")
                        button._axis = -1;
                    else if (str == "+")
                        button._axis = 1;
                }

                if (props.TryGetValue("keycode", out variantBuf))
                    Enum.TryParse(variantBuf.ToString(), out button._keycode);


                button.pressed = ScriptableObject.CreateInstance<DisplayProperties>();
                JsonUtility.FromJsonOverwrite(props.ToJSON(), button.pressed);
                button.unpressed = ScriptableObject.CreateInstance<DisplayProperties>();
                JsonUtility.FromJsonOverwrite(props.ToJSON(), button.unpressed);

                button.name = name;
                obj.SetActive(true);
                return button;
            }

            void Start()
            {
                if (_gameAction != GameInput.GameActions.DialogueAdvance)
                {
                    if (_gameAction == GameInput.GameActions.Jump)
                    {
                        if (_axis != 0)
                            _downFunc = Coyote;
                        else
                            _downFunc = Jump;
                    }
                    else
                    {
                        if (_axis != 0)
                            _downFunc = GameActionAxis;
                        else
                            _downFunc = GameActionPress;
                    }
                }
                else if (_keycode != KeyCode.None)
                    _downFunc = KeycodePress;

                // build the Swap Chain .
                if (_downFunc != null)
                {
                    if (pressed.x != unpressed.x || pressed.y != unpressed.y)
                    {
                        SwapChain += (prop, _) =>
                        {
                            transform.localPosition = new(GRID_OFF * prop.x, GRID_OFF * -prop.y);
                        };
                    }
                    if (pressed.scale != unpressed.scale)
                    {
                        SwapChain += (prop, pre) =>
                        {
                            // TODO: check the math on this. im lazy
                            var t = transform.localScale;
                            transform.localPosition -= new Vector3((t.x - pre.x) / 2, (t.y - pre.y) / 2, 0);
                            t /= pre.scale;
                            var preS = t;
                            t *= prop.scale;
                            transform.localScale = t;
                            transform.localPosition += new Vector3((t.x - preS.x) / 2, (t.y - preS.y) / 2, 0);
                        };
                    }
                    if (pressed.layer != unpressed.layer)
                    {
                        SwapChain += (prop, pre) =>
                        {
                            material.renderQueue -= pre.layer;
                            material.renderQueue += prop.layer;
                            _fakeAA.renderQueue = material.renderQueue + 1;
                        };
                    }
                    if (pressed.fakeAA != unpressed.fakeAA)
                    {
                        SwapChain += (prop, _) => _fakeAA.SetTexture(mainTex, prop._fakeAA);
                    }

                    SwapChain += (prop, _) =>
                    {
                        material.SetTexture(mainTex, prop._tex);
                        SetColor();
                    };

                    _lastState = _downFunc();
                }

                var prop = _lastState ? pressed : unpressed;

                float aspect = 0;
                if (unpressed._tex != InputDisplay.Blank)
                    aspect = (float)unpressed._tex.width / unpressed._tex.height;
                else if (pressed._tex != InputDisplay.Blank)
                    aspect = (float)pressed._tex.width / pressed._tex.height;
                else
                    return; // both textures blank?? why the hell we do all this

                transform.localPosition = new(GRID_OFF * prop.x, GRID_OFF * -prop.y);

                var t = transform.localScale;
                var pre = t;
                if (aspect > 1)
                    t.x *= aspect;
                else
                    t.y /= aspect;
                transform.localScale = t * prop.scale;
                transform.localPosition += new Vector3((t.x - pre.x) / 2, (t.y - pre.y) / 2, 0);

                material = GetComponentInChildren<MeshRenderer>().material;
                material.SetTexture(mainTex, prop._tex);

                _fakeAA = transform.GetChild(0).GetComponent<MeshRenderer>().material;
                _fakeAA.SetTexture(mainTex, prop._fakeAA);

                material.renderQueue += prop.layer;
                _fakeAA.renderQueue = material.renderQueue + 1;

                SetColor();
            }

            internal void FetchTextures()
            {
                pressed.FetchTextures();
                unpressed.FetchTextures();

                Texture2D GetTexture(string path)
                {
                    path = Path.Combine(InputDisplay.Folder, "Textures", path, $"{name}.png");
                    if (File.Exists(path))
                        return LoadTexture(path, name);
                    else
                        return InputDisplay.Blank;
                }

                pressed._tex = GetTexture(Settings.onTexture.Value);
                if (pressed.forceInvisible)
                    unpressed._tex = InputDisplay.Blank;
                else
                    unpressed._tex = GetTexture(Settings.offTexture.Value);
            }

            void Update()
            {
                if (_downFunc == null)
                    return;
                var s = _downFunc();
                if (_lastState != s)
                {
                    var pre = _lastState ? pressed : unpressed;
                    _lastState = s;
                    var post = _lastState ? pressed : unpressed;

                    SwapChain(post, pre);
                }
            }

            public void SetColor()
            {
                var colorMode = Settings.colorMode.Value;
                if (colorMode == InputDisplay.ColorMode.Always ||
                  ((_lastState ? InputDisplay.ColorMode.Pressed : InputDisplay.ColorMode.Unpressed) == colorMode))
                    material.SetColor(tintColor, currentColor);
                else
                    material.SetColor(tintColor, Color.white);
            }


            // down funcs
            bool GameActionPress() => gameInput.GetButton(_gameAction);
            bool GameActionAxis() => Math.Sign(gameInput.GetAxis(_gameAction)) == _axis;

            bool KeycodePress() => UniverseLib.Input.InputManager.GetKey(_keycode);

            bool Jump()
            {
                var j = gameInput.Controls.Gameplay.Jump;
                if (j == null)
                    return false;
                if (j.activeControl != null && j.activeControl.valueType == typeof(Vector2))
                    return _lastState; // kinda crazy but

                return Math.Abs(j.ReadValue<float>()) > 0;
                //return gameInput.GetButton(_gameAction);
            }

            bool Coyote()
            {
                // the scrolltimer code is handled by the DisplayObject
                return Math.Sign(scrollTimer) == _axis;
            }
        }
    }
}

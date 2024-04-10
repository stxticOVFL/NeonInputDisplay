using System;
using UnityEngine;
using Resource = InputDisplay.Resources.Resources;
using Settings = InputDisplay.InputDisplay.Settings;


namespace InputDisplay.Objects
{
    public class DisplayObject : MonoBehaviour
    {
        private static Texture2D _leftTex;
        private static Texture2D _rightTex;
        private static Texture2D _upTex;
        private static Texture2D _downTex;

        private static Texture2D _fireTex;
        private static Texture2D _discardTex;
        private static Texture2D _jumpTex;
        private static Texture2D _swapTex;

        private static Texture2D _scrollTex;
        private static Texture2D _scrollSB;
        private static Texture2D _scrollSF;

        private static Texture2D _fake2;
        private static Texture2D _fake3;
        private static Texture2D _fakeS;

        private GameObject _holder;

        private MeshRenderer up;
        private MeshRenderer down;
        private MeshRenderer left;
        private MeshRenderer right;
        private MeshRenderer jump;
        private MeshRenderer fire;
        private MeshRenderer discard;
        private MeshRenderer swap;

        private MeshRenderer scroll;
        private MeshRenderer scrollSB;
        private MeshRenderer scrollSF;

        private float scrollTimer = 0;

        public static Color currentColor;
        private static float offset;

        private float faustasCycle = 0;

        private static Texture2D LoadTexture(byte[] image, string name)
        {
            Texture2D tex = new(0, 0, TextureFormat.DXT5, false)
            {
                name = name
            };
            tex.LoadImage(image);
            return tex;
        }

        public static void Setup()
        {
            // setup the textures

            _upTex = LoadTexture(Resource.Up, "up");
            _downTex = LoadTexture(Resource.Down, "down");
            _leftTex = LoadTexture(Resource.Left, "left");
            _rightTex = LoadTexture(Resource.Right, "right");

            _fireTex = LoadTexture(Resource.Fire, "fire");
            _discardTex = LoadTexture(Resource.Discard, "discard");
            _jumpTex = LoadTexture(Resource.Jump, "jump");
            _swapTex = LoadTexture(Resource.Swap, "swap");

            _scrollTex = LoadTexture(Resource.ScrollJump, "scrollB");
            _scrollSB = LoadTexture(Resource.ScrollSBack, "scrollSB");
            _scrollSF = LoadTexture(Resource.ScrollSFront, "scrollSF");

            _fake2 = LoadTexture(Resource.Fake2, "fake2");
            _fake3 = LoadTexture(Resource.Fake3, "fake3");
            _fakeS = LoadTexture(Resource.FakeS, "fakeS");
        }

        internal static void Initialize() => InputDisplay.Display = new GameObject("Input Display", typeof(DisplayObject)).GetComponent<DisplayObject>();

        private MeshRenderer SetupIcon(GameObject obj, Texture2D tex, double x, double y, float scale = 1)
        {
            // grab the renderer and set the texture
            var renderer = obj.GetComponent<MeshRenderer>();
            obj.name = tex.name;
            renderer.material.SetTexture("_MainTex", tex);
            renderer.material.SetTextureScale("_MainTex", new Vector2(1 / 16f, 0.25f));
            // renderer.material.renderQueue++;

            obj.transform.localPosition = new Vector3((float)x, (float)y, 0);
            obj.transform.localRotation = Quaternion.identity;
            var fakeAA = obj.transform.GetChild(0).GetComponent<MeshRenderer>();
            if (scale != 1)
            {
                var t = obj.transform.localScale;
                float pre = t.x;
                t.x *= scale;
                obj.transform.localScale = t;
                // adjust position according to new scale
                obj.transform.localPosition += new Vector3((t.x - pre) / 2, 0, 0);

                    // jump or fire/discard need a different fakeAA texture because just stretching it looks wrong
                    if (scale > 3)
                        fakeAA.material.SetTexture("_MainTex", _fake3);
                    else
                        fakeAA.material.SetTexture("_MainTex", _fake2);
            }

            if (tex.name.StartsWith("scroll"))
            {
                // if we're the scrolling texture, we don't use fakeAA and we can just toggle visibility
                renderer.material.SetTextureScale("_MainTex", new Vector2(1 / 8f, tex == _scrollSB ? 1 : 0.5f));
                if (tex != _scrollSF)
                    renderer.material.renderQueue--;
                if (tex == _scrollSB)
                {
                    fakeAA.material.SetTexture("_MainTex", _fakeS);
                    fakeAA.material.renderQueue = renderer.material.renderQueue + 1;
                }
                else
                    fakeAA.enabled = false;
            }

            obj.SetActive(true);

            return renderer;
        }

        private void Start()
        {
            var cardUI = RM.ui.cardHUDUI;
            transform.SetParent(cardUI.transform, false);
            // we add here so it sits nicely under the ability icon holder
            transform.localPosition = new Vector3(0, -0.666f, 0) + cardUI.abilityIconHolder.transform.localPosition;
            transform.localRotation = Quaternion.identity;

            // copy the existing icon holder
            _holder = Instantiate(cardUI.abilityIconHolder, transform);
            foreach (Transform t in _holder.transform)
            {
                // eliminate all but the first one.
                if (t.GetSiblingIndex() == 0)
                    continue;
                Destroy(t.gameObject);
            }
            // the sole survivor (which will be killed later) will be the base
            var abase = _holder.transform.GetChild(0).gameObject;
            _holder.transform.DetachChildren();
            _holder.transform.localPosition = Vector3.zero;

            const double off = 0.666;

            up = SetupIcon(Instantiate(abase, _holder.transform), _upTex, off * 1, 0);
            down = SetupIcon(Instantiate(abase, _holder.transform), _downTex, off * 1, -off * 1);
            left = SetupIcon(Instantiate(abase, _holder.transform), _leftTex, 0, -off * 1);
            right = SetupIcon(Instantiate(abase, _holder.transform), _rightTex, off * 2, -off * 1);

            // scale floats at end are based on their texture width divided by regular
            fire = SetupIcon(Instantiate(abase, _holder.transform), _fireTex, off * 2, 0, 2.234375f);
            discard = SetupIcon(Instantiate(abase, _holder.transform), _discardTex, off * 4, 0, 2.234375f);
            jump = SetupIcon(Instantiate(abase, _holder.transform), _jumpTex, off * 3, -off * 1, 3.484375f);
            swap = SetupIcon(Instantiate(abase, _holder.transform), _swapTex, 0, 0);

            scroll = SetupIcon(Instantiate(abase, _holder.transform), _scrollTex, off * 3, -off * 1, 3.484375f);

            scrollSB = SetupIcon(Instantiate(abase, _holder.transform), _scrollSB, off * 6, -off * 0.5f);
            scrollSF = SetupIcon(Instantiate(abase, _holder.transform), _scrollSF, off * 6, -off * 0.5f);


            Destroy(abase);

            // BONUS: the 3rd ability icon is slightly off and is at a weird spot
            // let's move it and make it look even nicer
            cardUI.abilityIconHolder.transform.GetChild(2).localPosition = new Vector3((float)off * 2, 0, 0);
            Update();
        }

        private void Update()
        {
            var mode = Settings.DisplayMode;
            var flip = Settings.InvertPressed.Value;

            if (Settings.FaustasMode.Value)
            {
                faustasCycle = (faustasCycle + Time.deltaTime * Settings.FaustasSpeed.Value) % 360;
                currentColor = Color.HSVToRGB(faustasCycle / 360f, 0.8f, 0.8f);
            }

            offset = ((int)mode) / 8f;
            var input = Singleton<GameInput>.Instance;
            var x = input.GetAxis(GameInput.GameActions.MoveHorizontal);
            var y = input.GetAxis(GameInput.GameActions.MoveVertical);
            var jumpd = input.GetButtonDown(GameInput.GameActions.Jump, GameInput.InputType.Game);
            if (jumpd)
            {
                // if jump was just pushed on this frame, they either pressed spacebar or used a quick frame method like scrolling
                // so we use neon white's code against it
                try
                {
                    // this actually errors on scroll jump and a good chunk of the player's Update isn't actually run because of it in basegame
                    // but since we know that we can use that to our advantage
                    input.GetButton(GameInput.GameActions.Jump);
                }
                catch (Exception)
                {
                    var scroll = UnityEngine.InputSystem.Mouse.current.scroll;
                    scrollTimer = scroll.y.ReadValue() > 0 ? 0.1f : -0.1f;
                    jumpd = false ^ flip;
                }
            }
            else // if we didn't jump on this frame we might be holding it so check it as you normally would without worry
                jumpd = input.GetButton(GameInput.GameActions.Jump) ^ flip;


            var firei = input.GetButton(GameInput.GameActions.FireCard) ^ flip;
            var discardi = input.GetButton(GameInput.GameActions.FireCardAlt) ^ flip;
            var swapi = input.GetButton(GameInput.GameActions.SwapCard) ^ flip;

            // unity does textures kinda weird
            const float on = -0.75f, off = -0.25f;

            up.material.SetTextureOffset("_MainTex", new Vector2(offset, (y > 0 ^ flip) ? on : off));
            down.material.SetTextureOffset("_MainTex", new Vector2(offset, (y < 0 ^ flip) ? on : off));
            left.material.SetTextureOffset("_MainTex", new Vector2(offset, (x < 0 ^ flip) ? on : off));
            right.material.SetTextureOffset("_MainTex", new Vector2(offset, (x > 0 ^ flip) ? on : off));

            fire.material.SetTextureOffset("_MainTex", new Vector2(offset, firei ? on : off));
            discard.material.SetTextureOffset("_MainTex", new Vector2(offset, discardi ? on : off));
            jump.material.SetTextureOffset("_MainTex", new Vector2(offset, jumpd ? on : off));
            swap.material.SetTextureOffset("_MainTex", new Vector2(offset, swapi ? on : off));

            scrollSB.material.SetTextureOffset("_MainTex", new Vector2(offset, 0));

            up.transform.GetChild(0).gameObject.SetActive(!Settings.Borderless.Value);
            down.transform.GetChild(0).gameObject.SetActive(!Settings.Borderless.Value);
            left.transform.GetChild(0).gameObject.SetActive(!Settings.Borderless.Value);
            right.transform.GetChild(0).gameObject.SetActive(!Settings.Borderless.Value);

            fire.transform.GetChild(0).gameObject.SetActive(!Settings.Borderless.Value);
            discard.transform.GetChild(0).gameObject.SetActive(!Settings.Borderless.Value);
            jump.transform.GetChild(0).gameObject.SetActive(!Settings.Borderless.Value);
            swap.transform.GetChild(0).gameObject.SetActive(!Settings.Borderless.Value);

            scrollSB.transform.GetChild(0).gameObject.SetActive(!Settings.Borderless.Value);

            up.material.SetColor("_TintColor", (y > 0 ^ flip) && Settings.ColoredOff.Value && !Settings.AlwaysColor.Value ? Color.white : currentColor);
            down.material.SetColor("_TintColor", (y < 0 ^ flip) && Settings.ColoredOff.Value && !Settings.AlwaysColor.Value ? Color.white : currentColor);
            left.material.SetColor("_TintColor", (x < 0 ^ flip) && Settings.ColoredOff.Value && !Settings.AlwaysColor.Value ? Color.white : currentColor);
            right.material.SetColor("_TintColor", (x > 0 ^ flip) && Settings.ColoredOff.Value && !Settings.AlwaysColor.Value ? Color.white : currentColor);

            fire.material.SetColor("_TintColor", firei && Settings.ColoredOff.Value && !Settings.AlwaysColor.Value ? Color.white : currentColor);
            discard.material.SetColor("_TintColor", discardi && Settings.ColoredOff.Value && !Settings.AlwaysColor.Value ? Color.white : currentColor);
            jump.material.SetColor("_TintColor", jumpd && Settings.ColoredOff.Value && !Settings.AlwaysColor.Value ? Color.white : currentColor);
            swap.material.SetColor("_TintColor", swapi && Settings.ColoredOff.Value && !Settings.AlwaysColor.Value ? Color.white : currentColor);

            scroll.material.SetColor("_TintColor", Settings.ColoredOff.Value && !Settings.AlwaysColor.Value ? Color.white : currentColor);
            scrollSB.material.SetColor("_TintColor", Settings.ColoredOff.Value ? currentColor : Color.white);
            scrollSF.material.SetColor("_TintColor", Settings.ColoredOff.Value ? Color.white : currentColor);

            scrollSB.gameObject.SetActive(Settings.SeperateScroll.Value);

            scroll.enabled = false;
            scrollSF.enabled = false;
            if (scrollTimer != 0)
            {
                var usedScroll = Settings.SeperateScroll.Value ? scrollSF : scroll;
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
                usedScroll.material.SetTextureOffset("_MainTex", new Vector2(offset, negative ? 0 : -0.5f));
                usedScroll.enabled = true;
            }
        }
    }
}

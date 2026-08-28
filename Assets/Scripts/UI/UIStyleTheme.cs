using UnityEngine;
using UnityEngine.UI;

namespace KingdomsOfBharat.UI
{
    // Shared style tokens for the HUD/menu skin pass (Roadmap Section 4.3
    // "UI skin"). Scaffolded ahead of real art landing: every panel/button
    // currently hardcodes its own ad hoc color (the 4 runtime-generated
    // menus had already drifted into 2 different dark palettes before any
    // art existed), so this exists to fix that drift now with pure colors,
    // then let a real theme asset override them and layer 9-slice sprites
    // in later without another code change.
    //
    // A ScriptableObject rather than a static class so an artist can create
    // Assets/Resources/UI/UIStyleTheme.asset (via the CreateAssetMenu below)
    // once real palette decisions are made from actual art, without needing
    // a recompile - Current falls back to hardcoded defaults until that
    // asset exists, the same way AgeProfile/UpgradeProgress stay hardcoded
    // until there's a real reason to do otherwise.
    [CreateAssetMenu(fileName = "UIStyleTheme", menuName = "Kingdoms of Bharat/UI Style Theme")]
    public class UIStyleTheme : ScriptableObject
    {
        [SerializeField] private Color panelBackground = new Color(0.05f, 0.05f, 0.08f, 0.97f);
        [SerializeField] private Color panelBackdrop = new Color(0f, 0f, 0f, 0.6f);
        [SerializeField] private Color buttonNormal = new Color(0.25f, 0.25f, 0.3f, 1f);
        [SerializeField] private Color textPrimary = Color.white;
        [SerializeField] private Color textSecondary = new Color(0.85f, 0.85f, 0.85f, 1f);
        [SerializeField] private Color textSuccess = new Color(0.55f, 0.85f, 0.55f, 1f);

        // Both null until a real 9-slice frame/button sprite is sourced -
        // every call site checks for null and only assigns the sprite when
        // present, so today's flat colors keep working with zero behavior
        // change until these are filled in on a theme asset.
        [SerializeField] private Sprite panelFrameSprite;
        [SerializeField] private Sprite buttonBackgroundSprite;
        [SerializeField] private Sprite buttonHoverSprite;
        [SerializeField] private Sprite buttonPressedSprite;

        public Color PanelBackground => panelBackground;
        public Color PanelBackdrop => panelBackdrop;
        public Color ButtonNormal => buttonNormal;
        public Color TextPrimary => textPrimary;
        public Color TextSecondary => textSecondary;
        public Color TextSuccess => textSuccess;
        public Sprite PanelFrameSprite => panelFrameSprite;
        public Sprite ButtonBackgroundSprite => buttonBackgroundSprite;
        public Sprite ButtonHoverSprite => buttonHoverSprite;
        public Sprite ButtonPressedSprite => buttonPressedSprite;

        private static UIStyleTheme _current;

        public static UIStyleTheme Current
        {
            get
            {
                if (_current == null)
                {
                    _current = Resources.Load<UIStyleTheme>("UI/UIStyleTheme");
                }

                if (_current == null)
                {
                    _current = CreateInstance<UIStyleTheme>();
                }

                return _current;
            }
        }

        // Applies the panel frame sprite/background color to an Image if
        // one is present - shared by every panel/backdrop call site so the
        // "assign sprite only if we have one" check lives in one place.
        public void ApplyPanel(Image image)
        {
            if (image == null)
            {
                return;
            }

            image.color = PanelBackground;
            if (panelFrameSprite != null)
            {
                image.sprite = panelFrameSprite;
                image.type = Image.Type.Sliced;
            }
        }

        // Sets the base look today (flat color, or a 9-slice sprite once one
        // exists) and additionally wires real hover/pressed art into the
        // button's SpriteState when a Button component and hover/pressed
        // sprites are all present - falls back to Unity's default ColorTint
        // transition otherwise, so this is safe to call on any Image whether
        // or not it sits on a Button.
        public void ApplyButton(Image image)
        {
            if (image == null)
            {
                return;
            }

            image.color = ButtonNormal;
            if (buttonBackgroundSprite != null)
            {
                image.sprite = buttonBackgroundSprite;
                image.type = Image.Type.Sliced;
            }

            if (buttonHoverSprite == null || buttonPressedSprite == null
                || !image.TryGetComponent(out Selectable selectable))
            {
                return;
            }

            selectable.transition = Selectable.Transition.SpriteSwap;
            SpriteState state = selectable.spriteState;
            state.highlightedSprite = buttonHoverSprite;
            state.pressedSprite = buttonPressedSprite;
            selectable.spriteState = state;
        }
    }
}

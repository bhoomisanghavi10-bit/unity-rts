using NUnit.Framework;
using KingdomsOfBharat.UI;

namespace KingdomsOfBharat.Tests
{
    // Roadmap Section 4.3 "UI skin": no theme asset exists yet (no real art
    // has landed), so this only covers the hardcoded-default fallback path
    // - the same path every panel/button actually uses today. Once a real
    // Assets/Resources/UI/UIStyleTheme.asset is created, Current will start
    // returning that instead; this test doesn't assume which one it gets,
    // just that it's always non-null, stable, and has usable values.
    public class UIStyleThemeTests
    {
        [Test]
        public void Current_IsNeverNull()
        {
            Assert.IsNotNull(UIStyleTheme.Current);
        }

        [Test]
        public void Current_ReturnsSameCachedInstance()
        {
            UIStyleTheme first = UIStyleTheme.Current;
            UIStyleTheme second = UIStyleTheme.Current;
            Assert.AreSame(first, second);
        }

        [Test]
        public void DefaultColors_AreOpaqueOrIntentionallyTranslucent_NotDefaultBlack()
        {
            UIStyleTheme theme = UIStyleTheme.Current;

            // PanelBackdrop is a deliberate translucent dim, and
            // PanelBackground a deliberate near-opaque 0.97 (matching the
            // original hardcoded panel alpha it replaces) - not mistakes.
            // Everything else should read as fully opaque, real color.
            Assert.Greater(theme.PanelBackdrop.a, 0f);
            Assert.Greater(theme.PanelBackground.a, 0.9f);
            Assert.AreEqual(1f, theme.ButtonNormal.a, 0.01f);
            Assert.AreEqual(1f, theme.TextPrimary.a, 0.01f);
            Assert.AreEqual(1f, theme.TextSecondary.a, 0.01f);
            Assert.AreEqual(1f, theme.TextSuccess.a, 0.01f);
        }

        [Test]
        public void ApplyPanel_And_ApplyButton_AreNullSafe()
        {
            Assert.DoesNotThrow(() => UIStyleTheme.Current.ApplyPanel(null));
            Assert.DoesNotThrow(() => UIStyleTheme.Current.ApplyButton(null));
        }
    }
}

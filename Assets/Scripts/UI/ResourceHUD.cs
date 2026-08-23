using UnityEngine;
using KingdomsOfBharat.ResourceGathering;
using KingdomsOfBharat.Core;
using KingdomsOfBharat.Buildings;
using KingdomsOfBharat.Progression;

namespace KingdomsOfBharat.UI
{
    // Always-on resource counters, top-left corner. Hardcoded to the
    // Player's stockpile specifically (not a generic lookup) so it can
    // never accidentally end up displaying the AI's economy.
    public class ResourceHUD : MonoBehaviour
    {
        private GUIStyle _style;

        private void OnGUI()
        {
            ResourceStockpile stockpile = ResourceStockpile.For(FactionId.Player);
            if (stockpile == null)
            {
                return;
            }

            EnsureStyle();

            string civName = CivilizationProfile.For(CivilizationRegistry.For(FactionId.Player)).DisplayName;
            string ageName = AgeProfile.For(AgeProgress.CurrentAge(FactionId.Player)).DisplayName;

            GUI.Box(new Rect(8, 8, 160, 162), GUIContent.none);
            GUI.Label(new Rect(16, 12, 150, 20), $"Civilization: {civName}", _style);
            GUI.Label(new Rect(16, 34, 150, 20), $"Wood: {(int)stockpile.GetTotal(ResourceType.Wood)}", _style);
            GUI.Label(new Rect(16, 56, 150, 20), $"Food: {(int)stockpile.GetTotal(ResourceType.Food)}", _style);
            GUI.Label(new Rect(16, 78, 150, 20), $"Gold: {(int)stockpile.GetTotal(ResourceType.Gold)}", _style);
            GUI.Label(new Rect(16, 100, 150, 20), $"Stone: {(int)stockpile.GetTotal(ResourceType.Stone)}", _style);
            GUI.Label(new Rect(16, 122, 150, 20), $"Population: {Population.Current(FactionId.Player)}/{Population.Cap(FactionId.Player)}", _style);
            GUI.Label(new Rect(16, 144, 150, 20), $"Age: {ageName}", _style);
        }

        private void EnsureStyle()
        {
            if (_style != null)
            {
                return;
            }

            _style = new GUIStyle(GUI.skin.label) { fontSize = 14, fontStyle = FontStyle.Bold };
            _style.normal.textColor = Color.white;
        }
    }
}

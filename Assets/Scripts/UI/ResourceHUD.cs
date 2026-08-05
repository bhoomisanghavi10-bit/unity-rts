using UnityEngine;
using KingdomsOfBharat.ResourceGathering;

namespace KingdomsOfBharat.UI
{
    // Always-on resource counters, top-left corner.
    public class ResourceHUD : MonoBehaviour
    {
        private GUIStyle _style;

        private void OnGUI()
        {
            ResourceStockpile stockpile = ResourceStockpile.Instance;
            if (stockpile == null)
            {
                return;
            }

            EnsureStyle();

            GUI.Box(new Rect(8, 8, 160, 54), GUIContent.none);
            GUI.Label(new Rect(16, 12, 150, 20), $"Wood: {(int)stockpile.GetTotal(ResourceType.Wood)}", _style);
            GUI.Label(new Rect(16, 34, 150, 20), $"Food: {(int)stockpile.GetTotal(ResourceType.Food)}", _style);
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

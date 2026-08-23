using UnityEngine;
using TMPro;
using KingdomsOfBharat.ResourceGathering;
using KingdomsOfBharat.Core;
using KingdomsOfBharat.Buildings;
using KingdomsOfBharat.Progression;

namespace KingdomsOfBharat.UI
{
    // Always-on resource counters, top-left corner. Hardcoded to the
    // Player's stockpile specifically (not a generic lookup) so it can
    // never accidentally end up displaying the AI's economy. uGUI/TMP
    // replacement for the original OnGUI version - the labels are real
    // Canvas children wired up in the Inspector, this just pushes text
    // into them every frame instead of issuing GUI.Label draw calls.
    public class ResourceHUD : MonoBehaviour
    {
        [SerializeField] private TMP_Text civLabel;
        [SerializeField] private TMP_Text woodLabel;
        [SerializeField] private TMP_Text foodLabel;
        [SerializeField] private TMP_Text goldLabel;
        [SerializeField] private TMP_Text stoneLabel;
        [SerializeField] private TMP_Text populationLabel;
        [SerializeField] private TMP_Text ageLabel;

        private void Update()
        {
            ResourceStockpile stockpile = ResourceStockpile.For(FactionId.Player);
            if (stockpile == null)
            {
                return;
            }

            string civName = CivilizationProfile.For(CivilizationRegistry.For(FactionId.Player)).DisplayName;
            string ageName = AgeProfile.For(AgeProgress.CurrentAge(FactionId.Player)).DisplayName;

            civLabel.text = $"Civilization: {civName}";
            woodLabel.text = $"Wood: {(int)stockpile.GetTotal(ResourceType.Wood)}";
            foodLabel.text = $"Food: {(int)stockpile.GetTotal(ResourceType.Food)}";
            goldLabel.text = $"Gold: {(int)stockpile.GetTotal(ResourceType.Gold)}";
            stoneLabel.text = $"Stone: {(int)stockpile.GetTotal(ResourceType.Stone)}";
            populationLabel.text = $"Population: {Population.Current(FactionId.Player)}/{Population.Cap(FactionId.Player)}";
            ageLabel.text = $"Age: {ageName}";
        }
    }
}

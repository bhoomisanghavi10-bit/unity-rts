using UnityEngine;
using KingdomsOfBharat.AI;

namespace KingdomsOfBharat.Core
{
    // Item 46: PlayerPrefs-backed settings the player can actually change
    // at runtime, layered on top of every component's own Inspector-
    // configured default the same way MapRegistry/CivilizationRegistry
    // already override spawner defaults elsewhere in this project - a
    // component reads its GameSettings override once (Awake/Start), so
    // anything that never opens the Settings menu behaves exactly as
    // before this file existed.
    public static class GameSettings
    {
        private const string DifficultyKey = "Settings_Difficulty";
        private const string ColorblindKey = "Settings_Colorblind";
        private const string KeyPrefix = "Settings_Key_";

        // AiController's own [SerializeField] difficulty stays the source
        // of truth until the player has actually opened Settings and
        // changed it - this distinguishes "never touched" from "player
        // explicitly chose Normal", so a designer's Inspector choice isn't
        // silently overwritten by a PlayerPrefs default of the same value.
        public static bool HasDifficultyOverride => PlayerPrefs.HasKey(DifficultyKey);

        public static AiDifficulty Difficulty
        {
            get => (AiDifficulty)PlayerPrefs.GetInt(DifficultyKey, (int)AiDifficulty.Normal);
            set => PlayerPrefs.SetInt(DifficultyKey, (int)value);
        }

        // Swaps HealthBar's red/green HP gradient for a blue/orange one
        // (see HealthBar.cs) - red-green is the pairing deuteranopia/
        // protanopia (the two most common forms of color blindness) confuse
        // most easily, blue/orange is readable across all common types.
        public static bool ColorblindMode
        {
            get => PlayerPrefs.GetInt(ColorblindKey, 0) == 1;
            set => PlayerPrefs.SetInt(ColorblindKey, value ? 1 : 0);
        }

        // actionId identifies one rebindable action (e.g. "CycleStance",
        // "TrainUnit"). The caller always passes its own current field
        // value as defaultKey, so a player who's never opened Settings
        // gets exactly the key they'd have gotten before this file existed.
        public static KeyCode GetKey(string actionId, KeyCode defaultKey)
        {
            string prefKey = KeyPrefix + actionId;
            return PlayerPrefs.HasKey(prefKey)
                ? (KeyCode)PlayerPrefs.GetInt(prefKey)
                : defaultKey;
        }

        public static void SetKey(string actionId, KeyCode key)
        {
            PlayerPrefs.SetInt(KeyPrefix + actionId, (int)key);
        }

        public static void Save()
        {
            PlayerPrefs.Save();
        }
    }
}

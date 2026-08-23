using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using KingdomsOfBharat.Match;

namespace KingdomsOfBharat.UI
{
    // Full-screen overlay shown once MatchManager reaches an outcome.
    // MatchManager already freezes the match (Time.timeScale = 0) the
    // instant it decides Victory/Defeat - this just displays the result and
    // offers a restart; it doesn't own the freeze itself.
    public class GameOverScreen : MonoBehaviour
    {
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private TMP_Text titleLabel;
        [SerializeField] private Button restartButton;

        private void Awake()
        {
            panelRoot.SetActive(false);
            restartButton.onClick.AddListener(Restart);
        }

        private void Update()
        {
            if (MatchManager.Outcome == MatchOutcome.Ongoing || panelRoot.activeSelf)
            {
                return;
            }

            panelRoot.SetActive(true);
            bool won = MatchManager.Outcome == MatchOutcome.Victory;
            titleLabel.text = won ? "VICTORY!" : "DEFEAT";
            titleLabel.color = won ? new Color(1f, 0.85f, 0.2f) : new Color(0.85f, 0.2f, 0.2f);
        }

        // Time.timeScale must be restored before the reload - MatchManager's
        // freeze would otherwise persist into the freshly loaded scene
        // (timeScale is a global Unity setting, not scene state).
        private static void Restart()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }
}

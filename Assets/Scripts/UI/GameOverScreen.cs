using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using KingdomsOfBharat.Core;
using KingdomsOfBharat.Match;
using KingdomsOfBharat.Progression;

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

        // Wave 6 item 36 (Score system): built entirely in code rather than
        // a new scene-wired [SerializeField] - avoids the recurring
        // "new field null in the scene" gotcha several other sessions have
        // hit, and there's plenty of empty space between TitleLabel (top)
        // and RestartButton (bottom) in the existing panel to fit it.
        private TMP_Text _scoreLabel;

        private void Awake()
        {
            panelRoot.SetActive(false);
            restartButton.onClick.AddListener(Restart);
            _scoreLabel = BuildScoreLabel();
        }

        private void Update()
        {
            if (MatchManager.Outcome == MatchOutcome.Ongoing || panelRoot.activeSelf)
            {
                return;
            }

            panelRoot.SetActive(true);
            // Item 2 (Victory Conditions): Draw is only reachable via the
            // Time Limit tiebreaker (MatchManager.ResolveTimeLimitOutcome) -
            // elimination and scripted missions never produce it.
            switch (MatchManager.Outcome)
            {
                case MatchOutcome.Victory:
                    titleLabel.text = "VICTORY!";
                    titleLabel.color = new Color(1f, 0.85f, 0.2f);
                    break;
                case MatchOutcome.Draw:
                    titleLabel.text = "DRAW";
                    titleLabel.color = new Color(0.75f, 0.75f, 0.75f);
                    break;
                default:
                    titleLabel.text = "DEFEAT";
                    titleLabel.color = new Color(0.85f, 0.2f, 0.2f);
                    break;
            }

            _scoreLabel.text = BuildScoreText();
        }

        // Deliberately compares Player against Enemy only, not the optional
        // 3rd faction (Enemy2) - matches this project's existing convention
        // of treating Enemy2 as an experimental extra, not part of the
        // standard 2-side match summary.
        private static string BuildScoreText()
        {
            ScoreProgress.Breakdown player = ScoreProgress.Compute(FactionId.Player);
            ScoreProgress.Breakdown enemy = ScoreProgress.Compute(FactionId.Enemy);

            return "Final Score (You vs Enemy)\n\n"
                + $"Military:    {player.Military} vs {enemy.Military}\n"
                + $"Economy:     {player.Economy} vs {enemy.Economy}\n"
                + $"Technology:  {player.Technology} vs {enemy.Technology}\n"
                + $"Society:     {player.Society} vs {enemy.Society}\n\n"
                + $"Total:       {player.Total} vs {enemy.Total}";
        }

        private TMP_Text BuildScoreLabel()
        {
            var go = new GameObject("ScoreLabel");
            go.transform.SetParent(titleLabel.transform.parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(420f, 360f);
            rect.anchoredPosition = new Vector2(0f, 10f);

            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.fontSize = 20;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.font = titleLabel.font;
            tmp.enableWordWrapping = false;
            return tmp;
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

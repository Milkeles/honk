/* HUD overlay for the gameplay scene: shows the score and a row of hearts for lives, rebuilt from a
 * template on init. Driven by GameController via Initialize / SetLives / UpdateScore — holds no game
 * logic, only display. Pause/game-over hooks are stubs for later UI.
 *
 * Dependencies: Godot
 * Author(s): H. Hristov (Milkeles)
 * Created: 18/06/2026 (dd/mm/yyyy)
 * Updated: 19/06/2026 (dd/mm/yyyy)
 * Last change: Added comments.
*/

using Godot;
using System.Collections.Generic;

namespace Presentation
{
    public partial class GameplayView : CanvasLayer
    {
        #region fields
        [Export] private PackedScene _heartScene;

        private Node _heartContainer;
        private TransitionSceneView _transition;
        private CenterContainer _gameOverPopup;
        private Button _menuButton;
        private Button _replayButton;
        private Button _reviveButton;
        private Button _pauseMenuButton;
        private Button _pauseRestartButton;
        private Label _scoreLabel;

        private readonly List<TextureRect> _hearts = new();

        private int _score;
        private int _lives;
        private int _maxLives;
        #endregion

        #region events
        /// <summary>Emitted when the player taps revive on the game-over popup.</summary>
        [Signal]
        public delegate void RevivePressedEventHandler();
        #endregion

        #region public methods
        /// <summary>Builds the heart row and sets the initial lives and score.</summary>
        /// <param name="maxLives">Number of hearts to display.</param>
        /// <param name="startLives">Hearts lit at start.</param>
        /// <param name="startScore">Initial score value.</param>
        public void Initialize(int maxLives, int startLives, int startScore = 0)
        {
            _maxLives = maxLives;

            BuildHearts(maxLives);
            SetLives(startLives);
            UpdateScore(startScore);
        }

        /// <summary>Lights the first <paramref name="lives"/> hearts and dims the rest.</summary>
        public void SetLives(int lives)
        {
            _lives = lives;

            for (int i = 0; i < _hearts.Count; i++)
                _hearts[i].Visible = i < lives;
        }

        /// <summary>Updates the displayed score.</summary>
        public void UpdateScore(int score)
        {
            _score = score;

            if (_scoreLabel != null)
                _scoreLabel.Text = _score.ToString();
        }

        /// <summary>Hook for pause-state visuals.</summary>
        public void UpdatePause(bool isPaused) { }

        /// <summary>Hook for game-over visuals.</summary>
        public void ShowGameOver(int finalScore, bool canRevive)
        {
            GD.Print("Called");
            _gameOverPopup.Show();
            var spreadStars = _gameOverPopup.GetNode<Label>("%SpreadStars");

            for (int i = 1; i <= 3; i++)
            {
                spreadStars.GetNode<TextureRect>($"Star{i}").Visible = finalScore >= i * 20;
            }

            var currentScoreLabel = _gameOverPopup.GetNode<Label>("%CurrentScore");
            currentScoreLabel.Text = $"Score: {finalScore}";
            if (canRevive)
            {
                _reviveButton.Visible = true;
                _reviveButton.Text = "REVIVE (AD)";
            }
            else
            {
                _reviveButton.Visible = false;
            }

            var highScoreLabel = _gameOverPopup.GetNode<Label>("%HighScore");
            var save = GetNode<Services.SaveManager>("/root/SaveManager");
            highScoreLabel.Text = $"High Score: {save.HighScore}";
        }

        /// <summary>Hides the game-over popup (e.g. after a successful revive).</summary>
        public void HideGameOver() => _gameOverPopup.Hide();
        #endregion

        #region private methods
        // Rebuilds the heart row from %HeartTemplate. Re-entrant: clears prior hearts first so a
        // revive/restart re-init doesn't stack duplicates.
        private void BuildHearts(int maxLives)
        {
            foreach (Node child in _heartContainer.GetChildren())
                child.QueueFree();
            _hearts.Clear();

            var template = GetNode<TextureRect>("%HeartTemplate");

            for (int i = 0; i < maxLives; i++)
            {
                var heart = template.Duplicate() as TextureRect;
                heart.Visible = true;
                heart.Name = $"Heart{i + 1}";

                _heartContainer.AddChild(heart);

                // "Active" is the inner fill toggled per-life.
                var active = heart.GetNode<TextureRect>("Active");
                _hearts.Add(active);
            }
        }

        private void OnMenuPressed() => _transition.GoToScene("Menu");
        #endregion

        #region engine lifecycle
        public override void _Ready()
        {
            _heartContainer = GetNode("%HeartContainer");
            _scoreLabel = GetNode<Label>("%Score");
            _gameOverPopup = GetNode<CenterContainer>("%GameOverPopup");
    
            _transition = GetNode<TransitionSceneView>("/root/TransitionScene");

            _replayButton = GetNode<Button>("%ReplayBtn");
            _reviveButton = GetNode<Button>("%ReviveBtn");
            _menuButton = GetNode<Button>("%MenuBtn");
            _pauseMenuButton = GetNode<Button>("%PauseMenuBtn");
            _pauseRestartButton = GetNode<Button>("%PauseRestartBtn");

            _menuButton.Pressed += OnMenuPressed;
            _pauseMenuButton.Pressed += OnMenuPressed;
            _pauseRestartButton.Pressed += () => GetTree().ReloadCurrentScene();
            _replayButton.Pressed += () => GetTree().ReloadCurrentScene();
            _reviveButton.Pressed += () => EmitSignal(SignalName.RevivePressed);
        }
        #endregion
    }
}
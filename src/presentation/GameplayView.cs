/* HUD overlay for the gameplay scene: shows the score and a row of hearts for lives, rebuilt from a
 * template on init. Driven by GameController via Initialize / SetLives / UpdateScore — holds no game
 * logic, only display. Pause freezes the scene tree; popup visibility is handled by editor signals.
 *
 * Dependencies: Godot
 * Author(s): H. Hristov (Milkeles)
 * Created: 18/06/2026 (dd/mm/yyyy)
 * Updated: 28/06/2026 (dd/mm/yyyy)
 * Last change: Wired pause button to freeze the tree; pause-menu buttons process while paused
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
        private Button _pauseButton;
        private Button _menuButton;
        private Button _replayButton;
        private Button _reviveButton;
        private Button _pauseMenuButton;
        private Button _pauseRestartButton;
        private Button _continueButton ;
        private Label _scoreLabel;

        private readonly List<TextureRect> _hearts = new();

        private int _score;
        private int _lives;
        private int _maxLives;
        private bool _isPaused;
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

        /// <summary>Toggles pause: freezes/unfreezes the scene tree.</summary>
        public void TogglePause() => SetPaused(!_isPaused);

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
            _gameOverPopup.Show();
            //GD.Print($"finalScore={finalScore}");

            var stars = _gameOverPopup.GetNodeOrNull("%SpreadStars");
            //GD.Print($"SpreadStars found: {stars != null}  type: {stars?.GetType().Name}");

            var cur = _gameOverPopup.GetNodeOrNull<Label>("%CurrentScore");
            //GD.Print($"CurrentScore found: {cur != null}");
            if (cur != null) cur.Text = $"Score: {finalScore}";

            var hi = _gameOverPopup.GetNodeOrNull<Label>("%HighScore");
            //GD.Print($"HighScore found: {hi != null}");
            var save = GetNode<Services.SaveManager>("/root/SaveManager");
            if (hi != null) hi.Text = $"High Score: {save.HighScore}";

            _reviveButton.Visible = canRevive;
            if (canRevive) _reviveButton.Text = "REVIVE (AD)";
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

        private void SetPaused(bool paused)
        {
            // Don't allow pausing over the game-over screen.
            if (paused && _gameOverPopup.Visible) return;

            _isPaused = paused;
            GetTree().Paused = paused;
            UpdatePause(paused);
        }

        private void OnMenuPressed()
        {
            GetTree().Paused = false; // clear freeze before leaving, or the next scene starts paused
            _transition.GoToScene("Menu");
        }

        private void OnRestartPressed()
        {
            GetTree().Paused = false; // Paused survives a scene reload, so clear it first
            GetTree().ReloadCurrentScene();
        }
        #endregion

        #region engine lifecycle
        public override void _Ready()
        {
            _heartContainer = GetNode("%HeartContainer");
            _scoreLabel = GetNode<Label>("%Score");
            _gameOverPopup = GetNode<CenterContainer>("%GameOverPopup");

            _transition = GetNode<TransitionSceneView>("/root/TransitionScene");

            _pauseButton = GetNode<Button>("%PauseBtn");
            _replayButton = GetNode<Button>("%ReplayBtn");
            _reviveButton = GetNode<Button>("%ReviveBtn");
            _menuButton = GetNode<Button>("%MenuBtn");
            _pauseMenuButton = GetNode<Button>("%PauseMenuBtn");
            _pauseRestartButton = GetNode<Button>("%PauseRestartBtn");
            _continueButton = GetNode<Button>("%ContinueBtn");

            // The pause-menu buttons must stay clickable while the tree is frozen.
            _pauseMenuButton.ProcessMode = ProcessModeEnum.Always;
            _pauseRestartButton.ProcessMode = ProcessModeEnum.Always;
            _continueButton.ProcessMode = ProcessModeEnum.Always;

            _pauseButton.Pressed += TogglePause;
            _menuButton.Pressed += OnMenuPressed;
            _pauseMenuButton.Pressed += OnMenuPressed;
            _pauseRestartButton.Pressed += OnRestartPressed;
            _replayButton.Pressed += () => GetTree().ReloadCurrentScene();
            _reviveButton.Pressed += () => EmitSignal(SignalName.RevivePressed);
            _continueButton.Pressed += () => TogglePause();
        }
        #endregion
    }
}
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
        private Label _scoreLabel;

        private readonly List<TextureRect> _hearts = new();

        private int _score;
        private int _lives;
        private int _maxLives;
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
        public void UpdateGameOver(bool isGameOver) { }
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
        #endregion

        #region engine lifecycle
        public override void _Ready()
        {
            _heartContainer = GetNode("%HeartContainer");
            _scoreLabel = GetNode<Label>("%Score");
        }
        #endregion
    }
}
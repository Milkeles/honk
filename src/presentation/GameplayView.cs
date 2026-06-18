using Godot;
using System.Collections.Generic;

namespace Presentation
{
    public partial class GameplayView : CanvasLayer
    {
        [Export] private PackedScene _heartScene;

        private Node _heartContainer;
        private Label _scoreLabel;

        private readonly List<TextureRect> _hearts = new();

        private int _score;
        private int _lives;
        private int _maxLives;

        public override void _Ready()
        {
            _heartContainer = GetNode("%HeartContainer");
            _scoreLabel = GetNode<Label>("%Score");
        }

        public void Initialize(int maxLives, int startLives, int startScore = 0)
        {
            _maxLives = maxLives;

            BuildHearts(maxLives);
            SetLives(startLives);
            UpdateScore(startScore);
            GD.Print($"HUD initialized with {maxLives} max lives, {startLives} start lives, and {startScore} start score.");
        }

        private void BuildHearts(int maxLives)
        {
            _hearts.Clear();

            var template = GetNode<TextureRect>("%HeartTemplate");

            for (int i = 0; i < maxLives; i++)
            {
                var heart = template.Duplicate() as TextureRect;

                heart.Visible = true;
                heart.Name = $"Heart{i + 1}";

                _heartContainer.AddChild(heart);

                // "Active" is the inner fill/overlay
                var active = heart.GetNode<TextureRect>("Active");
                _hearts.Add(active);
            }
        }

        public void SetLives(int lives)
        {
            _lives = lives;

            for (int i = 0; i < _hearts.Count; i++)
            {
                _hearts[i].Visible = i < lives;
            }
        }

        public void UpdateScore(int score)
        {
            _score = score;

            if (_scoreLabel != null)
                _scoreLabel.Text = _score.ToString();
        }

        public void UpdatePause(bool isPaused)
        {
            // optional hook
        }

        public void UpdateGameOver(bool isGameOver)
        {
            // optional hook
        }
    }
}
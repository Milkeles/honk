/* Main menu screen: Play transitions to the gameplay scene via TransitionScene; Quit exits. Hosts the
 * SFX and Music volume sliders with live preview; Save commits both to SaveManager, Discard reverts
 * them to the last saved values. Sliders and buses initialize from SaveManager.
 *
 * Dependencies: TransitionSceneView, SaveManager, Godot
 * Author(s): H. Hristov (Milkeles)
 * Created: 18/06/2026 (dd/mm/yyyy)
 * Updated: 01/07/2026 (dd/mm/yyyy)
 * Last change: Added coins and highscore.
*/

using Godot;

namespace Presentation
{
    public partial class MenuView : Control
    {
        #region fields
        private TransitionSceneView _transition;
        private Services.SaveManager _save;

        private Slider _sfxSlider;
        private Slider _musicSlider;

        // Last-saved slider positions (0..100), used to revert on Discard.
        private float _savedSfx;
        private float _savedMusic;
        private Label _highscoreLabel, _coinsLabel;
        #endregion

        #region private methods
        private void OnPlayPressed() => _transition.GoToScene("Gameplay");
        private void OnCharacterPressed() => _transition.GoToScene("CharacterUpgrade");
        private void OnQuitPressed() => GetTree().Quit();

        private void OnSFXValueChanged(double value) => ApplyToBus("SFX", (float)value);

        private void OnMusicValueChanged(double value) => ApplyToBus("Music", (float)value);

        private void OnSavePressed()
        {
            _savedSfx = (float)_sfxSlider.Value;
            _savedMusic = (float)_musicSlider.Value;
            // SaveManager stores the slider values (0..100); the bus is already live.
            _save.SetVolumes(_savedMusic, _savedSfx);
        }

        private void OnDiscardPressed()
        {
            // Restore sliders to the last saved positions; setting Value re-fires the
            // value-changed handlers, which reapply the saved volume to each bus.
            _sfxSlider.Value = _savedSfx;
            _musicSlider.Value = _savedMusic;
        }

        // --- volume <-> slider mapping (0 -> -80dB mute, 50 -> 0dB, 100 -> +6dB) ---
        private static void ApplyToBus(string busName, float sliderValue)
        {
            int bus = AudioServer.GetBusIndex(busName);
            AudioServer.SetBusVolumeDb(bus, SliderToDb(sliderValue));
        }

        private static float SliderToDb(float value)
        {
            if (value <= 0f) return -80f;
            return value <= 50f
                ? Mathf.Lerp(-80f, 0f, value / 50f)
                : Mathf.Lerp(0f, 6f, (value - 50f) / 50f);
        }

        private static float DbToSlider(float db)
        {
            if (db <= -80f) return 0f;
            return db <= 0f
                ? Mathf.Remap(db, -80f, 0f, 0f, 50f)
                : Mathf.Remap(db, 0f, 6f, 50f, 100f);
        }
        #endregion

        #region engine lifecycle
        public override void _Ready()
        {
            _transition = GetNode<TransitionSceneView>("/root/TransitionScene");
            _save = GetNode<Services.SaveManager>("/root/SaveManager");

            GetNode<Button>("%Play").Pressed += OnPlayPressed;
            GetNode<Button>("%Character").Pressed += OnCharacterPressed;
            GetNode<Button>("%Quit").Pressed += OnQuitPressed;
            GetNode<Button>("%SaveBtn").Pressed += OnSavePressed;
            GetNode<Button>("%DiscardBtn").Pressed += OnDiscardPressed;

            _sfxSlider = GetNode<Slider>("%SFXSlider");
            _musicSlider = GetNode<Slider>("%MusicSlider");
            _highscoreLabel = GetNode<Label>("%HighscoreLabel");
            _coinsLabel = GetNode<Label>("%CoinsLabel");

            // Seed sliders + buses from the saved values, then connect change handlers.
            _savedSfx = _save.SfxVolume;
            _savedMusic = _save.MusicVolume;

            _sfxSlider.Value = _savedSfx;
            _musicSlider.Value = _savedMusic;
            ApplyToBus("SFX", _savedSfx);
            ApplyToBus("Music", _savedMusic);

            _sfxSlider.ValueChanged += OnSFXValueChanged;
            _musicSlider.ValueChanged += OnMusicValueChanged;
            _highscoreLabel.Text = $"High Score: {_save.HighScore}";
            _coinsLabel.Text = _save.Coins.ToString();
        }
        #endregion
    }
}
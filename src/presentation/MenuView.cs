/* Main menu screen: Play transitions to the gameplay scene via the global TransitionScene; Quit exits
 * the app. Display/navigation only.
 *
 * Dependencies: TransitionSceneView, Godot
 * Author(s): H. Hristov (Milkeles)
 * Created: 18/06/2026 (dd/mm/yyyy)
 * Updated: 19/06/2026 (dd/mm/yyyy)
 * Last change: Added comments.
*/

using Godot;

namespace Presentation
{
    public partial class MenuView : Control
    {
        #region fields
        private TransitionSceneView _transition;
        #endregion

        #region private methods
        private void OnPlayPressed() => _transition.GoToScene("Gameplay");

        private void OnQuitPressed() => GetTree().Quit();

        public void OnSFXValueChanged(float value)
        {
            int bus = AudioServer.GetBusIndex("SFX");
            float db;

            if (value <= 0)
            {
                db = -80.0f; // mute
            }
            else
            {
                // Map:
                // 0   -> -80 dB
                // 50  -> 0 dB
                // 100 -> +6 dB
                if (value <= 50)
                    db = Mathf.Lerp(-80f, 0f, value / 50f);
                else
                    db = Mathf.Lerp(0f, 6f, (value - 50f) / 50f);
            }

            AudioServer.SetBusVolumeDb(bus, db);
        }
        #endregion

        #region engine lifecycle
        public override void _Ready()
        {
            _transition = GetNode<TransitionSceneView>("/root/TransitionScene");

            GetNode<Button>("%Play").Pressed += OnPlayPressed;
            GetNode<Button>("%Quit").Pressed += OnQuitPressed;
            Slider slider = GetNode<Slider>("%SFXSlider");

            float db = AudioServer.GetBusVolumeDb(
                AudioServer.GetBusIndex("SFX"));

            if (db <= -80f)
                slider.Value = 0;
            else if (db <= 0f)
                slider.Value = Mathf.Remap(db, -80f, 0f, 0f, 50f);
            else
                slider.Value = Mathf.Remap(db, 0f, 6f, 50f, 100f);
            
        }
        #endregion
    }
}
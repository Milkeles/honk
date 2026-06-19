/* Global scene-transition overlay (autoload). Fades a full-screen ColorRect to opaque, swaps to the
 * target scene, then fades back, blocking input only during the fade. GoToScene takes a scene name
 * resolved under res://scenes/.
 *
 * Dependencies: Godot
 * Author(s): H. Hristov (Milkeles)
 * Created: 18/06/2026 (dd/mm/yyyy)
 * Updated: 19/06/2026 (dd/mm/yyyy)
 * Last change: Added comments
*/

using Godot;

namespace Presentation
{
    public partial class TransitionSceneView : CanvasLayer
    {
        #region fields
        [Export] private float _switchDuration = 1.0f;

        private ColorRect _colorRect;
        #endregion

        #region public methods
        /// <summary>Fades out, switches to res://scenes/{scene}.tscn, then fades back in.</summary>
        /// <param name="scene">Scene file name without path or extension.</param>
        public async void GoToScene(string scene)
        {
            _colorRect.MouseFilter = Control.MouseFilterEnum.Stop;

            Tween tween = GetTree().CreateTween();
            tween.TweenProperty(_colorRect, "modulate", new Color(1, 1, 1, 1), _switchDuration / 2f);
            await ToSignal(tween, Tween.SignalName.Finished);

            GetTree().ChangeSceneToFile($"res://scenes/{scene}.tscn");
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

            tween = GetTree().CreateTween();
            tween.TweenProperty(_colorRect, "modulate", new Color(1, 1, 1, 0), _switchDuration / 2f);
            await ToSignal(tween, Tween.SignalName.Finished);

            _colorRect.MouseFilter = Control.MouseFilterEnum.Ignore;
        }
        #endregion

        #region engine lifecycle
        public override void _Ready()
        {
            _colorRect = GetNode<ColorRect>("ColorRect");
            _colorRect.MouseFilter = Control.MouseFilterEnum.Ignore;
            _colorRect.Modulate = new Color(1, 1, 1, 0);
        }
        #endregion
    }
}
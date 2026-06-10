/* Presentation node for a single car. Driven by GameController in response to core events: shows its
 * turn arrow, slides up its lane (waiting -> front), curves across the box and off through the
 * destination lane, or drives in and crashes. Plays honk/crash effects and reports when an animation
 * finishes so the box can be freed. Visual twin of a Core.Car — holds no game logic; all world
 * positions are supplied from outside.
 *
 * Dependencies: Direction (Core), Godot
 * Author(s): H. Hristov (Milkeles)
 * Created: 04/06/2026 (dd/mm/yyyy)
 * Updated: 05/06/2026 (dd/mm/yyyy)
 * Last change: Removed unused _rotationOffset; SlideTo is now retargetable (fixes stuck back car)
*/

using Godot;
using Core;
using System.Linq;
using System.Threading.Tasks;

namespace Presentation
{
    public partial class CarView : Node2D
    {
        #region fields
        // Set false to disable keyboard testing (C crash, X crash-into, Space cross, H honk, S slide).
        private const bool DEBUG_INPUT = true;

        private const float SpriteForwardOffset = Mathf.Pi / 2f;
        private const float TurnControlDistance = 220f;
        private const float CurvePortion = 0.6f; // fraction of the cross spent curving vs. driving straight
        private const int ActiveZIndex = 100;     // draw a crossing/crashing car over queued cars and the map

        private const float DebugRadius = 256f;
        private const float DebugDuration = 1.0f;

        private static readonly string[] VariantPaths =
        {
            "res://assets/art/cars/Car1.png",
            "res://assets/art/cars/Car2.png",
            "res://assets/art/cars/Car3.png",
            "res://assets/art/cars/Car4.png",
            "res://assets/art/cars/Car5.png",
        };

        private MovementDirection _dir;
        private int _textureIndex = -1; // <0 until Initialize or _Ready picks one
        private bool _isCrossing;
        private bool _isSliding;
        private bool _hasCrashed;
        private bool _isHonking;

        private Vector2 _p0, _p1, _p2; // active Bezier path
        private Vector2 _exit;         // straight-leg endpoint after the curve
        private Tween _slideTween;     // current slide, kept so a new SlideTo can redirect it
        #endregion

        #region properties
        /// <summary>True while a queue slide is in progress; the car is not yet releasable.</summary>
        public bool IsSliding => _isSliding;
        #endregion

        #region events
        /// <summary>Emitted once a crossing or crash finishes and the node is about to free.</summary>
        [Signal]
        public delegate void CrossingFinishedEventHandler();

        /// <summary>Emitted when a waiting -> front slide completes.</summary>
        [Signal]
        public delegate void SlideFinishedEventHandler();
        #endregion

        #region public methods
        /// <summary>Configures the view before it enters the tree. <paramref name="textureSeed"/> picks
        /// the body sprite deterministically so a run looks identical on replay.</summary>
        public void Initialize(MovementDirection dir, int textureSeed)
        {
            _dir = dir;
            _textureIndex = (textureSeed % VariantPaths.Length + VariantPaths.Length) % VariantPaths.Length;
        }

        /// <summary>Slides straight to <paramref name="target"/> over <paramref name="duration"/>s, keeping
        /// heading. A call while already sliding redirects to the new target. Emits SlideFinished on arrival.</summary>
        public void SlideTo(Vector2 target, float duration)
        {
            if (_isCrossing || _hasCrashed) return;
            if (_slideTween != null && _slideTween.IsValid())
                _slideTween.Kill();

            _isSliding = true;
            _slideTween = CreateTween();
            _slideTween.TweenProperty(this, "position", target, duration);
            _slideTween.Finished += OnSlideFinished;
        }

        /// <summary>Curves from <paramref name="start"/> to <paramref name="laneEntry"/> (sublane mouth) over
        /// the first CurvePortion of <paramref name="duration"/>s, then drives straight to
        /// <paramref name="laneExit"/> (off-screen) for the rest, then frees. Faces the tangent throughout.</summary>
        public async void Cross(Vector2 start, Vector2 laneEntry, Vector2 laneExit, float duration)
        {
            if (_isCrossing || _hasCrashed) return;
            _isCrossing = true;
            ZIndex = ActiveZIndex;
            ZAsRelative = false;

            Vector2 exitDir = (laneExit - laneEntry).Normalized();

            _p0 = start;
            _p1 = laneEntry - exitDir * TurnControlDistance; // control behind the mouth, along the lane axis
            _p2 = laneEntry;
            _exit = laneExit;
            GlobalPosition = start;

            GetNodeOrNull<Sprite2D>(_dir.ToString())?.Hide();

            Tween tween = CreateTween();
            tween.TweenMethod(Callable.From<float>(UpdateCrossing), 0.0f, 1.0f, duration);
            await ToSignal(tween, Tween.SignalName.Finished);
            if (!IsInstanceValid(this)) return;

            EmitSignal(SignalName.CrossingFinished);
            QueueFree();
        }
        /// <summary>Slides into <paramref name="target"/> (the car ahead) over <paramref name="duration"/>s,
        /// then plays the crash and frees. The rear-end (impatience) crash.</summary>
        public async void CrashIntoRear(Vector2 target, float duration)
        {
            if (_hasCrashed) return;
            _hasCrashed = true;

            if (_slideTween != null && _slideTween.IsValid())
                _slideTween.Kill();

            Tween tween = CreateTween();
            tween.TweenProperty(this, "position", target, duration);
            await ToSignal(tween, Tween.SignalName.Finished);
            if (!IsInstanceValid(this)) return;

            await PlayCrashEffectAsync();
            if (!IsInstanceValid(this)) return;

            EmitSignal(SignalName.CrossingFinished);
            QueueFree();
        }
        
        /// <summary>Drives straight into <paramref name="point"/> (box centre) over
        /// <paramref name="driveDuration"/>s, plays the crash effect, then frees. Illegal-move/deadlock.</summary>
        public async void CrashInto(Vector2 point, float driveDuration)
        {
            if (_hasCrashed) return;
            _hasCrashed = true;
            ZIndex = ActiveZIndex;
            ZAsRelative = false;

            GetNodeOrNull<Sprite2D>(_dir.ToString())?.Hide();

            Tween tween = CreateTween();
            tween.TweenProperty(this, "global_position", point, driveDuration);
            await ToSignal(tween, Tween.SignalName.Finished);
            if (!IsInstanceValid(this)) return;

            await PlayCrashEffectAsync();
            if (!IsInstanceValid(this)) return;

            EmitSignal(SignalName.CrossingFinished);
            QueueFree();
        }

        /// <summary>Plays the crash effect in place, then frees. The rear-end (impatience) crash.</summary>
        public async void Crash()
        {
            if (_hasCrashed) return;
            _hasCrashed = true;

            await PlayCrashEffectAsync();
            if (!IsInstanceValid(this)) return;

            EmitSignal(SignalName.CrossingFinished);
            QueueFree();
        }

        /// <summary>Shows the anger mark for the duration of one honk sound.</summary>
        public async void Honk()
        {
            if (_isHonking) return;
            _isHonking = true;

            var angerMark = GetNode<Sprite2D>("HonkEffect/AngerMark");
            var sound = GetNode<AudioStreamPlayer2D>("HonkEffect/Sound");

            angerMark.Visible = true;
            sound.Play();
            await ToSignal(sound, AudioStreamPlayer2D.SignalName.Finished);
            if (!IsInstanceValid(this)) return;

            angerMark.Visible = false;
            _isHonking = false;
        }
        #endregion

        #region private methods
        private void OnSlideFinished()
        {
            _slideTween = null;
            _isSliding = false;
            EmitSignal(SignalName.SlideFinished);
        }

        private async Task PlayCrashEffectAsync()
        {
            foreach (Sprite2D spr in GetChildren().OfType<Sprite2D>())
                spr.Visible = false;

            var particles = GetNode<GpuParticles2D>("CrashEffect/Particles");
            var sound = GetNode<AudioStreamPlayer2D>("CrashEffect/Sound");

            particles.Emitting = true;
            sound.Play();
            await ToSignal(sound, AudioStreamPlayer2D.SignalName.Finished);
        }

        private void UpdateCrossing(float t)
        {
            if (t < CurvePortion)
            {
                float ct = t / CurvePortion;
                GlobalPosition = QuadBezier(_p0, _p1, _p2, ct);
                Vector2 tangent = QuadBezierTangent(_p0, _p1, _p2, ct);
                if (tangent.LengthSquared() > 0.0001f)
                    GlobalRotation = tangent.Angle() + SpriteForwardOffset;
            }
            else
            {
                float st = (t - CurvePortion) / (1f - CurvePortion);
                GlobalPosition = _p2.Lerp(_exit, st);
            }
        }

        private void DebugCross()
        {
            Vector2 entry = GlobalPosition;
            Vector2 turnPoint = entry + new Vector2(0f, DebugRadius);
            Vector2 exit = _dir switch
            {
                MovementDirection.Left => turnPoint + new Vector2(DebugRadius, 0f),
                MovementDirection.Right => turnPoint + new Vector2(-DebugRadius, 0f),
                _ => turnPoint + new Vector2(0f, DebugRadius),
            };
            Cross(entry, turnPoint, exit, DebugDuration);
        }

        private static Vector2 QuadBezier(Vector2 p0, Vector2 p1, Vector2 p2, float t)
        {
            float u = 1f - t;
            return u * u * p0 + 2f * u * t * p1 + t * t * p2;
        }

        private static Vector2 QuadBezierTangent(Vector2 p0, Vector2 p1, Vector2 p2, float t)
        {
            float u = 1f - t;
            return 2f * u * (p1 - p0) + 2f * t * (p2 - p1);
        }
        #endregion

        #region engine lifecycle
        public override void _Ready()
        {
            if (_textureIndex < 0) // dropped in editor for preview, no Initialize
            {
                _dir = (MovementDirection)GD.RandRange(0, 2);
                _textureIndex = GD.RandRange(0, VariantPaths.Length - 1);
            }

            GetNode<Sprite2D>("BodyVisual").Texture = GD.Load<Texture2D>(VariantPaths[_textureIndex]);
            GetNode<Sprite2D>(_dir.ToString()).Visible = true;
        }

        public override void _UnhandledKeyInput(InputEvent @event)
        {
            if (!DEBUG_INPUT) return;

            if (@event is InputEventKey key && key.Pressed && !key.Echo)
            {
                if (key.Keycode == Key.C) Crash();
                else if (key.Keycode == Key.X) CrashInto(GlobalPosition + new Vector2(0f, DebugRadius), DebugDuration);
                else if (key.Keycode == Key.Space) DebugCross();
                else if (key.Keycode == Key.H) Honk();
                else if (key.Keycode == Key.S) SlideTo(Position + new Vector2(0f, -DebugRadius), DebugDuration);
            }
        }
        #endregion
    }
}
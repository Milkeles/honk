/* Per-lane slot manager for one approach. The scene root is an Area2D (with a CollisionShape2D)
 * so the lane itself detects taps. Owns the front and waiting CarViews, places them at this lane's
 * marker anchors, slides the waiting car up on promotion, spawns/releases/crashes cars against its
 * two slots, and exposes this lane's box-facing entry/exit positions. Instanced four times in the
 * main scene, each rotated 90deg. Mirrors Core.Lane; holds no rules.
 *
 * Dependencies: Direction (Core), CarView, Godot
 * Author(s): H. Hristov (Milkeles)
 * Created: 05/06/2026 (dd/mm/yyyy)
 * Updated: 05/06/2026 (dd/mm/yyyy)
 * Last change: Root is Area2D (input on self); markers fetched via GetNode in _Ready
*/

using Godot;
using Core;

namespace Presentation
{
    public partial class LaneView : Area2D
    {
        #region fields
        [Export] public LaneOrigin Origin { get; set; }
        [Export] public PackedScene CarScene { get; set; }
        [Export] public float SlideDuration { get; set; } = 0.35f;
        private const float RearCrashDuration = 0.15f;
        private Marker2D _frontPos;
        private Marker2D _backPos;
        private Marker2D _spawnPos;
        private Marker2D _entryPos; // where a car crossing INTO this lane lands (box side)
        private Marker2D _exitPos;  // off-screen point where such a car is destroyed
        private CarView _settlingFront;

        private CarView _front;
        private CarView _waiting;
        private bool _inputEnabled = true;
        #endregion

        #region properties
        /// <summary>World point where a car crossing into this lane lands (Bezier end).</summary>
        public Vector2 EntryPoint => _entryPos.GlobalPosition;

        /// <summary>World point where a car leaving via this lane is destroyed (off-screen).</summary>
        public Vector2 ExitPoint => _exitPos.GlobalPosition;

        public bool HasFront => _front != null;
        public bool HasWaiting => _waiting != null;

        /// <summary>True when a front car exists and has finished sliding into place.</summary>
        public bool IsFrontReleasable => _front != null && !_front.IsSliding;
        #endregion

        #region events
        /// <summary>Emitted when this lane is tapped. Carries this lane's origin (as int).</summary>
        [Signal]
        public delegate void LaneTappedEventHandler(int origin);

        /// <summary>Emitted when the front car finishes sliding into the front slot. The controller relays it
        /// to the core so the car starts counting for conflict/deadlock checks.</summary>
        [Signal]
        public delegate void FrontSettledEventHandler(int origin);
        #endregion

        #region public methods
        /// <summary>Enables/disables tap detection (off during game over).</summary>
        public void SetInputEnabled(bool enabled)
        {
            _inputEnabled = enabled;
            InputPickable = enabled;
        }

        /// <summary>Spawns a car at the spawn point and slides it into the open slot (front if empty,
        /// else waiting). Mirrors Core.Lane.Add ordering.</summary>
        public void SpawnCar(MovementDirection dir, int textureSeed)
        {
            CarView car = CarScene.Instantiate<CarView>();
            car.Initialize(dir, textureSeed);
            AddChild(car);
            car.Position = _spawnPos.Position;
            car.Rotation = _frontPos.Rotation; // local; lane rotation orients it down-lane in world

            if (_front == null)
            {
                _front = car;
                SlideToFront(car);
            }
            else
            {
                _waiting = car;
                car.SlideTo(_backPos.Position, SlideDuration);
            }
        }

        /// <summary>Releases and returns the front car so the controller can drive its crossing.
        /// Promotes the waiting car into the front slot (sliding it up). Null if no front car.</summary>
        public CarView ReleaseFront()
        {
            if (_front == null) return null;

            CarView released = _front;
            _front = _waiting;
            _waiting = null;

            if (_front != null) SlideToFront(_front);
            return released;
        }


        /// <summary>Removes and returns the front car without promotion — for the illegal-move case,
        /// where the controller drives it into the box to crash. Null if no front car.</summary>
        public CarView TakeFront()
        {
            CarView f = _front;
            _front = null;
            return f;
        }

        /// <summary>The rear-end (impatience) crash: the waiting car slides into the front car, then both
        /// explode. Front explodes in place; waiting drives into it first.</summary>
        public void CrashQueue()
        {
            ClearSettling();

            if (_front != null && _waiting != null)
                _waiting.CrashIntoRear(_front.Position, RearCrashDuration);
            else
                _waiting?.Crash();

            _front?.Crash();
            _front = null;
            _waiting = null;
        }

        /// <summary>Immediately frees both slot cars with no animation. For revive/restart board reset.</summary>
        public void ClearCars()
        {
            ClearSettling();
            _front?.QueueFree();
            _waiting?.QueueFree();
            _front = null;
            _waiting = null;
        }

        /// <summary>Plays the honk effect on the waiting car (the impatient one), if present.</summary>
        public void HonkWaiting() => _waiting?.Honk();
        #endregion

        #region private methods
        private void OnLaneInput(Node viewport, InputEvent @event, long shapeIdx)
        {
            if (!_inputEnabled) return;

            bool tapped =
                (@event is InputEventScreenTouch touch && touch.Pressed) ||
                (@event is InputEventMouseButton mb && mb.Pressed && mb.ButtonIndex == MouseButton.Left);

            if (tapped)
                EmitSignal(SignalName.LaneTapped, (int)Origin);
        }

        private void SlideToFront(CarView car)
        {
            _settlingFront = car;
            car.SlideFinished += OnFrontSettled;
            car.SlideTo(_frontPos.Position, SlideDuration);
        }

        private void OnFrontSettled()
        {
            if (_settlingFront == null) return;

            _settlingFront.SlideFinished -= OnFrontSettled;
            bool stillFront = _front == _settlingFront;
            _settlingFront = null;

            if (stillFront)
                EmitSignal(SignalName.FrontSettled, (int)Origin);
        }
        private void ClearSettling()
        {
            if (_settlingFront == null) return;
            _settlingFront.SlideFinished -= OnFrontSettled;
            _settlingFront = null;
        }
        #endregion

        #region engine lifecycle
        public override void _Ready()
        {
            _frontPos = GetNode<Marker2D>("FrontPos");
            _backPos = GetNode<Marker2D>("BackPos");
            _spawnPos = GetNode<Marker2D>("SpawnPos");
            _entryPos = GetNode<Marker2D>("EntryPos");
            _exitPos = GetNode<Marker2D>("ExitPos");

            InputPickable = _inputEnabled;
            InputEvent += OnLaneInput;
        }
        #endregion
    }
}
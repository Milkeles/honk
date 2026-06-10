/* This class defines the data for a single car, the direction it intends to take,
 * its patience timer, and its honk count. On the screen counterpart is CarView.
 *
 * Dependencies: Direction, Timer
 * Author(s): H. Hristov (Milkeles)
 * Created: 04/06/2026 (dd/mm/yyyy)
 * Updated: N/A
 * Last change: N/A
*/

using System;

namespace Core {
    public class Car : IDisposable
    {
        #region constants
        private const int MAX_HONKS = 3;
        #endregion

        #region fields
        private LaneOrigin _laneOrigin;
        private MovementDirection _movementDirection;
        private int _honkCount;
        private float _honkCooldown;
        private bool _disposed;
        private readonly GameTimer _timer;
        private Action _onTimerFinished;
        #endregion

        #region constructors
        /// <param name="honkCooldown">Seconds between honks.</param>
        /// <param name="laneOrigin">Defaults to first <see cref="LaneOrigin"/> member — ensure 0 is a valid state.</param>
        /// <param name="movementDirection">Defaults to first <see cref="MovementDirection"/> member — ensure 0 is a valid state.</param>
        public Car(float honkCooldown = 5.0f, LaneOrigin laneOrigin = default, MovementDirection movementDirection = default)
        {
            HonkCooldown = honkCooldown;
            LaneOrigin = laneOrigin;
            MovementDirection = movementDirection;
            _timer = new GameTimer(_honkCooldown);
            SubscribeTimer();
        }
        #endregion

        #region properties
        /// <summary>Throws if value is not a defined <see cref="LaneOrigin"/> member.</summary>
        public LaneOrigin LaneOrigin
        {
            get => _laneOrigin;
            protected set
            {
                if (!Enum.IsDefined(typeof(LaneOrigin), value))
                    throw new System.ComponentModel.InvalidEnumArgumentException(nameof(value));
                _laneOrigin = value;
            }
        }

        /// <summary>Throws if value is not a defined <see cref="MovementDirection"/> member.</summary>
        public MovementDirection MovementDirection
        {
            get => _movementDirection;
            protected set
            {
                if (!Enum.IsDefined(typeof(MovementDirection), value))
                    throw new System.ComponentModel.InvalidEnumArgumentException(nameof(value));
                _movementDirection = value;
            }
        }

        /// <summary>Capped at <c>MAX_HONKS</c>. Incremented automatically by the patience timer.</summary>
        public int HonkCount
        {
            get => _honkCount;
            protected set
            {
                if (value > MAX_HONKS)
                    throw new ArgumentOutOfRangeException(nameof(value), $"Honk count should not exceed {MAX_HONKS}");
                _honkCount = value;
            }
        }

        /// <summary>Seconds between honks. Minimum 0.3s. Does not affect a timer already running.</summary>
        public float HonkCooldown
        {
            get => _honkCooldown;
            set
            {
                if (value < 0.3f)
                    throw new ArgumentOutOfRangeException(nameof(value), "Honk cooldown must be at least 0.3s");
                _honkCooldown = value;
            }
        }
        #endregion

        #region events
        public event Action<Car> Honked;
        
        /// <summary>Fires once after the final honk, when the car's patience is exhausted.</summary>
        public event Action<Car> PatienceExpired;
        #endregion

        #region public methods

        /// <summary>Advances the car's patience timer. Called by Lane each frame.</summary>
        public void Tick(float delta) => _timer.Tick(delta);

        public void Dispose()
        {
            if (_disposed) return;
            _timer.OnFinished -= _onTimerFinished;
            Honked = null;
            PatienceExpired = null;
            _disposed = true;
        }

        #endregion

        #region private methods
        private void SubscribeTimer()
        {
            _onTimerFinished = () =>
            {
                ++HonkCount;
                Honked?.Invoke(this);

                if (HonkCount < MAX_HONKS)
                {
                    _timer.Reset();
                    SubscribeTimer();
                }
                else
                {
                    PatienceExpired?.Invoke(this);
                }
            };
            _timer.OnFinished += _onTimerFinished;
        }
        #endregion

        // Destructors, engine lifecycle methods, etc.
        #region other
        ~Car() => Dispose();
        #endregion
    }
}

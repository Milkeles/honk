/* Reusable helper timer for core-layer systems (no Node dependency).
 * Safe to use in headless, non-node contexts — no Godot lifecycle required.
 * 
 * Dependencies: None.
 * Author: H. Hristov (Milkeles)
 * Created: 04/06/2026 (dd/mm/yyyy)
 * Updated: N/A
 * Last change: N/A
 */

namespace Core {
    public class GameTimer
    {
        // Plain field instead of auto-property — avoids getter overhead in the per-frame Tick hot path.
        public float RemainingTime;

        // Stored so Reset() can restore the timer without requiring the caller to re-supply it.
        private readonly float _duration;

        // Prevents the callback from firing more than once per cycle if Tick overshoots zero.
        private bool _fired;

        /// <summary>
        /// Invoked once when the timer reaches zero. Assign or chain listeners before starting.
        /// Automatically cleared on Reset() — re-subscribe if needed after resetting.
        /// </summary>
        public event System.Action OnFinished;

        /// <summary>
        /// Returns true once the timer has counted down to zero or below.
        /// </summary>
        public bool IsFinished => RemainingTime <= 0f;

        /// <summary>
        /// Initialises the timer and sets the countdown duration.
        /// </summary>
        /// <param name="duration">Total countdown time in seconds.</param>
        public GameTimer(float duration)
        {
            _duration = duration;
            RemainingTime = duration;
        }

        /// <summary>
        /// Advances the timer by <paramref name="delta"/> seconds.
        /// Fires <see cref="OnFinished"/> once when the timer expires.
        /// Should be called once per physics or process frame.
        /// </summary>
        /// <param name="delta">Elapsed time since the last frame, in seconds.</param>
        public void Tick(float delta)
        {
            // Skip once finished — avoids redundant work and double-firing the event.
            if (_fired) return;

            RemainingTime -= delta;

            if (RemainingTime <= 0f)
            {
                _fired = true;
                OnFinished?.Invoke();
            }
        }

        /// <summary>
        /// Restarts the timer from its original duration and clears the fired flag.
        /// Note: subscribers to OnFinished are cleared — re-subscribe after resetting if needed.
        /// </summary>
        public void Reset()
        {
            RemainingTime = _duration;
            _fired = false;
            OnFinished = null; // Clear to avoid stale listeners accumulating across reuses.
        }

        // /// <summary>
        // /// Restarts the timer with a new duration, replacing the stored one.
        // /// Note: subscribers to OnFinished are cleared — re-subscribe after resetting if needed.
        // /// </summary>
        // public void Reset(float newDuration)
        // {
        //     _duration = newDuration; // requires removing readonly
        //     RemainingTime = newDuration;
        //     _fired = false;
        //     OnFinished = null;
        // }

        /// <summary>
        /// Set listener to null.
        /// </summary>
        public void ClearListeners()
        {
            OnFinished = null;
        }
    }
}
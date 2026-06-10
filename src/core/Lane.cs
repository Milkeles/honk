/* This class defines a single approach to the intersection. It holds the
 * releasable front car and the single waiting car behind it, advances the
 * waiting car's patience, promotes the waiting car when the front is released,
 * and resolves the rear-end crash when the waiting car's patience runs out.
 * On-screen counterpart is LaneView.
 *
 * Dependencies: Direction, Car
 * Author(s): Alex D. (Emperor Chichi), H. Hristov (Milkeles)
 * Created: 06/04/26 (mm/dd/yy)
 * Updated: 06/05/26 (mm/dd/yy)
*/

using System;

namespace Core {
    public class Lane
    {
        #region fields
        private readonly LaneOrigin _origin;
        private Car _front;
        private Car _waiting;
        #endregion

        #region constructors
        public Lane(LaneOrigin origin)
        {
            _origin = origin;
        }
        #endregion

        #region properties
        public LaneOrigin Origin => _origin;

        public Car Front => _front;

        public Car Waiting => _waiting;

        public bool IsEmpty => _front == null;

        public bool IsFull => _waiting != null;
        #endregion

        #region events
        /// <summary>Fires whenever the waiting car honks. Read Waiting.HonkCount for the count.</summary>
        public event Action<Lane> Honked;

        /// <summary>Fires when the waiting car rear-ends the front car. Both cars are gone;
        /// a life is lost. Carries (lane, front victim, rammer).</summary>
        public event Action<Lane, Car, Car> Crashed;
        #endregion

        #region public methods
        /// <summary>
        /// Adds a newly arrived car to the front slot if empty, otherwise the waiting slot.
        /// Only the waiting car runs a live patience timer.
        /// </summary>
        public void Add(Car car)
        {
            if (_front == null)
                _front = car;
            else if (_waiting == null)
                AttachWaiting(car);
        }

        /// <summary>Advances the lane. Only the waiting car's patience runs down.</summary>
        public void Tick(float delta) => _waiting?.Tick(delta);

        /// <summary>
        /// Removes and returns the front car, then promotes the waiting car to front.
        /// Returns null if the lane is empty. Does not dispose the car — its lifetime
        /// passes to the caller.
        /// </summary>
        public Car ReleaseFront()
        {
            Car released = _front;
            if (released == null) return null;

            DetachWaiting();
            _front = _waiting;
            _waiting = null;
            return released;
        }

        /// <summary>Clears the lane and disposes any cars it holds.</summary>
        public void Clear()
        {
            DetachWaiting();
            _waiting?.Dispose();
            _front?.Dispose();
            _waiting = null;
            _front = null;
        }
        #endregion

        #region private methods
        private void AttachWaiting(Car car)
        {
            _waiting = car;
            car.Honked += OnWaitingHonked;
            car.PatienceExpired += OnWaitingExpired;
        }

        private void DetachWaiting()
        {
            if (_waiting == null) return;
            _waiting.Honked -= OnWaitingHonked;
            _waiting.PatienceExpired -= OnWaitingExpired;
        }

        private void OnWaitingHonked(Car _) => Honked?.Invoke(this);

        private void OnWaitingExpired(Car rammer)
        {
            DetachWaiting();
            Car victim = _front;
            _front = null;
            _waiting = null;

            Crashed?.Invoke(this, victim, rammer);
            victim?.Dispose();
            rammer.Dispose();
        }
        #endregion
    }
}
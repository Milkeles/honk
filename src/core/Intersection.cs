/* The shared crossing area. Owns the four lanes, releases cars across the box one
 * at a time, turns an illegal release into a crash with the right-of-way car,
 * surfaces lane honks and rear-end (impatience) crashes, detects deadlock, and
 * resolves a deadlock by crashing a random pair. Pure logic, no rendering.
 *
 * Dependencies: Lane, Car, Direction
 * Author(s): H. Hristov (Milkeles)
 * Created: 04/06/2026 (dd/mm/yyyy)
 * Updated: 05/06/2026 (dd/mm/yyyy)
 * Last change: Added Tick and lane-event surfacing (CarHonked, impatience crashes); random lane pick
*/
using System;
using System.Collections.Generic;

namespace Core {
    public class Intersection : IDisposable
    {
        #region fields
        private readonly Lane[] _lanes;
        private Car _crossing;
        private bool _crashing;
        private bool _disposed;
        #endregion

        #region constructors
        public Intersection()
        {
            _lanes = new Lane[4]
            {
                new Lane(LaneOrigin.North),
                new Lane(LaneOrigin.West),
                new Lane(LaneOrigin.South),
                new Lane(LaneOrigin.East),
            };

            foreach (Lane lane in _lanes)
            {
                lane.Honked += OnLaneHonked;
                lane.Crashed += OnLaneCrashed;
            }
        }
        #endregion

        #region properties
        public bool IsBusy => _crossing != null || _crashing;

        public Car Crossing => _crossing;
        #endregion

        #region events
        /// <summary>Fires when two cars crash, by illegal move, deadlock, or impatience. Both cars
        /// are being removed; the subscriber (Game) deducts one life. Cause is for visuals/audio.</summary>
        public event Action<Car, Car, CrashCause> Crashed;

        /// <summary>Fires when a waiting car honks. Carries the honking car.</summary>
        public event Action<Car> CarHonked;
        #endregion

        #region public methods
        /// <summary>Advances every lane's queue patience. Runs even while the box is busy.</summary>
        public void Tick(float delta)
        {
            foreach (Lane lane in _lanes) lane.Tick(delta);
        }

        /// <summary>
        /// Releases the front car of the given lane. A legal move begins a crossing and makes
        /// the box busy until <see cref="FinishAnimation"/>. An illegal move sends the chosen
        /// car in to crash the conflicting right-of-way car. No-op while busy or empty.
        /// Returns true only on a legal crossing.
        /// </summary>
        public bool TryReleaseLane(LaneOrigin origin)
        {
            if (IsBusy) return false;

            Lane lane = _lanes[(int)origin];
            if (lane.Front == null) return false;

            Car mover = lane.ReleaseFront();

            if (TryGetConflict(mover, out Car other))
            {
                PullFront(other);
                Crash(mover, other, CrashCause.IllegalMove);
                return false;
            }

            _crossing = mover;
            return true;
        }

        /// <summary>True when at least one car is present and none of them has a legal move.</summary>
        public bool IsDeadlocked()
        {
            if (IsBusy) return false;

            bool anyCar = false;
            foreach (Lane lane in _lanes)
            {
                if (lane.Front == null) continue;
                anyCar = true;
                if (!TryGetConflict(lane.Front, out _)) return false;
            }
            return anyCar;
        }

        /// <summary>Resolves a deadlock by crashing a random pair of front cars.</summary>
        public void CrashRandomPair(Random rng)
        {
            List<Lane> occupied = new List<Lane>();
            foreach (Lane lane in _lanes)
                if (lane.Front != null) occupied.Add(lane);

            if (occupied.Count < 2) return;

            int i = rng.Next(occupied.Count);
            int j = rng.Next(occupied.Count - 1);
            if (j >= i) j++;

            Crash(occupied[i].ReleaseFront(), occupied[j].ReleaseFront(), CrashCause.Deadlock);
        }

        /// <summary>Clears the box once a crossing or box-crash animation has finished. Driven by
        /// the CarView animation-finished event via Game.</summary>
        public void FinishAnimation()
        {
            _crossing?.Dispose();
            _crossing = null;
            _crashing = false;
        }

        /// <summary>Picks a uniformly random lane that has room for another car.</summary>
        public bool TryGetAvailableLane(Random rng, out LaneOrigin origin)
        {
            int count = 0;
            for (int i = 0; i < _lanes.Length; i++)
                if (!_lanes[i].IsFull) count++;

            if (count == 0) { origin = default; return false; }

            int pick = rng.Next(count);
            for (int i = 0; i < _lanes.Length; i++)
            {
                if (_lanes[i].IsFull) continue;
                if (pick-- == 0) { origin = (LaneOrigin)i; return true; }
            }

            origin = default; // unreachable
            return false;
        }

        public bool TryAddCar(Car car)
        {
            if (car == null) throw new ArgumentNullException(nameof(car));

            Lane lane = _lanes[(int)car.LaneOrigin];
            if (lane.IsFull) return false;
            lane.Add(car);
            return true;
        }

        public void Dispose()
        {
            if (_disposed) return;
            foreach (Lane lane in _lanes) lane.Clear();
            _crossing?.Dispose();
            _crossing = null;
            Crashed = null;
            CarHonked = null;
            _disposed = true;
        }
        #endregion

        #region private methods
        private void OnLaneHonked(Lane lane) => CarHonked?.Invoke(lane.Waiting);

        private void OnLaneCrashed(Lane lane, Car victim, Car rammer)
            => Crashed?.Invoke(victim, rammer, CrashCause.Impatience);

        private void Crash(Car a, Car b, CrashCause cause)
        {
            _crashing = true;
            Crashed?.Invoke(a, b, cause);
            a?.Dispose();
            b?.Dispose();
        }

        private void PullFront(Car car)
        {
            if (car == null) return;
            _lanes[(int)car.LaneOrigin].ReleaseFront();
        }

        private bool TryGetConflict(Car current, out Car other)
        {
            other = null;
            if (current == null) return false;

            int origin = (int)current.LaneOrigin;
            Car right = _lanes[(origin + 1) % 4].Front;
            Car opposite = _lanes[(origin + 2) % 4].Front;

            switch (current.MovementDirection)
            {
                case MovementDirection.Right:
                    return false;

                case MovementDirection.Straight:
                    if (right != null) { other = right; return true; }
                    return false;

                case MovementDirection.Left:
                    if (opposite != null && opposite.MovementDirection != MovementDirection.Left)
                    { other = opposite; return true; }
                    if (right != null && right.MovementDirection != MovementDirection.Right)
                    { other = right; return true; }
                    return false;

                default:
                    return false;
            }
        }
        #endregion

        #region other
        ~Intersection() => Dispose();
        #endregion
    }
}
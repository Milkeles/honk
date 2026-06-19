/* The shared crossing area. Owns the four lanes, releases cars across the box one at a time, turns an
 * illegal release into a crash with the right-of-way car, surfaces lane honks and rear-end (impatience)
 * crashes, detects deadlock, and resolves it by crashing a random pair. Cars are only considered for
 * conflict/deadlock once their front slide has settled (ReadyFront). Pure logic, no rendering.
 *
 * Dependencies: Lane, Car, Direction
 * Author(s): H. Hristov (Milkeles)
 * Created: 04/06/2026 (dd/mm/yyyy)
 * Updated: 19/06/2026 (dd/mm/yyyy)
 * Last change: Conflict/deadlock use ReadyFront; shared right-of-way predicate; empty-board + settle API
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

        /// <summary>True when no lane holds any car.</summary>
        public bool AllLanesEmpty
        {
            get
            {
                foreach (Lane lane in _lanes)
                    if (!lane.IsEmpty) return false;
                return true;
            }
        }
        #endregion

        #region events
        public event Action<Car, Car, CrashCause> Crashed;
        public event Action<Car> CarHonked;
        #endregion

        #region public methods
        /// <summary>Advances every lane's queue patience. Runs even while the box is busy.</summary>
        public void Tick(float delta)
        {
            foreach (Lane lane in _lanes) lane.Tick(delta);
        }

        /// <summary>Marks a lane's front car settled once its slide-to-front has finished.</summary>
        public void MarkFrontReady(LaneOrigin origin) => _lanes[(int)origin].MarkFrontReady();

        /// <summary>Releases the front car of <paramref name="origin"/>. Legal -> begins a crossing (box
        /// busy until FinishAnimation). Illegal -> the chosen car crashes the conflicting right-of-way car.
        /// No-op while busy, empty, or the front is still arriving. True only on a legal crossing.</summary>
        public bool TryReleaseLane(LaneOrigin origin)
        {
            if (IsBusy) return false;

            Lane lane = _lanes[(int)origin];
            if (!lane.IsFrontReady) return false;

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

        /// <summary>True when at least one settled car is present and none has a legal move.</summary>
        public bool IsDeadlocked()
        {
            if (IsBusy) return false;
            return IsConfigDeadlocked(BuildReadyDirs());
        }

        /// <summary>Pure right-of-way deadlock test over a per-lane direction array (index = LaneOrigin,
        /// null = no car). Used live and to check that a fresh burst is solvable.</summary>
        public static bool IsConfigDeadlocked(MovementDirection?[] dirs)
        {
            bool any = false;
            for (int i = 0; i < 4; i++)
            {
                if (dirs[i] == null) continue;
                any = true;
                if (!HasConflict(i, dirs, out _)) return false;
            }
            return any;
        }

        /// <summary>Resolves a deadlock by crashing a random pair of settled front cars.</summary>
        public void CrashRandomPair(Random rng)
        {
            List<Lane> occupied = new List<Lane>();
            foreach (Lane lane in _lanes)
                if (lane.ReadyFront != null) occupied.Add(lane);

            if (occupied.Count < 2) return;

            int i = rng.Next(occupied.Count);
            int j = rng.Next(occupied.Count - 1);
            if (j >= i) j++;

            Crash(occupied[i].ReleaseFront(), occupied[j].ReleaseFront(), CrashCause.Deadlock);
        }

        /// <summary>Clears the box once a crossing or box-crash animation has finished.</summary>
        public void FinishAnimation()
        {
            _crossing?.Dispose();
            _crossing = null;
            _crashing = false;
        }

        /// <summary>Picks a uniformly random lane with room for another car.</summary>
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

            origin = default;
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

        // Current settled-front directions (index = LaneOrigin, null = none/arriving).
        private MovementDirection?[] BuildReadyDirs()
        {
            var dirs = new MovementDirection?[4];
            for (int i = 0; i < 4; i++)
                dirs[i] = _lanes[i].ReadyFront?.MovementDirection;
            return dirs;
        }

        private bool TryGetConflict(Car current, out Car other)
        {
            other = null;
            if (current == null) return false;

            int origin = (int)current.LaneOrigin;
            int rightLane = (origin + 1) % 4;
            int oppLane = (origin + 2) % 4;
            Car right = _lanes[rightLane].ReadyFront;
            Car opposite = _lanes[oppLane].ReadyFront;

            var dirs = new MovementDirection?[4];
            dirs[origin] = current.MovementDirection;
            dirs[rightLane] = right?.MovementDirection;
            dirs[oppLane] = opposite?.MovementDirection;

            if (HasConflict(origin, dirs, out int otherLane))
            {
                other = otherLane == rightLane ? right : opposite;
                return true;
            }
            return false;
        }

        // The right-of-way rule as a pure function. Right: always legal. Straight: yields to the right
        // lane. Left: yields to oncoming unless it is also turning left, and to the right lane unless it
        // is turning right.
        private static bool HasConflict(int origin, MovementDirection?[] dirs, out int otherLane)
        {
            otherLane = -1;
            MovementDirection? self = dirs[origin];
            if (self == null) return false;

            int rightLane = (origin + 1) % 4;
            int oppLane = (origin + 2) % 4;
            MovementDirection? right = dirs[rightLane];
            MovementDirection? opp = dirs[oppLane];

            switch (self.Value)
            {
                case MovementDirection.Right:
                    return false;

                case MovementDirection.Straight:
                    if (right != null) { otherLane = rightLane; return true; }
                    return false;

                case MovementDirection.Left:
                    if (opp != null && opp.Value != MovementDirection.Left) { otherLane = oppLane; return true; }
                    if (right != null && right.Value != MovementDirection.Right) { otherLane = rightLane; return true; }
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
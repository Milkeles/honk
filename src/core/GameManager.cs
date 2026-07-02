using System;

namespace Core {
    public class GameManager : IDisposable
    {
        #region tunables
        private const int BurstMin = 2;
        private const int BurstMax = 3;
        private const float SpawnIntervalStart = 2.5f;
        private const float SpawnIntervalMin = 0.8f;

        private const float PatienceStart = 4.0f;   // seconds per honk; total patience is 3x this
        private const float PatienceMin = 1.5f;

        private const float RampDuration = 90f;     // seconds of play to reach max difficulty
        #endregion

        #region fields
        private readonly Intersection _intersection;
        private readonly Random _rng;
        private readonly Upgrades _upgrades;

        private readonly int _maxLives;
        private readonly int _scorePerCar;
        private readonly float _regenPerCar;

        private int _lives;
        private int _score;
        private float _regenProgress;   // accumulates toward the next restored heart
        private float _elapsed;
        private float _timeUntilSpawn;
        private bool _isGameOver;
        private bool _disposed;
        #endregion

        #region constructors
        public GameManager(int seed, Upgrades upgrades)
        {
            _rng = new Random(seed);
            _upgrades = upgrades;

            _maxLives = upgrades.MaxHearts;
            _scorePerCar = upgrades.ScorePerCar;
            _regenPerCar = upgrades.RegenPerCar;

            _lives = _maxLives;
            _timeUntilSpawn = SpawnIntervalStart;

            _intersection = new Intersection();
            _intersection.Crashed += OnCrashed;
            _intersection.CarHonked += OnCarHonked;
        }
        #endregion

        #region properties
        public int Lives => _lives;
        public int Score => _score;
        public bool IsGameOver => _isGameOver;
        public int MaxLives => _maxLives;
        #endregion

        #region events
        /// <summary>A new car arrived in a lane's queue. Read car.LaneOrigin to place it.</summary>
        public event Action<Car> CarEntered;

        /// <summary>A car began legally crossing the box. Animate it through; on finish call
        /// <see cref="NotifyAnimationFinished"/>.</summary>
        public event Action<Car> CarCrossing;

        /// <summary>A waiting car honked. Read car.HonkCount for the count.</summary>
        public event Action<Car> CarHonked;

        /// <summary>Two cars crashed; a life has already been deducted. For box crashes
        /// (IllegalMove, Deadlock) call <see cref="NotifyAnimationFinished"/> when the crash
        /// animation ends; an Impatience crash does not occupy the box.</summary>
        public event Action<Car, Car, CrashCause> Crashed;

        /// <summary>The board gridlocked; a random pair is about to crash to break it.</summary>
        public event Action Deadlock;

        /// <summary>A life was lost. Carries the remaining count.</summary>
        public event Action<int> LifeLost;

        /// <summary>Lives reached zero. Carries the final score.</summary>
        public event Action<int> GameOver;
        #endregion

        #region public methods
        /// <summary>Advances the simulation: queue patience, spawning, and gridlock resolution.
        /// No-op once the game is over.</summary>
        public void Tick(float delta)
        {
            if (_isGameOver) return;

            _elapsed += delta;
            _intersection.Tick(delta);
            if (_isGameOver) return;

            UpdateSpawning(delta);

            if (_intersection.IsDeadlocked())
            {
                Deadlock?.Invoke();
                _intersection.CrashRandomPair(_rng);
            }
        }

        /// <summary>Player command: release the front car of the given lane. A legal release scores
        /// and starts a crossing; an illegal one crashes (surfaced via Crashed).</summary>
        public void ReleaseLane(LaneOrigin origin)
        {
            if (_isGameOver) return;

            if (_intersection.TryReleaseLane(origin))
            {
                _score += _scorePerCar;
                AccumulateRegen();
                CarCrossing?.Invoke(_intersection.Crossing);
            }
        }

        /// <summary>Frees the box once a crossing or box-crash animation has finished. Driven by
        /// the CarView animation-finished event.</summary>
        public void NotifyAnimationFinished() => _intersection.FinishAnimation();

        /// <summary>Marks a lane's front car settled (slide finished), so it counts for conflict/deadlock.
        /// Driven by the view's slide-finished signal.</summary>
        public void NotifyFrontSettled(LaneOrigin origin) => _intersection.MarkFrontReady(origin);

        /// <summary>Clears the board and restores lives, resuming a lost run. Call on player revive.</summary>
        public void Revive(int lives)
        {
            _intersection.ResetBoard();
            _lives = lives;
            _regenProgress = 0f;
            _isGameOver = false;
            _timeUntilSpawn = CurrentSpawnInterval();
        }

        /// <summary>
        /// Disposes the GameManager, unsubscribing from events and releasing resources. After calling Dispose, the GameManager should not be used.
        /// </summary>
        public void Dispose()
        {
            if (_disposed) return;
            _intersection.Crashed -= OnCrashed;
            _intersection.CarHonked -= OnCarHonked;
            _intersection.Dispose();

            CarEntered = null;
            CarCrossing = null;
            CarHonked = null;
            Crashed = null;
            Deadlock = null;
            LifeLost = null;
            GameOver = null;
            _disposed = true;
        }
        #endregion

        #region private methods
        // Health regen: accrue per correct car; each whole point restores one heart (capped at max).
        private void AccumulateRegen()
        {
            if (_regenPerCar <= 0f || _lives >= _maxLives) return;

            _regenProgress += _regenPerCar;
            while (_regenProgress >= 1f && _lives < _maxLives)
            {
                _regenProgress -= 1f;
                _lives++;
                LifeLost?.Invoke(_lives); // reused as a "lives changed" signal
            }
        }

        // private void UpdateSpawning(float delta)
        // {
        //     _timeUntilSpawn -= delta;
        //     if (_timeUntilSpawn > 0f) return;

        //     _timeUntilSpawn = CurrentSpawnInterval();

        //     if (!_intersection.TryGetAvailableLane(_rng, out LaneOrigin origin)) return;

        //     Car car = new Car(CurrentPatience(), origin, RandomDirection());
        //     _intersection.TryAddCar(car);
        //     CarEntered?.Invoke(car);
        // }
        private void UpdateSpawning(float delta)
        {
            // Never leave the board empty: a fresh burst forces a real decision instead of trivial fast-tapping.
            if (_intersection.AllLanesEmpty && !_intersection.IsBusy)
            {
                SpawnBurst();
                _timeUntilSpawn = CurrentSpawnInterval();
                return;
            }

            _timeUntilSpawn -= delta;
            if (_timeUntilSpawn > 0f) return;

            _timeUntilSpawn = CurrentSpawnInterval();
            SpawnSingle();
        }

        /* 2-3 cars into distinct (empty) lanes at once, checked so the set isn't an unavoidable deadlock —
         * the player can't act until they all settle, so the board must be solvable on arrival. */
        private void SpawnBurst()
        {
            int count = _rng.Next(BurstMin, BurstMax + 1);

            int[] lanes = { 0, 1, 2, 3 };
            Shuffle(lanes);

            var dirs = new MovementDirection?[4];
            for (int i = 0; i < count; i++)
                dirs[lanes[i]] = RandomDirection();

            if (Intersection.IsConfigDeadlocked(dirs))
                dirs[lanes[0]] = MovementDirection.Right; // a right turn is always legal -> guarantees solvable

            for (int i = 0; i < count; i++)
                SpawnCarInLane((LaneOrigin)lanes[i], dirs[lanes[i]].Value);
        }

        private void SpawnSingle()
        {
            if (!_intersection.TryGetAvailableLane(_rng, out LaneOrigin origin)) return;
            SpawnCarInLane(origin, RandomDirection());
        }

        private void SpawnCarInLane(LaneOrigin origin, MovementDirection dir)
        {
            Car car = new Car(CurrentPatience(), origin, dir);
            _intersection.TryAddCar(car);
            CarEntered?.Invoke(car);
        }

        private void Shuffle(int[] array)
        {
            for (int i = array.Length - 1; i > 0; i--)
            {
                int j = _rng.Next(i + 1);
                (array[i], array[j]) = (array[j], array[i]);
            }
        }

        private void OnCarHonked(Car car) => CarHonked?.Invoke(car);

        private void OnCrashed(Car a, Car b, CrashCause cause)
        {
            _lives--;
            _regenProgress = 0f; // any mistake resets regen progress
            Crashed?.Invoke(a, b, cause);
            LifeLost?.Invoke(_lives);

            if (_lives <= 0 && !_isGameOver)
            {
                _isGameOver = true;
                GameOver?.Invoke(_score);
            }
        }

        private MovementDirection RandomDirection() => (MovementDirection)_rng.Next(3);
        private float CurrentSpawnInterval() => Ramp(SpawnIntervalStart, SpawnIntervalMin);
        private float CurrentPatience() => Ramp(PatienceStart, PatienceMin);
        
        // Linear ramp from start to end across RampDuration, then held at end.
        private float Ramp(float start, float end)
        {
            float t = Math.Min(_elapsed / RampDuration, 1f);
            return start + (end - start) * t;
        }
        #endregion

        #region other
        ~GameManager() => Dispose();
        #endregion
    }
}
/* Sim root and the core's public surface. Owns the intersection (and through it the
 * four lanes), the player's lives and score, and the time-based difficulty ramp.
 * Exposes the inbound commands (Tick, ReleaseLane, NotifyAnimationFinished) and raises
 * the outbound events the presentation layer reacts to. Spawns arrivals from a seeded
 * RNG. Knows nothing about Godot.
 *
 * Dependencies: Intersection, Lane, Car, Direction
 * Author(s): H. Hristov (Milkeles)
 * Created: 05/06/2026 (dd/mm/yyyy)
 * Updated: N/A
 * Last change: N/A
*/
using System;

public class GameManager : IDisposable
{
    #region tunables
    private const int StartLives = 3;

    private const float SpawnIntervalStart = 2.5f;
    private const float SpawnIntervalMin = 0.8f;

    private const float PatienceStart = 4.0f;   // seconds per honk; total patience is 3x this
    private const float PatienceMin = 1.5f;

    private const float RampDuration = 90f;      // seconds of play to reach max difficulty
    #endregion

    #region fields
    private readonly Intersection _intersection;
    private readonly Random _rng;

    private int _lives;
    private int _score;
    private float _elapsed;
    private float _timeUntilSpawn;
    private bool _isGameOver;
    private bool _disposed;
    #endregion

    #region constructors
    public GameManager(int seed)
    {
        _rng = new Random(seed);
        _lives = StartLives;
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
        if (_isGameOver) return; // an impatience crash during the tick may have ended the game

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
            _score++;
            CarCrossing?.Invoke(_intersection.Crossing);
        }
    }

    /// <summary>Frees the box once a crossing or box-crash animation has finished. Driven by
    /// the CarView animation-finished event.</summary>
    public void NotifyAnimationFinished() => _intersection.FinishAnimation();

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
    private void UpdateSpawning(float delta)
    {
        _timeUntilSpawn -= delta;
        if (_timeUntilSpawn > 0f) return;

        _timeUntilSpawn = CurrentSpawnInterval();

        if (!_intersection.TryGetAvailableLane(_rng, out LaneOrigin origin)) return;

        Car car = new Car(CurrentPatience(), origin, RandomDirection());
        _intersection.TryAddCar(car);
        CarEntered?.Invoke(car);
    }

    private void OnCarHonked(Car car) => CarHonked?.Invoke(car);

    private void OnCrashed(Car a, Car b, CrashCause cause)
    {
        _lives--;
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
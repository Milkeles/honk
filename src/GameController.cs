/* The bridge between the core simulation and the Godot scene, attached to the main scene root. Owns the
 * seeded GameManager and finds the four LaneViews by name, pumps GameManager.Tick each frame, turns lane
 * taps into ReleaseLane commands, and translates core events into spawn / cross / crash / honk calls.
 * Resolves a crossing's destination lane via (origin + offset) % 4. Frees the box once all box-occupying
 * animations finish. Intersection and Lanes are private to GameManager — this talks only to that facade.
 *
 * Dependencies: GameManager (Core), Direction (Core), Car (Core), LaneView, CarView, Godot
 * Author(s): H. Hristov (Milkeles)
 * Created: 05/06/2026 (dd/mm/yyyy)
 * Updated: 05/06/2026 (dd/mm/yyyy)
 * Last change: Removed unused _turnSharpness and debug-path comments
*/

using Godot;
using Core;
using Presentation;
using System.Collections.Generic;

namespace Controller
{
    public partial class GameController : Node2D
    {
        #region fields
        [Export] private int _seed = 12345;
        [Export] private float _crossDuration = 0.7f;
        [Export] private float _crashDriveDuration = 0.35f;

        private readonly Dictionary<LaneOrigin, LaneView> _lanes = new();
        private GameplayView _hud;
        private GameManager _game;
        private Vector2 _center;
        private int _pendingBoxAnims; // box-occupying animations still running this crossing/crash
        private int _textureCounter;  // deterministic per-car texture seed
        #endregion

        #region private methods
        // --- input ---------------------------------------------------------
        private void OnLaneTapped(int origin) => _game.ReleaseLane((LaneOrigin)origin);

        private void OnFrontSettled(int origin) => _game.NotifyFrontSettled((LaneOrigin)origin);

        // --- core events ---------------------------------------------------
        private void OnCarEntered(Car car)
            => _lanes[car.LaneOrigin].SpawnCar(car.MovementDirection, _textureCounter++);

        private void OnCarCrossing(Car car)
        {
            LaneView origin = _lanes[car.LaneOrigin];
            LaneView dest = _lanes[DestinationOf(car.LaneOrigin, car.MovementDirection)];

            CarView view = origin.ReleaseFront();
            if (view == null) return;

            BeginBoxAnim(view);
            view.Cross(view.GlobalPosition, dest.EntryPoint, dest.ExitPoint, _crossDuration);
        }

        private void OnCarHonked(Car car) => _lanes[car.LaneOrigin].HonkWaiting();

        private void OnCrashed(Car a, Car b, CrashCause cause)
        {
            if (cause == CrashCause.Impatience)
            {
                _lanes[a.LaneOrigin].CrashQueue();
                return;
            }

            DriveIntoCentreAndCrash(a);
            DriveIntoCentreAndCrash(b);
        }

        private void OnGameOver(int finalScore)
        {
            foreach (LaneView lane in _lanes.Values)
                lane.SetInputEnabled(false);

            GD.Print($"Game over. Score: {finalScore}");
            // TODO: show game-over UI / offer restart
        }
        private void DriveIntoCentreAndCrash(Car car)
        {
            CarView view = _lanes[car.LaneOrigin].ReleaseFront();
            if (view == null) return;

            BeginBoxAnim(view);
            view.CrashInto(_center, _crashDriveDuration);
        }

        private void BeginBoxAnim(CarView view)
        {
            _pendingBoxAnims++;
            view.CrossingFinished += OnBoxAnimationFinished;
        }

        // Frees the box only once every car in the crossing/crash has finished, so a two-car crash
        // doesn't release it while the second car is still on screen.
        private void OnBoxAnimationFinished()
        {
            if (--_pendingBoxAnims > 0) return;
            _pendingBoxAnims = 0;
            _game.NotifyAnimationFinished();
        }

        // straight = +2 (opposite), right = +1, left = +3, over LaneOrigin order N, W, S, E.
        private LaneOrigin DestinationOf(LaneOrigin origin, MovementDirection dir)
        {
            int offset = dir switch
            {
                MovementDirection.Straight => 2,
                MovementDirection.Right => 1,
                MovementDirection.Left => 3,
                _ => 2,
            };
            return (LaneOrigin)(((int)origin + offset) % 4);
        }
        #endregion

        #region engine lifecycle
        public override void _Ready()
        {
            _lanes[LaneOrigin.North] = GetNode<LaneView>("NorthLane");
            _lanes[LaneOrigin.South] = GetNode<LaneView>("SouthLane");
            _lanes[LaneOrigin.East]  = GetNode<LaneView>("EastLane");
            _lanes[LaneOrigin.West]  = GetNode<LaneView>("WestLane");

            foreach (var (origin, lane) in _lanes)
            {
                lane.Origin = origin;
                lane.LaneTapped += OnLaneTapped;
                lane.FrontSettled += OnFrontSettled;
            }

            Marker2D centerMarker = GetNodeOrNull<Marker2D>("Center");
            _center = centerMarker != null
                ? centerMarker.GlobalPosition
                : (_lanes[LaneOrigin.North].EntryPoint + _lanes[LaneOrigin.South].EntryPoint
                   + _lanes[LaneOrigin.East].EntryPoint + _lanes[LaneOrigin.West].EntryPoint) / 4f;

            _game = new GameManager(_seed);
            _game.CarEntered += OnCarEntered;
            _game.CarCrossing += OnCarCrossing;
            _game.CarHonked += OnCarHonked;
            _game.Crashed += OnCrashed;
            _game.GameOver += OnGameOver;

            _hud = GetNode<GameplayView>("%HUD");

            _hud.Initialize(
                maxLives: _game.Lives,
                startLives: _game.Lives,
                startScore: _game.Score
            );

            _game.LifeLost += _hud.SetLives;
            _game.CarCrossing += _ => _hud.UpdateScore(_game.Score);
        }

        public override void _Process(double delta) => _game?.Tick((float)delta);

        public override void _ExitTree() => _game?.Dispose();
        #endregion
    }
}
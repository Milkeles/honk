# HONK!

A fast-paced traffic-control arcade game where every tap keeps the intersection alive — or wrecks it.

## 1. Introduction

### 1.1. Purpose
This document is the combined design and technical reference for **HONK!**, a minimal real-time traffic-control game. It captures the intended experience, mechanics, rules, architecture, and development conventions in a single place.

### 1.2. Project Scope
HONK! is a small, self-contained arcade game with one mode: survive an escalating 4-way intersection for as long as possible. The scope is deliberately tight — one screen, one input type, no meta-progression and no content unlocks. The project's core was built in a single day, so the design favors a small, well-defined core over breadth, later completed over the course of three weeks to a publication-ready stage and maintained occasionally.

## 2. Game Overview

### 2.1. High Concept and Elevator Pitch
You are the traffic at a chaotic 4-way intersection. Cars pour in from every direction, each one losing patience by the second. Tap a lane to wave its car through and keep everything flowing — for as long as your nerves and your lives hold out.

### 2.2. Genre and Theme
Real-time arcade / reflex puzzle. Theme: urban traffic control descending into controlled chaos.

### 2.3. Target Audience
Casual and arcade players who enjoy short, high-pressure, "one more go" sessions on mobile. No prior genre knowledge required.

### 2.4. Design Influence
HONK! sits in the lineage of fast, readable, high-pressure micro-management arcade games — the kind built around a single repeated decision and a tight failure loop.

## 3. Core Experience

### 3.1. Player Fantasy
A rookie traffic controller thrown into an intersection that's busier than they can comfortably handle — surviving on quick reads and quicker reflexes.

### 3.2. Core Gameplay Loop
Read the lanes → decide which car is safe (and most urgent) to release → tap it → repeat, faster and faster, until you run out of lives.

### 3.3. Key Differentiators
Depth from a single input: one tap, four choices, but real prioritization and prediction under time pressure. The patience-timer-to-crash escalation creates visible, diegetic urgency without leaning on extra UI.

## 4. Game Mechanics

### 4.1. Core Gameplay Mechanics
- The player manages a 4-way intersection in real time.
- Cars continuously arrive from all four directions.
- Each lane presents its front car and the direction that car intends to take.
- Tapping a lane releases its front car into the intersection.
- The run continues until the player runs out of lives.

### 4.2. Player Actions and Controls

#### 4.2.1. Input Lexicon
A single input: tap one of the four lanes/directions to release that lane's car. There are no other controls during play.

#### 4.2.2. Diegetic Feedback
Cars communicate their state through the world rather than abstract UI. A waiting car honks up to three times as a warning while its patience runs low; collisions and deadlocks resolve with clear, readable effects so the player understands exactly why a life was lost.

### 4.3. Game Rules and Systems
- **One car per lane** is in play at a time, which keeps the intersection legible and avoids constant deadlocks.
- **Lives:** the player has a small pool of lives and loses the run when it's exhausted.
- A life is lost when:
  - **Illegal move** — a car is released into a path that isn't clear.
  - **Deadlock** — the intersection locks into a state the player failed to prevent in time.
  - **Rage crash** — a car's patience (anger) timer expires before it's released; an impatient driver crashes into the car ahead, clearing both and costing a life. The car honks up to three times first.

## 5. Progression and Difficulty

### 5.1. Player Progression
A single run is pure endless survival; the player's skill is the main driver as the intersection speeds up, with score as the measure of a run.

Persistent progression is planned as a stretch goal if time allows. The player advances as a traffic controller, and upgrading the controller improves survivability rather than adding new abilities — more lives, life regeneration, and similar resilience boosts.

### 5.2. Difficulty Curve
Traffic intensifies over the course of a run — arrivals grow more frequent and patience timers tighten — pushing the player from comfortable scheduling toward controlled panic.

## 6. Game Flow

### 6.1. First-Time Experience
A run begins on a calm intersection with infrequent arrivals, giving the player room to learn the single tap input before pressure ramps up.

### 6.2. Session Structure
Sessions are short and self-contained: play until lives run out, see the result, restart. Built for fast, repeatable "one more go" loops.

### 6.3. Pacing
Each run is a single rising curve from sparse to overwhelming, with little downtime once traffic picks up.

## 7. User Interface and Feedback

### 7.1. HUD and GUI Elements
Minimal and glanceable: remaining lives, current score, the four lanes with each front car's intended direction, and the patience state of waiting cars.

### 7.2. Feedback Systems
State is communicated diegetically wherever possible — escalating honks as patience runs low, and distinct crash/clear effects on collisions and deadlocks so every life lost is understood immediately.

## 8. Audio and Visual Design

### 8.1. Art Direction
A clean, flat, minimalist style where lanes, car directions, and danger states all read at a glance. Art and logos are produced in Krita and Inkscape.

### 8.2. Audio Direction
A small set: a honk and a theme track. The honk is both the game's name and its core feedback, escalating to signal rising danger. Audio is created in LMMS.

### 8.3. Visual Effects and Polish
Lightweight and in service of readability — simple crash and clear effects, with haptic feedback on a crash at most. Polish is a stretch goal within the day's scope.

## 9. Architecture

The project is split so the gameplay can be built and tested before any visuals exist.

### 9.1. Layers
- **Headless core** — pure logic, no Godot nodes. Owns all rules and state: lane contents, patience timers, legal-move checks, deadlock detection, and lives. Knows nothing about rendering or input.
- **Controller** — the bridge. Collects player input and the frame delta, turns them into commands for the core, and forwards the core's events to the visual layer. Holds no game rules of its own.
- **Visual layer** — renders lanes, cars, honks, crashes, and UI by reacting to events out of the core. It never reaches into core state.

### 9.2. Communication
A clean one-way split, carried over Godot signals (no event bus needed):
- **Commands** flow *into* the core — e.g. `release_lane(direction)`, `tick(delta)`.
- **Events** flow *out of* the core — e.g. `car_entered`, `car_honked`, `deadlock`, `life_lost`, `game_over`.

### 9.3. Principles
- The core advances only through an injected `tick(delta)` and never reads wall-clock time itself, keeping it deterministic and unit-testable (timers can be fast-forwarded in tests).
- The core stays off `Node` (a plain `RefCounted` class), so it carries no scene-tree dependencies while still being able to emit signals.

## 10. Development

### 10.1. Tech Stack
- Godot Engine 4.6.3 (.NET / Mono)
- C#

### 10.2. Branching Strategy
The project follows a trunk-based development approach. The main branch serves as the single source of truth and should always remain in a stable, deployable state. Development work is carried out on short-lived branches that are merged back into main promptly upon completion. Branch names follow the convention `type/short-description`, where `type` is one of:

- `feature/` - new functionality;
- `fix/` - bug fixes;
- `docs/` - documentation changes;
- `chore/` - maintenance tasks such as dependency updates or configuration changes;

### 10.3. Commit Convention
Commits follow a standard label prefix to keep the history readable and traceable:

- `feat:` - a new feature;
- `fix:` - a bug fix;
- `docs:` - documentation changes only;
- `chore:` - maintenance, tooling, or dependency changes;
- `style:` - formatting or visual changes that do not affect functionality;
- `refactor:` - code changes that neither fix a bug nor add a feature;

Commit messages should be written in the imperative mood and kept concise.

### 10.4. Versioning
The project follows Semantic Versioning (SemVer): `MAJOR.MINOR.PATCH`

- `MAJOR` - breaking changes or significant milestone releases;
- `MINOR` - new features added;
- `PATCH` - bug fixes, copy changes, or minor visual corrections;

### 10.5. Change Tracking
All changes are tracked through Git commit history on GitHub. Each merge into main represents a stable checkpoint in the project's development.

## 11. Project Status
Built in a single day. Early prototype.

## 12. Authors
- **Milkeles** — Lead Developer (full-stack; does everything).
- **Chichi** — Secondary Programmer.

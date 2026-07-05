/* Player progression: the four upgradable stats (health, regen, coin gain, rebirth) and all cost/effect
 * formulas as pure functions. SaveManager owns an instance, persists the levels, and drives purchases;
 * gameplay reads the derived effects (MaxHearts, RegenPerCar, etc.).
 *
 * Dependencies: None
 * Author(s): H. Hristov (Milkeles)
 * Created: 01/07/2026 (dd/mm/yyyy)
 * Updated: N/A
 * Last change: N/A
*/

namespace Core
{
    public enum Stat { Health, Regen, Gain }

    public class Upgrades
    {
        #region constants
        public const int MaxStatLevel = 3;
        public const int MaxRebirth = 10;

        private const int BaseHearts = 3;
        private const float RegenPerLevel = 0.025f;
        private const float CoinGainPerLevel = 0.25f;
        #endregion

        #region properties
        public int HealthLevel { get; private set; }
        public int RegenLevel { get; private set; }
        public int GainLevel { get; private set; }
        public int Rebirth { get; private set; }

        // Derived gameplay effects.
        public int MaxHearts => BaseHearts + HealthLevel; // (3 - 6)
        public float RegenPerCar => RegenLevel * RegenPerLevel; // (0 - 0.075)
        public float CoinMultiplier => 1f + GainLevel * CoinGainPerLevel; // (1 - 1.75)
        public int ScorePerCar => 1 + Rebirth; // (1 - 11)

        public bool AllStatsMaxed =>
            HealthLevel >= MaxStatLevel && RegenLevel >= MaxStatLevel && GainLevel >= MaxStatLevel;

        public bool CanRebirth => AllStatsMaxed && Rebirth < MaxRebirth;
        #endregion

        #region constructors
        public Upgrades(int healthLevel, int regenLevel, int gainLevel, int rebirth)
        {
            HealthLevel = healthLevel;
            RegenLevel = regenLevel;
            GainLevel = gainLevel;
            Rebirth = rebirth;
        }
        #endregion

        #region public methods
        public int LevelOf(Stat stat) => stat switch
        {
            Stat.Health => HealthLevel,
            Stat.Regen => RegenLevel,
            Stat.Gain => GainLevel,
            _ => 0,
        };

        public bool IsMaxed(Stat stat) => LevelOf(stat) >= MaxStatLevel;

        // Stat upgrade cost: 250 + level*250 + rebirth*100 (level = current, i.e. the next purchase).
        public int CostOf(Stat stat) => 250 + LevelOf(stat) * 250 + Rebirth * 100;

        public int RebirthCost() => 5000 + Rebirth * 5000;

        /// <summary>Applies a stat purchase (increments its level). Caller must have checked
        /// affordability and deducted coins. No-op if already maxed.</summary>
        public bool ApplyUpgrade(Stat stat)
        {
            if (IsMaxed(stat)) return false;
            switch (stat)
            {
                case Stat.Health: HealthLevel++; break;
                case Stat.Regen: RegenLevel++; break;
                case Stat.Gain: GainLevel++; break;
            }
            return true;
        }

        /// <summary>Applies a rebirth: resets the three stats, increments rebirth. Caller checks
        /// CanRebirth and deducts the cost first.</summary>
        public bool ApplyRebirth()
        {
            if (!CanRebirth) return false;
            HealthLevel = 0;
            RegenLevel = 0;
            GainLevel = 0;
            Rebirth++;
            return true;
        }
        #endregion
    }
}
/* Persists player progress to user://save.json: highscore, coins, audio volumes, and upgrade levels.
 * Autoloaded as /root/SaveManager. Owns the Upgrades progression object, loading it from and committing
 * it to disk; purchases check coins here and apply level changes to Upgrades atomically.
 *
 * Dependencies: Upgrades (Core), Godot
 * Author(s): H. Hristov (Milkeles)
 * Created: 28/06/2026 (dd/mm/yyyy)
 * Updated: 01/07/2026 (dd/mm/yyyy)
 * Last change: Added Upgrades ownership & purchase/rebirth methods
*/

using Godot;
using System.Collections.Generic;

namespace Services
{
    public partial class SaveManager : Node
    {
        #region fields
        private const string SavePath = "user://save.json";

        private SaveData _data = new();
        private Core.Upgrades _upgrades;
        #endregion

        #region properties
        public int HighScore => _data.HighScore;
        public int Coins => _data.Coins;
        public float MusicVolume => _data.MusicVolume;
        public float SfxVolume => _data.SfxVolume;

        /// <summary>The player's progression (levels + derived effects). Persisted via CommitUpgrades.</summary>
        public Core.Upgrades Upgrades => _upgrades;
        #endregion

        #region public methods
        /// <summary>Stores <paramref name="score"/> if it beats the saved highscore. True if updated.</summary>
        public bool TrySetHighScore(int score)
        {
            if (score <= _data.HighScore) return false;
            _data.HighScore = score;
            Save();
            return true;
        }

        public void AddCoins(int amount)
        {
            _data.Coins += amount;
            Save();
        }

        /// <summary>Deducts <paramref name="amount"/> if affordable. True on success.</summary>
        public bool SpendCoins(int amount)
        {
            if (_data.Coins < amount) return false;
            _data.Coins -= amount;
            Save();
            return true;
        }

        public void SetVolumes(float music, float sfx)
        {
            _data.MusicVolume = music;
            _data.SfxVolume = sfx;
            Save();
        }

        /// <summary>Buys one level of <paramref name="stat"/> if not maxed and affordable. Deducts coins,
        /// applies the level, and persists. Returns true on success.</summary>
        public bool TryBuyUpgrade(Core.Stat stat)
        {
            if (_upgrades.IsMaxed(stat)) return false;

            int cost = _upgrades.CostOf(stat);
            if (_data.Coins < cost) return false;

            _data.Coins -= cost;
            _upgrades.ApplyUpgrade(stat);
            CommitUpgrades();
            return true;
        }

        /// <summary>Performs a rebirth if all stats are maxed and affordable. Deducts coins, resets stats,
        /// increments rebirth, and persists. Returns true on success.</summary>
        public bool TryRebirth()
        {
            if (!_upgrades.CanRebirth) return false;

            int cost = _upgrades.RebirthCost();
            if (_data.Coins < cost) return false;

            _data.Coins -= cost;
            _upgrades.ApplyRebirth();
            CommitUpgrades();
            return true;
        }
        #endregion

        #region private methods
        private void CommitUpgrades()
        {
            _data.Upgrades["health"] = _upgrades.HealthLevel;
            _data.Upgrades["regen"] = _upgrades.RegenLevel;
            _data.Upgrades["gain"] = _upgrades.GainLevel;
            _data.Upgrades["rebirth"] = _upgrades.Rebirth;
            Save();
        }

        private void Save()
        {
            using var f = FileAccess.Open(SavePath, FileAccess.ModeFlags.Write);
            f.StoreString(Json.Stringify(_data.ToDict()));
        }

        private void Load()
        {
            if (FileAccess.FileExists(SavePath))
            {
                using var f = FileAccess.Open(SavePath, FileAccess.ModeFlags.Read);
                var parsed = Json.ParseString(f.GetAsText());
                if (parsed.VariantType == Variant.Type.Dictionary)
                    _data.FromDict((Godot.Collections.Dictionary)parsed);
            }

            _upgrades = new Core.Upgrades(
                _data.Upgrades.GetValueOrDefault("health", 0),
                _data.Upgrades.GetValueOrDefault("regen", 0),
                _data.Upgrades.GetValueOrDefault("gain", 0),
                _data.Upgrades.GetValueOrDefault("rebirth", 0));
        }
        #endregion

        #region engine lifecycle
        public override void _Ready() => Load();
        #endregion
    }
}
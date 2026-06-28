/* Persists player progress to user://save.json: highscore, coins, audio volumes, upgrades. Autoloaded as
 * /root/SaveManager. Loads on ready; each setter writes through to disk.
 *
 * Dependencies: Godot
 * Author(s): H. Hristov (Milkeles)
 * Created: 28/06/2026 (dd/mm/yyyy)
 * Updated: N/A
 * Last change: N/A
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
        #endregion

        #region properties
        public int HighScore => _data.HighScore;
        public int Coins => _data.Coins;
        public float MusicVolume => _data.MusicVolume;
        public float SfxVolume => _data.SfxVolume;
        public IReadOnlyDictionary<string, int> Upgrades => _data.Upgrades;
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

        public int GetUpgrade(string key) => _data.Upgrades.GetValueOrDefault(key, 0);

        public void SetUpgrade(string key, int level)
        {
            _data.Upgrades[key] = level;
            Save();
        }
        #endregion

        #region private methods
        private void Save()
        {
            using var f = FileAccess.Open(SavePath, FileAccess.ModeFlags.Write);
            f.StoreString(Json.Stringify(_data.ToDict()));
        }

        private void Load()
        {
            if (!FileAccess.FileExists(SavePath)) return;

            using var f = FileAccess.Open(SavePath, FileAccess.ModeFlags.Read);
            var parsed = Json.ParseString(f.GetAsText());
            if (parsed.VariantType == Variant.Type.Dictionary)
                _data.FromDict((Godot.Collections.Dictionary)parsed);
        }
        #endregion

        #region engine lifecycle
        public override void _Ready() => Load();
        #endregion
    }
}
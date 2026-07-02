/* Serializable player-progress fields and their dictionary mapping for SaveManager. Upgrade levels
 * (health, regen, gain, rebirth) live under the "upgrades" sub-dictionary by name.
 *
 * Dependencies: Godot, Updates
 * Author(s): H. Hristov (Milkeles)
 * Created: 28/06/2026 (dd/mm/yyyy)
 * Updated: 01/07/2026 (dd/mm/yyyy)
 * Last change: Documented upgrade keys; volume defaults corrected to 50
*/

using Godot;
using System.Collections.Generic;

namespace Services
{
    public class SaveData
    {
        public int HighScore;
        public int Coins;
        public float MusicVolume = 50f;
        public float SfxVolume = 50f;

        // Upgrade levels by key: "health", "regen", "gain", "rebirth".
        public Dictionary<string, int> Upgrades = new();

        public Godot.Collections.Dictionary ToDict()
        {
            var up = new Godot.Collections.Dictionary();
            foreach (var kv in Upgrades) up[kv.Key] = kv.Value;

            return new Godot.Collections.Dictionary
            {
                ["highScore"] = HighScore,
                ["coins"] = Coins,
                ["musicVolume"] = MusicVolume,
                ["sfxVolume"] = SfxVolume,
                ["upgrades"] = up,
            };
        }

        public void FromDict(Godot.Collections.Dictionary d)
        {
            HighScore = (int)d.GetValueOrDefault("highScore", 0);
            Coins = (int)d.GetValueOrDefault("coins", 0);
            MusicVolume = (float)d.GetValueOrDefault("musicVolume", 50f);
            SfxVolume = (float)d.GetValueOrDefault("sfxVolume", 50f);

            Upgrades.Clear();
            if (d.GetValueOrDefault("upgrades", new Godot.Collections.Dictionary()).Obj
                is Godot.Collections.Dictionary up)
                foreach (var key in up.Keys)
                    Upgrades[(string)key] = (int)up[key];
        }
    }
}
/* Serializable player-progress fields and their dictionary mapping for SaveManager.
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
    public class SaveData
    {
        public int HighScore;
        public int Coins;
        public float MusicVolume = 1.0f;
        public float SfxVolume = 1.0f;
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
            MusicVolume = (float)d.GetValueOrDefault("musicVolume", 1.0f);
            SfxVolume = (float)d.GetValueOrDefault("sfxVolume", 1.0f);

            Upgrades.Clear();
            if (d.GetValueOrDefault("upgrades", new Godot.Collections.Dictionary()).Obj
                is Godot.Collections.Dictionary up)
                foreach (var key in up.Keys)
                    Upgrades[(string)key] = (int)up[key];
        }
    }
}
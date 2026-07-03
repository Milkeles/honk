/* Wraps the AdMob plugin for rewarded ads. Autoloaded as /root/AdManager. Preloads the rewarded unit
 * and re-loads after each show; ShowRewarded invokes onReward only if the user earned the reward.
 *
 * Dependencies: AdMob plugin, Godot
 * Author(s): H. Hristov (Milkeles)
 * Created: 28/06/2026 (dd/mm/yyyy)
 * Updated: N/A
 * Last change: N/A
*/

using Godot;
using System;

namespace Services
{
    public partial class AdManager : Node
    {
        #region fields

        private Action _onReward;
        private RewardedAd _rewarded; // <-- your plugin's rewarded type
        #endregion

        #region public methods
        /// <summary>Shows the rewarded ad; calls <paramref name="onReward"/> only if the reward is earned.</summary>
        public void ShowRewarded(Action onReward)
        {
            _onReward = onReward;
        }
        #endregion

        #region private methods

        private void OnEarned(/* reward args */)
        {
            Action cb = _onReward;
            _onReward = null;
            cb?.Invoke();
        }
        #endregion

        #region engine lifecycle
        public override void _Ready() => Load();
        #endregion
    }
}
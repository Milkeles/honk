/* Wraps the Poing Studios AdMob plugin for the rewarded ad. Autoloaded as /root/AdManager. Loads the
 * rewarded unit on ready and re-loads after each dismissal; ShowRewarded invokes onReward only when the
 * reward is actually earned.
 *
 * Dependencies: AdMob plugin (Poing Studios), Godot
 * Author(s): H. Hristov (Milkeles)
 * Created: 28/06/2026 (dd/mm/yyyy)
 * Updated: 28/06/2026 (dd/mm/yyyy)
 * Last change: Rewrote against the actual Poing API (RewardedAdLoader, callback objects, listeners)
*/

using Godot;
using System;
using PoingStudios.AdMob.Api;
using PoingStudios.AdMob.Api.Core;
using PoingStudios.AdMob.Api.Listeners;

namespace Services
{
    public partial class AdManager : Node
    {
        #region fields
        private const string RewardedId = "ca-app-pub-2324868124525352/5425148269";

        private RewardedAd _rewardedAd;
        private Action _onReward;
        private bool _rewardEarned;
        #endregion

        #region public methods
        /// <summary>Shows the rewarded ad if loaded; calls <paramref name="onReward"/> only if earned.
        /// If no ad is ready, kicks off a load and skips this attempt (no reward).</summary>
        public void ShowRewarded(Action onReward)
        {
            _onReward = onReward;
            _rewardEarned = false;

            if (_rewardedAd != null)
            {
                _rewardedAd.Show(new OnUserEarnedRewardListener
                {
                    OnUserEarnedReward = _ => _rewardEarned = true,
                });
            }
            else
            {
                Load(); // nothing ready; preload for next time
            }
        }
        #endregion

        #region private methods
        private void Load()
        {
            new RewardedAdLoader().Load(RewardedId, new AdRequest(), new RewardedAdLoadCallback
            {
                OnAdLoaded = ad =>
                {
                    _rewardedAd = ad;
                    _rewardedAd.FullScreenContentCallback = new FullScreenContentCallback
                    {
                        OnAdDismissedFullScreenContent = OnDismissed,
                        OnAdFailedToShowFullScreenContent = _ => OnDismissed(),
                    };
                },
                OnAdFailedToLoad = _ => _rewardedAd = null,
            });
        }

        private void OnDismissed()
        {
            _rewardedAd?.Destroy();
            _rewardedAd = null;
            Load(); // preload the next one

            if (_rewardEarned)
            {
                Action cb = _onReward;
                _onReward = null;
                cb?.Invoke();
            }
        }
        #endregion

        #region engine lifecycle
        public override void _Ready()
        {
            MobileAds.Initialize(new OnInitializationCompleteListener
            {
                OnInitializationComplete = _ => Load(),
            });
        }
        #endregion
    }
}
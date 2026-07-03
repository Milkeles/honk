/* Wraps the Poing Studios AdMob plugin for the rewarded (revive) ad. Autoloaded as /root/AdManager.
 * Reads the ad unit id from res://ad_config.json (kept out of the public repo), initializes the SDK,
 * loads the rewarded unit, and re-loads after each dismissal. ShowRewarded invokes onReward only when
 * the reward is actually earned. If no config is present, ads are disabled and ShowRewarded no-ops.
 *
 * Dependencies: AdMob plugin (Poing Studios), Godot
 * Author(s): H. Hristov (Milkeles)
 * Created: 28/06/2026 (dd/mm/yyyy)
 * Updated: 28/06/2026 (dd/mm/yyyy)
 * Last change: Ad unit id loaded from ad_config.json; no test fallback; guarded when unconfigured
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
        private const string ConfigPath = "res://ad_config.json";

        private string _reviveAdId = "";
        private bool _isConfigured;

        private RewardedAd _rewardedAd;
        private Action _onReward;
        private bool _rewardEarned;
        #endregion

        #region public methods
        /// <summary>Shows the rewarded ad if one is loaded; calls <paramref name="onReward"/> only if the
        /// reward is earned. If ads are unconfigured or no ad is ready, does nothing (and preloads one).</summary>
        public void ShowRewarded(Action onReward)
        {
            _onReward = onReward;
            _rewardEarned = false;

            if (!_isConfigured)
            {
                GD.PushWarning("AdManager: no ad unit configured; revive ad unavailable.");
                _onReward = null;
                return;
            }

            if (_rewardedAd != null)
            {
                _rewardedAd.Show(new OnUserEarnedRewardListener
                {
                    OnUserEarnedReward = _ => _rewardEarned = true,
                });
            }
            else
            {
                Load(); // nothing ready; preload for next time (this attempt yields no reward)
            }
        }
        #endregion

        #region private methods
        private void LoadConfig()
        {
            if (!FileAccess.FileExists(ConfigPath))
            {
                GD.PushWarning($"AdManager: {ConfigPath} not found; ads disabled.");
                return;
            }

            using var f = FileAccess.Open(ConfigPath, FileAccess.ModeFlags.Read);
            var parsed = Json.ParseString(f.GetAsText());

            if (parsed.VariantType != Variant.Type.Dictionary)
            {
                GD.PushWarning("AdManager: ad_config.json malformed; ads disabled.");
                return;
            }

            var d = (Godot.Collections.Dictionary)parsed;
            
            _reviveAdId = d.TryGetValue("revive_ad_id", out Variant id) ? id.AsString() : "";
            _isConfigured = !string.IsNullOrEmpty(_reviveAdId);

            if (!_isConfigured)
                GD.PushWarning("AdManager: revive_ad_id missing/empty; ads disabled.");
        }

        private void Load()
        {
            if (!_isConfigured) return;

            new RewardedAdLoader().Load(_reviveAdId, new AdRequest(), new RewardedAdLoadCallback
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
            LoadConfig();

            MobileAds.Initialize(new OnInitializationCompleteListener
            {
                OnInitializationComplete = _ => Load(),
            });
        }
        #endregion
    }
}
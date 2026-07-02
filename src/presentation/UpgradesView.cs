/* Character-upgrades screen. Reads the player's Upgrades and coin balance from SaveManager, displays each
 * stat's level and next cost, and buys on button press. Rebirth is gated until all stats are maxed.
 * When a stat maxes out, its cost label shows "(Max({level}))" and its buy button hides. Display + input
 * only; all purchase rules live in SaveManager/Upgrades.
 *
 * Dependencies: Upgrades (Core), SaveManager, TransitionSceneView, Godot
 * Author(s): H. Hristov (Milkeles)
 * Created: 28/06/2026 (dd/mm/yyyy)
 * Updated: N/A
 * Last change: N/A
*/

using Godot;
using Core;

namespace Presentation
{
    public partial class UpgradesView : Control
    {
        #region fields
        private Services.SaveManager _save;
        private TransitionSceneView _transition;

        private Button _healthButton, _regenButton, _gainButton, _rebirthButton;
        private Label _healthLevel, _regenLevel, _gainLevel, _rebirthLevel;
        private Label _healthCost, _regenCost, _gainCost, _rebirthCost;
        private Label _coinsLabel;
        #endregion

        #region private methods
        private void OnHealthPressed() => Buy(Stat.Health);
        private void OnRegenPressed() => Buy(Stat.Regen);
        private void OnGainPressed() => Buy(Stat.Gain);

        private void Buy(Stat stat)
        {
            if (_save.TryBuyUpgrade(stat))
                Refresh();
            // else: not enough coins or maxed — optionally play a "denied" cue here.
        }

        private void OnRebirthPressed()
        {
            if (_save.TryRebirth())
                Refresh();
        }

        private void OnBackPressed() => _transition.GoToScene("Menu");

        // Re-reads Upgrades + coins and repaints every stat row, rebirth row, and the coin total.
        private void Refresh()
        {
            Upgrades u = _save.Upgrades;

            UpdateStatRow(Stat.Health, u, _healthLevel, _healthCost, _healthButton);
            UpdateStatRow(Stat.Regen, u, _regenLevel, _regenCost, _regenButton);
            UpdateStatRow(Stat.Gain, u, _gainLevel, _gainCost, _gainButton);

            UpdateRebirthRow(u);

            _coinsLabel.Text = _save.Coins.ToString();
        }

        private void UpdateStatRow(Stat stat, Upgrades u, Label levelLabel, Label costLabel, Button button)
        {
            int level = u.LevelOf(stat);

            if (u.IsMaxed(stat))
            {
                levelLabel.Text = $"(Max({level}))";
                costLabel.Visible = false;
                button.Visible = false;
                return;
            }

            levelLabel.Text = $"Level: {level}";
            int cost = u.CostOf(stat);
            costLabel.Text = $"Cost: {cost}";
            costLabel.Visible = true;
            button.Visible = true;
            button.Disabled = _save.Coins < cost;
        }

        private void UpdateRebirthRow(Upgrades u)
        {
            _rebirthLevel.Text = u.Rebirth >= Upgrades.MaxRebirth
                ? $"(Max({u.Rebirth}))"
                : $"Rebirth: {u.Rebirth}";

            if (u.Rebirth >= Upgrades.MaxRebirth)
            {
                _rebirthCost.Visible = false;
                _rebirthButton.Visible = false;
                return;
            }

            int cost = u.RebirthCost();
            _rebirthCost.Text = $"Cost: {cost}";
            _rebirthButton.Visible = true;
            // Enabled only when all stats maxed AND affordable.
            _rebirthButton.Disabled = !u.CanRebirth || _save.Coins < cost;
        }
        #endregion
		
        #region engine lifecycle
        public override void _Ready()
        {
            _save = GetNode<Services.SaveManager>("/root/SaveManager");
            _transition = GetNode<TransitionSceneView>("/root/TransitionScene");

            _healthButton = GetNode<Button>("%HealthBtn");
            _regenButton  = GetNode<Button>("%RegenBtn");
            _gainButton   = GetNode<Button>("%GainBtn");
            _rebirthButton = GetNode<Button>("%RebirthBtn");

            _healthLevel = GetNode<Label>("%HealthLevelLabel");
            _regenLevel  = GetNode<Label>("%RegenLevelLabel");
            _gainLevel   = GetNode<Label>("%GainLevelLabel");
            _rebirthLevel = GetNode<Label>("%RebirthLevelLabel");

            _healthCost = GetNode<Label>("%HealthUpdateCostLabel");
            _regenCost  = GetNode<Label>("%RegenUpdateCostLabel");
            _gainCost   = GetNode<Label>("%GainUpdateCostLabel");
            _rebirthCost = GetNode<Label>("%RebirthCostLabel");

            _coinsLabel = GetNodeOrNull<Label>("%CoinsLabel");

            _healthButton.Pressed += OnHealthPressed;
            _regenButton.Pressed += OnRegenPressed;
            _gainButton.Pressed += OnGainPressed;
            _rebirthButton.Pressed += OnRebirthPressed;

            var back = GetNodeOrNull<Button>("%BackBtn");
            if (back != null) back.Pressed += OnBackPressed;

            Refresh();
        }
        #endregion
    }
}
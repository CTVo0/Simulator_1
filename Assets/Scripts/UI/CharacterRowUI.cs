using System;
using CombatSim.Core;
using TMPro;
using UnityEngine;

namespace CombatSim.UI
{
    public class CharacterRowUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI nameLabel;
        [SerializeField] private TMP_InputField hpInput;
        [SerializeField] private TMP_InputField combatInput;
        [SerializeField] private TMP_InputField defenseInput;
        [SerializeField] private TMP_InputField dmgMinInput;
        [SerializeField] private TMP_InputField dmgMaxInput;
        [SerializeField] private TMP_InputField decayInput;

        private CharacterStats _stats;
        private Action _onStatsChanged;

        public void Bind(CharacterStats stats, Action onStatsChanged)
        {
            _stats = stats;
            _onStatsChanged = onStatsChanged;

            nameLabel.text = stats.Name;
            SyncInputsFromStats();

            hpInput.onEndEdit.RemoveAllListeners();
            combatInput.onEndEdit.RemoveAllListeners();
            defenseInput.onEndEdit.RemoveAllListeners();
            dmgMinInput.onEndEdit.RemoveAllListeners();
            dmgMaxInput.onEndEdit.RemoveAllListeners();
            decayInput.onEndEdit.RemoveAllListeners();

            hpInput.onEndEdit.AddListener(val =>
            {
                int maxHp = Mathf.Max(0, ParseInt(val, _stats.MaxHP));
                _stats.MaxHP = maxHp;
                _stats.CurrentHP = Mathf.Min(_stats.CurrentHP, maxHp);
                SyncInputsFromStats();
                _onStatsChanged?.Invoke();
            });

            combatInput.onEndEdit.AddListener(val =>
            {
                _stats.CombatSkill = Mathf.Max(0, ParseInt(val, _stats.CombatSkill));
                SyncInputsFromStats();
                _onStatsChanged?.Invoke();
            });

            defenseInput.onEndEdit.AddListener(val =>
            {
                _stats.DefenseSkill = Mathf.Max(0, ParseInt(val, _stats.DefenseSkill));
                SyncInputsFromStats();
                _onStatsChanged?.Invoke();
            });

            dmgMinInput.onEndEdit.AddListener(val =>
            {
                _stats.DamageMin = Mathf.Max(0, ParseInt(val, _stats.DamageMin));
                if (_stats.DamageMin > _stats.DamageMax)
                    _stats.DamageMax = _stats.DamageMin;
                SyncInputsFromStats();
                _onStatsChanged?.Invoke();
            });

            dmgMaxInput.onEndEdit.AddListener(val =>
            {
                _stats.DamageMax = Mathf.Max(0, ParseInt(val, _stats.DamageMax));
                if (_stats.DamageMax < _stats.DamageMin)
                    _stats.DamageMin = _stats.DamageMax;
                SyncInputsFromStats();
                _onStatsChanged?.Invoke();
            });

            decayInput.onEndEdit.AddListener(val =>
            {
                _stats.AccuracyDecay = Mathf.Clamp(ParseFloat(val, _stats.AccuracyDecay), 0f, 90f);
                SyncInputsFromStats();
                _onStatsChanged?.Invoke();
            });
        }

        private void SyncInputsFromStats()
        {
            hpInput.text = _stats.MaxHP.ToString();
            combatInput.text = _stats.CombatSkill.ToString();
            defenseInput.text = _stats.DefenseSkill.ToString();
            dmgMinInput.text = _stats.DamageMin.ToString();
            dmgMaxInput.text = _stats.DamageMax.ToString();
            decayInput.text = _stats.AccuracyDecay.ToString("0.##");
        }

        private static int ParseInt(string s, int fallback) => int.TryParse(s, out int v) ? v : fallback;
        private static float ParseFloat(string s, float fallback) => float.TryParse(s, out float v) ? v : fallback;
    }
}
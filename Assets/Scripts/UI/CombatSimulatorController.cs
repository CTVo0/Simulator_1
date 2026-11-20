using System;
using System.Collections.Generic;
using System.Linq;
using CombatSim.Battlefield;
using CombatSim.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CombatSim.UI
{
    public class CombatSimulatorController : MonoBehaviour
    {
        [Header("Battlefield")]
        [SerializeField] private ArenaManager arenaManager;

        [Header("Header")]
        [SerializeField] private TextMeshProUGUI turnBadgeText;

        [Header("Panel 1 - Characters")]
        [SerializeField] private TMP_InputField characterCountInput;
        [SerializeField] private Button buildCharactersButton;
        [SerializeField] private Transform characterListContainer;
        [SerializeField] private CharacterRowUI characterRowPrefab;

        [Header("Panel 2 - Attack Settings")]
        [SerializeField] private TMP_Dropdown attackerDropdown;
        [SerializeField] private TMP_Dropdown targetDropdown;
        [SerializeField] private TextMeshProUGUI attackerChipText;
        [SerializeField] private TextMeshProUGUI targetChipText;
        [SerializeField] private Button attackButton;
        [SerializeField] private Button attack5Button;
        [SerializeField] private Button resetHpButton;

        [Header("Panel 3 - Result & Battle Log")]
        [SerializeField] private TextMeshProUGUI hitChanceText;
        [SerializeField] private Image hitChanceBarFill;
        [SerializeField] private TextMeshProUGUI matchupSummaryText;
        [SerializeField] private TextMeshProUGUI battleLogText;

        private readonly List<CharacterStats> _characters = new List<CharacterStats>();
        private readonly List<string> _logEntries = new List<string>();
        private int _turn = 1;

        private static readonly Color[] Palette =
        {
            new Color32(77, 145, 255, 255),
            new Color32(255, 99, 118, 255),
            new Color32(245, 189, 79, 255),
            new Color32(154, 123, 255, 255),
            new Color32(53, 205, 160, 255),
            new Color32(240, 138, 194, 255),
            new Color32(105, 198, 216, 255),
            new Color32(210, 219, 120, 255)
        };

        private void Start()
        {
            characterCountInput.text = "3";

            buildCharactersButton.onClick.AddListener(BuildCharacterList);
            attackButton.onClick.AddListener(() => ExecuteSingleAttack());
            attack5Button.onClick.AddListener(ExecuteAttackFive);
            resetHpButton.onClick.AddListener(ResetAllHP);

            attackerDropdown.onValueChanged.AddListener(_ => RefreshAllUI());
            targetDropdown.onValueChanged.AddListener(_ => RefreshAllUI());

            BuildCharacterList();
            ClearAndInitLog("[SYSTEM] Combat simulator ready.");
        }

        public void BuildCharacterList()
        {
            int count = Mathf.Clamp(int.TryParse(characterCountInput.text, out int parsed) ? parsed : 3, 1, 26);
            characterCountInput.text = count.ToString();

            if (_characters.Count != count)
            {
                _characters.Clear();
                int[] defaultHps = { 120, 100, 90, 110 };
                int[] defaultAtks = { 150, 120, 100, 135 };
                int[] defaultDefs = { 80, 100, 120, 95 };

                for (int i = 0; i < count; i++)
                {
                    string charName = ((char)('A' + i)).ToString();
                    _characters.Add(new CharacterStats(
                        charName,
                        defaultHps[i % 4],
                        defaultAtks[i % 4],
                        defaultDefs[i % 4],
                        8,
                        15,
                        5f,
                        Palette[i % Palette.Length]
                    ));
                }
            }

            foreach (Transform child in characterListContainer)
            {
                Destroy(child.gameObject);
            }

            foreach (var stats in _characters)
            {
                CharacterRowUI row = Instantiate(characterRowPrefab, characterListContainer);
                row.Bind(stats, RefreshAllUI);
            }

            UpdateDropdowns();
            if (arenaManager != null)
                arenaManager.RebuildUnits(_characters);
            RefreshAllUI();
        }

        private void UpdateDropdowns()
        {
            int prevA = attackerDropdown.value;
            int prevT = targetDropdown.value;

            List<string> options = _characters.Select(c => c.Name).ToList();
            attackerDropdown.ClearOptions();
            targetDropdown.ClearOptions();
            attackerDropdown.AddOptions(options);
            targetDropdown.AddOptions(options);

            attackerDropdown.SetValueWithoutNotify(Mathf.Clamp(prevA, 0, _characters.Count - 1));
            int defaultTarget = prevT > 0 ? prevT : Mathf.Min(1, _characters.Count - 1);
            targetDropdown.SetValueWithoutNotify(Mathf.Clamp(defaultTarget, 0, _characters.Count - 1));

            if (attackerDropdown.value == targetDropdown.value && _characters.Count > 1)
            {
                targetDropdown.SetValueWithoutNotify(attackerDropdown.value == 0 ? 1 : 0);
            }
        }

        private void RefreshAllUI()
        {
            if (_characters.Count == 0) return;

            int aIdx = attackerDropdown.value;
            int tIdx = targetDropdown.value;
            CharacterStats attacker = _characters[aIdx];
            CharacterStats target = _characters[tIdx];

            float hitChance = CombatRules.CalculateHitChance(attacker.CombatSkill, target.DefenseSkill);
            hitChanceText.text = $"{hitChance * 100f:0.00}%";
            hitChanceBarFill.fillAmount = hitChance;

            matchupSummaryText.text =
                $"{attacker.Name} → {target.Name} · Combat {attacker.CombatSkill} vs Defense {target.DefenseSkill} · Crit 10%";

            attackerChipText.text =
                $"{attacker.Name}: ATK {attacker.CombatSkill} · DMG {attacker.DamageMin}–{attacker.DamageMax} · Decay {attacker.AccuracyDecay:0.#}%";
            targetChipText.text =
                $"{target.Name}: DEF {target.DefenseSkill} · HP {target.CurrentHP}/{target.MaxHP}";

            turnBadgeText.text = $"TURN {_turn:00}";

            if (arenaManager != null)
                arenaManager.RefreshAll(aIdx, tIdx);
        }

        private bool ExecuteSingleAttack(float? hitChanceOverride = null, int? shotNumber = null)
        {
            int aIdx = attackerDropdown.value;
            int tIdx = targetDropdown.value;
            CharacterStats attacker = _characters[aIdx];
            CharacterStats target = _characters[tIdx];

            if (attacker == target)
            {
                AppendLog("<color=#9AABC4>Không thể tự tấn công chính mình.</color>");
                return false;
            }

            if (target.IsDead)
            {
                AppendLog($"<color=#FF7583>{target.Name} đã bị hạ gục.</color>");
                return false;
            }

            AttackOutcome outcome = CombatRules.ResolveAttack(attacker, target, hitChanceOverride);
            _turn++;

            string prefix = shotNumber.HasValue ? $"Đòn {shotNumber.Value}/5 · " : "";
            float pctDisplay = Mathf.Round(outcome.HitChanceUsed * 1000f) / 10f;

            if (outcome.IsHit)
            {
                if (outcome.IsCritical)
                {
                    AppendLog(
                        $"<color=#FF9F43><b>{prefix}{attacker.Name} CRITICAL HIT {target.Name} · {pctDisplay:0.#}% accuracy · {outcome.FinalDamage} damage ({outcome.BaseDamage}×2) · {outcome.TargetRemainingHP}/{target.MaxHP} HP</b></color>"
                    );
                }
                else
                {
                    AppendLog(
                        $"<color=#45DF98>{prefix}{attacker.Name} HIT {target.Name} · {pctDisplay:0.#}% accuracy · {outcome.FinalDamage} damage · {outcome.TargetRemainingHP}/{target.MaxHP} HP</color>"
                    );
                }
            }
            else
            {
                AppendLog(
                    $"<color=#FF7583>{prefix}{attacker.Name} MISS {target.Name} · {pctDisplay:0.#}% accuracy</color>"
                );
            }

            SafePlayFeedback(aIdx, tIdx, outcome);
            RefreshAllUI();
            return true;
        }

        private void SafePlayFeedback(int aIdx, int tIdx, AttackOutcome outcome)
        {
            if (arenaManager == null) return;
            try
            {
                arenaManager.PlayAttackFeedback(aIdx, tIdx, outcome);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[CombatSim] Thiếu tham chiếu hiệu ứng trên Prefab: {ex.Message}");
            }
        }

        private void ExecuteAttackFive()
        {
            Debug.Log("[Attack x5] START");

            int aIdx = attackerDropdown.value;
            int tIdx = targetDropdown.value;

            CharacterStats attacker = _characters[aIdx];
            CharacterStats target = _characters[tIdx];

            if (attacker == target)
            {
                AppendLog("<color=#9AABC4>Không thể tự tấn công chính mình.</color>");
                return;
            }

            float baseChance = CombatRules.CalculateHitChance(
                attacker.CombatSkill,
                target.DefenseSkill
            );

            float decayPerShot = attacker.AccuracyDecay / 100f;

            AppendLog(
                $"<color=#9AABC4>BẮT ĐẦU ATTACK ×5 · Accuracy ban đầu {baseChance * 100f:0.0}% · {attacker.Name} giảm {attacker.AccuracyDecay:0.0} điểm % mỗi đòn</color>"
            );

            for (int i = 0; i < 5; i++)
            {
                Debug.Log($"[Attack x5] Loop iteration: {i + 1}");

                if (target.IsDead)
                {
                    Debug.Log("[Attack x5] Target dead -> STOP");
                    AppendLog($"<color=#FF7583>{target.Name} đã bị hạ gục; dừng chuỗi tấn công.</color>");
                    break;
                }

                float currentChance = Mathf.Clamp(
                    baseChance - decayPerShot * i,
                    CombatRules.MinHitChance,
                    CombatRules.MaxHitChance
                );

                Debug.Log($"[Attack x5] Chance: {currentChance * 100f}%");

                bool result = ExecuteSingleAttack(currentChance, i + 1);

                Debug.Log($"[Attack x5] Attack result: {result}");

                if (!result)
                {
                    Debug.Log("[Attack x5] ExecuteSingleAttack returned false -> STOP");
                    break;
                }
            }

            Debug.Log("[Attack x5] END");
        }

        private void ResetAllHP()
        {
            foreach (var c in _characters)
            {
                c.ResetHP();
            }
            _turn = 1;
            ClearAndInitLog("[SYSTEM] HP đã được hồi đầy.");
            RefreshAllUI();
        }

        private void ClearAndInitLog(string systemMessage)
        {
            _logEntries.Clear();
            AppendLog($"<color=#9AABC4>{systemMessage}</color>");
        }

        private void AppendLog(string richTextLine)
        {
            _logEntries.Insert(0, richTextLine);
            if (_logEntries.Count > 120)
                _logEntries.RemoveAt(_logEntries.Count - 1);

            battleLogText.text = string.Join("\n", _logEntries);
        }
    }
}
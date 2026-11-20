using System.Collections.Generic;
using CombatSim.Core;
using UnityEngine;

namespace CombatSim.Battlefield
{
    public class ArenaManager : MonoBehaviour
    {
        [Header("Prefabs & References")]
        [SerializeField] private UnitView unitPrefab;
        [SerializeField] private FloatingText floatingTextPrefab;
        [SerializeField] private Transform unitsContainer;
        [SerializeField] private LineRenderer targetLine;

        [Header("Arena Bounds (World Units)")]
        [SerializeField] private Vector2 arenaSize = new Vector2(14f, 4.2f);
        [SerializeField] private Vector2 arenaCenterOffset = new Vector2(0f, 1.8f);

        private readonly List<UnitView> _unitViews = new List<UnitView>();

        public void RebuildUnits(List<CharacterStats> characters)
        {
            foreach (var view in _unitViews)
            {
                if (view != null) Destroy(view.gameObject);
            }
            _unitViews.Clear();

            int n = characters.Count;
            if (n == 0) return;

            int cols = Mathf.Min(6, Mathf.CeilToInt(Mathf.Sqrt(n * 1.6f)));
            int rows = Mathf.CeilToInt((float)n / cols);

            for (int i = 0; i < n; i++)
            {
                int col = i % cols;
                int row = i / cols;

                float normX = ((col + 0.5f) / cols) - 0.5f;
                float normY = 0.5f - ((row + 0.5f) / rows); // Hàng 0 ở trên

                Vector3 pos = new Vector3(
                    arenaCenterOffset.x + normX * arenaSize.x,
                    arenaCenterOffset.y + normY * arenaSize.y,
                    0f
                );

                UnitView view = Instantiate(unitPrefab, pos, Quaternion.identity, unitsContainer);
                view.Bind(characters[i]);
                _unitViews.Add(view);
            }
        }

        public void RefreshAll(int attackerIndex, int targetIndex)
        {
            for (int i = 0; i < _unitViews.Count; i++)
            {
                _unitViews[i].Refresh();
                _unitViews[i].SetAsAttacker(i == attackerIndex);
            }

            if (attackerIndex >= 0 && attackerIndex < _unitViews.Count &&
                targetIndex >= 0 && targetIndex < _unitViews.Count &&
                attackerIndex != targetIndex)
            {
                targetLine.enabled = true;
                targetLine.SetPosition(0, _unitViews[attackerIndex].transform.position);
                targetLine.SetPosition(1, _unitViews[targetIndex].transform.position);
            }
            else
            {
                targetLine.enabled = false;
            }
        }

        public void PlayAttackFeedback(int attackerIndex, int targetIndex, AttackOutcome outcome)
        {
            if (attackerIndex < 0 || targetIndex < 0 || attackerIndex >= _unitViews.Count || targetIndex >= _unitViews.Count)
                return;

            UnitView attackerView = _unitViews[attackerIndex];
            UnitView targetView = _unitViews[targetIndex];

            if (outcome.IsHit)
            {
                Color critOrange = new Color32(255, 159, 67, 255);
                Color normalGold = new Color32(255, 223, 119, 255);

                targetView.TriggerFlash(outcome.IsCritical ? critOrange : Color.white, outcome.IsCritical ? 0.38f : 0.25f);
                SpawnFloatingText(
                    targetView.transform.position + Vector3.up * 0.6f,
                    outcome.IsCritical ? $"CRIT! -{outcome.FinalDamage} HP" : $"-{outcome.FinalDamage} HP",
                    outcome.IsCritical ? critOrange : normalGold
                );
            }
            else
            {
                attackerView.TriggerFlash(Color.white, 0.2f);
                Vector3 midPoint = (attackerView.transform.position + targetView.transform.position) * 0.5f;
                SpawnFloatingText(midPoint, "MISS!", Color.white);
            }
        }

        private void SpawnFloatingText(Vector3 position, string text, Color color)
        {
            if (floatingTextPrefab == null) return;
            FloatingText ft = Instantiate(floatingTextPrefab, position, Quaternion.identity);
            ft.Init(text, color);
        }
    }
}
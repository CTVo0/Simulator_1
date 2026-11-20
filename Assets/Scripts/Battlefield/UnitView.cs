using CombatSim.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CombatSim.Battlefield
{
    public class UnitView : MonoBehaviour
    {
        [Header("Sprites")]
        [SerializeField] private SpriteRenderer bodyRenderer;
        [SerializeField] private SpriteRenderer flashRingRenderer;
        [SerializeField] private SpriteRenderer attackerRingRenderer;
        [SerializeField] private GameObject deadCrossOverlay;
        [SerializeField] private bool preserveOriginalSpriteColor = true; // Bật để giữ nguyên màu gốc ảnh Pixel Art

        [Header("World Space UI")]
        [SerializeField] private CanvasGroup unitCanvasGroup;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI hpText;
        [SerializeField] private Image hpFillImage;

        private CharacterStats _stats;
        private float _flashTimer;

        public CharacterStats Stats => _stats;

        public void Bind(CharacterStats stats)
        {
            _stats = stats;
            nameText.text = stats.Name;

            // Nếu dùng Pixel Art thì tô màu phân biệt lên bảng tên thay vì nhuộm lên thân nhân vật
            if (preserveOriginalSpriteColor)
                nameText.color = stats.UnitColor;

            if (flashRingRenderer != null)
                flashRingRenderer.gameObject.SetActive(false);

            Refresh();
        }

        public void SetAsAttacker(bool isAttacker)
        {
            if (attackerRingRenderer != null)
                attackerRingRenderer.gameObject.SetActive(isAttacker);
        }

        public void TriggerFlash(Color flashColor, float duration = 0.28f)
        {
            if (flashRingRenderer == null) return;
            flashRingRenderer.color = flashColor;
            flashRingRenderer.gameObject.SetActive(true);
            _flashTimer = duration;
        }

        public void Refresh()
        {
            if (_stats == null) return;

            bool isDead = _stats.IsDead;
            float alpha = isDead ? 0.38f : 1f;

            // Giữ màu trắng (Color.white) nếu dùng Pixel Art để không làm sai lệch màu ảnh gốc
            Color baseColor = preserveOriginalSpriteColor ? Color.white : _stats.UnitColor;
            baseColor.a = alpha;
            bodyRenderer.color = baseColor;

            if (unitCanvasGroup != null) unitCanvasGroup.alpha = alpha;
            if (deadCrossOverlay != null) deadCrossOverlay.SetActive(isDead);

            hpText.text = $"{_stats.CurrentHP} / {_stats.MaxHP} HP";
            float pct = _stats.MaxHP > 0 ? Mathf.Clamp01((float)_stats.CurrentHP / _stats.MaxHP) : 0f;
            hpFillImage.fillAmount = pct;

            if (pct > 0.5f)
                hpFillImage.color = new Color32(57, 216, 137, 255);
            else if (pct > 0.25f)
                hpFillImage.color = new Color32(244, 198, 78, 255);
            else
                hpFillImage.color = new Color32(255, 88, 109, 255);
        }

        private void Update()
        {
            if (_flashTimer > 0f)
            {
                _flashTimer -= Time.deltaTime;
                if (_flashTimer <= 0f && flashRingRenderer != null)
                    flashRingRenderer.gameObject.SetActive(false);
            }
        }
    }
}
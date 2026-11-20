using TMPro;
using UnityEngine;

namespace CombatSim.Battlefield
{
    public class FloatingText : MonoBehaviour
    {
        [SerializeField] private TextMeshPro textMesh;
        [SerializeField] private float floatSpeed = 1.2f;
        [SerializeField] private float duration = 0.9f;

        private float _timer;
        private Color _startColor;

        public void Init(string content, Color color)
        {
            textMesh.text = content;
            _startColor = color;
            textMesh.color = color;
            _timer = duration;
        }

        private void Update()
        {
            _timer -= Time.deltaTime;
            transform.position += Vector3.up * (floatSpeed * Time.deltaTime);

            float alpha = Mathf.Clamp01(_timer / (duration * 0.5f));
            textMesh.color = new Color(_startColor.r, _startColor.g, _startColor.b, alpha);

            if (_timer <= 0f)
            {
                Destroy(gameObject);
            }
        }
    }
}
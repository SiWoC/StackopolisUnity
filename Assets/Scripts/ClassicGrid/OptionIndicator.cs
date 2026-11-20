using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SiWoC.Stackopolis.ClassicGrid
{
    public class OptionIndicator : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] Image image;
        [SerializeField] float periodSeconds = 1f;
        [SerializeField] float minAlpha = 0.35f;
        [SerializeField] float maxAlpha = 1f;

        Color tint = Color.white;

        public event Action Clicked;

        public void Show(Color color)
        {
            tint = color;
            gameObject.SetActive(true);
            Apply(maxAlpha);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            Clicked?.Invoke();
        }

        void Update_DisabledNoLongerBlinking()
        {
            float range = Mathf.Max(0f, maxAlpha - minAlpha);
            float alpha = range > 0f && periodSeconds > 0f
                ? minAlpha + Mathf.PingPong(Time.time * (2f * range / periodSeconds), range)
                : maxAlpha;
            Apply(alpha);
        }

        void Apply(float alpha)
        {
            image.color = new Color(tint.r, tint.g, tint.b, alpha);
        }
    }
}

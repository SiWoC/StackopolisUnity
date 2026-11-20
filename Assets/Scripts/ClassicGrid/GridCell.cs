using System;
using UnityEngine;
using UnityEngine.UI;

namespace SiWoC.Stackopolis.ClassicGrid
{
    /// <summary>
    /// One lot on the Classic 5×5 grid.
    /// </summary>
    public class GridCell : MonoBehaviour
    {
        [SerializeField] int x;
        [SerializeField] int y;
        [SerializeField] OptionIndicator optionIndicator;
        [SerializeField] Image towerSquare;

        Action<GridCell> clicked;

        public int X => x;
        public int Y => y;
        public OptionIndicator OptionIndicator => optionIndicator;
        public Image TowerSquare => towerSquare;

        public void Bind(Action<GridCell> onClicked)
        {
            clicked = onClicked;
            optionIndicator.Clicked += OnOptionClicked;
        }

        public void SetTowerSquare(Sprite sprite)
        {
            towerSquare.sprite = sprite;
            towerSquare.enabled = sprite;
        }

        public void ClearTowerSquare()
        {
            towerSquare.sprite = null;
            towerSquare.enabled = false;
        }

        void OnDestroy()
        {
            optionIndicator.Clicked -= OnOptionClicked;
        }

        void OnOptionClicked()
        {
            clicked?.Invoke(this);
        }
    }
}

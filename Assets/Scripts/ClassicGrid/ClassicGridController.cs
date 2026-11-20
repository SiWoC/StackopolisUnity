using SiWoC.Stackopolis.Globals;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SiWoC.Stackopolis.ClassicGrid
{
    public class ClassicGridController : MonoBehaviour
    {
        enum ConfirmScrapKind
        {
            None,
            ScrapNew,
            ReplaceExisting
        }

        [SerializeField] string buildTowerSceneName = "BuildTower";
        [SerializeField] ClassicRules rules = new ClassicRules();
        [SerializeField] TowerTypeVisuals visuals;
        [SerializeField] GridCell[] cells;
        [SerializeField] Toggle residentialToggle;
        [SerializeField] Toggle commercialToggle;
        [SerializeField] Toggle officeToggle;
        [SerializeField] Toggle luxuryToggle;
        [SerializeField] Image miniature;
        [SerializeField] TMP_Text towerTypeText;
        [SerializeField] TMP_Text cityInfoText;
        [SerializeField] GameObject placePanel;
        [SerializeField] GameObject confirmScrapPanel;
        [SerializeField] TMP_Text confirmScrapText;
        [SerializeField] TMP_Text pendingInfoText;
        [SerializeField] Button goButton;
        [SerializeField] Button yesScrapButton;
        [SerializeField] Button dontScrapButton;
        [SerializeField] float efficiencyThreshold = 0.8f;
        [SerializeField] Color positiveColor = new Color32(0x4F, 0xC8, 0x93, 0xFF);
        [SerializeField] Color cautionColor = new Color32(0xE8, 0x7E, 0x5D, 0xFF);

        CityState city;
        TowerType selectedTowerType = TowerType.Residential;
        bool placing;
        ConfirmScrapKind confirmKind;
        GridCell pendingReplaceCell;
        string cityInfoFormat = "City population: {0}";
        string pendingInfoFormat = "Tower type: {0}\nPopulation: {1}\nEfficiency: {2:P0}";
        string confirmScrapNewText = "Are you sure you want to scrap this new tower?";
        string confirmScrapExistingFormat = "Are you sure you want place your new tower here and scrap the existing tower?\nThis will take the city population\nfrom {0} to {1}.";

        void Start()
        {
            city = CityState.FromJson(Settings.LoadClassicCityJson());
            confirmScrapPanel.SetActive(false);

            for (int i = 0; i < cells.Length; i++)
                cells[i].Bind(OnCellClicked);

            ApplyCityVisuals();

            if (BuildSession.Instance.HasPendingTower)
                EnterPlaceMode();
            else
                EnterSelectMode();
        }

        public void OnResidential(bool on)
        {
            if (!on)
                return;

            selectedTowerType = TowerType.Residential;
            RefreshTypePresentation(selectedTowerType);
            RefreshPlacementIndicators();
        }

        public void OnCommercial(bool on)
        {
            if (!on)
                return;

            selectedTowerType = TowerType.Commercial;
            RefreshTypePresentation(selectedTowerType);
            RefreshPlacementIndicators();
        }

        public void OnOffice(bool on)
        {
            if (!on)
                return;

            selectedTowerType = TowerType.Office;
            RefreshTypePresentation(selectedTowerType);
            RefreshPlacementIndicators();
        }

        public void OnLuxury(bool on)
        {
            if (!on)
                return;

            selectedTowerType = TowerType.Luxury;
            RefreshTypePresentation(selectedTowerType);
            RefreshPlacementIndicators();
        }

        public void OnGo()
        {
            BuildSession.Instance.TowerTypeToBuild = selectedTowerType;
            GameFlow.Instance.Forward(buildTowerSceneName);
        }

        public void OnScrap()
        {
            confirmKind = ConfirmScrapKind.ScrapNew;
            pendingReplaceCell = null;
            confirmScrapText.text = confirmScrapNewText;
            // Scrapping the pending tower: positive when efficiency is below the threshold.
            ApplyScrapButtonColors(BuildSession.Instance.PendingTower.Efficiency < efficiencyThreshold);
            confirmScrapPanel.SetActive(true);
        }

        public void OnConfirmScrap()
        {
            if (confirmKind == ConfirmScrapKind.ScrapNew)
            {
                HideConfirmScrap();
                BuildSession.Instance.ClearPendingTower();
                EnterSelectMode();
                return;
            }

            GridCell cell = pendingReplaceCell;
            HideConfirmScrap();
            PlacePendingOn(cell);
        }

        public void OnDontScrap()
        {
            HideConfirmScrap();
        }

        public void OnBack()
        {
            GameFlow.Instance.Back();
        }

        void OnCellClicked(GridCell cell)
        {
            if (!placing)
                return;

            PendingTower pending = BuildSession.Instance.PendingTower;
            if (!rules.CanPlace(pending.TowerType, cell.X, cell.Y, city))
                return;

            CityState.Lot lot = city.GetLot(cell.X, cell.Y);
            if (lot.Occupied)
            {
                OpenConfirmReplaceExisting(cell, lot, pending);
                return;
            }

            PlacePendingOn(cell);
        }

        void PlacePendingOn(GridCell cell)
        {
            PendingTower pending = BuildSession.Instance.PendingTower;
            city.Place(cell.X, cell.Y, pending.TowerType, pending.Floors, pending.Population, pending.HasRoof);
            cell.SetTowerSquare(visuals.GridSquare(pending.TowerType));
            Settings.SaveClassicCity(city.ToJson());
            BuildSession.Instance.ClearPendingTower();
            EnterSelectMode();
        }

        void OpenConfirmReplaceExisting(GridCell cell, CityState.Lot lot, PendingTower pending)
        {
            confirmKind = ConfirmScrapKind.ReplaceExisting;
            pendingReplaceCell = cell;
            int populationAfter = city.Population - lot.Population + pending.Population;
            confirmScrapText.text = string.Format(confirmScrapExistingFormat, city.Population, populationAfter);
            // Confirming places the pending tower: positive when efficiency meets the threshold.
            ApplyScrapButtonColors(pending.Efficiency >= efficiencyThreshold);
            confirmScrapPanel.SetActive(true);
        }

        void HideConfirmScrap()
        {
            confirmKind = ConfirmScrapKind.None;
            pendingReplaceCell = null;
            confirmScrapPanel.SetActive(false);
        }

        void ApplyScrapButtonColors(bool yesIsPositive)
        {
            Color yes = yesIsPositive ? positiveColor : cautionColor;
            Color dont = yesIsPositive ? cautionColor : positiveColor;
            SetButtonColor(yesScrapButton, yes);
            SetButtonColor(dontScrapButton, dont);
        }

        static void SetButtonColor(Button button, Color color)
        {
            ColorBlock block = button.colors;
            block.normalColor = color;
            block.highlightedColor = color;
            block.selectedColor = color;
            block.pressedColor = color * 0.85f;
            button.colors = block;
            button.targetGraphic.color = color;
        }

        void EnterPlaceMode()
        {
            placing = true;
            HideConfirmScrap();
            PendingTower pending = BuildSession.Instance.PendingTower;
            selectedTowerType = pending.TowerType;

            SetTypeToggleOn(pending.TowerType);
            pendingInfoText.text = string.Format(pendingInfoFormat, pending.TowerType, pending.Population, pending.Efficiency);

            SetTypeTogglesInteractable(false);
            goButton.interactable = false;
            placePanel.SetActive(true);

            RefreshTypePresentation(pending.TowerType);
            RefreshCityInfo();
            RefreshPlacementIndicators();
        }

        void EnterSelectMode()
        {
            placing = false;
            HideConfirmScrap();
            placePanel.SetActive(false);

            RefreshTypeToggles();
            SetTypeTogglesInteractable(true);
            goButton.interactable = true;
            SyncSelectedFromToggles();

            RefreshTypePresentation(selectedTowerType);
            RefreshCityInfo();
            RefreshPlacementIndicators();
        }

        void RefreshTypePresentation(TowerType towerType)
        {
            miniature.sprite = visuals.Miniature(towerType);
            towerTypeText.text = visuals.Description(towerType);
        }

        void RefreshCityInfo()
        {
            cityInfoText.text = string.Format(cityInfoFormat, city.Population);
        }

        void ApplyCityVisuals()
        {
            for (int i = 0; i < cells.Length; i++)
            {
                GridCell cell = cells[i];
                CityState.Lot lot = city.GetLot(cell.X, cell.Y);
                if (lot.Occupied)
                    cell.SetTowerSquare(visuals.GridSquare(lot.TowerType));
                else
                    cell.ClearTowerSquare();
            }
        }

        void RefreshTypeToggles()
        {
            SetToggleUnlocked(residentialToggle, rules.IsUnlocked(TowerType.Residential, city.Population));
            SetToggleUnlocked(commercialToggle, rules.IsUnlocked(TowerType.Commercial, city.Population));
            SetToggleUnlocked(officeToggle, rules.IsUnlocked(TowerType.Office, city.Population));
            SetToggleUnlocked(luxuryToggle, rules.IsUnlocked(TowerType.Luxury, city.Population));
        }

        void SetTypeTogglesInteractable(bool interactable)
        {
            residentialToggle.interactable = interactable && rules.IsUnlocked(TowerType.Residential, city.Population);
            commercialToggle.interactable = interactable && rules.IsUnlocked(TowerType.Commercial, city.Population);
            officeToggle.interactable = interactable && rules.IsUnlocked(TowerType.Office, city.Population);
            luxuryToggle.interactable = interactable && rules.IsUnlocked(TowerType.Luxury, city.Population);
        }

        void RefreshPlacementIndicators()
        {
            TowerType type = placing && BuildSession.Instance.HasPendingTower
                ? BuildSession.Instance.PendingTower.TowerType
                : selectedTowerType;
            Color color = ColorFor(type);

            for (int i = 0; i < cells.Length; i++)
            {
                GridCell cell = cells[i];
                if (rules.CanPlace(type, cell.X, cell.Y, city))
                    cell.OptionIndicator.Show(color);
                else
                    cell.OptionIndicator.Hide();
            }
        }

        Color ColorFor(TowerType towerType) => ToggleFor(towerType).graphic.color;

        Toggle ToggleFor(TowerType towerType) => towerType switch
        {
            TowerType.Residential => residentialToggle,
            TowerType.Commercial => commercialToggle,
            TowerType.Office => officeToggle,
            TowerType.Luxury => luxuryToggle,
            _ => residentialToggle
        };

        void SetTypeToggleOn(TowerType towerType)
        {
            residentialToggle.SetIsOnWithoutNotify(towerType == TowerType.Residential);
            commercialToggle.SetIsOnWithoutNotify(towerType == TowerType.Commercial);
            officeToggle.SetIsOnWithoutNotify(towerType == TowerType.Office);
            luxuryToggle.SetIsOnWithoutNotify(towerType == TowerType.Luxury);
        }

        static void SetToggleUnlocked(Toggle toggle, bool unlocked)
        {
            toggle.interactable = unlocked;
            if (!unlocked && toggle.isOn)
                toggle.SetIsOnWithoutNotify(false);
        }

        void SyncSelectedFromToggles()
        {
            if (luxuryToggle.isOn)
                selectedTowerType = TowerType.Luxury;
            else if (officeToggle.isOn)
                selectedTowerType = TowerType.Office;
            else if (commercialToggle.isOn)
                selectedTowerType = TowerType.Commercial;
            else
                selectedTowerType = TowerType.Residential;
        }
    }
}

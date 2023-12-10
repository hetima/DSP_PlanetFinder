using System;
using System.Text;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace PlanetFinderMod
{
    public class UIPlanetFinderListItem : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        public static Sprite circleSprite;
        public static Color valueTextNormalColor;
        public static Color valueTextMiningColor;

        public UIPlanetFinderWindow window;
        public bool firstRefleshed;
        public PlanetListData listData;
        public PlanetData planetData;
        public StarData starData; //null if target is planet
        public string distanceStr;
        public Color planetColor;

        public string planetName
        {
            get
            {
                return nameText.text;
            }
            set
            {
                nameText.text = value;
            }
        }

        public int itemId
                {
            get
            {
                return _itemId;
            }
            set
            {
                if (_itemId != value)
                {
                    _itemId = value;
                    sb = (_itemId == 7) ? window.sbOil : window.sb;
                    veinIcon.sprite = window.itemSelection.GetIconForItemId(_itemId);
                    veinIcon.enabled = (veinIcon.sprite != null);
                }
            }
        }

        private int _itemId;

        [SerializeField]
        public Text nameText;

        [SerializeField]
        public Text valueText;

        [SerializeField]
        public Text valueSketchText;

        [SerializeField]
        public Image labelIcon;

        [SerializeField]
        public Image veinIcon;

        [SerializeField]
        public Sprite iconHide;

        [SerializeField]
        public UIButton iconButton;

        [SerializeField]
        public UIButton locateBtn;

        [SerializeField]
        public GameObject baseObj;

        public StringBuilder sb;

        public static UIPlanetFinderListItem CreateListViewPrefab()
        {
            RectTransform rect;
            UIPlanetFinderListItem item = Util.CreateGameObject<UIPlanetFinderListItem>("list-item", 600f, 24f);
            Image bg = Util.CreateGameObject<Image>("list-item", 600f, 24f);
            bg.gameObject.SetActive(true);
            RectTransform baseTrans = Util.NormalizeRectWithTopLeft(bg, 0f, 0f, item.transform);
            item.baseObj = bg.gameObject;

            bg.color = new Color(0.2f, 0.2f, 0.2f, 0.1f);
            (item.transform as RectTransform).sizeDelta = new Vector2(600f, 24f);
            baseTrans.sizeDelta = new Vector2(600f, 24f);
            baseTrans.localScale = Vector3.one;

            UIResAmountEntry src =GameObject.Instantiate<UIResAmountEntry>(UIRoot.instance.uiGame.planetDetail.entryPrafab, baseTrans);
            src.gameObject.SetActive(true);

            float rightPadding = 0f;
            float leftPadding = 22f;

            //locate button
            UIButton btn = Util.MakeIconButtonB(Util.astroIndicatorIcon, 22);
            if (btn != null)
            {
                btn.gameObject.name = "locate-btn";
                rect = Util.NormalizeRectWithTopLeft(btn, 538f - rightPadding, 3f, baseTrans);

                //btn.onClick +=
                btn.tips.tipTitle = "Locate Planet".Translate();
                btn.tips.tipText = "Show the planet on the starmap.".Translate();
                btn.tips.corner = 3;
                btn.tips.offset = new Vector2(18f, -20f);
                item.locateBtn = btn;
            }

            //nameText
            item.nameText = src.labelText;
            item.nameText.text = "";
            item.nameText.fontSize = 16;
            item.nameText.rectTransform.anchoredPosition = new Vector2(8f + leftPadding, 0f);

            //valueText
            item.valueText = src.valueText;
            item.valueText.text = "";
            item.valueText.color = item.nameText.color;
            item.valueText.supportRichText = true;
            rect = Util.NormalizeRectWithTopLeft(item.valueText, 380f - rightPadding - leftPadding, 2f);
            rect.sizeDelta = new Vector2(100f, 24f);
            valueTextNormalColor = item.valueText.color;
            valueTextMiningColor = new Color(0.1f, 0.7f, 0.8f, valueTextNormalColor.a);

            //valueSketchText
            item.valueSketchText = GameObject.Instantiate<Text>(item.valueText, item.valueText.transform.parent);
            item.valueSketchText.gameObject.name = "valueSketchText";
            item.valueSketchText.alignment = TextAnchor.MiddleLeft;
            rect = Util.NormalizeRectWithTopLeft(item.valueSketchText, 504f - rightPadding - leftPadding, 2f);
            rect.sizeDelta = new Vector2(24f, 24f);

            //veinIcon
            item.veinIcon = src.iconImage;
            if (item.veinIcon != null)
            {
                rect = Util.NormalizeRectWithTopLeft(item.veinIcon, 482f - rightPadding - leftPadding, 2f);
                item.veinIcon.enabled = false;

                //labelIcon
                item.labelIcon = GameObject.Instantiate<Image>(item.veinIcon, baseTrans);
                item.labelIcon.gameObject.name = "labelIcon";
                rect = Util.NormalizeRectWithTopLeft(item.labelIcon, 16f, 12f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(24f, 24f);
                rect.localScale = new Vector3(0.3f, 0.3f, 1f);

                item.labelIcon.material = null; //これのせいでめっちゃ光る
                UIStationWindow stationWindow = UIRoot.instance.uiGame.stationWindow;
                circleSprite = stationWindow.storageUIPrefab.transform.Find("storage-icon-empty/white")?.GetComponent<Image>()?.sprite;
                item.labelIcon.sprite = circleSprite;
                item.labelIcon.enabled = true;
            }

            item.iconHide = src.iconHide;
            item.iconButton = src.iconButton;
            //GameObject.Destroy(src.valueText.gameObject);
            GameObject.Destroy(src);
            Util.NormalizeRectWithTopLeft(item, 0f, 0f);
            return item;
        }

        void Start()
        {
            locateBtn.onClick += OnLocateButtonClick;
            locateBtn.gameObject.SetActive(false);
        }

        public void SetUpDisplayName()
        {
            if (starData != null)
            {
                planetName = starData.displayName + distanceStr;
            }
            else if (planetData != null)
            {
                string prefix = "";
                if (PLFN.showPrefix.Value)
                {
                    if (planetData.type == EPlanetType.Gas)
                    {
                        prefix += PLFN.gasGiantPrefix.Value;
                    }
                    if ((planetData.singularity & EPlanetSingularity.TidalLocked) != EPlanetSingularity.None)
                    {
                        prefix += PLFN.tidalLockedPrefix.Value;
                    }
                    if (!string.IsNullOrEmpty(prefix))
                    {
                        prefix += " ";
                    }
                }
                planetName = prefix + planetData.displayName + distanceStr;
            }
        }

        public void SetUpPlanetColor()
        {
            if (planetData != null)
            {
                ThemeProto themeProto = LDB.themes.Select(planetData.theme);
                if (themeProto != null && themeProto.thumbMat != null)
                {
                    Material mat = themeProto.thumbMat[planetData.style % themeProto.thumbMat.Length];
                    Color c = mat.GetColor("_Color");
                    if (c != null)
                    {
                        if (c.a < 0.5f)
                        {
                            c.a = 0.5f;
                        }
                        float shine = Mathf.Max(new float[] { c.r, c.g, c.b });
                        if (shine > 0.6f)
                        {
                            c.a = 1.6f - shine;
                        }
                        c.a /= 1.4f;
                        planetColor = c;
                    }
                    else
                    {
                        planetColor = new Color(1f, 1f, 1f, 0.6f);
                    }
                }
            }
            else if (starData != null)
            {
                UIVirtualStarmap starmap = UIRoot.instance.galaxySelect.starmap;
                planetColor = starmap.starColors.Evaluate(starData.color);
                if (starData.type == EStarType.NeutronStar)
                {
                    planetColor = starmap.neutronStarColor;
                }
                else if (starData.type == EStarType.WhiteDwarf)
                {
                    planetColor = starmap.whiteDwarfColor;
                }
                else if (starData.type == EStarType.BlackHole)
                {
                    planetColor = starmap.blackholeColor;
                }
                planetColor.a = 0.8f;
            }
            else
            {
                planetColor = new Color(1f, 1f, 1f, 0.6f);
            }
        }

        public void Init(in PlanetListData d, UIPlanetFinderWindow window_)
        {
            listData = d;
            firstRefleshed = false;
            window = window_;
            distanceStr = d.distanceStr;
            itemId = d.itemId;
            planetData = d.planetData;
            starData = d.starData;
            SetUpDisplayName();
            SetUpPlanetColor();
            labelIcon.sprite = null;
            valueText.color = valueTextNormalColor;

            int planetId = 0;
            if (planetData != null)
            {
                planetId = planetData.id;
            }
            else if (starData != null)
            {
                planetId = starData.id;
            }

            if (planetId > 0 && PLFN.aDSPStarMapMemoIntg.canGetSignalIconId && PLFN.integrationWithDSPStarMapMemo.Value)
            {
                int sign = PLFN.aDSPStarMapMemoIntg.GetSignalIconId(planetId);
                if (sign != 0)
                {
                    labelIcon.sprite = LDB.signals.IconSprite(sign);
                    labelIcon.rectTransform.localScale = Vector3.one;
                    labelIcon.color = Color.white;
                }
            }

            if (labelIcon.sprite == null)
            {
                labelIcon.color = planetColor;
                labelIcon.sprite = circleSprite;
                labelIcon.rectTransform.localScale = new Vector3(0.3f, 0.3f, 1f);
            }
        }

        private void OnLocateButtonClick(int obj)
        {
            if (planetData != null) {
                PLFN.LocatePlanet(planetData.id);
            }
            else if (starData != null)
            {
                PLFN.LocatePlanet(0, starData.id);
            }
        }

        public static int veinCount = 15;

        public long TargetItemAmount()
        {
            long result = 0;
            if (planetData != null)
            {
                return TargetItemAmountForPlanet(listData);
            }
            else if (starData != null)
            {
                return TargetItemAmountForStar(listData);
            }
            return result;
        }
        public long TargetItemAmountForStar(PlanetListData ld)
        {
            long amount = 0;
            if (ld.starData != null)
            {
                if (itemId <= veinCount)
                {
                    foreach (PlanetListData item in ld.planetList)
                    {
                        amount += TargetItemAmountForPlanet(item);
                    }
                }
                else
                {
                    return 0;
                }
            }
            return amount;
        }

        public long TargetItemAmountForPlanet(PlanetListData ld)
        {
            PlanetData pd = ld.planetData;
            long amount = 0;
            if (pd != null)
            {
                if (pd.type == EPlanetType.Gas)
                {
                    return 0;
                }

                if (pd.waterItemId >= 1000 && pd.waterItemId == itemId)
                {
                    return 0;
                }
                if (itemId <= veinCount)
                {
                    return ld.ItemCount(itemId);
                }
            }
            return amount;
        }

        public int TargetItemAmountSketch()
        {
            if (planetData != null)
            {
                return TargetItemAmountSketchForPlanet(listData);
            }
            else if (starData != null)
            {
                return TargetItemAmountSketchForStar(listData);
            }
            return 0;
        }
        public int TargetItemAmountSketchForStar(PlanetListData ld)
        {
            int amount = 0;
            if (ld.starData != null)
            {
                if (itemId <= veinCount)
                {
                    foreach (PlanetListData item in ld.planetList)
                    {
                        amount += TargetItemAmountSketchForPlanet(item);
                    }
                }
                else
                {
                    return 0;
                }
            }
            return amount;
        }

        public int TargetItemAmountSketchForPlanet(PlanetListData ld)
        {
            PlanetData pd = ld.planetData;
            int amount = 0;
            if (pd != null) {
                if (pd.type == EPlanetType.Gas)
                {
                    return 0;
                }

                if (pd.waterItemId >= 1000 && pd.waterItemId == itemId)
                {
                    return 0;
                }
                if (itemId <= veinCount)
                {
                    return ld.ItemCountSketch(itemId);
                } 
            }
            return amount;
        }

        public void Update()
        {
            if (planetData != null || starData != null)
            {
                int step = Time.frameCount % 30;
                if (step == 0 || window.needsReflesh)
                {
                    RefreshValues();
                }
            }
        }

        public void RefreshValues()
        {
            if (!baseObj.activeSelf)
            {
                return;
            }
            //else if (onlyNewlyEmerged)
            //{
            //    return true;
            //}
            //if (!shown)
            //{
            //    return true;
            //}
            int OldItemId = _itemId;
            valueText.color = valueTextNormalColor;
            itemId = window.targetItemId;
            if (itemId != 0)
            {
                if (!GameMain.data.gameDesc.isInfiniteResource)
                {
                    long amount = TargetItemAmount();
                    if (amount > 0)
                    {
                        if (itemId == 7)
                        {
                            double num3 = (double)amount * (double)VeinData.oilSpeedMultiplier;
                            StringBuilderUtility.WritePositiveFloat(sb, 0, 8, (float)num3, 2, ' ');
                        }
                        else
                        {
                            if (amount < 1_000_000_000L)
                            {
                                StringBuilderUtility.WriteCommaULong(sb, 0, 16, (ulong)amount, 1, ' ');
                            }
                            else
                            {
                                StringBuilderUtility.WriteKMG(sb, 15, amount, false);
                            }
                        }
                        string newVlue = sb.ToString();
                        if (firstRefleshed && OldItemId == _itemId)
                        {
                            if (newVlue != valueText.text)
                            {
                                valueText.text = newVlue;
                                valueText.color = valueTextMiningColor;
                            }
                        }
                        else
                        {
                            valueText.text = newVlue;
                        }
                    }
                    else
                    {
                        valueText.text = "";
                    }
                }

                int amountSketch = TargetItemAmountSketch();
                if (amountSketch > 0)
                {
                    valueSketchText.text = "(" + amountSketch.ToString() + ")";
                }
                else
                {
                    valueSketchText.text = "";
                }
            }
            else if (PLFN.showPowerState.Value && planetData?.factory?.powerSystem != null)
            {
                valueText.text = PowerState(out int networkCount);
                if (networkCount > 1)
                {
                    valueSketchText.text = "(" + networkCount.ToString() + ")";
                }
                else
                {
                    valueSketchText.text = "";
                }
            }
            else
            {
                valueText.text = "";
                valueSketchText.text = "";
            }

            firstRefleshed = true;
        }

        public string PowerState(out int networkCount)
        {
            string result = "";
            networkCount = 0;
            if (planetData == null)
            {
                return result;
            }

            long energyCapacity = 0L;
            long energyRequired = 0L;
            long energyX = 0L;
            PowerSystem powerSystem = planetData.factory.powerSystem;
            for (int i = 1; i < powerSystem.netCursor; i++)
            {
                PowerNetwork powerNetwork = powerSystem.netPool[i];
                if (powerNetwork != null && powerNetwork.id == i)
                {
                    networkCount++;
                    energyCapacity += powerNetwork.energyCapacity;
                    energyRequired += powerNetwork.energyRequired;
                    if (powerNetwork.energyExchanged < 0L)
                    {
                        energyX -= powerNetwork.energyExchanged;
                    }
                }
            }
            if (energyCapacity > 0L || energyX > 0L)
            {
                StringBuilderUtility.WriteKMG(window.sbWatt, 8, energyRequired * 60L, false);
                result = window.sbWatt.ToString();
                StringBuilderUtility.WriteKMG(window.sbWatt, 8, energyCapacity * 60L, false);
                result += " / " + window.sbWatt.ToString().Trim();
                if (energyX > 0L)
                {
                    StringBuilderUtility.WriteKMG(window.sbWatt, 8, energyX * 60L, false);
                    result += " + " + window.sbWatt.ToString().Trim();
                }
                double ratio = (double)energyRequired / (double)(energyCapacity + energyX);
                if (ratio > 0.9f)
                {
                    result += " (" + ratio.ToString("P1") + ")";
                    if (energyX <= 0L)
                    {
                        if (ratio > 0.99f)
                        {
                            result = "<color=#FF404D99>" + result + "</color>";
                        }
                        else
                        {
                            result = "<color=#DB883E85>" + result + "</color>";
                        }
                    }
                }
            }
            else
            {
                result = "--";
            }
            return result;
        }

        public bool disableUIAction = false;
        public void OnPointerClick(PointerEventData eventData)
        {
            if (!disableUIAction && eventData.button == PointerEventData.InputButton.Right)
            {
                window.ShowMenu(this);
            }
        }

        public void OnPointerEnter(PointerEventData _eventData)
        {
            if (!disableUIAction)
            {
                locateBtn.gameObject.SetActive(true);
            }
        }

        public void OnPointerExit(PointerEventData _eventData)
        {
            if (!disableUIAction)
            {
                locateBtn.gameObject.SetActive(false);
            }
        }

        public void LockAppearance()
        {
            Button b = GetComponent<Button>();
            if (b != null)
            {
                b.enabled = false;
            }
        }

        public void UnlockAppearance()
        {
            Button b = GetComponent<Button>();
            if (b != null)
            {
                b.enabled = true;
            }
        }
    }
}

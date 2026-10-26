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
                }
            }
        }

        private int _itemId;

        [SerializeField]
        public Text nameText;

        [SerializeField]
        public Text distanceText;

        [SerializeField]
        public Text prefixLabel;

        [SerializeField]
        public Text rateText;

        [SerializeField]
        public Text rateUnitText;

        [SerializeField]
        public Text valueText;

        [SerializeField]
        public Text valueSketchText;

        [SerializeField]
        public Text groupText;

        [SerializeField]
        public Image labelIcon;

        [SerializeField]
        public Sprite iconHide;

        [SerializeField]
        public UIButton iconButton;

        [SerializeField]
        public UIButton viewBtn;

        [SerializeField]
        public UIButton locateBtn;

        [SerializeField]
        public GameObject baseObj;

        public StringBuilder sb;

        public static UIPlanetFinderListItem CreateListViewPrefab()
        {
            RectTransform rect;
            UIPlanetFinderListItem item = Util.CreateGameObject<UIPlanetFinderListItem>("list-item", PLFN.LIST_ITEM_WIDTH, 24f);
            Image bg = Util.CreateGameObject<Image>("list-item", PLFN.LIST_ITEM_WIDTH, 24f);
            bg.gameObject.SetActive(true);
            RectTransform baseTrans = Util.NormalizeRectWithTopLeft(bg, 0f, 0f, item.transform);
            item.baseObj = bg.gameObject;

            bg.color = new Color(0.2f, 0.2f, 0.2f, 0.1f);
            (item.transform as RectTransform)!.sizeDelta = new Vector2(PLFN.LIST_ITEM_WIDTH, 24f);
            baseTrans.sizeDelta = new Vector2(PLFN.LIST_ITEM_WIDTH, 24f);
            baseTrans.localScale = Vector3.one;

            UIPlanetDetail planetDetail = UIRoot.instance.uiGame.planetDetail;
            UIResAmountEntry src = GameObject.Instantiate<UIResAmountEntry>(planetDetail.entryPrafab, baseTrans);
            src.gameObject.SetActive(true);

            const float leftPadding = 22f;
            //the row buttons sit at the right end; they are pushed right so that the number block,
            //which was shifted right as well, does not run into the view button
            const float viewBtnX = 668f;
            const float locateBtnX = 694f;
            // view button
            UIButton viewBtn = Util.MakeIconButtonB(Util.viewPlanetIcon, 22);
            if (viewBtn != null)
            {
                viewBtn.gameObject.name = "view-btn";
                rect = Util.NormalizeRectWithTopLeft(viewBtn, viewBtnX, 3f, baseTrans);

                //btn.onClick +=
                viewBtn.tips.tipTitle = "View Planet".Translate();
                viewBtn.tips.tipText = "Show the planet on the starmap.".Translate();
                viewBtn.tips.corner = 3;
                viewBtn.tips.offset = new Vector2(18f, -20f);
                item.viewBtn = viewBtn;
            }

            //locate button
            //it sits to the right of the view button, shifted right together with the buttons so that
            //it does not overlap the number block
            UIButton locateBtn = Util.MakeIconButtonB(Util.astroIndicatorIcon, 22);
            if (locateBtn != null)
            {
                locateBtn.gameObject.name = "locate-btn";
                rect = Util.NormalizeRectWithTopLeft(locateBtn, locateBtnX, 3f, baseTrans);

                //btn.onClick +=
                locateBtn.tips.tipTitle = "Locate Planet".Translate();
                locateBtn.tips.tipText = "Locate the planet.".Translate();
                locateBtn.tips.corner = 3;
                locateBtn.tips.offset = new Vector2(18f, -20f);
                item.locateBtn = locateBtn;
            }

            //nameText
            item.nameText = src.labelText;
            item.nameText.text = "";
            item.nameText.iconMapping = planetDetail.nameInput.textComponent.iconMapping;
            item.nameText.fontSize = 16;
            item.nameText.rectTransform.anchoredPosition = new Vector2(8f + leftPadding, 0f);

            //Layout of the numbers on the right side of a row, in pixels from the left edge of the row.
            //The row text font is DIN, about 7px per character at 14pt (measured in the running game).
            //Each box is exactly as wide as the longest value it can show, and boxes are separated by a
            //small fixed buffer, so the three texts can never overlap whatever the numbers grow to.
            const float charWidth = 7f;
            //buffer between two neighbouring numbers
            const float numberGap = 4f;
            //the amount shows at most a 10 digit value
            const float amountDigits = 10f;
            //"(A/B)" is right aligned here; a vein count needs at most "(999/999)" = 9 characters
            //the three numbers can be shifted as a block; the vein icon is positioned independently and
            //keeps its own position, independent of the number block.
            //66 = the power text right edge moves from 66px to 132px away from the distance right edge,
            //so that the long power state string no longer runs into the distance text.
            //110 = another 44px so that even the longest power string (with the "(93.7%)" suffix)
            //clears the distance text completely
            const float blockShift = 110f;
            const float veinCountRight = 511f + blockShift;
            const float veinCountWidth = 9f * charWidth;
            //"[A/B]" is right aligned here; a vein group count needs at most "[999/999]" = 9 characters
            const float groupRight = veinCountRight - veinCountWidth - numberGap;
            const float groupWidth = 9f * charWidth;
            //the amount is right aligned here
            const float amountRight = groupRight - groupWidth - numberGap;
            const float amountWidth = amountDigits * charWidth;
            //Vertical placement inside a row, which is 24px high.
            //These are the original positions: the three number boxes start at 2, the distance and prefix
            //boxes at 0, and the icons are anchored at their centre 12px from the row top.
            //Text boxes start at 2px and the distance/prefix boxes at 0px.
            const float numberTop = 2f;
            const float distanceTop = 0f;
            const float iconTop = 12f;

            //valueText
            item.valueText = src.valueText;
            item.valueText.text = "";
            item.valueText.color = item.nameText.color;
            item.valueText.supportRichText = true;
            //the clone inherits the prefab's stretch anchors, so pin it to an exact size
            rect = Util.SetFixedSizeAtTopLeft(item.valueText, amountRight - amountWidth, numberTop, amountWidth, 24f);
            item.valueText.alignment = TextAnchor.MiddleRight;
            item.valueText.horizontalOverflow = HorizontalWrapMode.Overflow;
            item.valueText.verticalOverflow = VerticalWrapMode.Overflow;
            valueTextNormalColor = item.valueText.color;
            valueTextMiningColor = new Color(0.1f, 0.7f, 0.8f, valueTextNormalColor.a);

            //groupText: number of vein groups already being mined / total vein groups, shown as [A/B]
            item.groupText = GameObject.Instantiate<Text>(item.valueText, item.valueText.transform.parent);
            item.groupText.gameObject.name = "groupText";
            item.groupText.alignment = TextAnchor.MiddleRight;
            //same font size as the amount text
            item.groupText.fontSize = 14;
            rect = Util.SetFixedSizeAtTopLeft(item.groupText, groupRight - groupWidth, numberTop, groupWidth, 24f);
            item.groupText.horizontalOverflow = HorizontalWrapMode.Overflow;
            item.groupText.verticalOverflow = VerticalWrapMode.Overflow;

            //valueSketchText
            item.valueSketchText = GameObject.Instantiate<Text>(item.valueText, item.valueText.transform.parent);
            item.valueSketchText.gameObject.name = "valueSketchText";
            item.valueSketchText.alignment = TextAnchor.MiddleRight;
            //same font size as the amount text
            item.valueSketchText.fontSize = 14;
            rect = Util.SetFixedSizeAtTopLeft(item.valueSketchText, veinCountRight - veinCountWidth, numberTop, veinCountWidth, 24f);
            item.valueSketchText.horizontalOverflow = HorizontalWrapMode.Overflow;
            item.valueSketchText.verticalOverflow = VerticalWrapMode.Overflow;

            //rateText / rateUnitText: for resources extracted from a gas giant or ice giant the amount
            //field shows the extraction speed. The number is right aligned and the unit starts exactly
            //at the number's right edge, so the last digit always touches the unit.
            item.rateText = GameObject.Instantiate<Text>(item.valueText, item.valueText.transform.parent);
            item.rateText.gameObject.name = "rateText";
            item.rateText.alignment = TextAnchor.MiddleRight;
            item.rateText.fontSize = 14;
            rect = Util.SetFixedSizeAtTopLeft(item.rateText, amountRight - 15f * charWidth, numberTop, 15f * charWidth, 24f);
            item.rateText.horizontalOverflow = HorizontalWrapMode.Overflow;
            item.rateText.verticalOverflow = VerticalWrapMode.Overflow;

            item.rateUnitText = GameObject.Instantiate<Text>(item.valueText, item.valueText.transform.parent);
            item.rateUnitText.gameObject.name = "rateUnitText";
            item.rateUnitText.alignment = TextAnchor.MiddleLeft;
            item.rateUnitText.fontSize = 14;
            rect = Util.SetFixedSizeAtTopLeft(item.rateUnitText, amountRight, numberTop, 2f * charWidth, 24f);
            item.rateUnitText.horizontalOverflow = HorizontalWrapMode.Overflow;
            item.rateUnitText.verticalOverflow = VerticalWrapMode.Overflow;

            //the row does not show the selected resource icon, only the planet type dot is kept
            GameObject.Destroy(src.iconImage);

            //the game update added a "vein highlight" button to the resource entry (UIPlanetDetail.SetupVeinHighlightButton),
            //it shows up after the amount in this list too, so remove it together with its hover area.
            if (src.highlightButton != null)
            {
                GameObject.Destroy(src.highlightButton.gameObject);
            }
            if (src.highlightButtonHoverArea != null)
            {
                GameObject.Destroy(src.highlightButtonHoverArea.gameObject);
            }

            //distanceText: the distance from the current system, moved out of the name and placed at its
            //own fixed position, so that it is not dragged around by the number block
            item.distanceText = GameObject.Instantiate<Text>(item.nameText, item.nameText.transform.parent);
            item.distanceText.gameObject.name = "distanceText";
            item.distanceText.alignment = TextAnchor.MiddleRight;
            item.distanceText.color = item.nameText.color;
            const float distanceWidth = 10f * 7f;
            const float distanceRight = 311f;
            const float distanceShiftLeft = 5f;
            Util.NormalizeRectWithTopLeft(item.distanceText, distanceRight - distanceWidth - distanceShiftLeft, distanceTop);
            item.distanceText.rectTransform.sizeDelta = new Vector2(distanceWidth, 24f);
            item.distanceText.horizontalOverflow = HorizontalWrapMode.Overflow;
            item.distanceText.verticalOverflow = VerticalWrapMode.Overflow;

            //prefixLabel: the [GAS] / [TL] marks, taken out of the name and put left of the distance
            const float prefixWidth = 10f * 7f;
            //pushed 15px closer to the distance; a planet is either a gas giant or tidal locked, so the
            //label is 5 characters at most and the box has room for the maximum 10 without touching it
            const float prefixShiftRight = 25f;
            item.prefixLabel = GameObject.Instantiate<Text>(item.nameText, item.nameText.transform.parent);
            item.prefixLabel.gameObject.name = "prefixLabel";
            item.prefixLabel.alignment = TextAnchor.MiddleRight;
            item.prefixLabel.color = item.nameText.color;
            Util.NormalizeRectWithTopLeft(item.prefixLabel, distanceRight - distanceWidth - 7f - prefixWidth + prefixShiftRight, distanceTop);
            item.prefixLabel.rectTransform.sizeDelta = new Vector2(prefixWidth, 24f);
            item.prefixLabel.horizontalOverflow = HorizontalWrapMode.Overflow;
            item.prefixLabel.verticalOverflow = VerticalWrapMode.Overflow;

            //labelIcon
            item.labelIcon = Util.CreateGameObject<Image>("labelIcon", 24f, 24f); 
            item.labelIcon.transform.SetParent(item.nameText.transform.parent, false);
            item.labelIcon.gameObject.name = "labelIcon";
            rect = Util.NormalizeRectWithTopLeft(item.labelIcon, 16f, iconTop);
            rect.pivot = new Vector2(0.5f, 0.5f);
            item.labelIcon.material = null;

            UIStationWindow stationWindow = UIRoot.instance.uiGame.stationWindow;
            circleSprite = stationWindow.storageUIPrefab.transform.Find("storage-icon-empty/white")?.GetComponent<Image>()?.sprite;
            item.labelIcon.sprite = circleSprite;
            item.labelIcon.enabled = true;


            item.iconHide = src.iconHide;
            item.iconButton = src.iconButton;
            //GameObject.Destroy(src.valueText.gameObject);
            GameObject.Destroy(src);
            Util.NormalizeRectWithTopLeft(item, 0f, 0f);
            return item;
        }

        void Start()
        {
            viewBtn.onClick += OnViewButtonClick;
            viewBtn.gameObject.SetActive(false);
            if (locateBtn != null)
            {
                locateBtn.onClick += OnLocateButtonClick;
                locateBtn.gameObject.SetActive(false);
            }
        }

        //rows are recycled, so visibility of the row buttons has to follow the current setting every time
        private void ShowRowButtons(bool show)
        {
            viewBtn.gameObject.SetActive(show);
            if (locateBtn != null)
            {
                locateBtn.gameObject.SetActive(show && PLFN.showLocateButtonInList.Value);
            }
        }

        /// <summary>
        /// [GAS] / [TL] marks of this row, empty for a star system or when the prefix setting is off.
        /// </summary>
        public string PlanetTypePrefix()
        {
            if (planetData == null || !PLFN.showPrefix.Value)
            {
                return "";
            }
            string prefix = "";
            if (UIPlanetFinderWindow.IsGasGiant(planetData))
            {
                prefix += PLFN.gasGiantPrefix.Value;
            }
            if ((planetData.singularity & EPlanetSingularity.TidalLocked) != EPlanetSingularity.None)
            {
                prefix += PLFN.tidalLockedPrefix.Value;
            }
            return prefix;
        }

        public void SetUpDisplayName()
        {
            if (starData != null)
            {
                planetName = starData.displayName;
            }
            else if (planetData != null)
            {
                planetName = planetData.displayName;
            }
            //the [GAS] / [TL] marks are shown in their own field, left of the distance, not in the name
            if (prefixLabel != null)
            {
                prefixLabel.text = PlanetTypePrefix();
            }
            //the distance is shown in its own field, left of the vein icon, not behind the name
            if (distanceText != null)
            {
                //planets in the current system have no distance; show "--" like the power display does
                distanceText.text = string.IsNullOrEmpty(distanceStr) ? "--" : distanceStr.Trim();
                //the setting can be toggled while the game runs, and rows are recycled
                distanceText.fontSize = PLFN.distanceTextSmallFont.Value ? 14 : nameText.fontSize;
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

        void OnViewButtonClick(int obj)
        {
            PLFN.ViewPlanet(planetData, starData);
        }

        private void OnLocateButtonClick(int obj)
        {
            PLFN.LocatePlanet(planetData, starData);
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

        /// <summary>
        /// Vein group arrays to aggregate this row over: the row's own planet, or every planet of a star system.
        /// Entries are null when the veins of that planet are not available.
        /// </summary>
        public static List<VeinGroup[]> CollectVeinGroups(PlanetListData ld)
        {
            List<VeinGroup[]> result = new List<VeinGroup[]>();
            if (ld == null || ld.planetData != null)
            {
                //planet row: VeinGroups() dereferences planetData, so only call it when there is one
                if (ld != null && ld.planetData != null)
                {
                    result.Add(ld.VeinGroups());
                }
            }
            else if (ld.planetList != null)
            {
                //star row: a star system holds the sum of its planets
                foreach (PlanetListData planet in ld.planetList)
                {
                    if (planet?.planetData != null)
                    {
                        result.Add(planet.VeinGroups());
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// Vein counts of the target resource as "mined / total", summed over the row.
        /// Total is the same number the previous version showed in parentheses (sum of VeinGroup.count, which the
        /// mod scans for the whole galaxy). For a star row it is the sum over all of its planets.
        /// Mined means the vein has a mining machine attached (VeinData.minerCount). That can only be read while
        /// the planet factory is loaded, so planets whose data was released count as not mined.
        /// </summary>
        public void GetVeinUnitCountForPlanet(PlanetListData ld, out int mined, out int total)
        {
            mined = 0;
            total = 0;
            if (ld == null || itemId <= 0)
            {
                return;
            }
            foreach (VeinGroup[] veinGroups in CollectVeinGroups(ld))
            {
                if (veinGroups == null)
                {
                    continue;
                }
                for (int i = 1; i < veinGroups.Length; i++)
                {
                    if ((int)veinGroups[i].type == itemId)
                    {
                        total += veinGroups[i].count;
                    }
                }
            }
        }

        /// <summary>
        /// Vein group count of the target resource as "mined / total", summed over the row.
        /// A group counts as mined when any vein of the group has a mining machine attached.
        /// </summary>
        public void GetVeinGroupCountForPlanet(PlanetListData ld, out int mined, out int total)
        {
            mined = 0;
            total = 0;
            if (ld == null || itemId <= 0)
            {
                return;
            }
            List<VeinGroup[]> groupArrays = CollectVeinGroups(ld);
            foreach (VeinGroup[] veinGroups in groupArrays)
            {
                if (veinGroups == null)
                {
                    continue;
                }
                for (int i = 1; i < veinGroups.Length; i++)
                {
                    if ((int)veinGroups[i].type == itemId)
                    {
                        total++;
                    }
                }
            }
            if (total <= 0)
            {
                return;
            }
            //same order as CollectVeinGroups, so the matching planet is found by index
            List<PlanetData> planets = CollectPlanets(ld);
            for (int a = 0; a < groupArrays.Count; a++)
            {
                VeinGroup[] veinGroups = groupArrays[a];
                if (veinGroups == null || a >= planets.Count)
                {
                    continue;
                }
                PlanetFactory factory = planets[a]?.factory;
                VeinData[] veinPool = factory?.veinPool;
                if (factory == null || veinPool == null)
                {
                    continue;
                }
                int groupCount = veinGroups.Length;
                bool[] minedGroup = new bool[groupCount];
                int cursor = factory.veinCursor;
                if (cursor > veinPool.Length)
                {
                    cursor = veinPool.Length;
                }
                for (int i = 1; i < cursor; i++)
                {
                    VeinData vein = veinPool[i];
                    if (vein.id == 0 || vein.minerCount <= 0)
                    {
                        continue;
                    }
                    int groupIndex = vein.groupIndex;
                    if (groupIndex > 0 && groupIndex < groupCount)
                    {
                        minedGroup[groupIndex] = true;
                    }
                }
                for (int i = 1; i < veinGroups.Length; i++)
                {
                    if ((int)veinGroups[i].type == itemId && minedGroup[i])
                    {
                        mined++;
                    }
                }
            }
        }

        /// <summary>
        /// Planets this row aggregates over, in the same order as CollectVeinGroups: the row's own planet,
        /// or every planet of a star system.
        /// </summary>
        public static List<PlanetData> CollectPlanets(PlanetListData ld)
        {
            List<PlanetData> result = new List<PlanetData>();
            if (ld == null)
            {
                return result;
            }
            if (ld.planetData != null)
            {
                result.Add(ld.planetData);
            }
            else if (ld.planetList != null)
            {
                foreach (PlanetListData planet in ld.planetList)
                {
                    result.Add(planet?.planetData);
                }
            }
            return result;
        }

        /// <summary>
        /// Extraction speed of the target resource as a gas giant resource, in units per second.
        /// PlanetData.gasSpeeds is exactly what the game adds to the extraction progress per second
        /// (see PlayerAction_Mine.GameTick), so it is the per second yield.
        /// Gas giant resources have no vein reserves, which is why the amount column is otherwise empty.
        /// A star row sums the speed of all of its planets.
        /// </summary>
        public float TargetGasExtractSpeed()
        {
            float speed = 0f;
            foreach (PlanetData pd in CollectPlanets(listData))
            {
                speed += GasExtractSpeedForPlanet(pd);
            }
            return speed;
        }

        public float GasExtractSpeedForPlanet(PlanetData pd)
        {
            if (pd == null || pd.gasItems == null || pd.gasSpeeds == null)
            {
                return 0f;
            }
            int count = pd.gasItems.Length;
            if (pd.gasSpeeds.Length < count)
            {
                count = pd.gasSpeeds.Length;
            }
            for (int i = 0; i < count; i++)
            {
                if (pd.gasItems[i] == itemId)
                {
                    return pd.gasSpeeds[i];
                }
            }
            return 0f;
        }

        private void ClearRateText()
        {
            rateText.text = "";
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
                    //a resource extracted from a gas giant or ice giant (hydrogen, deuterium, fire ice, ...)
                    //has no vein reserve on that planet, so show its extraction speed instead, the same way
                    //crude oil shows a rate. This takes priority over a vein reserve: a planet can have both
                    //a solid fire ice vein and fire ice in its atmosphere.
                    float gasSpeed = TargetGasExtractSpeed();
                    long amount = gasSpeed > 0f ? 0L : TargetItemAmount();
                    if (gasSpeed > 0f)
                    {
                        //4 decimals, and the unit is a separate right aligned field so that the last
                        //digit of the number always touches the unit
                        StringBuilderUtility.WritePositiveFloat(sb, 0, 16, gasSpeed, 4, ' ');
                        rateText.text = sb.ToString().Trim();
                        valueText.text = "";
                    }
                    else if (amount > 0)
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
                                StringBuilderUtility.WriteKMG(sb, 15, amount, false, '\u2009', ' ');
                            }
                        }
                        //StringBuilderUtility pads the value to a fixed field with leading spaces.
                        //They are invisible because the text is right aligned, but they make the string
                        //much wider than the number itself and push it over the neighbouring texts.
                        string newVlue = sb.ToString().TrimStart(' ');
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
                        ClearRateText();
                    }
                    else
                    {
                        valueText.text = "";
                    }
                }

                //vein count as "(mined/total)"; cyan when anything is being mined there, default grey otherwise
                GetVeinUnitCountForPlanet(listData, out int veinMined, out int veinTotal);
                if (veinTotal > 0)
                {
                    valueSketchText.text = "(" + veinMined.ToString() + "/" + veinTotal.ToString() + ")";
                    valueSketchText.color = veinMined > 0 ? valueTextMiningColor : valueTextNormalColor;
                }
                else
                {
                    valueSketchText.text = "";
                }

                //vein group count as "[mined/total]", placed between the amount and the vein count
                GetVeinGroupCountForPlanet(listData, out int groupMined, out int groupTotal);
                if (groupTotal > 0)
                {
                    groupText.text = "[" + groupMined.ToString() + "/" + groupTotal.ToString() + "]";
                    groupText.color = groupMined > 0 ? valueTextMiningColor : valueTextNormalColor;
                }
                else
                {
                    groupText.text = "";
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
                groupText.text = "";
                ClearRateText();
            }
            else
            {
                valueText.text = "";
                valueSketchText.text = "";
                groupText.text = "";
                ClearRateText();
            }

            //the rate fields are only used for gas giant resources; the unit is a separate field so that
            //the last digit of the number always ends exactly where the unit begins
            rateUnitText.text = (rateText.text.Length > 0) ? "/s" : "";

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
                StringBuilderUtility.WriteKMGPower(window.sbWatt, 8, energyRequired * 60L, false, '\u2009', ' ');
                StringBuilderUtility.WriteKMGPower(window.sbWatt2, 8, energyCapacity * 60L, false, '\u2009', ' ');
                result = window.sbWatt.ToString() + " / " + window.sbWatt2.ToString().Trim();
                if (energyX > 0L)
                {
                    StringBuilderUtility.WriteKMGPower(window.sbWatt3, 8, energyX * 60L, false, '\u2009', ' ');
                    result += " + " + window.sbWatt3.ToString().Trim();
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
                ShowRowButtons(true);
            }
        }

        public void OnPointerExit(PointerEventData _eventData)
        {
            if (!disableUIAction)
            {
                ShowRowButtons(false);
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

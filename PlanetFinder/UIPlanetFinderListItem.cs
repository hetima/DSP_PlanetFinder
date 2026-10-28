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
        public static Color gasRateHighlightColor;
        //deuterium item id (1120 hydrogen, 1121 deuterium, 1011 fire ice)
        private const int DeuteriumItemId = 1121;
        private const float HighGasSpeedThreshold = 0.1f;
        //orbital collector prototype id (0x839); the game's ACH_OrbitalCollectorCount uses the same
        private const short OrbitalCollectorProtoId = 2105;

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
        public Text valueText;

        [SerializeField]
        public Text gasRateText;

        [SerializeField]
        public Text valueSketchText;

        [SerializeField]
        public Text groupText;

        [SerializeField]
        public Image labelIcon;

        [SerializeField]
        public Image iconHalo;


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
            const float iconTop = 12f;        //类型色点中心（轴心 0.5,0.5）

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
            //deuterium with a high extraction speed, a deep green that reads well on the dark row
            gasRateHighlightColor = new Color(0.30f, 0.82f, 0.42f, valueTextNormalColor.a);

            //gasRateText: for a gas resource the extraction speed is shown in the (A/B) slot.
            //The game's texts carry a shadow/outline that looks like a glow on bright colours, so it is
            //removed from this single field only.
            item.gasRateText = GameObject.Instantiate<Text>(item.valueText, item.valueText.transform.parent);
            item.gasRateText.gameObject.name = "gasRateText";
            item.gasRateText.alignment = TextAnchor.MiddleRight;
            item.gasRateText.fontSize = 14;
            rect = Util.SetFixedSizeAtTopLeft(item.gasRateText, veinCountRight - veinCountWidth, numberTop, veinCountWidth, 24f);
            item.gasRateText.horizontalOverflow = HorizontalWrapMode.Overflow;
            item.gasRateText.verticalOverflow = VerticalWrapMode.Overflow;
            foreach (Shadow glow in item.gasRateText.GetComponents<Shadow>())
            {
                //disable instead of Destroy: Destroy is deferred to the end of the frame, so a row
                //cloned in the same frame would otherwise keep the glow
                glow.enabled = false;
            }
            item.gasRateText.text = "";

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

            //iconHalo: a thin ring around the colour dot of a black hole / neutron star. It is a child of
            //the dot, centred by the anchors instead of being positioned with hand-computed offsets, so
            //it stays concentric whatever the dot's pivot, scale or position is.
            item.iconHalo = Util.CreateGameObject<Image>("iconHalo", 48f, 48f);
            item.iconHalo.transform.SetParent(item.labelIcon.transform, false);
            item.iconHalo.raycastTarget = false;
            item.iconHalo.material = null;
            item.iconHalo.sprite = PlanetThumbnail.OutlineSprite;
            RectTransform haloRect = item.iconHalo.rectTransform;
            haloRect.anchorMin = new Vector2(0.5f, 0.5f);
            haloRect.anchorMax = new Vector2(0.5f, 0.5f);
            haloRect.pivot = new Vector2(0.5f, 0.5f);
            haloRect.anchoredPosition = Vector2.zero;
            haloRect.sizeDelta = new Vector2(48f, 48f);
            haloRect.localScale = Vector3.one;
            item.iconHalo.color = new Color(0.70f, 0.82f, 1f, 0.5f);
            item.iconHalo.gameObject.SetActive(false);


            item.iconHide = src.iconHide;
            item.iconButton = src.iconButton;

            //row tooltip trigger: an invisible raycast area over the icon and the name
            Image tipHit = Util.CreateGameObject<Image>("row-tip-hit", 226f, 24f);
            tipHit.transform.SetParent(baseTrans, false);
            tipHit.material = null;
            tipHit.color = new Color(0f, 0f, 0f, 0f);
            tipHit.raycastTarget = true;
            Util.NormalizeRectWithTopLeft(tipHit, 14f, 0f);
            RowHoverForward forward = tipHit.gameObject.AddComponent<RowHoverForward>();
            forward.item = item;

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
        public void ShowRowButtons(bool show)
        {
            viewBtn.gameObject.SetActive(show);
            if (locateBtn != null)
            {
                locateBtn.gameObject.SetActive(show && PLFN.showLocateButtonInList.Value);
            }
        }

        /// <summary>
        /// Type text shown in the row tooltip: the planet theme name (e.g. ocean planet) or the star
        /// type in the wording of the game's info panel (e.g. blue giant (B-type)).
        /// </summary>
        public string ObjectTypeText()
        {
            if (planetData != null)
            {
                ThemeProto theme = LDB.themes.Select(planetData.theme);
                string name = "";
                if (theme != null)
                {
                    //ThemeProto.displayName can stay untranslated; DisplayName is the localization key,
                    //so translate it explicitly and only fall back to the property when that fails
                    name = Localization.Translate(theme.DisplayName);
                    if (string.IsNullOrEmpty(name) || name == theme.DisplayName && !PLFN.IsChineseUi())
                    {
                        name = theme.displayName;
                    }
                }
                if (IsDeuteriumRichGasGiant())
                {
                    name += PLFN.IsChineseUi() ? "（多重氢）" : " (Deuterium-rich)";
                }
                return name;
            }
            if (starData != null)
            {
                return StarTypeText(starData);
            }
            return "";
        }

        private static bool loggedThemeName;

        private static bool IsChineseUi()
        {
            return Localization.CurrentLanguageLCID == Localization.LCID_ZHCN;
        }

        private static bool ContainsCjk(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return false;
            }
            for (int i = 0; i < text.Length; i++)
            {
                if (text[i] >= '\u4e00' && text[i] <= '\u9fff')
                {
                    return true;
                }
            }
            return false;
        }

        public static string StarTypeText(StarData sd)
        {
            bool zh = PLFN.IsChineseUi();
            string spectr = sd.spectr.ToString();
            switch (sd.type)
            {
                case EStarType.BlackHole:
                    return zh ? "黑洞" : "Black Hole";
                case EStarType.NeutronStar:
                    return zh ? "中子星" : "Neutron Star";
                case EStarType.WhiteDwarf:
                    return zh ? "白矮星" : "White Dwarf";
                case EStarType.GiantStar:
                    //the star info panel calls a B class giant "blue giant", so use the colour name
                    //and append the spectral class in brackets
                    return GiantColorName(sd, zh) + (zh ? "（" + spectr + "型）" : " (" + spectr + "-type)");
                default:
                    return zh ? spectr + "型恒星" : spectr + "-type Star";
            }
        }

        private static string GiantColorName(StarData sd, bool zh)
        {
            switch (sd.spectr)
            {
                case ESpectrType.O:
                case ESpectrType.B:
                    return zh ? "蓝巨星" : "Blue Giant";
                case ESpectrType.A:
                    return zh ? "白巨星" : "White Giant";
                case ESpectrType.F:
                    return zh ? "黄白巨星" : "Yellow-White Giant";
                case ESpectrType.G:
                    return zh ? "黄巨星" : "Yellow Giant";
                case ESpectrType.K:
                    return zh ? "橙巨星" : "Orange Giant";
                case ESpectrType.M:
                    return zh ? "红巨星" : "Red Giant";
                default:
                    return zh ? "巨星" : "Giant";
            }
        }

        /// <summary>
        /// Some gas giants carry a high deuterium output; the type text marks them so they can be told
        /// apart from ordinary gas giants.
        /// </summary>
        private bool IsDeuteriumRichGasGiant()
        {
            if (planetData == null || !UIPlanetFinderWindow.IsGasGiant(planetData))
            {
                return false;
            }
            int[] items = planetData.gasItems;
            float[] speeds = planetData.gasSpeeds;
            if (items == null || speeds == null)
            {
                return false;
            }
            int count = Mathf.Min(items.Length, speeds.Length);
            for (int i = 0; i < count; i++)
            {
                if (items[i] == DeuteriumItemId && speeds[i] > HighGasSpeedThreshold)
                {
                    return true;
                }
            }
            return false;
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
                //a planet row shows the game's round planet icon, a star row the round star icon of its
                //type tinted with the star colour. A black hole and a neutron star keep the original
                //colour dot and only get a thin ring around it.
                Sprite icon = null;
                bool tint = false;
                if (PLFN.showPlanetThumbnail.Value)
                {
                    if (planetData != null)
                    {
                        icon = PlanetThumbnail.Get(planetData);
                    }
                    else if (starData != null)
                    {
                        icon = PlanetThumbnail.GetStar(starData);
                        tint = true;
                    }
                }

                bool dotOnly = starData != null
                    && (starData.type == EStarType.BlackHole || starData.type == EStarType.NeutronStar);
                if (dotOnly)
                {
                    icon = null;
                }
                bool ringed = starData != null
                    && (starData.type == EStarType.BlackHole || starData.type == EStarType.NeutronStar);

                if (icon != null)
                {
                    labelIcon.color = tint ? planetColor : Color.white;
                    labelIcon.sprite = icon;
                    //every star icon keeps the same outer box; a giant only looks bigger because its
                    //sprite has a larger bright core, a white dwarf is slightly smaller
                    float scale = 1f;
                    if (tint && starData.type == EStarType.WhiteDwarf)
                    {
                        scale = 0.8f;
                    }
                    else if (tint)
                    {
                        scale = 0.85f;
                    }
                    labelIcon.rectTransform.localScale = new Vector3(scale, scale, 1f);
                }
                else
                {
                    labelIcon.color = planetColor;
                    labelIcon.sprite = circleSprite;
                    labelIcon.rectTransform.localScale = new Vector3(0.3f, 0.3f, 1f);
                }
                if (iconHalo != null)
                {
                    iconHalo.gameObject.SetActive(ringed);
                }
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
                    //a null placeholder keeps the order aligned with CollectPlanets()
                    result.Add(planet?.planetData != null ? planet.VeinGroups() : null);
                }
            }
            return result;
        }

        /// <summary>
        /// Vein cells of the target resource as "mined / total", summed over the row.
        /// Total is the sum of VeinGroup.count. A cell counts as mined when at least one mining machine
        /// is attached to it, so a cell used by several machines is still counted once.
        /// That can only be read while the planet factory is loaded, so planets whose data was released
        /// count as not mined.
        /// </summary>
        public void GetVeinCellCountForPlanet(PlanetListData ld, out int mined, out int total)
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
                        total += veinGroups[i].count;
                    }
                }
            }
            if (total <= 0)
            {
                return;
            }
            bool countReused = PLFN.countReusedVeinGroups.Value;
            //same order as CollectVeinGroups, so the matching planet is found by index
            List<PlanetData> planets = CollectPlanets(ld);
            for (int a = 0; a < groupArrays.Count; a++)
            {
                if (groupArrays[a] == null || a >= planets.Count)
                {
                    continue;
                }
                PlanetFactory factory = planets[a]?.factory;
                VeinData[] veinPool = factory?.veinPool;
                if (factory == null || veinPool == null)
                {
                    continue;
                }
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
                    if ((int)vein.type == itemId)
                    {
                        //a cell covered by several machines counts once, or once per machine
                        mined += countReused ? (vein.minerCount > 4 ? 4 : vein.minerCount) : 1;
                    }
                }
            }
        }

        /// <summary>
        /// Vein group count of the target resource as "mined / total", summed over the row.
        /// By default a group counts as mined when any of its veins has a mining machine attached, so a
        /// reused group is counted once. With PLFN.countReusedVeinGroups the same group is counted once
        /// per mining machine covering it, so the mined count can exceed the total.
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
            bool countReused = PLFN.countReusedVeinGroups.Value;
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
                List<HashSet<int>> groupMiners = countReused ? NewMinerSets(groupCount) : null;
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
                        if (groupMiners != null)
                        {
                            AddMiners(groupMiners[groupIndex], vein);
                        }
                    }
                }
                for (int i = 1; i < veinGroups.Length; i++)
                {
                    if ((int)veinGroups[i].type != itemId)
                    {
                        continue;
                    }
                    if (groupMiners != null)
                    {
                        mined += groupMiners[i].Count;
                    }
                    else if (minedGroup[i])
                    {
                        mined++;
                    }
                }
            }
        }

        private static List<HashSet<int>> NewMinerSets(int groupCount)
        {
            List<HashSet<int>> result = new List<HashSet<int>>(groupCount);
            for (int i = 0; i < groupCount; i++)
            {
                result.Add(new HashSet<int>());
            }
            return result;
        }

        /// <summary>
        /// Records every mining machine covering this vein, so that one machine covering several veins
        /// of the same group is still counted once for that group.
        /// </summary>
        private static void AddMiners(HashSet<int> miners, VeinData vein)
        {
            int count = vein.minerCount;
            if (count > 4)
            {
                count = 4;
            }
            for (int m = 0; m < count; m++)
            {
                int minerId = m == 0 ? vein.minerId0 : m == 1 ? vein.minerId1 : m == 2 ? vein.minerId2 : vein.minerId3;
                if (minerId <= 0)
                {
                    //the id is not known yet; a synthetic one per vein still counts the coverage
                    minerId = -vein.id * 8 - m;
                }
                miners.Add(minerId);
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
        /// <summary>
        /// Number of orbital collectors already built on the planet (0 for a star row sums its planets).
        /// Orbital collectors are stations whose entity uses the orbital collector prototype (2105), which
        /// is how the game's own ACH_OrbitalCollectorCount counts them.
        /// </summary>
        public int OrbitalCollectorCount(PlanetListData ld)
        {
            int count = 0;
            foreach (PlanetData pd in CollectPlanets(ld))
            {
                PlanetFactory factory = pd?.factory;
                PlanetTransport transport = factory?.transport;
                StationComponent[] stationPool = transport?.stationPool;
                if (factory == null || transport == null || stationPool == null)
                {
                    continue;
                }
                int cursor = transport.stationCursor;
                if (cursor > stationPool.Length)
                {
                    cursor = stationPool.Length;
                }
                for (int i = 1; i < cursor; i++)
                {
                    StationComponent station = stationPool[i];
                    if (station == null || station.id != i)
                    {
                        continue;
                    }
                    int entityId = station.entityId;
                    if (entityId > 0 && entityId < factory.entityPool.Length)
                    {
                        EntityData entity = factory.entityPool[entityId];
                        if (entity.id != 0 && entity.protoId == OrbitalCollectorProtoId)
                        {
                            count++;
                        }
                    }
                }
            }
            return count;
        }

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
            int OldItemId = _itemId;
            valueText.color = valueTextNormalColor;
            valueSketchText.color = valueTextNormalColor;
            gasRateText.color = valueTextNormalColor;
            groupText.color = valueTextNormalColor;
            gasRateText.text = "";
            itemId = window.targetItemId;
            if (itemId != 0)
            {
                //a resource extracted from a gas giant or ice giant (hydrogen, deuterium, fire ice, ...)
                //has no vein reserve on that planet, so its extraction speed is shown in the group field.
                //This takes priority over a vein reserve: a planet can have both a solid fire ice vein
                //and fire ice in its atmosphere.
                float gasSpeed = TargetGasExtractSpeed();
                long amount = 0L;
                if (!GameMain.data.gameDesc.isInfiniteResource && gasSpeed <= 0f)
                {
                    amount = TargetItemAmount();
                }
                if (gasSpeed > 0f)
                {
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
                }
                else
                {
                    //also clears a value left over by the recycled row (infinite resource mode)
                    valueText.text = "";
                }

                //vein cells as "(mined/total)"; cyan when anything is being mined there, grey otherwise
                GetVeinCellCountForPlanet(listData, out int veinMined, out int veinTotal);
                if (gasSpeed > 0f)
                {
                    //gas resources have no vein cells; the (A/B) slot shows the extraction speed of the
                    //selected resource instead (4 decimals, like the game's own gas display)
                    StringBuilderUtility.WritePositiveFloat(sb, 0, 16, gasSpeed, 4, ' ');
                    gasRateText.text = sb.ToString().Trim() + "/s";
                    //highlight a high-yield deuterium gas giant
                    bool highDeuterium = itemId == DeuteriumItemId && gasSpeed > HighGasSpeedThreshold;
                    gasRateText.color = (PLFN.highlightHighDeuteriumRate.Value && highDeuterium) ? gasRateHighlightColor : valueTextNormalColor;
                    valueSketchText.text = "";
                    //the [A/B] slot shows how many orbital collectors are already built there,
                    //bracketed so that it is not mistaken for a vein group count; nothing is shown
                    //when there is none, so that empty rows stay clean
                    int collectorCount = OrbitalCollectorCount(listData);
                    groupText.text = collectorCount > 0 ? "[" + collectorCount.ToString() + "]" : "";
                }
                else
                {
                    if (PLFN.showVeinCellCount.Value && veinTotal > 0)
                    {
                        valueSketchText.text = "(" + veinMined.ToString() + "/" + veinTotal.ToString() + ")";
                        valueSketchText.color = veinMined > 0 ? valueTextMiningColor : valueTextNormalColor;
                    }
                    else
                    {
                        valueSketchText.text = "";
                    }
                }

                if (gasSpeed <= 0f)
                {
                    //vein groups as "[mined/total]", placed between the amount and the vein cell count
                    GetVeinGroupCountForPlanet(listData, out int groupMined, out int groupTotal);
                    if (PLFN.showVeinGroupCount.Value && groupTotal > 0)
                    {
                        groupText.text = "[" + groupMined.ToString() + "/" + groupTotal.ToString() + "]";
                        groupText.color = groupMined > 0 ? valueTextMiningColor : valueTextNormalColor;
                    }
                    else
                    {
                        groupText.text = "";
                    }
                }

            }
            else if (PLFN.showPowerState.Value && planetData?.factory?.powerSystem != null)
            {
                //the power state keeps the original single line display: the same text with the
                //percentage and the colour warning as the 1.2.6 build
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
            }
            else
            {
                valueText.text = "";
                valueSketchText.text = "";
                groupText.text = "";
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

using System;
using System.Text;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace PlanetFinderMod
{
    public class UIPlanetFinderListItem : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public UIPlanetFinderWindow window;
        public PlanetData planetData;
        public StarData starData; //null if target is planet
        public string distanceStr;

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
                _itemId = value;
                sb = (_itemId == 7) ? window.sbOil : window.sb;
                veinIcon.sprite = window.itemSelection.GetIconForItemId(_itemId);
                veinIcon.enabled = (veinIcon.sprite != null);
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
        public Image veinIcon;

        [SerializeField]
        public Sprite iconHide;

        [SerializeField]
        public UIButton iconButton;

        [SerializeField]
        public UIButton locateBtn;

        public StringBuilder sb;

        public static UIPlanetFinderListItem CreatePrefab()
        {
            RectTransform rect;
            UIPlanetFinderListItem item = Util.CreateGameObject<UIPlanetFinderListItem>("list-item", 600f, 24f);
            RectTransform baseTrans = Util.NormalizeRectWithTopLeft(item, 0f, 0f);

            Image bg = item.gameObject.AddComponent<Image>();
            bg.color = new Color(0.2f, 0.2f, 0.2f, 0.1f);
            baseTrans.sizeDelta = new Vector2(600f, 24f);
            UIResAmountEntry src=GameObject.Instantiate<UIResAmountEntry>(UIRoot.instance.uiGame.planetDetail.entryPrafab, baseTrans);
            src.gameObject.SetActive(true);

            float rightPadding = 0f;
            float leftPadding = 0f;

            //locate button
            UIButton btn = Util.MakeIconButtonB(Util.astroIndicatorIcon, 22);
            if (btn != null)
            {
                btn.gameObject.name = "locate-btn";
                rect = Util.NormalizeRectWithTopLeft(btn, 534f - rightPadding, 1f, baseTrans);

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

            //valueSketchText
            item.valueSketchText = GameObject.Instantiate<Text>(item.valueText, item.valueText.transform.parent);
            item.valueSketchText.gameObject.name = "valueSketchText";
            item.valueSketchText.alignment = TextAnchor.UpperLeft;
            rect = Util.NormalizeRectWithTopLeft(item.valueSketchText, 504f - rightPadding - leftPadding, 2f);
            rect.sizeDelta = new Vector2(24f, 24f);

            //veinIcon
            item.veinIcon = src.iconImage;
            rect = Util.NormalizeRectWithTopLeft(item.veinIcon, 482f - rightPadding - leftPadding, 2f);
            item.veinIcon.enabled = true;

            item.iconHide = src.iconHide;
            item.iconButton = src.iconButton;
            item.gameObject.SetActive(true);
            //GameObject.Destroy(src.valueText.gameObject);
            GameObject.Destroy(src);



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
            else
            {
                string gas = "";
                if (planetData.type == EPlanetType.Gas)
                {
                    gas = "[GAS] ";
                }
                planetName = gas + planetData.displayName + distanceStr;
            }
        }

        public void Init(in PlanetListData d, UIPlanetFinderWindow window_)
        {
            window = window_;
            distanceStr = d.distanceStr;
            itemId = d.itemId;
            planetData = d.planetData;
            starData = d.starData;
            SetUpDisplayName();

            

        }

        private void OnLocateButtonClick(int obj)
        {
            PLFN.LocatePlanet(planetData.id);
        }

        public static int veinCount = 15;
        public long TargetItemAmount()
        {
            long amount = 0;
            if (planetData.type == EPlanetType.Gas)
            {
                return -1L;
            }

            if (planetData.waterItemId >= 1000 && planetData.waterItemId == itemId)
            {
                return -1L;
            }
            if (itemId <= veinCount)
            {
                if (planetData.veinAmounts[itemId] > 0)
                {
                    return planetData.veinAmounts[itemId];
                }
            }

            return amount;
        }

        public int TargetItemAmountSketch()
        {
            int amount = 0;
            if (planetData.type == EPlanetType.Gas)
            {
                return -1;
            }

            if (planetData.waterItemId >= 1000 && planetData.waterItemId == itemId)
            {
                return -1;
            }
            if (itemId <= veinCount)
            {
                if (planetData.veinSpotsSketch != null && planetData.veinSpotsSketch[itemId] > 0)
                {
                    return planetData.veinSpotsSketch[itemId];
                }
            }

            return amount;
        }

        public bool RefreshValues(bool shown, bool onlyNewlyEmerged = false)
        {
            if (shown != gameObject.activeSelf)
            {
                gameObject.SetActive(shown);
            }
            else if (onlyNewlyEmerged)
            {
                return true;
            }
            if (!shown)
            {
                return true;
            }
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
                        valueText.text = sb.ToString();
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
            else if (PLFN.showPowerState.Value && planetData.factory?.powerSystem != null)
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

            return true;
        }

        public string PowerState(out int networkCount)
        {
            long energyCapacity = 0L;
            long energyRequired = 0L;
            string result = "";
            networkCount = 0;
            PowerSystem powerSystem = planetData.factory.powerSystem;
            for (int i = 1; i < powerSystem.netCursor; i++)
            {
                PowerNetwork powerNetwork = powerSystem.netPool[i];
                if (powerNetwork != null && powerNetwork.id == i)
                {
                    networkCount++;
                    energyCapacity += powerNetwork.energyCapacity;
                    energyRequired += powerNetwork.energyRequired;
                }
            }
            if (energyCapacity > 0)
            {
                StringBuilderUtility.WriteKMG(window.sbWatt, 8, energyRequired * 60L, false);
                result = window.sbWatt.ToString();
                StringBuilderUtility.WriteKMG(window.sbWatt, 8, energyCapacity * 60L, false);
                result += " / " + window.sbWatt.ToString().Trim();
                double ratio = (double)energyRequired / (double)energyCapacity;
                if (ratio > 0.9f)
                {
                    result += " (" + ratio.ToString("P1") +")";
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
            else
            {
                result = "--";
            }

            return result;
        }

        public void OnPointerEnter(PointerEventData _eventData)
        {
            locateBtn.gameObject.SetActive(true);
        }

        public void OnPointerExit(PointerEventData _eventData)
        {
            locateBtn.gameObject.SetActive(false);
        }
    }
}

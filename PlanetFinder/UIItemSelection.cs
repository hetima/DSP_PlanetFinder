using System;
using System.Text;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace PlanetFinderMod
{
    //1:鉄鉱脈 / 1001:鉄鉱石
    //2:銅鉱脈 / 1002:銅鉱石
    //3:シリコン鉱脈 / 1003:シリコン鉱石
    //4:チタン鉱脈 / 1004:チタン鉱石
    //5:石鉱脈 / 1005:石
    //6:石炭鉱脈 / 1006:石炭
    //7:油田 / 1007:原油
    //8:メタンハイドレート鉱脈 / 1011:メタンハイドレート
    //9:キンバーライト鉱脈 / 1012:キンバーライト鉱石
    //10:フラクタルシリコン鉱脈 / 1013:フラクタルシリコン
    //11:有機結晶鉱脈 / 1117:有機結晶
    //12:光格子結晶鉱脈 / 1014:光格子結晶
    //13:紡錘状石筍結晶鉱脈 / 1015:紡錘状石筍結晶
    //14:単極磁石鉱脈 / 1016:単極磁石

    //水
    //硫酸
    //水素
    //重水素
    //メタンハイドレート

    public class UIItemSelection : MonoBehaviour
    {
        public static UIButton buttonPrefab;
        public static Sprite emptyImage;

        public UIPlanetFinderWindow planetFinderWindow;
        public RectTransform rectTrans;
        public List<UIButton> buttons;
        public List<string> buttonNames;

        public static int[] additionalMaterials = { 1000, 1116, 0, 1120, 1121, 1011 };

        public HashSet<int> items;
        public int lastSelectedItemId;

        public static UIButton CreatePrefab()
        {
            UIButton result = null;
            UIButton[] buildingWarnButtons = UIRoot.instance.optionWindow.buildingWarnButtons;
            if (buildingWarnButtons != null && buildingWarnButtons.Length > 0)
            {
                UIButton src = buildingWarnButtons[0];
                //Image warnImg = src.transform.Find("image")?.GetComponent<Image>();
                result = GameObject.Instantiate<UIButton>(src);
                result.onClick += null;
                result.gameObject.name = "item-button";
                result.tips.tipText = "";
                result.tips.tipTitle = "";
                result.tips.delay = 0.6f;
                result.tips.corner = 8;
                RectTransform rect = result.transform as RectTransform;
                rect.sizeDelta = new Vector2(28f, 28f);
                foreach (Transform item in rect)
                {
                    (item as RectTransform).sizeDelta = new Vector2(28f, 28f);
                    if (item.gameObject.name == "image")
                    {
                        item.GetComponent<Image>().color = Color.white;
                    }
                }
                if (result.transitions.Length > 1)
                {
                    result.transitions[1].mouseoverColor = Color.white;
                    result.transitions[1].normalColor = Color.white;
                }
            }
            return result;
        }

        public static UIButton CreateSelectButton(UIItemSelection owner)
        {
            if (buttonPrefab == null)
            {
                emptyImage = UIRoot.instance.uiGame.beltWindow.iconTagImage?.sprite;
                buttonPrefab = CreatePrefab();
            }
            UIButton result = GameObject.Instantiate<UIButton>(buttonPrefab);
            result.onClick += owner.OnSelectButtonClick;
            result.onRightClick += owner.OnSelectButtonRightClick;

            return result;
        }
        
        public static void SetButtonIcon(UIButton btn, Sprite sprite, string name, int obj)
        {
            Image Img = btn.transform.Find("image")?.GetComponent<Image>();
            if (Img != null)
            {
                Img.sprite = sprite;
            }
            btn.data = obj;
            if (name != null)
            {
                btn.tips.tipTitle = name;
                btn.tips.corner = 9;
                btn.tips.offset = new Vector2(-26f, 4f);
            }
        }

        public Sprite GetIconForItemId(int itemId)
        {
            if (itemId == 0)
            {
                return null;
            }
            foreach (UIButton btn in buttons)
            {
                if (btn.data != itemId)
                {
                    continue;
                }
                Image Img = btn.transform.Find("image")?.GetComponent<Image>();
                if (Img != null)
                {
                    return Img.sprite;
                }
            }
            return null;
        }

        public string MouseOverButtonName()
        {
            for (int i = 0; i < buttons.Count; i++)
            {
                if (buttons[i]._isPointerEnter)
                {
                    return buttonNames[i];
                }
            }
            return null;
        }

        public static UIItemSelection CreateInstance()
        {
            UIItemSelection itemSelection = Util.CreateGameObject<UIItemSelection>("item-selection", 500, 30);
            itemSelection.Init();

            return itemSelection;
        }

        private void Init()
        {
            items = new HashSet<int>();
            rectTrans = transform as RectTransform;
            
            //Text label = Util.CreateText("Vein", 14);
            //Util.NormalizeRectWithTopLeft(label, 0f, 0f, rectTrans);

            //Vein
            float top = 4f;
            float left = 0f;
            buttons = new List<UIButton>(40);
            buttonNames = new List<string>(40);
            for (int i = 1; i < 15; i++)
            {
                VeinProto veinProto = LDB.veins.Select(i);
                ItemProto itemProto = LDB.items.Select(veinProto.MiningItem);

                if (veinProto != null && itemProto != null)
                {

                    UIButton btn = CreateSelectButton(this);
                    RectTransform rect = Util.NormalizeRectWithTopLeft(btn, left, top, rectTrans);
                    btn.button.interactable = true;

                    SetButtonIcon(btn, veinProto.iconSprite, veinProto.name, veinProto.ID);
                    
                    buttons.Add(btn);
                    buttonNames.Add(veinProto.name);
                    left += 30f;
                }
            }

            //水, 硫酸, 水素, 重水素, メタンハイドレート
            left += 6f;
            foreach (int itemId in additionalMaterials)
            {
                if (itemId == 0)
                {
                    left += 6f;
                    continue;
                }
                ItemProto itemProto = LDB.items.Select(itemId);
                if (itemProto != null)
                {
                    UIButton btn = CreateSelectButton(this);
                    RectTransform rect = Util.NormalizeRectWithTopLeft(btn, left, top, rectTrans);
                    btn.button.interactable = true;
                    SetButtonIcon(btn, itemProto.iconSprite, itemProto.name, itemId);
                    buttons.Add(btn);
                    buttonNames.Add(itemProto.name);
                    left += 30f;
                }
            }

            //次期バージョン
            //left = 0f;
            //top += 36f;
            //for (int i = 0; i < 16; i++)
            //{
            //    UIButton btn = CreateSelectButton(this);
            //    RectTransform rect = Util.NormalizeRectWithTopLeft(btn, left, top, rectTrans);
            //    btn.button.interactable = true;
            //    SetButtonIcon(btn, emptyImage, "", 0);
            //    buttons.Add(btn);
            //    buttonNames.Add("");
            //    left += 30f;
            //}

        }
        public void RefreshButtons()
        {
            foreach (UIButton btn in buttons)
            {
                btn.highlighted = items.Contains(btn.data);
            }
        }

        public void ToggleItemSelected(int itemId, bool multiple = false)
        {
            if (items.Contains(itemId))
            {
                if (!multiple)
                {
                    items.Clear();
                    lastSelectedItemId = 0;
                }
                else
                {
                    items.Remove(itemId);
                    if (lastSelectedItemId == itemId)
                    {
                        if (items.Count > 0)
                        {
                            //どのアイテムになるか不定
                            foreach (var item in items)
                            {
                                lastSelectedItemId = item;
                            }
                        }
                        else
                        {
                            lastSelectedItemId = 0;
                        }
                    }
                }
            }
            else
            {
                if (!multiple)
                {
                    items.Clear();
                }
                items.Add(itemId);
                lastSelectedItemId = itemId;
            }
            RefreshButtons();
            planetFinderWindow.SetUpData();
        }

        public void OnSelectButtonClick(int obj)
        {
            if (obj == 0)
            {

            }
            else
            {
                //ToggleItemSelected(obj, false);
                ToggleItemSelected(obj, VFInput.shift);
            }
        }
        public void OnSelectButtonRightClick(int obj)
        {

        }

        public int NextTargetItemId(int targetItemId)
        {
            if (items.Count <= 1)
            {
                return targetItemId;
            }
            int min = int.MaxValue;
            int max = int.MaxValue;
            foreach (var item in items)
            {
                if (targetItemId >= item && min > item)
                {
                    min = item;
                }
                if (targetItemId < item && max > item)
                {
                    max = item;
                }
            }

            if (max != int.MaxValue)
            {
                return max;
            }
            else if (min != int.MaxValue)
            {
                return min;
            }

            return targetItemId;
        }

    }
}

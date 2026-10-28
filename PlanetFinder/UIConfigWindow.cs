using System;
using System.Text;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace PlanetFinderMod
{
    public class UIConfigWindow : ManualBehaviour, MyWindow
    {
        public RectTransform windowTrans;
        public RectTransform tab1;
        //public RectTransform tab2;
        public UIButton tabBtn1;
        //public UIButton tabBtn2;

        public static UIConfigWindow CreateWindow()
        {
            UIConfigWindow win = MyWindowCtl.CreateWindow<UIConfigWindow>("PLFDConfigWindow", "PlanetFinder Config");
            return win;
        }

        public void TryClose()
        {
            base._Close();
        }

        public bool isFunctionWindow()
        {
            return true;
        }

        /// <summary>
        /// Every label that has an English and a Chinese text. The panel is created during game start,
        /// before Localization finished loading, so the real language is only known when the window is
        /// opened; the labels are therefore stored and re-applied on every open.
        /// </summary>
        private readonly System.Collections.Generic.List<(Text text, string en, string zh)> languageEntries
            = new System.Collections.Generic.List<(Text text, string en, string zh)>();

        private void ApplyLanguage()
        {
            bool useChinese = PLFN.IsChineseUi();
            foreach (var entry in languageEntries)
            {
                if (entry.text != null)
                {
                    entry.text.text = useChinese ? entry.zh : entry.en;
                }
            }
        }

        /// <summary>
        /// Label text in the game language: Chinese for a Chinese game, English otherwise.
        /// </summary>
        private static string L(string en, string zh)
        {
            return PLFN.IsChineseUi() ? zh : en;
        }

        public void OpenWindow()
        {
            ApplyLanguage();
            MyWindowCtl.OpenWindow(this);
            if (windowTrans != null)
            {
                windowTrans.sizeDelta = new Vector2(640f, 516f);
            }
        }

        public override void _OnCreate()
        {
            windowTrans = MyWindowCtl.GetRectTransform(this);
            windowTrans.sizeDelta = new Vector2(640f, 516f);

            CreateUI();
        }

        internal void CreateUI()
        {
            //tabs
            RectTransform base_ = windowTrans;

            float y_ = 54;
            float x_ = 36f;
            float tabx_ = 36f;
            int tabIndex_ = 1;
            RectTransform AddTab(string label, out UIButton outBtn)
            {
                GameObject tab = new GameObject();
                RectTransform tabRect = tab.AddComponent<RectTransform>();
                Util.NormalizeRectWithMargin(tabRect, 48f, 36f, 0f, 0f, windowTrans);
                tab.name = "tab-" + tabIndex_.ToString();
                //tab button
                UIDESwarmPanel swarmPanel = UIRoot.instance.uiGame.dysonEditor.controlPanel.hierarchy.swarmPanel;
                UIButton src = swarmPanel.orbitButtons[0];
                UIButton btn = GameObject.Instantiate<UIButton>(src);
                RectTransform btnRext = Util.NormalizeRectWithTopLeft(btn, tabx_, 44f, windowTrans);
                btnRext.sizeDelta = new Vector2(100f, 24f);
                //(btn.transform.Find("frame").transform as RectTransform).sizeDelta = btnRext.sizeDelta;
                btn.transform.Find("frame").gameObject.SetActive(false);
                // btn.transitions[0] btn btn.transitions[1]==text btn.transitions[2]==frame
                if (btn.transitions.Length >= 3)
                {
                    btn.transitions[0].normalColor = new Color(0.1f, 0.1f, 0.1f, 0.68f);
                    btn.transitions[0].highlightColorOverride = new Color(0.9906f, 0.5897f, 0.3691f, 0.4f);
                    btn.transitions[1].normalColor = new Color(1f, 1f, 1f, 0.6f);
                    btn.transitions[1].highlightColorOverride = new Color(0.2f, 0.1f, 0.1f, 0.9f);
                }
                Text btnText = btn.transform.Find("Text").GetComponent<Text>();
                btnText.text = label;
                btnText.fontSize = 16;
                btn.data = tabIndex_;
                tabIndex_++;
                tabx_ += 100f;

                outBtn = btn;
                return tabRect;
            }

            void AddElement(RectTransform rect_, float height)
            {
                if (rect_ != null)
                {
                    Util.NormalizeRectWithTopLeft(rect_, x_, y_, base_);
                }
                y_ += height;
            }

            Text CreateText(string label_)
            {
                Text src_ = MyWindowCtl.GetTitleText(this);
                Text txt_ = GameObject.Instantiate<Text>(src_);
                txt_.gameObject.name = "label";
                txt_.text = label_;
                txt_.color = new Color(1f, 1f, 1f, 0.38f);
                (txt_.transform as RectTransform).sizeDelta = new Vector2(txt_.preferredWidth + 40f, 30f);
                return txt_;
            }
            RectTransform MakeCheckBox(BepInEx.Configuration.ConfigEntry<bool> config, string en, string zh)
            {
                RectTransform result = MyCheckBox.CreateCheckBox(config, en);
                MyCheckBox box = result != null ? result.GetComponent<MyCheckBox>() : null;
                if (box != null && box.labelText != null)
                {
                    languageEntries.Add((box.labelText, en, zh));
                }
                return result;
            }

            Text MakeLabel(string en, string zh)
            {
                Text label = CreateText(en);
                languageEntries.Add((label, en, zh));
                return label;
            }

            Text txt;

            //General tab
            tab1 = AddTab(L("General", "通用"), out tabBtn1);

            tabBtn1.gameObject.SetActive(false);

            base_ = tab1;
            RectTransform rect;
            y_ = 0f;
            x_ = 0f;
            rect = MyKeyBinder.CreateKeyBinder(PLFN.mainWindowHotkey, "Main Hotkey");
            Text keyBinderLabel = rect != null ? rect.GetComponentInChildren<Text>() : null;
            if (keyBinderLabel != null)
            {
                languageEntries.Add((keyBinderLabel, "Main Hotkey", "主快捷键"));
            }
            AddElement(rect, 90f);

            rect = MakeCheckBox(PLFN.showButtonInMainPanel, "Show Button In Main Panel", "主面板显示按钮");
            AddElement(rect, 26f);
            rect = MakeCheckBox(PLFN.showButtonInStarmap, "Show Button In Starmap", "星图显示按钮");
            AddElement(rect, 26f);

            txt = MakeLabel("Window Size", "窗口行数");
            AddElement(txt.transform as RectTransform, 32f);
            rect = MySlider.CreateSlider(PLFN.mainWindowSize, 4f, 40f, "0", 200f);
            AddElement(rect, 26f);

            rect = MakeCheckBox(PLFN.showPowerState, "Show Power State In List", "列表显示用电状态");
            AddElement(rect, 26f);
            rect = MakeCheckBox(PLFN.showPrefix, "Show [GAS] [TL] Prefix", "显示 [GAS] [TL] 前缀");
            AddElement(rect, 26f);
            rect = MakeCheckBox(PLFN.showLocateButtonInList, "Show Locate Button In List", "列表显示定位按钮");
            AddElement(rect, 26f);

            rect = MakeCheckBox(PLFN.showVeinCellCount, "Show Vein Cell Count", "显示矿片数 (A/B)");
            AddElement(rect, 26f);
            rect = MakeCheckBox(PLFN.showVeinGroupCount, "Show Vein Group Count", "显示矿点数 [A/B]");
            AddElement(rect, 26f);
            RectTransform reusedRect = MakeCheckBox(PLFN.countReusedVeinGroups, "Count Reused Vein Groups", "矿点按矿机重复计数");
            AddElement(reusedRect, 26f);

            //"Count Reused Vein Groups" only makes sense while the vein group count is shown,
            //so it is disabled and dimmed while that option is off
            MyCheckBox reusedBox = reusedRect.GetComponent<MyCheckBox>();
            void SyncReusedBox()
            {
                if (reusedBox != null)
                {
                    reusedBox.SetInteractable(PLFN.showVeinGroupCount.Value);
                }
            }
            PLFN.showVeinGroupCount.SettingChanged += (sender, args) => SyncReusedBox();
            SyncReusedBox();

            rect = MakeCheckBox(PLFN.highlightHighDeuteriumRate, "Highlight High Deuterium Rate", "重氢高产高亮");
            AddElement(rect, 26f);
            rect = MakeCheckBox(PLFN.showPlanetThumbnail, "Show Planet Thumbnail", "显示行星图标");
            AddElement(rect, 26f);
            rect = MakeCheckBox(PLFN.showRowTooltip, "Show Planet Type Per Row", "显示每行星球类型");
            AddElement(rect, 26f);

            x_ = 290f;
            y_ = 0f;

            rect = MakeCheckBox(PLFN.showFavButtonInStarmap, "Show Fav Button In Starmap", "星图显示收藏按钮");
            AddElement(rect, 26f);
            rect = MakeCheckBox(PLFN.distanceTextSmallFont, "Small Font For Distance", "距离使用小字号");
            AddElement(rect, 26f);

            if (PLFN.aDSPStarMapMemoIntg.canGetSignalIconId)
            {
                rect = MakeCheckBox(PLFN.integrationWithDSPStarMapMemo, "Integration With DSPStarMapMemo", "联动 DSPStarMapMemo");
                AddElement(rect, 22f);
                x_ += 32f;
                txt = MakeLabel("Display Icons / Search Memo", "显示图标 / 搜索便签");
                txt.fontSize = 14;
                AddElement(txt.rectTransform, 24f);
                x_ -= 32f;

            }
            if (PLFN.aLSTMIntg.canOpenPlanetId)
            {
                rect = MakeCheckBox(PLFN.integrationWithLSTM, "Integration With LSTM", "联动 LSTM");
                AddElement(rect, 22f);
                x_ += 32f;
                txt = MakeLabel("Open LSTM From Context Panel", "右键菜单打开 LSTM");
                txt.fontSize = 14;
                AddElement(txt.rectTransform, 24f);
                x_ -= 32f;
            }
            if (PLFN.aCruiseAssistIntg.canSelectPlanet)
            {
                rect = MakeCheckBox(PLFN.integrationWithCruiseAssist, "Integration With CruiseAssist", "联动 CruiseAssist");
                AddElement(rect, 22f);
                x_ += 32f;
                txt = MakeLabel("Set CruiseAssist From Context Panel", "右键菜单设置 CruiseAssist");
                txt.fontSize = 14;
                AddElement(txt.rectTransform, 24f);
                x_ -= 32f;
                
            }
            //tab2 = AddTab("", out tabBtn2);
            //base_ = tab2;
            y_ = 0f;
            x_ = 0f;


            //OnTabButtonClick(1);
        }

        public override void _OnDestroy()
        {
        }

        public override bool _OnInit()
        {
            windowTrans.anchoredPosition = new Vector2(0, 0);
            return true;
        }

        public override void _OnFree()
        {
        }

        public override void _OnRegEvent()
        {
            //tabBtn1.onClick += OnTabButtonClick;
            //tabBtn2.onClick += OnTabButtonClick;
        }

        public override void _OnUnregEvent()
        {
            //tabBtn1.onClick -= OnTabButtonClick;
            //tabBtn2.onClick -= OnTabButtonClick;
        }

        public override void _OnOpen()
        {

        }

        public override void _OnClose()
        {

        }

        public override void _OnUpdate()
        {
            if (VFInput.escape && !VFInput.inputing)
            {
                VFInput.UseEscape();
                base._Close();
            }
        }

        //public void OnTabButtonClick(int obj)
        //{
        //    if (obj == 1)
        //    {
        //        tabBtn2.highlighted = false;
        //        tab2.gameObject.SetActive(false);

        //        tabBtn1.highlighted = true;
        //        tab1.gameObject.SetActive(true);
        //    }
        //    else
        //    {
        //        tabBtn1.highlighted = false;
        //        tab1.gameObject.SetActive(false);

        //        tabBtn2.highlighted = true;
        //        tab2.gameObject.SetActive(true);
        //    }
        //}



    }
}

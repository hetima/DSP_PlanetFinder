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

        public void OpenWindow()
        {
            MyWindowCtl.OpenWindow(this);
        }

        public override void _OnCreate()
        {
            windowTrans = MyWindowCtl.GetRectTransform(this);
            windowTrans.sizeDelta = new Vector2(640f, 376f);

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
                Util.NormalizeRectWithMargin(tabRect, 54f + 28f, 36f, 0f, 0f, windowTrans);
                tab.name = "tab-" + tabIndex_.ToString();
                //tab button
                UIDESwarmPanel swarmPanel = UIRoot.instance.uiGame.dysonEditor.controlPanel.hierarchy.swarmPanel;
                UIButton src = swarmPanel.orbitButtons[0];
                UIButton btn = GameObject.Instantiate<UIButton>(src);
                RectTransform btnRext = Util.NormalizeRectWithTopLeft(btn, tabx_, 54f, windowTrans);
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
            Text txt;

            //General tab
            tab1 = AddTab("General", out tabBtn1);

            tabBtn1.gameObject.SetActive(false);

            base_ = tab1;
            RectTransform rect;
            y_ = 0f;
            x_ = 0f;
            rect = MyKeyBinder.CreateKeyBinder(PLFN.mainWindowHotkey, "Main Hotkey");
            AddElement(rect, 90f);

            rect = MyCheckBox.CreateCheckBox(PLFN.showButtonInMainPanel, "Show Button In Main Panel");
            AddElement(rect, 26f);
            rect = MyCheckBox.CreateCheckBox(PLFN.showButtonInStarmap, "Show Button In Starmap");
            AddElement(rect, 26f);

            txt = CreateText("Window Size");
            AddElement(txt.transform as RectTransform, 32f);
            rect = MySlider.CreateSlider(PLFN.mainWindowSize, 4f, 12f, "0", 200f);
            AddElement(rect, 26f);

            rect = MyCheckBox.CreateCheckBox(PLFN.showPowerState, "Show Power State In List");
            AddElement(rect, 26f);
            rect = MyCheckBox.CreateCheckBox(PLFN.showPrefix, "Show [GAS] [TL] Prefix");
            AddElement(rect, 26f);
            
            
            x_ = 290f;
            y_ = 0f;

            rect = MyCheckBox.CreateCheckBox(PLFN.showFavButtonInStarmap, "Show Fav Button In Starmap");
            AddElement(rect, 26f);

            if (PLFN.aDSPStarMapMemoIntg.canGetSignalIconId)
            {
                rect = MyCheckBox.CreateCheckBox(PLFN.integrationWithDSPStarMapMemo, "Integration With DSPStarMapMemo");
                AddElement(rect, 22f);
                x_ += 32f;
                txt = CreateText("Display Icons / Search Memo");
                txt.fontSize = 14;
                AddElement(txt.rectTransform, 24f);
                x_ -= 32f;

            }
            if (PLFN.aLSTMIntg.canOpenPlanetId)
            {
                rect = MyCheckBox.CreateCheckBox(PLFN.integrationWithLSTM, "Integration With LSTM");
                AddElement(rect, 22f);
                x_ += 32f;
                txt = CreateText("Open LSTM From Context Panel");
                txt.fontSize = 14;
                AddElement(txt.rectTransform, 24f);
                x_ -= 32f;
            }
            if (PLFN.aCruiseAssistIntg.canSelectPlanet)
            {
                rect = MyCheckBox.CreateCheckBox(PLFN.integrationWithCruiseAssist, "Integration With CruiseAssist");
                AddElement(rect, 22f);
                x_ += 32f;
                txt = CreateText("Set CruiseAssist From Context Panel");
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

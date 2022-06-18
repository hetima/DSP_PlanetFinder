
using System;
using System.Text;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace PlanetFinderMod
{
    public enum Scope
    {
        None = 0,
        Star,
        CurrentStar,
        Planet,
        HasFactory,
        Fav,
        Recent,
        Gas,
    }


    public struct PlanetListData
    {
        public int distanceForSort;
        public string distanceStr;
        public PlanetData planetData;
        public StarData starData;
        public int itemId;
        public long amount;
    }

    public class UIPlanetFinderWindow : ManualBehaviour, IPointerEnterHandler, IPointerExitHandler, IEventSystemHandler, MyWindow
    {
        public RectTransform windowTrans;
        public RectTransform contentTrans;

        public Scope scope;
        public List<UIButton> scopeButtons;
        public int targetItemId;

        public UIItemSelection itemSelection;
        public UIListView planetListView;
        public Text countText;


        public StringBuilder sb;
        public StringBuilder sbOil;
        public StringBuilder sbWatt;

        internal bool _eventLock;
        public bool isPointEnter;
        private bool focusPointEnter;

        private bool calculated;
        private bool calculating;

        public static UIPlanetFinderWindow CreateInstance()
        {
            UIPlanetFinderWindow win = MyWindowCtl.CreateWindow<UIPlanetFinderWindow>("PlanetFinderWindow", "Planet Finder");

            return win;
        }

        public void BeginGame()
        {
            if (DSPGame.IsMenuDemo)
            {
                return;
            }
            calculated = false;
            calculating = false;
        }

        public void RequestCalcAll()
        {
            if (calculated)
            {
                return;
            }
            PlanetData calcPlanet = null;
            GalaxyData galaxy = GameMain.galaxy;
            for (int i = 0; i < galaxy.starCount; i++)
            {
                StarData star = galaxy.stars[i];
                for (int j = 0; j < star.planetCount; j++)
                {
                    PlanetData planet = star.planets[j];
                    //calculatingとかloadedとかのチェックはしてくれるので、ここではcalculatedだけチェックでよい
                    if (!planet.calculated)
                    {
                        if (!calculating)
                        {
                            PlanetModelingManager.RequestCalcPlanet(planet);
                            //or planet.RunCalculateThread();
                        }
                        calcPlanet = planet;
                    }
                }
                if (calculating && calcPlanet != null)
                {
                    //とりあえずまだ計算中ということだけ分かればよい
                    break;
                }
            }
            if (calcPlanet == null)
            {
                calculated = true;
                calculating = false;
            }
            else
            {
                calculating = true;
            }
        }

        public void SetUpAndOpen(int _itemId = 0)
        {
            SetUpData();
            //UIRoot.instance.uiGame.ShutPlayerInventory();
            MyWindowCtl.OpenWindow(this);
        }


        public bool isFunctionWindow()
        {
            return false;
        }

        public Vector2 WindowSize()
        {
            float rows = Mathf.Round(PLFN.mainWindowSize.Value);
            if (rows < 4f)
            {
                rows = 4f;
            }
            if (rows > 16f)
            {
                rows = 16f;
            }
            return new Vector2(640, 174 + 28 * rows);
        }

        protected override void _OnCreate()
        {
            _eventLock = true;
            calculated = false;
            calculating = false;
            windowTrans = MyWindowCtl.GetRectTransform(this);
            windowTrans.sizeDelta = WindowSize();

            GameObject go = new GameObject("content");
            contentTrans = go.AddComponent<RectTransform>();
            Util.NormalizeRectWithMargin(contentTrans, 60f, 28f, 20f, 28f, windowTrans);

            //listview
            Image bg = Util.CreateGameObject<Image>("list-bg", 100f, 100f);
            bg.color = new Color(0f, 0f, 0f, 0.56f);
            Util.NormalizeRectWithMargin(bg, 70f, 0f, 20f, 16f, contentTrans);

            planetListView = Util.CreateListView(UIPlanetFinderListItem.CreateListViewPrefab, "list-view", null, 16f);
            Util.NormalizeRectWithMargin(planetListView.transform, 0f, 0f, 0f, 0f, bg.transform);
            //ここでサイズ調整…
            //(planetListView.m_ItemRes.com_data.transform as RectTransform).sizeDelta = new Vector2(600f, 24f);

            //scope buttons
            float scopex_ = 4f;
            UIButton AddScope(string label, int data)
            {
                UIDESwarmPanel swarmPanel = UIRoot.instance.uiGame.dysonEditor.controlPanel.hierarchy.swarmPanel;
                UIButton src = swarmPanel.orbitButtons[0];
                UIButton btn = GameObject.Instantiate<UIButton>(src);
                // btn.transitions[0] btn btn.transitions[1]==text btn.transitions[2]==frame
                if (btn.transitions.Length >= 2)
                {
                    btn.transitions[0].normalColor = new Color(0.1f, 0.1f, 0.1f, 0.68f);
                    btn.transitions[0].highlightColorOverride = new Color(0.9906f, 0.5897f, 0.3691f, 0.4f);
                    btn.transitions[1].normalColor = new Color(1f, 1f, 1f, 0.6f);
                    btn.transitions[1].highlightColorOverride = new Color(0.2f, 0.1f, 0.1f, 0.9f);
                }
                Text btnText = btn.transform.Find("Text").GetComponent<Text>();
                btnText.text = label;
                btnText.fontSize = 16;
                btn.data = data;

                RectTransform btnRect = Util.NormalizeRectWithTopLeft(btn, scopex_, 0f, contentTrans);
                btnRect.sizeDelta = new Vector2(btnText.preferredWidth + 14f, 22f);
                btn.transform.Find("frame").gameObject.SetActive(false);
                //(btn.transform.Find("frame").transform as RectTransform).sizeDelta = btnRect.sizeDelta;
                scopex_ += btnRect.sizeDelta.x + 0f;
                
                return btn;
            }
            scopeButtons = new List<UIButton>(8);
            //scopeButtons.Add(AddScope("Star", (int)Scope.Star));
            scopeButtons.Add(AddScope("All Planet", (int)Scope.Planet));
            scopeButtons.Add(AddScope("Current Star", (int)Scope.CurrentStar));
            scopeButtons.Add(AddScope("Has Factory", (int)Scope.HasFactory));
            //scopeButtons.Add(AddScope("★", (int)Scope.Fav));
            scopeButtons.Add(AddScope("Recent", (int)Scope.Recent));
            scope = Scope.Planet;
            foreach (var btn in scopeButtons)
            {
                btn.highlighted = (btn.data == (int)scope);
            }

            //itemSelection
            itemSelection = UIItemSelection.CreateInstance();
            RectTransform rect = Util.NormalizeRectWithTopLeft(itemSelection, 0f, 28f, contentTrans);
            itemSelection.planetFinderWindow = this;

            countText = Util.CreateText("", 14, "result-count");
            Util.NormalizeRectWithBottomLeft(countText, 2f, 0f, contentTrans);

            string sbFormat = "                ";
            sb = new StringBuilder(sbFormat, sbFormat.Length + 2);
            sbFormat = "         /s";
            sbOil = new StringBuilder(sbFormat, sbFormat.Length + 2);
            sbWatt = new StringBuilder("         W", 12);

            //config button
            Sprite sprite = null;
            UIButton[] buildingWarnButtons = UIRoot.instance.optionWindow.buildingWarnButtons;
            if (buildingWarnButtons.Length >= 4)
            {
                UIButton warnBtn = buildingWarnButtons[3];
                Image warnImg = warnBtn.transform.Find("image")?.GetComponent<Image>();
                if (warnImg != null)
                {
                    sprite = warnImg.sprite;
                }
            }
            UIButton configBtn = Util.MakeIconButtonC(sprite, 32f);
            configBtn.button.onClick.AddListener(new UnityAction(this.OnConfigButtonClick));
            rect = Util.NormalizeRectWithTopLeft(configBtn, 158f, 20f, windowTrans);
            configBtn.gameObject.name = "config-button";
            configBtn.gameObject.SetActive(true);
            Image configImg = configBtn.gameObject.GetComponent<Image>();
            configImg.color = new Color(1f, 1f, 1f, 0.17f);
            if (configBtn.transitions.Length > 0)
            {
                configBtn.transitions[0].mouseoverColor = new Color(1f, 1f, 1f, 0.67f);
                configBtn.transitions[0].normalColor = new Color(1f, 1f, 1f, 0.17f);
                configBtn.transitions[0].pressedColor = new Color(1f, 1f, 1f, 0.5f);
            }

            //menu
            CreateMenuBox();

            _eventLock = false;
        }

        private void OnScrollRectChanged(Vector2 val)
        {
            if (planetListView.m_ScrollRect.verticalScrollbar.size < 0.1f)
            {
                planetListView.m_ScrollRect.verticalScrollbar.size = 0.1f;
            }
            else if (planetListView.m_ScrollRect.verticalScrollbar.size >= 0.99f)
            {
                planetListView.m_ScrollRect.verticalScrollbar.size = 0.001f;
            }
        }

        private void OnScopeButtonClick(int obj)
        {

            scope = (Scope)obj;
            
            foreach (var btn in scopeButtons)
            {
                btn.highlighted = (btn.data == (int)scope);
            }
            SetUpData();
        }

        private void OnConfigButtonClick()
        {
            PLFN._configWin.OpenWindow();
        }

        protected override void _OnDestroy()
        {

        }

        protected override bool _OnInit()
        {
            windowTrans.anchoredPosition = new Vector2(370f, -446f + (windowTrans.sizeDelta.y / 2)); // pivot=0.5 なので /2
            PLFN.mainWindowSize.SettingChanged += (sender, args) => {
                windowTrans.sizeDelta = WindowSize();
            };
            menuTarget = null;
            return true;
        }

        protected override void _OnFree()
        {

        }
        protected override void _OnRegEvent()
        {
            foreach (var btn in scopeButtons)
            {
                btn.onClick += OnScopeButtonClick;
            }
            planetListView.m_ScrollRect.onValueChanged.AddListener(OnScrollRectChanged);
        }
        protected override void _OnUnregEvent()
        {
            foreach (var btn in scopeButtons)
            {
                btn.onClick -= OnScopeButtonClick;
            }
            planetListView.m_ScrollRect.onValueChanged.RemoveListener(OnScrollRectChanged);
        }

        protected override void _OnOpen()
        {

        }

        protected override void _OnClose()
        {
            planetListView.Clear();
            isPointEnter = false;
        }

        protected override void _OnUpdate()
        {
            if (VFInput.escape && !UIRoot.instance.uiGame.starmap.active && !VFInput.inputing)
            {
                VFInput.UseEscape();
                if (PLFN._configWin.active)
                {
                    PLFN._configWin._Close();
                }
                else if (menuComboBox.isDroppedDown)
                {
                    menuComboBox.isDroppedDown = false;
                }
                else
                {
                    base._Close();
                }
            }
            if (_eventLock)
            {
                return;
            }

            if (_planetList.Count > 0)
            {
                AddToListView(planetListView, 5, _planetList);
            }

            bool valid = true;
            int step = Time.frameCount % 30;

            if (Input.GetKeyDown(KeyCode.Tab))
            {
                int current = targetItemId;
                targetItemId = itemSelection.NextTargetItemId(targetItemId, PLFN.showPowerState.Value);
                if (targetItemId != current)
                {
                    valid = RefreshListView(planetListView);
                }
            }
            else if (step == 0)
            {
                valid = RefreshListView(planetListView);
                //UIListViewのStart()で設定されるのでその後に呼ぶ必要がある
                planetListView.m_ScrollRect.scrollSensitivity = 28f;

                if (!calculated)
                {
                    if (!calculating)
                    {
                        MyWindowCtl.SetTitle(this, "Planet Finder*");
                    }
                    RequestCalcAll();
                    if (calculated)
                    {
                        MyWindowCtl.SetTitle(this, "Planet Finder");
                    }
                }
            }
            else
            {
                RefreshListView(planetListView, true);
            }
            if (!valid)
            {
                SetUpData();
            }

            if (!menuComboBox.isDroppedDown && menuTarget != null)
            {
                menuTarget.UnlockAppearance();
                menuTarget = null;
            }
        }

        public void TryClose()
        {
            base._Close();
        }
        //ホイールズーム対策用 UITutorialWindowなどの真似
        public void OnPointerEnter(PointerEventData _eventData)
        {
            this.isPointEnter = true;
        }
        public void OnPointerExit(PointerEventData _eventData)
        {
            this.isPointEnter = false;
        }

        public void OnApplicationFocus(bool focus)
        {
            if (!focus)
            {
                this.focusPointEnter = this.isPointEnter;
                this.isPointEnter = false;
            }
            if (focus)
            {
                this.isPointEnter = this.focusPointEnter;
            }
        }

        internal List<PlanetListData> _planetList = new List<PlanetListData>(200);

        public void SetUpData()
        {
            _eventLock = true;
            _planetList.Clear();
            planetListView.Clear();
            targetItemId = itemSelection.lastSelectedItemId;

            SetUpItemList();
            _planetList.Sort((a, b) => a.distanceForSort - b.distanceForSort);

            _eventLock = false;
            if (_planetList.Count > 0)
            {
                AddToListView(planetListView, 20, _planetList);
            }
            RefreshListView(planetListView);

        }


        public const int veinCount = 15;
        public bool IsTargetPlanet(PlanetData planet, int itemId)
        {
            if (planet.type == EPlanetType.Gas)
            {
                if (planet.gasItems != null)
                {
                    foreach (var item in planet.gasItems)
                    {
                        if (item == itemId)
                        {
                            return true;
                        }
                    }
                }
                return false;
            }

            if (planet.waterItemId >= 1000 && planet.waterItemId == itemId)
            {
                return true;
            }
            if (itemId <= veinCount)
            {
                if (!planet.calculated)
                {
                    return false;
                }
                VeinGroup[] runtimeVeinGroups = planet.runtimeVeinGroups;
                if (runtimeVeinGroups == null)
                {
                    if (planet.data == null)
                    {
                        return false;
                    }
                    VeinData[] veinPool = planet.data.veinPool;
                    int veinCursor = planet.data.veinCursor;
                    for (int i = 1; i < veinCursor; i++)
                    {
                        if (veinPool[i].id == i && (int)veinPool[i].type == itemId)
                        {
                            if (veinPool[i].amount > 0)
                            {
                                return true;
                            }
                        }
                    }
                    return false;
                }
                else
                {
                    for (int i = 1; i < runtimeVeinGroups.Length; i++)
                    {
                        if ((int)runtimeVeinGroups[i].type == itemId)
                        {
                            if (runtimeVeinGroups[i].amount > 0)
                            {
                                return true;
                            }
                        }
                    }
                }
                //if (planet.loaded)
                //{
                //    if (planet.veinAmounts[itemId] > 0)
                //    {
                //        return true;
                //    }
                //}
                //else
                //{
                //    if (planet.veinSpotsSketch != null && planet.veinSpotsSketch[itemId] > 0)
                //    {
                //        return true;
                //    }
                //}
            }

            return false;
        }

        public bool IsTargetPlanet(PlanetData planet, HashSet<int> filterItems, bool orSearch = false)
        {
            foreach (int itemId in filterItems)
            {
                if(IsTargetPlanet(planet, itemId) == orSearch)
                {
                    return orSearch;
                }
            }
            return !orSearch;
        }

        public bool IsTargetStar(StarData star, int itemId)
        {
            for (int j = 0; j < star.planetCount; j++)
            {
                PlanetData planet = star.planets[j];
                if (IsTargetPlanet(planet, itemId))
                {
                    return true;
                }
            }
            return false;
        }


        public List<PlanetData> PlanetsWithFactory()
        {

            int factoryCount = GameMain.data.factoryCount;
            List<PlanetData> targets = new List<PlanetData>(factoryCount);

            for (int i = 0; i < factoryCount; i++)
            {
                int planetId = GameMain.data.factories[i].planetId;
                PlanetData planetData =  GameMain.data.factories[i].planet;
                if (planetData != null)
                {
                    targets.Add(planetData);
                }
            }
            return targets;
        }

        public List<PlanetData> PlanetsWithFav()
        {
            GalaxyData galaxy = GameMain.galaxy;
            List<PlanetData> targets = new List<PlanetData>(100);
            for (int i = 0; i < galaxy.starCount; i++)
            {
                StarData star = galaxy.stars[i];
                for (int j = 0; j < star.planetCount; j++)
                {
                    PlanetData planet = star.planets[j];
                    if (!string.IsNullOrEmpty(planet.overrideName) && planet.overrideName.Contains("★"))
                    {
                        targets.Add(planet);
                    }
                }
            }
            return targets;
        }


        public void SetUpItemList()
        {
            GalaxyData galaxy = GameMain.galaxy;
            HashSet<int> filterItems = itemSelection.items;
            List<PlanetData> targets = null;
            int sortIndex = -1;

            //scope
            if (scope == Scope.HasFactory)
            {
                targets = PlanetsWithFactory();
            }
            else if (scope == Scope.CurrentStar)
            {
                StarData starData = GameMain.localStar;
                if (starData != null)
                {
                    targets = new List<PlanetData>(starData.planets.Length);
                    targets.AddRange(starData.planets);

                }
            }
            else if (scope == Scope.Fav)
            {
                targets = PlanetsWithFav();
            }
            else if (scope == Scope.Recent)
            {
                targets = PLFN.recentPlanets;
            }

            if (filterItems.Count == 0)
            {
                if (scope == Scope.Planet)
                {
                    targets = new List<PlanetData>(320);
                    for (int i = 0; i < galaxy.starCount; i++)
                    {
                        StarData star = galaxy.stars[i];
                        targets.AddRange(star.planets);
                    }
                }

                if (targets != null)
                {
                    foreach (var item in targets)
                    {
                        if (scope == Scope.Recent)
                        {
                            sortIndex++;
                        }
                        AddStore(item, sortIndex);
                    }
                    countText.text = "Result: " + targets.Count.ToString();
                }
                else
                {
                    countText.text = "";
                }
                return;
            }

            //filter by item
            //foreach (int itemId in filterItems){
            List<PlanetData> phasedTargets = new List<PlanetData>(targets != null ? targets.Count : 320);
            if (targets != null)
            {
                foreach (PlanetData planet in targets)
                {
                    if (IsTargetPlanet(planet, filterItems))
                    {
                        phasedTargets.Add(planet);
                    }
                }
            }
            else
            {
                for (int i = 0; i < galaxy.starCount; i++)
                {
                    StarData star = galaxy.stars[i];
                    for (int j = 0; j < star.planetCount; j++)
                    {
                        PlanetData planet = star.planets[j];
                        if (IsTargetPlanet(planet, filterItems))
                        {
                            phasedTargets.Add(planet);
                        }
                    }
                }
            }

            targets = phasedTargets;
            //}

            if (targets != null)
            {
                foreach (var item in targets)
                {
                    if (scope == Scope.Recent)
                    {
                        sortIndex++;
                    }
                    AddStore(item, sortIndex);
                }
                countText.text = "Result: " + targets.Count.ToString();
            }
            else
            {
                countText.text = "";
            }
        }

        internal void AddStore(PlanetData planet, int sortIndex = -1, StarData star = null)
        {
            string distanceStr = "";
            int distanceForSort;
            int localPlanetId = GameMain.localPlanet != null ? GameMain.localPlanet.id : 0;
            if (localPlanetId == planet.id)
            {
                distanceForSort = 0;
                distanceStr = "";
            }
            else if (localPlanetId / 100 == planet.id / 100)
            {
                distanceForSort = planet.index;
                distanceStr = "";
            }
            else
            {
                int nearStar = 0;
                if (GameMain.data?.localPlanet == null)
                {
                    StarData nearestStar = null;
                    PlanetData nearestPlanet = null;
                    GameMain.data?.GetNearestStarPlanet(ref nearestStar, ref nearestPlanet);
                    if (nearestStar != null)
                    {
                        nearStar = nearestStar.id;
                    }
                }
                float distancef = StarDistance.DistanceFromHere(planet.star.id, nearStar);
                distanceForSort = (int)(distancef * 10000f) + (planet.star.id * 10) + planet.index;
                distanceStr = string.Format("  ({0:F1}ly)", distancef);
            }

            if (sortIndex >= 0)
            {
                distanceForSort = sortIndex;
            }

            //int targetItemId = itemSelection.lastSelectedItemId;

            PlanetListData d = new PlanetListData()
            {
                planetData = planet,
                starData = star,
                distanceForSort = distanceForSort,
                distanceStr = distanceStr,
                itemId = targetItemId,
            };
            _planetList.Add(d);

        }

        internal int AddToListView(UIListView listView, int count, List<PlanetListData> list)
        {
            if (list.Count < count)
            {
                count = list.Count;
            }
            if (count == 0)
            {
                return count;
            }

            for (int i = 0; i < count; i++)
            {
                PlanetListData d = list[0];
                list.RemoveAt(0);
                UIPlanetFinderListItem e = listView.AddItem<UIPlanetFinderListItem>();
                e.Init(in d, this);
            }
            return count;
        }

        internal bool RefreshListView(UIListView listView, bool onlyNewlyEmerged = false)
        {
            if (_eventLock)
            {
                return true;
            }

            RectTransform contentRect = (RectTransform)listView.m_ContentPanel.transform;
            float top = -contentRect.anchoredPosition.y;
            float height = ((RectTransform)contentRect.parent).rect.height;
            float bottom = top - height - 120f; //項目の高さ適当な決め打ち

            for (int i = 0; i < listView.ItemCount; i++)
            {
                UIData data = listView.GetItemNode(i);
                UIPlanetFinderListItem e = (UIPlanetFinderListItem)data.com_data;
                if (e != null)
                {
                    float itemPos = ((RectTransform)data.transform).localPosition.y;
                    bool shown = itemPos <= top && itemPos > bottom;
                    if (targetItemId != e.itemId)
                    {
                        e.itemId = targetItemId;
                    }
                    if (onlyNewlyEmerged && !shown)
                    {
                        continue;
                    }
                    if (!e.RefreshValues(shown, onlyNewlyEmerged))
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        //menu

        public UIComboBox menuComboBox;
        UIPlanetFinderListItem menuTarget;
        public enum EMenuCommand
        {
            OpenStarmap = 0,
            OpenLSTM,
            SetCruiseAssist,
        }

        public void ShowMenu(UIPlanetFinderListItem item)
        {
            if (menuComboBox.isDroppedDown)
            {
                menuComboBox.isDroppedDown = false;
                return;
            }

            RectTransform rect = menuComboBox.m_DropDownList;
            //anchorMax = new Vector2(1f, 0f);
            //anchorMin = new Vector2(0f, 0f);
            //pivot = new Vector2(0f, 1f);

            UIRoot.ScreenPointIntoRect(Input.mousePosition, rect.parent as RectTransform, out Vector2 pos);
            pos.x = pos.x + 20f;
            pos.y = pos.y + 30f;
            menuComboBox.m_DropDownList.anchoredPosition = pos;

            menuTarget = item;
            menuTarget.LockAppearance();
            RefreshMenuBox();
            menuComboBox.OnPopButtonClick();
        }

        internal void RefreshMenuBox()
        {
            List<string> items = menuComboBox.Items;
            List<int> itemsData = menuComboBox.ItemsData;
            items.Clear();
            itemsData.Clear();

            int itemCount = 1;
            items.Add("Show In Starmap");
            itemsData.Add((int)EMenuCommand.OpenStarmap);

            if (PLFN.aLSTMIntg.canOpenPlanetId && PLFN.integrationWithLSTM.Value)
            {
                items.Add("Open LSTM");
                itemsData.Add((int)EMenuCommand.OpenLSTM);
                itemCount++;
            }
            if (PLFN.aCruiseAssistIntg.canSelectPlanet && PLFN.integrationWithCruiseAssist.Value)
            {
                items.Add("CruiseAssist");
                itemsData.Add((int)EMenuCommand.SetCruiseAssist);
                itemCount++;
            }


            menuComboBox.DropDownCount = itemCount;

        }


        public void OnMenuBoxItemIndexChange()
        {
            if (_eventLock)
            {
                return;
            }
            int num = menuComboBox.itemIndex;
            if (num < 0) //recursion
            {
                return;
            }
            if (menuTarget != null)
            {
                EMenuCommand itemData = (EMenuCommand)menuComboBox.ItemsData[num];
                switch (itemData)
                {
                    case EMenuCommand.OpenStarmap:
                        PLFN.LocatePlanet(menuTarget.planetData?.id ?? 0);
                        break;
                    case EMenuCommand.OpenLSTM:
                        PLFN.aLSTMIntg.OpenPlanetId(menuTarget.planetData?.id ?? 0);
                        break;
                    case EMenuCommand.SetCruiseAssist:
                        PLFN.aCruiseAssistIntg.SelectPlanetOrStar(menuTarget.planetData, menuTarget.planetData.star);
                        break;
                    default:
                        break;
                }

                menuTarget.UnlockAppearance();
                menuTarget = null;
            }


            //UIRealtimeTip.Popup("" + itemData, false, 0);
            menuComboBox.itemIndex = -1; //recursion
        }

        internal void CreateMenuBox()
        {
            // Main Button : Image,Button
            // -Pop sign : Image
            // -Text : Text
            // Dropdown List ScrollBox : ScrollRect

            UIStatisticsWindow statisticsWindow = UIRoot.instance.uiGame.statWindow;
            UIComboBox src = statisticsWindow.productAstroBox;
            UIComboBox box = GameObject.Instantiate<UIComboBox>(src, windowTrans);
            box.gameObject.name = "menu-box";

            RectTransform boxRect = Util.NormalizeRectWithTopLeft(box, 20f, 20f, windowTrans);

            RectTransform btnRect = box.transform.Find("Main Button")?.transform as RectTransform;
            if (btnRect != null)
            {
                btnRect.pivot = new Vector2(1f, 0f);
                btnRect.anchorMax = Vector2.zero;
                btnRect.anchorMin = Vector2.zero;
                btnRect.anchoredPosition = new Vector2(boxRect.sizeDelta.x, 0f);
                btnRect.sizeDelta = new Vector2(20, boxRect.sizeDelta.y);

                Button btn = btnRect.GetComponent<Button>();
                btnRect.Find("Text")?.gameObject.SetActive(false);
                btnRect.gameObject.SetActive(false);
            }

            box.onItemIndexChange.AddListener(OnMenuBoxItemIndexChange);
            menuComboBox = box;

            //Dropdown List ScrollBox
            RectTransform vsRect = menuComboBox.m_Scrollbar.transform as RectTransform;
            vsRect.sizeDelta = new Vector2(0, vsRect.sizeDelta.y);

            RefreshMenuBox();

        }

    }
}

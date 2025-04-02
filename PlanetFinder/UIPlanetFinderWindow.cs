
using System;
using System.Text;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using System.Threading;


namespace PlanetFinderMod
{
    public enum Scope
    {
        None = 0,
        CurrentStar,
        Planet,
        HasFactory,
        Fav,
        Recent,
        Gas,
        System,
    }


    public class PlanetListData
    {
        public int distanceForSort;
        public string distanceStr;
        public PlanetData planetData; // planet only
        public StarData starData; // star only
        public List<PlanetListData> planetList; // star only
        public int itemId;
        public long amount;
        public bool shouldShow;
        public VeinGroup[] cachedVeinGroups = null;

        public static int CalculatedOrLoadedCount(List<PlanetListData> list)
        {
            int count = 0;
            foreach (PlanetListData item in list)
            {
                if (item.IsCalculatedOrLoaded())
                {
                    count++;
                }
            }
            return count;
        }

        public bool IsCalculatedOrLoaded()
        {
            return (planetData.scanned || cachedVeinGroups != null);
        }

        public VeinGroup[] VeinGroups()
        {
            if (!planetData.scanned)
            {
                return cachedVeinGroups;
            }
            else
            {
                return planetData.runtimeVeinGroups;
            }
        }

        public bool IsContainItem(int itemId)
        {
            VeinGroup[] runtimeVeinGroups = VeinGroups();

            if (runtimeVeinGroups == null)
            {
                return false;
            }
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
            return false;
        }

        public long ItemCount(int itemId)
        {
            long amount = 0;
            VeinGroup[] runtimeVeinGroups = VeinGroups();

            if (runtimeVeinGroups == null)
            {
                return 0;
            }
            for (int i = 1; i < runtimeVeinGroups.Length; i++)
            {
                if ((int)runtimeVeinGroups[i].type == itemId)
                {
                    amount += runtimeVeinGroups[i].amount;
                }
            }
            return amount;
        }

        public int ItemCountSketch(int itemId)
        {
            int amountSketch = 0;
            VeinGroup[] runtimeVeinGroups = VeinGroups();

            if (runtimeVeinGroups == null)
            {
                return 0;
            }
            for (int i = 1; i < runtimeVeinGroups.Length; i++)
            {
                if ((int)runtimeVeinGroups[i].type == itemId)
                {
                    amountSketch += runtimeVeinGroups[i].count;
                }
            }
            return amountSketch;
        }


    }


    public class UIPlanetFinderWindow : ManualBehaviour, IPointerEnterHandler, IPointerExitHandler, IEventSystemHandler, MyWindow
    {
        public RectTransform windowTrans;
        public RectTransform contentTrans;
        public bool needsReflesh;

        public Scope scope;
        public List<UIButton> scopeButtons;
        public int targetItemId;

        public UIItemSelection itemSelection;
        public MyListView planetListView;
        public Text countText;
        public InputField searchField;
        public string searchString;
        public UIButton searchFieldClearBtn;

        public StringBuilder sb;
        public StringBuilder sbOil;
        public StringBuilder sbWatt;

        internal bool _eventLock;
        public bool isPointEnter;
        private bool focusPointEnter;

        //private bool calculated;
        //private bool calculating;

        public List<PlanetListData> _allPlanetList = new List<PlanetListData>(800);
        public List<PlanetListData> _allStarList = new List<PlanetListData>(80);
        public List<PlanetListData> _planetList = new List<PlanetListData>(800);

        private static Thread _planetCalculateThread = null;
        public bool _stopThread;
        private static UIPlanetFinderWindow _instance = null;

        private static void PlanetCalculateThreadMain()
        {
            long seedKey64 = GameMain.data.gameDesc.seedKey64;
            int planetCount = _instance._allPlanetList?.Count ?? 0;
            Thread.Sleep(3000);
            MyWindowCtl.SetTitle(_instance, "Planet Finder");
            for (int i = 0; i < planetCount; i++)
            {
                if(_instance._stopThread || planetCount != _instance._allPlanetList.Count || seedKey64 != GameMain.data.gameDesc.seedKey64)
                {
                    return;
                }
                PlanetListData item = _instance._allPlanetList[i];
                MyWindowCtl.SetTitle(_instance, "Init... (" + PlanetListData.CalculatedOrLoadedCount(_instance._allPlanetList).ToString() + "/" + planetCount.ToString() + ")");
                if (!item.IsCalculatedOrLoaded() )
                {
                    bool needsFree = true;
                    if (item.planetData.scanning)
                    {
                        needsFree = false;
                    }
                    else
                    {
                        if (_instance._stopThread)
                        {
                            return;
                        }
                        PlanetModelingManager.RequestScanPlanet(item.planetData);
                    }
                    for (;;)
                    {
                        Thread.Sleep(100);
                        if (_instance._stopThread)
                        {
                            return;
                        }
                        if (item.planetData.scanned)
                        {
                            VeinGroup[] v = item.planetData.runtimeVeinGroups;
                            int num = v.Length;
                            int num2 = (num >= 1) ? num : 1;
                            item.cachedVeinGroups = new VeinGroup[num2];
                            Array.Copy(v, item.cachedVeinGroups, num);
                            item.cachedVeinGroups[0].SetNull();

                            if (needsFree)
                            {
                                StarData localStar = GameMain.localStar;
                                int starId;
                                if (localStar != null)
                                {
                                    starId = localStar.id;
                                }
                                else
                                {
                                    starId = 0;
                                }
                                if (item.planetData.id / 100 != starId)
                                {
                                    //free
                                    if (item.planetData.data != null)
                                    {
                                        item.planetData.data.Free();
                                        item.planetData.data = null;
                                    }
                                    //item.planetData.modData = null;
                                    if (item.planetData.aux != null)
                                    {
                                        item.planetData.aux.Free();
                                        item.planetData.aux = null;
                                    }
                                    item.planetData.scanned = false;
                                }
                            }
                            break;
                        }
                    }
                }
                else
                {
                    VeinGroup[] v = item.planetData.runtimeVeinGroups;
                    int num = v.Length;
                    int num2 = (num >= 1) ? num : 1;
                    item.cachedVeinGroups = new VeinGroup[num2];
                    Array.Copy(v, item.cachedVeinGroups, num);
                    item.cachedVeinGroups[0].SetNull();
                }
            }
            MyWindowCtl.SetTitle(_instance, "Planet Finder");
            PLFN.Log("loading vein data completed");

        }


        public static UIPlanetFinderWindow CreateInstance()
        {
            if (_instance != null)
            {
                return _instance;
            }
            _instance = MyWindowCtl.CreateWindow<UIPlanetFinderWindow>("PlanetFinderWindow", "Planet Finder");
            return _instance;
        }

        public void BeginGame()
        {
            if (DSPGame.IsMenuDemo)
            {
                return;
            }
            _eventLock = true;
            //calculated = false;
            //calculating = false;

            GalaxyData galaxy = GameMain.galaxy;
            _allPlanetList.Clear();
            _allStarList.Clear();
            _planetList.Clear();
            for (int i = 0; i < galaxy.starCount; i++)
            {
                StarData star = galaxy.stars[i];
                PlanetListData sd = new PlanetListData()
                {
                    planetData = null,
                    starData = star,
                    distanceForSort = 0,
                    distanceStr = null,
                    itemId = targetItemId,
                    shouldShow = false,
                    planetList = new List<PlanetListData>(),
                };
                _allStarList.Add(sd);
                for (int j = 0; j < star.planetCount; j++)
                {
                    PlanetData planetData = star.planets[j];
                    PlanetListData d = new PlanetListData()
                    {
                        planetData = planetData,
                        starData = null,
                        distanceForSort = 0,
                        distanceStr = null,
                        itemId = targetItemId,
                        shouldShow = false,
                    };
                    _allPlanetList.Add(d);
                    sd.planetList.Add(d);
                }
            }
            searchField.text = "";
            searchString = null;
            _stopThread = false;
            _planetCalculateThread = new Thread(new ThreadStart(PlanetCalculateThreadMain));
            _planetCalculateThread.Start();
            _eventLock = false;
        }

        public void EndGame()
        {
            if (_planetCalculateThread != null)
            {
                _stopThread = true;
                _planetCalculateThread.Abort();
                _planetCalculateThread = null;
                //Wait for the calculation thread to abort
                Thread.Sleep(1600);
            }
            MyWindowCtl.SetTitle(this, "Planet Finder");
        }

        //public void RequestCalcAll()
        //{
        //    if (calculated)
        //    {
        //        return;
        //    }
        //    GalaxyData galaxy = GameMain.galaxy;
        //    int planetsCount = 0;
        //    int calculatingPlanetsCount = 0;

        //    for (int i = 0; i < galaxy.starCount; i++)
        //    {
        //        StarData star = galaxy.stars[i];
        //        for (int j = 0; j < star.planetCount; j++)
        //        {
        //            PlanetData planet = star.planets[j];
        //            planetsCount++;
        //            //calculatingとかloadedとかのチェックはしてくれるので、ここではcalculatedだけチェックでよい
        //            if (!planet.calculated)
        //            {
        //                if (!calculating)
        //                {
        //                    PlanetModelingManager.RequestCalcPlanet(planet);
        //                    //or planet.RunCalculateThread();
        //                }
        //                calculatingPlanetsCount++;
        //            }
        //        }
        //    }
        //    if (calculatingPlanetsCount == 0)
        //    {
        //        calculated = true;
        //        calculating = false;
        //        MyWindowCtl.SetTitle(this, "Planet Finder");
        //    }
        //    else
        //    {
        //        calculating = true;
        //        MyWindowCtl.SetTitle(this, "Init... (" + (planetsCount - calculatingPlanetsCount).ToString() + "/" + planetsCount.ToString() +")");
        //    }
        //}

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
        private void PopulateItem(MonoBehaviour item, int rowIndex)
        {
            UIPlanetFinderListItem child = item as UIPlanetFinderListItem;
            child.Init(_planetList[rowIndex], this);
            child.RefreshValues();
        }

        public override void _OnCreate()
        {
            _eventLock = true;
            //calculated = false;
            //calculating = false;
            windowTrans = MyWindowCtl.GetRectTransform(this);
            windowTrans.sizeDelta = WindowSize();

            GameObject go = new GameObject("content");
            contentTrans = go.AddComponent<RectTransform>();
            Util.NormalizeRectWithMargin(contentTrans, 60f, 28f, 20f, 28f, windowTrans);

            //listview
            Image bg = Util.CreateGameObject<Image>("list-bg", 100f, 100f);
            bg.color = new Color(0f, 0f, 0f, 0.56f);
            Util.NormalizeRectWithMargin(bg, 70f, 0f, 20f, 16f, contentTrans);

            planetListView = MyListView.CreateListView(UIPlanetFinderListItem.CreateListViewPrefab(), PopulateItem, "list-view");
            Util.NormalizeRectWithMargin(planetListView.transform, 0f, 0f, 0f, 0f, bg.transform);
            //ここでサイズ調整…
            //(planetListView.m_ItemRes.com_data.transform as RectTransform).sizeDelta = new Vector2(600f, 24f);
            planetListView.m_ScrollRect.scrollSensitivity = 28f;

            //scope buttons
            float scopex_ = 4f;
            UIButton AddScope(string label, int data)
            {
                UIButton btn = Util.MakeHiliteTextButton(label);
                btn.data = data;

                RectTransform btnRect = Util.NormalizeRectWithTopLeft(btn, scopex_, 0f, contentTrans);
                //btnRect.sizeDelta = new Vector2(btnText.preferredWidth + 14f, 22f);
                //(btn.transform.Find("frame").transform as RectTransform).sizeDelta = btnRect.sizeDelta;
                scopex_ += btnRect.sizeDelta.x + 0f;
                
                return btn;
            }
            scopeButtons = new List<UIButton>(8);
            scopeButtons.Add(AddScope("Sys", (int)Scope.System));
            scopeButtons.Add(AddScope("All Planet", (int)Scope.Planet));
            scopeButtons.Add(AddScope("Current Star", (int)Scope.CurrentStar));
            scopeButtons.Add(AddScope("Has Factory", (int)Scope.HasFactory));
            scopeButtons.Add(AddScope("★", (int)Scope.Fav));
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


            //search field
            UIStationWindow stationWindow = UIRoot.instance.uiGame.stationWindow;
            //public InputField nameInput;
            searchField = GameObject.Instantiate<InputField>(stationWindow.nameInput);
            searchField.gameObject.name = "search-field";
            Destroy(searchField.GetComponent<UIButton>());
            searchField.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.05f);
            rect = Util.NormalizeRectWithTopLeft(searchField, 370f, -4f, contentTrans);
            rect.sizeDelta = new Vector2(210, rect.sizeDelta.y);
            searchField.textComponent.fontSize = 16;

            //searchFieldClearBtn
            searchFieldClearBtn = Util.MakeSmallTextButton("X", 18f, 20f);
            searchFieldClearBtn.gameObject.name = "search-clear-btn";
            Util.NormalizeRectWithTopLeft(searchFieldClearBtn, 557f, 0f, contentTrans);
            searchFieldClearBtn.transform.SetParent(contentTrans, false);
            searchFieldClearBtn.onClick += OnClearSearchField;
            searchFieldClearBtn.gameObject.SetActive(false);


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

        public override void _OnDestroy()
        {

        }

        public override bool _OnInit()
        {
            windowTrans.anchoredPosition = new Vector2(370f, -446f + (windowTrans.sizeDelta.y / 2)); // pivot=0.5 なので /2
            PLFN.mainWindowSize.SettingChanged += (sender, args) => {
                windowTrans.sizeDelta = WindowSize();
            };
            menuTarget = null;
            return true;
        }

        public override void _OnFree()
        {

        }
        public override void _OnRegEvent()
        {
            foreach (var btn in scopeButtons)
            {
                btn.onClick += OnScopeButtonClick;
            }
            planetListView.m_ScrollRect.onValueChanged.AddListener(OnScrollRectChanged);
        }
        public override void _OnUnregEvent()
        {
            foreach (var btn in scopeButtons)
            {
                btn.onClick -= OnScopeButtonClick;
            }
            planetListView.m_ScrollRect.onValueChanged.RemoveListener(OnScrollRectChanged);
        }

        public override void _OnOpen()
        {
            searchField.onValueChanged.AddListener(new UnityAction<string>(this.OnSearchFieldValueChanged));
            searchField.onEndEdit.AddListener(new UnityAction<string>(OnSearchFieldEndEdit));
        }

        public override void _OnClose()
        {
            popupMenuBase.SetActive(false);
            searchField.onValueChanged.RemoveAllListeners();
            searchField.onEndEdit.RemoveAllListeners();
            if (searchField.isFocused)
            {
                EventSystem.current.SetSelectedGameObject(null);
            }
            isPointEnter = false;
        }

        public override void _OnUpdate()
        {
            needsReflesh = false;
            if (VFInput.escape && !UIRoot.instance.uiGame.starmap.active && !VFInput.inputing)
            {
                VFInput.UseEscape();
                if (PLFN._configWin.active)
                {
                    PLFN._configWin._Close();
                }
                else if (popupMenuBase.activeSelf)
                {
                    popupMenuBase.SetActive(false);
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


            //bool valid = true;
            int step = Time.frameCount % 30;

            if (Input.GetKeyDown(KeyCode.Tab))
            {
                int current = targetItemId;
                targetItemId = itemSelection.NextTargetItemId(targetItemId, PLFN.showPowerState.Value);
                if (targetItemId != current)
                {
                    needsReflesh = true;
                }
            }
            else if (step == 0)
            {
                //if (!calculated)
                //{
                //    RequestCalcAll();
                //}
            }


            if (popupMenuBase.activeSelf && (Input.GetKeyDown(KeyCode.Mouse0) || Input.GetKeyDown(KeyCode.Mouse1) || Input.GetKeyDown(KeyCode.Mouse2)))
            {
                Camera worldCamera = UIRoot.instance.overlayCanvas.worldCamera;
                if (!RectTransformUtility.RectangleContainsScreenPoint(popupMenuBase.transform as RectTransform, Input.mousePosition, worldCamera))
                {
                    popupMenuBase.SetActive(false);
                }
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

        private void OnSearchFieldEndEdit(string str)
        {
            OnSearchFieldValueChanged(str);
            EventSystem.current.SetSelectedGameObject(windowTrans.gameObject);
        }

        private void OnSearchFieldValueChanged(string str)
        {
            string text = searchField.text;
            if (string.IsNullOrEmpty(text))
            {
                text = null;
                searchFieldClearBtn.gameObject.SetActive(false);
                //searchFieldでesc押したりその他動作で、その後escでウィンドウ閉じなくなるので対策
                if (!searchField.isFocused || searchField.wasCanceled || EventSystem.current.currentSelectedGameObject == null)
                {
                    EventSystem.current.SetSelectedGameObject(windowTrans.gameObject);
                }
            }
            else
            {
                searchFieldClearBtn.gameObject.SetActive(true);
            }
            if (text != searchString)
            {
                searchString = text;
                SetUpData();
            }
        }
        public void OnClearSearchField(int obj)
        {
            string text = searchField.text;
            if (!string.IsNullOrWhiteSpace(text))
            {
                searchField.text = "";
                searchString = null;
                searchFieldClearBtn.gameObject.SetActive(false);
                SetUpData();
            }
        }


        public void SetUpData()
        {
            _eventLock = true;
            targetItemId = itemSelection.lastSelectedItemId;

            SetUpItemList();

            _eventLock = false;


            //RefreshListView(planetListView);

        }


        public const int veinCount = 15;
        public bool IsTargetPlanet(PlanetListData planetListData, int itemId)
        {
            PlanetData planet = planetListData.planetData;
            if (planet != null)
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
                    return planetListData.IsContainItem(itemId);
                }
            }
            return false;
        }

        public bool IsTargetPlanet(PlanetListData planetListData, HashSet<int> filterItems, bool orSearch = false)
        {
            if (planetListData.planetData != null)
            {
                foreach (int itemId in filterItems)
                {
                    if (IsTargetPlanet(planetListData, itemId) == orSearch)
                    {
                        return orSearch;
                    }
                }
                return !orSearch;
            }
            else if(planetListData.starData != null)
            {
                return IsTargetStar(planetListData, filterItems, orSearch);
            }
            return false;
        }

        
        public bool IsTargetStar(PlanetListData planetListData, HashSet<int> filterItems, bool orSearch = false)
        {
            HashSet<int> notFound = new HashSet<int>(filterItems); 
            if(planetListData.starData != null)
            {
                foreach (PlanetListData item in planetListData.planetList)
                {
                    foreach (int itemId in filterItems)
                    {
                        if (IsTargetPlanet(item, itemId))
                        {
                            notFound.Remove(itemId);
                        }
                    }
                }
            }
            if (orSearch)
            {
                return  filterItems.Count != notFound.Count;
            }
            else
            {
                return notFound.Count == 0;
            }
        }


        public void FilterPlanetsWithFactory()
        {
            foreach (PlanetListData d in _allPlanetList)
            {
                int entityCount = d.planetData?.factory?.entityCount ?? 0;
                if (entityCount > 0)
                {
                    d.shouldShow = true;
                }
                else
                {
                    d.shouldShow = false;
                }
            }
        }

        public void FilterCurrentStar()
        {
            StarData localStar = GameMain.localStar;
            int starId;
            if (localStar != null)
            {
                starId = localStar.id;
            }
            else
            {
                starId = 0;
            }
            foreach (PlanetListData d in _allPlanetList)
            {
                if (d.planetData.id / 100 == starId)
                {
                    d.shouldShow = true;
                }
                else
                {
                    d.shouldShow = false;
                }
            }
        }

        public void FilterRecentPlanets()
        {
            List<PlanetData> recentPlanets = PLFN.recentPlanets;
            int i = 0;
            foreach (PlanetListData d in _allPlanetList)
            {
                if (recentPlanets.Contains(d.planetData))
                {
                    d.shouldShow = true;
                    d.distanceForSort = i++;
                }
                else
                {
                    d.shouldShow = false;
                }
            }
        }

        public void FilterPlanetsWithFav()
        {
            foreach (PlanetListData d in _allPlanetList)
            {
                PlanetData planet = d.planetData;
                if (PLFN.IsFavPlanet(planet))
                {
                    d.shouldShow = true;
                }
                else
                {
                    d.shouldShow = false;
                }
            }
        }


        public void SetUpItemList()
        {
            GalaxyData galaxy = GameMain.galaxy;
            HashSet<int> filterItems = itemSelection.items;

            planetListView.SetItemCount(0);
            foreach (PlanetListData d in _allPlanetList)
            {
                d.distanceForSort = -1;
                d.shouldShow = scope != Scope.System;
            }
            foreach (PlanetListData d in _allStarList)
            {
                d.distanceForSort = -1;
                d.shouldShow = scope == Scope.System;
            }
            //scope
            if (scope == Scope.HasFactory)
            {
                FilterPlanetsWithFactory();
            }
            else if (scope == Scope.CurrentStar)
            {
                FilterCurrentStar();
            }
            else if (scope == Scope.Fav)
            {
                FilterPlanetsWithFav();
            }
            else if (scope == Scope.Recent)
            {
                FilterRecentPlanets();
            }
            //else if (scope == Scope.Planet)
            //{
            //    //all
            //    foreach (PlanetListData d in _allPlanetList)
            //    {
            //        d.shouldShow = true;
            //    }
            //}

            List<PlanetListData> list = scope == Scope.System ? _allStarList : _allPlanetList;

            //filter by item
            if (filterItems.Count > 0)
            {
                foreach (PlanetListData d in list)
                {
                    if (d.shouldShow && IsTargetPlanet(d, filterItems))
                    {
                        //d.shouldShow = true;
                    }
                    else
                    {
                        d.shouldShow = false;
                    }
                }
            }

            //
            if (!string.IsNullOrWhiteSpace(searchString))
            {
                string str = searchString.Trim().ToLower();
                foreach (PlanetListData d in list)
                {
                    if (!d.shouldShow)
                    {
                        continue;
                    }
                    string planetName = "";
                    int planetId = 0;
                    if (d.planetData != null)
                    {
                        planetName = d.planetData.displayName.ToLower();
                        //[GAS] [TL]
                        if (d.planetData.type == EPlanetType.Gas)
                        {
                            planetName = PLFN.gasGiantPrefix.Value.ToLower() + ' ' + planetName;
                        }
                        if ((d.planetData.singularity & EPlanetSingularity.TidalLocked) != EPlanetSingularity.None)
                        {
                            planetName = PLFN.tidalLockedPrefix.Value.ToLower() + ' ' + planetName;
                        }

                        planetId = d.planetData.id;
                    }
                    else if (d.starData != null)
                    {
                        planetName = d.starData.displayName.ToLower();
                        planetId = d.starData.id;
                    }
                    else
                    {
                        d.shouldShow = false;
                        continue;
                    }

                    if (planetName.Contains(str))
                    {
                        continue;
                    }
                    else if (planetId > 0 && PLFN.integrationWithDSPStarMapMemo.Value && PLFN.aDSPStarMapMemoIntg.canGetDesc)
                    {
                        string memo = PLFN.aDSPStarMapMemoIntg.GetDesc(planetId);
                        if (!string.IsNullOrWhiteSpace(memo) && memo.ToLower().Contains(str))
                        {
                            continue;
                        }
                    }
                    d.shouldShow = false;
                }
            }

            //add to list
            _planetList.Clear();
            foreach (PlanetListData item in list)
            {
                if (item.shouldShow)
                {
                    _planetList.Add(item);
                }
            }

            //sort
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
            foreach (PlanetListData item in _planetList)
            {
                SetupDistanceForSort(item, nearStar);
            }
            _planetList.Sort((a, b) => a.distanceForSort - b.distanceForSort);
            planetListView.SetItemCount(_planetList.Count);

            if (_planetList.Count > 0)
            {
                countText.text = "Result: " + _planetList.Count.ToString();
            }
            else
            {
                countText.text = "";
            }
        }

        internal void SetupDistanceForSort(PlanetListData d, int nearStar)
        {
            PlanetData planet=d.planetData;
            if (planet == null)
            {
                planet = d.starData.planets[0];
            }
            if (planet == null)
            {
                return;
            }
            int sortIndex = d.distanceForSort;

            string distanceStr = "";
            int distanceForSort;
            int localPlanetId = GameMain.localPlanet != null ? GameMain.localPlanet.id : 0;
            if (localPlanetId == planet.id)
            {
                distanceForSort = 0;
                distanceStr = "";
            }
            else
            if (localPlanetId / 100 == planet.id / 100)
            {
                distanceForSort = planet.index + 40;
                distanceStr = "";
            }
            else
            {
                float distancef = StarDistance.DistanceFromHere(planet.star.id, nearStar);
                distanceForSort = (int)(distancef * 10000f) + (planet.star.id * 100) + planet.index;
                distanceStr = string.Format("  ({0:F1}ly)", distancef);
            }

            if (sortIndex >= 0)
            {
                distanceForSort = sortIndex;
            }

            d.distanceForSort = distanceForSort;
            d.distanceStr = distanceStr;
            //int targetItemId = itemSelection.lastSelectedItemId;

            //PlanetListData d = new PlanetListData()
            //{
            //    planetData = planet,
            //    starData = star,
            //    distanceForSort = distanceForSort,
            //    distanceStr = distanceStr,
            //    itemId = targetItemId,
            //};
            //_planetList.Add(d);

        }


        //menu

        public enum EMenuCommand
        {
            Close = 0,
            OpenStarmap,
            Fav,
            OpenLSTM,
            SetCruiseAssist,
        }

        public GameObject popupMenuBase;
        public UIPlanetFinderListItem popupMenuListItem;
        public UIPlanetFinderListItem menuTarget;
        public UIButton favBtn;

        public const float popupMenuTopMargin = 30f;

        public void CreateMenuBox()
        {
            UIItemTip uiitemTip = GameObject.Instantiate<UIItemTip>(Configs.builtin.uiItemTipPrefab, windowTrans);
            (uiitemTip.transform as RectTransform).sizeDelta = new Vector2(568f, 90f);
            popupMenuBase = uiitemTip.gameObject;
            foreach (Transform child in uiitemTip.transform)
            {
                if (child.name == "bg" || child.name == "border" || child.name == "shadow")
                {
                    continue;
                }

                GameObject.Destroy(child.gameObject);
            }
            foreach (Component child in uiitemTip.transform.GetComponents<Component>())
            {
                if (child.GetType() == typeof(RectTransform))
                {
                    continue;
                }
                GameObject.Destroy(child);
            }

            //close
            UIButton btn = Util.MakeSmallTextButton("X", 18f, 20f);
            btn.gameObject.name = "close-btn";
            Util.NormalizeRectWithTopLeft(btn, 542f, 6f, popupMenuBase.transform);
            btn.onClick += OnMenuSelect;
            btn.gameObject.SetActive(true);

            //fav
            favBtn = Util.MakeHiliteTextButton("★", 20f, 20f);
            favBtn.onClick += OnMenuSelect;
            favBtn.data = (int)EMenuCommand.Fav;
            RectTransform btnRect = Util.NormalizeRectWithTopLeft(favBtn, 32f, 60f, popupMenuBase.transform);


            //OpenStarmap btn
            btn = Util.MakeSmallTextButton("Locate", 44f, 20f);
            btn.gameObject.name = "locate-d-btn";
            Util.NormalizeRectWithTopLeft(btn, 60f, 60f, popupMenuBase.transform);
            btn.onClick += OnMenuSelect;
            btn.data = (int)EMenuCommand.OpenStarmap;
            btn.gameObject.SetActive(true);

            float x_ = 32f;
            //OpenLSTM
            if (PLFN.aLSTMIntg.canOpenPlanetId && PLFN.integrationWithLSTM.Value)
            {
                btn = Util.MakeSmallTextButton("LSTM", 40f, 20f);
                btn.gameObject.name = "filter-s-btn";
                Util.NormalizeRectWithTopLeft(btn, x_, 8f, popupMenuBase.transform);
                btn.onClick += OnMenuSelect;
                btn.data = (int)EMenuCommand.OpenLSTM;
                btn.gameObject.SetActive(true);
                x_ += 50f;
            }

            //CruiseAssist
            if (PLFN.aCruiseAssistIntg.canSelectPlanet && PLFN.integrationWithCruiseAssist.Value)
            {
                btn = Util.MakeSmallTextButton("CruiseAssist", 60f, 20f);
                btn.gameObject.name = "filter-item-btn";
                Util.NormalizeRectWithTopLeft(btn, x_, 8f, popupMenuBase.transform);
                btn.onClick += OnMenuSelect;
                btn.data = (int)EMenuCommand.SetCruiseAssist;
                btn.gameObject.SetActive(true);
            }


            UIPlanetFinderListItem baseItem = UIPlanetFinderListItem.CreateListViewPrefab();
            //Destroy(baseItem.transform.Find("locate-btn").gameObject);
            (baseItem.transform as RectTransform).sizeDelta = new Vector2(566f, 24f);
            (baseItem.baseObj.transform as RectTransform).sizeDelta = new Vector2(566f, 24f);
            popupMenuListItem = baseItem;
            Util.NormalizeRectWithTopLeft(popupMenuListItem, 1f, popupMenuTopMargin, popupMenuBase.transform);
            popupMenuBase.SetActive(false);
        }

        public void OnMenuSelect(int obj)
        {
            popupMenuBase.SetActive(false);
            if (_eventLock || menuTarget == null)
            {
                return;
            }

            switch ((EMenuCommand)obj)
            {
                case EMenuCommand.OpenStarmap:
                    if (menuTarget.planetData != null)
                    {
                        PLFN.LocatePlanet(menuTarget.planetData.id);
                    }
                    else if (menuTarget.starData != null)
                    {
                        PLFN.LocatePlanet(0, menuTarget.starData.id);
                    }
                    break;
                case EMenuCommand.OpenLSTM:
                    PLFN.aLSTMIntg.OpenPlanetId(menuTarget.planetData?.id ?? 0);
                    break;
                case EMenuCommand.SetCruiseAssist:
                    PLFN.aCruiseAssistIntg.SelectPlanetOrStar(menuTarget.planetData, menuTarget.planetData.star);
                    break;
                case EMenuCommand.Fav:
                    PLFN.SetFavPlanet(menuTarget.planetData, !PLFN.IsFavPlanet(menuTarget.planetData));
                    favBtn.highlighted = PLFN.IsFavPlanet(menuTarget.planetData);
                    menuTarget.SetUpDisplayName();
                    //popupMenuListItem.SetUpDisplayName(); //すぐ閉じるので不要
                    break;
                default:
                    break;
            }
            menuTarget = null;
        }

        public void ShowMenu(UIPlanetFinderListItem item)
        {
            if (popupMenuBase.activeSelf)
            {
                popupMenuBase.SetActive(false);
                return;
            }
            if (item == popupMenuListItem)
            {
                return;
            }
            if (item.listData.planetData != null)
            {
                menuTarget = item;
                ////menuTarget.LockAppearance();
                popupMenuListItem.Init(menuTarget.listData, this);
                popupMenuListItem.disableUIAction = true;
                Vector3 pos = menuTarget.transform.position;

                popupMenuBase.transform.position = pos;
                Vector2 localPos = (popupMenuBase.transform as RectTransform).localPosition;
                localPos.x -= 1f;
                localPos.y += popupMenuTopMargin;
                (popupMenuBase.transform as RectTransform).localPosition = localPos;

                favBtn.highlighted = PLFN.IsFavPlanet(menuTarget.planetData);
                popupMenuBase.SetActive(true);
            }

        }
    }
}

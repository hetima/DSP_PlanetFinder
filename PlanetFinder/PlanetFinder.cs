//using System.Text;
using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;
using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using static PlanetFinderMod.UIPlanetFinderWindow;

namespace PlanetFinderMod
{

    [BepInPlugin(__GUID__, __NAME__, "1.1.3")]
    public class PLFN : BaseUnityPlugin
    {
        public const string __NAME__ = "PlanetFinder";
        public const string __GUID__ = "com.hetima.dsp." + __NAME__;

        new internal static ManualLogSource Logger;

        public static UIPlanetFinderWindow planetFinder;
        public static UIConfigWindow _configWin;
        public static List<PlanetData> recentPlanets;

        public static ConfigEntry<KeyboardShortcut> mainWindowHotkey;
        public static ConfigEntry<bool> showButtonInMainPanel;
        public static ConfigEntry<bool> showButtonInStarmap;
        public static ConfigEntry<bool> showFavButtonInStarmap;
        public static ConfigEntry<bool> showPowerState;
        public static ConfigEntry<bool> showPrefix;
        public static ConfigEntry<string> gasGiantPrefix;
        public static ConfigEntry<string> tidalLockedPrefix;
        public static ConfigEntry<float> mainWindowSize;

        public static ConfigEntry<bool> integrationWithDSPStarMapMemo;
        public static ConfigEntry<bool> integrationWithLSTM;
        public static ConfigEntry<bool> integrationWithCruiseAssist;


        void Awake()
        {
            Logger = base.Logger;

            Harmony harmony = new Harmony(__GUID__);

            mainWindowHotkey = Config.Bind("Keyboard Shortcuts", "mainWindowHotkey", KeyboardShortcut.Deserialize("F + LeftControl"),
                "Hotkey to open/close window");
            showButtonInMainPanel = Config.Bind("UI", "showButtonInMainPanel", true,
                "show open/close button in main panel");
            showButtonInStarmap = Config.Bind("UI", "showButtonInStarmap", true,
                "show open/close button in starmap");
            showFavButtonInStarmap = Config.Bind("UI", "showFavButtonInStarmap", false,
                "show toggle favorited button in starmap planet detail panel");
            showPowerState = Config.Bind("UI", "showPowerState", true,
                "show power consumption");
            showPrefix = Config.Bind("UI", "showPrefix", true,
                "show prefix [GAS] and [TL]");
            gasGiantPrefix = Config.Bind("UI", "gasGiantPrefix", "[GAS]",
                "gas giant prefix default is [GAS]");
            tidalLockedPrefix = Config.Bind("UI", "tidalLockedPrefix", "[TL]",
                "tidal locked prefix default is [TL]");
            mainWindowSize = Config.Bind("UI", "mainWindowSize", 8f,
                "Main window row size (4-16)");

            integrationWithDSPStarMapMemo = Config.Bind("Integration", "integrationWithDSPStarMapMemo", false,
                "Display icons set by DSPStarMapMemo");
            integrationWithLSTM = Config.Bind("Integration", "integrationWithLSTM", false,
                "open LSTM from context menu");
            integrationWithCruiseAssist = Config.Bind("Integration", "integrationWithCruiseAssist", false,
                "set CruiseAssist from context menu");
            harmony.PatchAll(typeof(Patch));
            harmony.PatchAll(typeof(MyWindowCtl.Patch));
            harmony.PatchAll(typeof(StarDistance.Patch));
        }

        public static void Log(string str)
        {
            Logger.LogInfo(str);
        }

        public static void UniverseExplorationRequired()
        {
            UIMessageBox b = UIMessageBox.Show("Upgrades Required".Translate(), "To use this feature, Universe Exploration 4 is required.".Translate(), "OK", 0);
        }

        public static void LocatePlanet(int planetId, int starId = 0)
        {
            if (planetId <= 0 && starId <= 0)
            {
                return;
            }
            //planetFinder.keepOpen = true;
            //int local = GameMain.localPlanet != null ? GameMain.localPlanet.id : 0;

            if (GameMain.history.universeObserveLevel >= 4)
            {
                UIRoot.instance.uiGame.ShutPlayerInventory();
                UIRoot.instance.uiGame.ShutAllFunctionWindow();
                //UIRoot.instance.uiGame.ShutAllFullScreens();
                UIRoot.instance.uiGame.OpenStarmap();
                int starIdx = planetId > 0 ? planetId / 100 : starId;
                int planetIdx = planetId > 0 ? planetId % 100 : 0;
                UIStarmap map = UIRoot.instance.uiGame.starmap;
                //PlanetData planet = GameMain.galaxy.PlanetById(planetId);
                //if (planet != null){}
                
                map.focusPlanet = null;
                map.focusStar = map.starUIs[starIdx - 1];
                map.OnCursorFunction2Click(0);
                if (planetIdx > 0 && map.focusStar == null)
                {
                    map.focusPlanet = map.planetUIs[planetIdx - 1];
                    map.OnCursorFunction2Click(0);
                    //map.SetViewStar(star.star, true);
                    map.focusPlanet = map.planetUIs[planetIdx - 1]; //Function Panelを表示させるため
                    map.focusStar = null;
                }
                else
                {
                    //map.focusStar = map.starUIs[starIdx - 1]; //
                }
                
            }
            else
            {
                UniverseExplorationRequired();
            }

            //_win.keepOpen = false;
        }

        public static void TogglePlanetFinderWindow()
        {
            //int itemId = Util.ItemIdHintUnderMouse();
            //if (itemId > 0)
            //{
            //    _win.SetUpAndOpen(itemId, 0, false);
            //    return;
            //}

            if (planetFinder.active)
            {
                planetFinder._Close();
            }
            else
            {
                planetFinder.SetUpAndOpen();
            }
        }

        public static void AddRecentPlanet(PlanetData planetData)
        {
            if (recentPlanets.Contains(planetData))
            {
                recentPlanets.Remove(planetData);
            }
            recentPlanets.Insert(0, planetData);
            if (recentPlanets.Count > 99)
            {
                recentPlanets.RemoveAt(recentPlanets.Count - 1);
            }
        }


        public static bool IsFavPlanet(PlanetData planetData)
        {
            string name = planetData.overrideName;
            if (string.IsNullOrEmpty(name))
            {
                return false;
            }
            if (name.Contains("★"))
            {
                return true;
            }
            return false;
        }

        public static void SetFavPlanet(PlanetData planetData, bool flag)
        {
            if (flag)
            {
                if (IsFavPlanet(planetData))
                {
                    return;
                }
                string newName = planetData.displayName;
                newName += "★";
                planetData.overrideName = newName;

                GameMain.gameScenario?.NotifyOnPlanetNameChange();
                planetData.NotifyOnDisplayNameChange();
                GameMain.galaxy.NotifyAstroNameChange();
            }
            else
            {
                if (!IsFavPlanet(planetData))
                {
                    return;
                }
                string newName = planetData.overrideName;
                newName = newName.Replace("★", "").Trim();
                if (string.Equals(newName, planetData.overrideName))
                {
                    return;
                }

                if (string.Equals(newName, planetData.name))
                {
                    planetData.overrideName = "";
                }
                else
                {
                    planetData.overrideName = newName;
                }
                GameMain.gameScenario?.NotifyOnPlanetNameChange();
                planetData.NotifyOnDisplayNameChange();
                GameMain.galaxy.NotifyAstroNameChange();
            }

        }

        private void Update()
        {
            if (!GameMain.isRunning || GameMain.isPaused || GameMain.instance.isMenuDemo)
            {
                return;
            }

            if (VFInput.inputing)
            {
                return;
            }
            if (mainWindowHotkey.Value.IsDown())
            {
                //SelectItem();
                TogglePlanetFinderWindow();
            }
            else if (VFInput._closePanelE)
            {
                if (planetFinder.active)
                {
                    planetFinder._Close();
                }
            }
        }

        private void FixedUpdate()
        {
            if (planetFinder != null && planetFinder.isPointEnter)
            {
                VFInput.inScrollView = true;
            }
        }

        public static GameObject panelButtonGO;
        public static GameObject starmapButtonGO;
        public static GameObject favBtnGO;
        public static void CreateUI()
        {
            //main panel button
            UIPlanetGlobe panel = UIRoot.instance.uiGame.planetGlobe;
            Button src = panel.button2;
            UIGameMenu gameMenu = UIRoot.instance.uiGame.gameMenu;
            Sprite icon = gameMenu.dfIconButton?.transform.Find("icon")?.GetComponent<Image>()?.sprite;

            Button b = GameObject.Instantiate<Button>(src, src.transform.parent);
            panelButtonGO = b?.gameObject;
            RectTransform rect = panelButtonGO.transform as RectTransform;
            UIButton btn = panelButtonGO.GetComponent<UIButton>();
            Image img = panelButtonGO.transform.Find("button-2/icon")?.GetComponent<Image>();

            if (img != null)
            {
                img.sprite = icon;
            }
            if (panelButtonGO != null && btn != null)
            {
                panelButtonGO.name = "open-planet-finder";
                rect.localScale = new Vector3(0.6f, 0.6f, 0.6f);
                rect.anchoredPosition3D = new Vector3(136f, -60f, 0f);
                b.onClick.RemoveAllListeners();
                btn.onClick += OnShowWindowButtonClick;
                btn.tips.tipTitle = "Planet Finder";
                btn.tips.tipText = "open/close Planet Finder Window";
                btn.tips.corner = 9;
                btn.tips.offset = new Vector2(-20f, -20f);

                rect = panelButtonGO.transform as RectTransform;
                panelButtonGO.SetActive(showButtonInMainPanel.Value);
                showButtonInMainPanel.SettingChanged += (sender, args) => {
                    panelButtonGO.SetActive(showButtonInMainPanel.Value);
                };
            }

            //starmap button
            Transform parent = UIRoot.instance.uiGame.starmap.northButton?.transform.parent;
            if (parent != null)
            {
                starmapButtonGO = GameObject.Instantiate(panelButtonGO, parent);
                starmapButtonGO.SetActive(showButtonInStarmap.Value);
                btn = starmapButtonGO.GetComponent<UIButton>();
                btn.onClick += OnShowWindowButtonClick;
                btn.tips.corner = 7;
                btn.tips.offset = new Vector2(25f, -8f);
                rect = starmapButtonGO.transform as RectTransform;
                rect.anchorMin = new Vector2(1f, 0f);
                rect.anchorMax = new Vector2(1f, 0f);
                rect.pivot = new Vector2(1f, 0f);
                rect.anchoredPosition = new Vector2(-16f, 48f);
                showButtonInStarmap.SettingChanged += (sender, args) => {
                    starmapButtonGO.SetActive(showButtonInStarmap.Value);
                };
            }

            //AddFavButtonToStarmap
            UIPlanetDetail planetDetail = UIRoot.instance.uiGame.planetDetail;
            parent = planetDetail?.transform;
            if (parent != null)
            {
                UIButton favBtn = Util.MakeHiliteTextButton("★", 18f, 18f);
                favBtn.onClick += OnStarmapFavButtonClick;
                RectTransform btnRect = Util.NormalizeRectWithTopLeft(favBtn, 218f, 38f, parent);
                favBtnGO = favBtn.gameObject;
                favBtnGO.name = "planetfinder-fav";
                favBtnGO.SetActive(showFavButtonInStarmap.Value);
                showFavButtonInStarmap.SettingChanged += (sender, args) => {
                    favBtnGO.SetActive(showFavButtonInStarmap.Value);
                };
            }

        }

        public static void OnStarmapFavButtonClick(int obj)
        {
            UIPlanetDetail planetDetail = UIRoot.instance.uiGame.planetDetail;
            PlanetData planetData = planetDetail.planet;
            PLFN.SetFavPlanet(planetData, !PLFN.IsFavPlanet(planetData));
            favBtnGO.GetComponent<UIButton>().highlighted = PLFN.IsFavPlanet(planetData);
            if (!planetDetail.nameInput.isFocused)
            {
                planetDetail.nameInput.text = planetDetail.planet.displayName;
            }
        }

        public static void OnShowWindowButtonClick(int obj)
        {
            //showStationInfo = !_showStationInfo;
            TogglePlanetFinderWindow();
        }

        public static LSTMIntg aLSTMIntg;
        public static DSPStarMapMemoIntg aDSPStarMapMemoIntg;
        public static CruiseAssistIntg aCruiseAssistIntg;
        static class Patch
        {
            internal static bool _initialized = false;


            [HarmonyPrefix, HarmonyPatch(typeof(GameMain), "Begin")]
            public static void GameMain_Begin_Prefix()
            {
                if (!_initialized)
                {
                    _initialized = true;
                    aLSTMIntg = new LSTMIntg();
                    aDSPStarMapMemoIntg = new DSPStarMapMemoIntg();
                    aCruiseAssistIntg = new CruiseAssistIntg();

                    planetFinder = UIPlanetFinderWindow.CreateInstance();
                    _configWin = UIConfigWindow.CreateWindow();
                    recentPlanets = new List<PlanetData>(100);

                    CreateUI();
                }
                planetFinder?.BeginGame();
            }

            [HarmonyPrefix, HarmonyPatch(typeof(DSPGame), "EndGame")]
            public static void DSPGame_EndGame_Prefix()
            {
                planetFinder?.EndGame();
            }


            [HarmonyPostfix, HarmonyPatch(typeof(GameData), "ArrivePlanet")]
            public static void GameData_ArrivePlanet(PlanetData planet)
            {
                AddRecentPlanet(planet);
            }

            //OnPlanetDataSet と _OnUpdate(30フレームおき)
            [HarmonyPostfix, HarmonyPatch(typeof(UIPlanetDetail), "RefreshDynamicProperties")]
            public static void UIPlanetDetail_RefreshDynamicProperties(UIPlanetDetail __instance)
            {
                if (showFavButtonInStarmap.Value && favBtnGO.activeSelf)
                {
                    favBtnGO.GetComponent<UIButton>().highlighted = PLFN.IsFavPlanet(__instance.planet);
                }
            }
        }

    }
}


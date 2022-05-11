using System;
//using System.Text;
using System.Reflection;
using System.Reflection.Emit;
using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;
using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace PlanetFinderMod
{

    [BepInPlugin(__GUID__, __NAME__, "0.1.0")]
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
        public static ConfigEntry<bool> showPowerState;


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
            showPowerState = Config.Bind("UI", "showPowerState", true,
                "show power consumption");
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

        public static void LocatePlanet(int planetId)
        {
            if (planetId <= 0)
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
                int starIdx = planetId / 100;
                int planetIdx = planetId % 100;
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

        }

        public static void OnShowWindowButtonClick(int obj)
        {
            //showStationInfo = !_showStationInfo;
            TogglePlanetFinderWindow();
        }

        public static LSTMIntg aLSTMIntg;
        public static DSPStarMapMemoIntg aDSPStarMapMemoIntg;
        static class Patch
        {
            internal static bool _initialized = false;


            [HarmonyPrefix, HarmonyPatch(typeof(GameMain), "Begin")]
            public static void GameMain_Begin_Prefix()
            {
                if (!_initialized)
                {
                    _initialized = true;
                    planetFinder = UIPlanetFinderWindow.CreateInstance();
                    _configWin = UIConfigWindow.CreateWindow();
                    recentPlanets = new List<PlanetData>(100);

                    aLSTMIntg = new LSTMIntg();
                    aDSPStarMapMemoIntg = new DSPStarMapMemoIntg();
                    CreateUI();
                }
            }

            [HarmonyPostfix, HarmonyPatch(typeof(GameData), "ArrivePlanet")]
            public static void GameData_ArrivePlanet(PlanetData planet)
            {
                AddRecentPlanet(planet);
            }

        }

    }


    public class LSTMIntg
    {
        public MethodInfo _openPlanetId;

        public readonly bool canOpenPlanetId;

        public LSTMIntg()
        {
            canOpenPlanetId = false;
            _openPlanetId = null;
            try
            {
                Dictionary<string, PluginInfo> plugins = BepInEx.Bootstrap.Chainloader.PluginInfos;
                if (plugins.TryGetValue("com.hetima.dsp.LSTM", out PluginInfo pluginInfo) && pluginInfo.Instance != null)
                {
                    _openPlanetId = pluginInfo.Instance.GetType().GetMethod("IntegrationOpenPlanetId", new[] { typeof(int) });
                    canOpenPlanetId = (_openPlanetId != null);
                }
            }
            catch (Exception)
            {

            }
        }

        public void OpenPlanetId(int planetId)
        {
            if (_openPlanetId != null)
            {
                object[] param = { planetId };
                _openPlanetId.Invoke(null, param);
            }
        }
    }

    public class DSPStarMapMemoIntg
    {
        //public MethodInfo _openPlanetId;

        public readonly bool canGetSignalIconId;
        private FieldInfo _memoPool;
        private FieldInfo _signalIconIdField;
        //private MemberInfo memoStruct;

        public struct Memo
        {
            public int id;
            public int[] signalIconId;
            public Color color;
            public string desc;
        }

        public DSPStarMapMemoIntg()
        {
            canGetSignalIconId = false;
            try
            {
                Dictionary<string, PluginInfo> plugins = BepInEx.Bootstrap.Chainloader.PluginInfos;
                if (plugins.TryGetValue("Appun.DSP.plugin.StarMapMemo", out PluginInfo pluginInfo) && pluginInfo.Instance != null)
                {
                    Type classType = pluginInfo.Instance.GetType().Assembly.GetType("DSPStarMapMemo.MemoPool");
                    Type classType2 = pluginInfo.Instance.GetType().Assembly.GetType("DSPStarMapMemo.MemoPool+Memo");
                    if (classType != null && classType2 != null)
                    {
                        _signalIconIdField = classType2.GetField("signalIconId");
                        _memoPool = classType.GetField("memoPool", BindingFlags.Public | BindingFlags.Static);
                    }
                    canGetSignalIconId = (_memoPool != null);
                }
            }
            catch (Exception)
            {

            }
        }

        public int GetSignalIconId(int planetId)
        {
            if (_memoPool != null && _signalIconIdField != null)
            {
                object d = _memoPool.GetValue(null);
                if (d == null)
                {
                    return 0;
                }
                object[] arg = new object[] { planetId, null };
                d.GetType().InvokeMember("TryGetValue", BindingFlags.InvokeMethod, null, d, arg);
                if (arg.Length == 2 && arg[1] != null)
                {
                    object value = arg[1];
                    object signalIconIdObj = _signalIconIdField.GetValue(value);
                    if (signalIconIdObj != null)
                    {
                        int[] signalIconId = signalIconIdObj as int[];
                        for (int i = 0; i < signalIconId.Length; i++)
                        {
                            if (signalIconId[i] != 0)
                            {
                                return signalIconId[i];
                            }
                        }
                    }
                }
            }
            return 0;
        }

    }
}


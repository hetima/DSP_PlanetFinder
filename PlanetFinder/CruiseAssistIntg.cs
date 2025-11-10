using System;
//using System.Text;
using System.Reflection;
using BepInEx;
using System.Collections.Generic;

namespace PlanetFinderMod
{
    public class CruiseAssistIntg
    {
        public MethodInfo _selectPlanetOrStar;
        public MethodInfo _selectPlanetOrStar_CAAP_N;

        public readonly bool canSelectPlanet;

        public CruiseAssistIntg()
        {
            canSelectPlanet = false;
            _selectPlanetOrStar = null;
            _selectPlanetOrStar_CAAP_N = null;
            try
            {
                PluginInfo pluginInfo;
                Dictionary<string, PluginInfo> plugins = BepInEx.Bootstrap.Chainloader.PluginInfos;
                if (plugins.TryGetValue("tanu.CruiseAssist", out pluginInfo) && pluginInfo.Instance != null)
                {
                    Type classType = pluginInfo.Instance.GetType().Assembly.GetType("tanu.CruiseAssist.CruiseAssistStarListUI");
                    _selectPlanetOrStar = classType.GetMethod("SelectStar", new[] { typeof(StarData), typeof(PlanetData) });
                    canSelectPlanet = (_selectPlanetOrStar != null);
                }
                else if (plugins.TryGetValue("nord.CruiseAssist", out pluginInfo) && pluginInfo.Instance != null)
                {
                    Type classType = pluginInfo.Instance.GetType().Assembly.GetType("tanu.CruiseAssist.CruiseAssistStarListUI");
                    _selectPlanetOrStar_CAAP_N = classType.GetMethod("SelectStar", new[] { typeof(StarData), typeof(PlanetData), typeof(bool) });
                    canSelectPlanet = (_selectPlanetOrStar_CAAP_N != null);
                }
            }
            catch (Exception)
            {

            }
        }

        public void SelectPlanetOrStar(PlanetData planet, StarData star = null)
        {
            if (_selectPlanetOrStar == null && _selectPlanetOrStar_CAAP_N == null)
            {
                return;
            }
            if (planet != null && star == null)
            {
                star = planet.star;
            }
            if (_selectPlanetOrStar != null && (planet != null || star != null))
            {
                object[] param = { star, planet };
                _selectPlanetOrStar.Invoke(null, param);
            }
            else if (_selectPlanetOrStar_CAAP_N != null && (planet != null || star != null))
            {
                object[] param = { star, planet, true };
                _selectPlanetOrStar_CAAP_N.Invoke(null, param);
            }
        }
    }
}


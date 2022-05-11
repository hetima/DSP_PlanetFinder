using System;
//using System.Text;
using System.Reflection;
using BepInEx;
using System.Collections.Generic;

namespace PlanetFinderMod
{
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
}


using System;
//using System.Text;
using System.Reflection;
using BepInEx;
using System.Collections.Generic;

namespace PlanetFinderMod
{
    public class DSPStarMapMemoIntg
    {
        public readonly bool canGetSignalIconId;
        public readonly bool canGetDesc;
        private FieldInfo _memoPool;
        private FieldInfo _signalIconIdField;
        private FieldInfo _descField;

        public DSPStarMapMemoIntg()
        {
            canGetSignalIconId = false;
            canGetDesc = false;
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
                        _descField = classType2.GetField("desc");
                        _memoPool = classType.GetField("memoPool", BindingFlags.Public | BindingFlags.Static);
                    }
                    canGetSignalIconId = (_memoPool != null && _signalIconIdField != null);
                    canGetDesc = (_memoPool != null && _descField != null);
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
                bool flag = (bool)d.GetType().InvokeMember("TryGetValue", BindingFlags.InvokeMethod, null, d, arg);
                if (flag && arg.Length == 2 && arg[1] != null)
                {
                    object signalIconIdObj = _signalIconIdField.GetValue(arg[1]);
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
        public string GetDesc(int planetId)
        {
            if (_memoPool != null && _descField != null)
            {
                object d = _memoPool.GetValue(null);
                if (d == null)
                {
                    return null;
                }
                object[] arg = new object[] { planetId, null };
                bool flag = (bool)d.GetType().InvokeMember("TryGetValue", BindingFlags.InvokeMethod, null, d, arg);
                if (flag && arg.Length == 2 && arg[1] != null)
                {
                    object descFieldObj = _descField.GetValue(arg[1]);
                    if (descFieldObj != null)
                    {
                        string desc = descFieldObj as string;
                        return desc;
                    }
                }
            }
            return null;
        }
    }
}


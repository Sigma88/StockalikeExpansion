using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Kopernicus;
using Random = System.Random;
using SASSPlugin;


namespace SASSxPlugin
{
    [KSPAddon(KSPAddon.Startup.Instantly, true)]
    public class FixLoadingScreen : SASSPlugin.FixLoadingScreen
    {
        public static bool useSASSxLoadingScreen = true;

        void Awake()
        {
            useSASSxLoadingScreen = GetSetting("useSASSxLoadingScreen", true);
            if (useSASSLoadingScreen && useSASSxLoadingScreen)
                useSASSLoadingScreen = GetSetting("keepSASSLoadingScreen", false);

            if (useSASSxLoadingScreen)
            {
                newLoadingScreen = new SASSxLoadingScreen();
            }
        }

        bool GetSetting(string name, bool Default)
        {
            bool output = Default;

            foreach (ConfigNode Settings in GameDatabase.Instance.GetConfigNodes("SASSxLoadingScreen"))
            {
                if (Settings.HasValue(name))
                {
                    NumericParser<bool> userSetting = new NumericParser<bool>();
                    userSetting.SetFromString(Settings.GetValue(name));
                    if (userSetting.value == Default)
                    {
                        return Default;
                    }
                    else
                    {
                        output = !Default;
                    }
                }
            }

            return output;
        }
    }

    public class SASSxLoadingScreen : SASSLoadingScreen
    {
        public override void UpdateScreens(LoadingScreen.LoadingScreenState screen)
        {
            List<string> newTips = new List<string>();
            List<Texture2D> newScreens = new List<Texture2D>();

            if (SASSPlugin.FixLoadingScreen.useSASSLoadingScreen)
            {
                newTips.AddRange(SASSTips());
                newScreens.AddRange(SASSScreens());
            }

            if (FixLoadingScreen.useSASSxLoadingScreen)
            {
                newTips.AddRange(SASSxTips());
                newScreens.AddRange(SASSxScreens());
            }

            if (newTips.Count > 0)
                screen.tips = newTips.ToArray();

            if (newScreens.Count > 0)
                screen.screens = newScreens.ToArray();
        }

        public List<string> SASSxTips()
        {
            return new List<string>();
        }

        public List<Texture2D> SASSxScreens()
        {
            return new List<Texture2D>();
        }
    }
}

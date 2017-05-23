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
    class FixLoadingScreen : SASSPlugin.FixLoadingScreen
    {
        void Awake()
        {
            bool useSASSxLoadingScreen = true;

            foreach (ConfigNode Settings in GameDatabase.Instance.GetConfigNodes("SASSxLoadingScreen"))
            {
                if (Settings.HasValue("useSASSxLoadingScreen"))
                {
                    NumericParser<bool> userSetting = new NumericParser<bool>();
                    userSetting.SetFromString(Settings.GetValue("useSASSxLoadingScreen"));
                    if (userSetting.value == false)
                    {
                        useSASSxLoadingScreen = false;
                    }
                    if (userSetting.value == true)
                    {
                        useSASSxLoadingScreen = true;
                        break;
                    }
                }
            }

            if (useSASSxLoadingScreen)
            {
                newLoadingScreen = new SASSxLoadingScreen();
            }
        }
    }

    class SASSxLoadingScreen : NewLoadingScreen
    {
        public void UpdateScreen()
        {
            Debug.Log("SigmaLog: LOADING SCREENS INTERCEPTED BY SASSx");
        }
    }
}

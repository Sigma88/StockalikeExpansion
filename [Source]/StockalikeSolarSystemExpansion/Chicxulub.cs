using UnityEngine;
using Kopernicus;
using Kopernicus.Components;
using Kopernicus.Configuration;
using Kopernicus.OnDemand;
using System.Text;
using System.IO;
using System.Reflection;
using System.Linq;
using KSP.UI.Screens;


namespace SASSXPlugin
{
    [KSPAddon(KSPAddon.Startup.EveryScene, false)]
    public class ChicxulubFixer : MonoBehaviour
    {
        public static CelestialBody earth = null;
        public static CelestialBody impactor = null;
        public static Texture2D MainTex = null;
        public static Texture2D BumpMap = null;
        public static CBAttributeMapSO CraterBiomeMap = ScriptableObject.CreateInstance<CBAttributeMapSO>();
        public static CBAttributeMapSO PlateauBiomeMap = ScriptableObject.CreateInstance<CBAttributeMapSO>();

        public static Time now = Time.NULL;
        public enum Time
        {
            NULL,
            PREASTEROID,
            ASTEROID,
            POSTASTEROID
        }

        void Update()
        {
            if (HighLogic.LoadedScene == GameScenes.MAINMENU)
            {
                if (earth == null)
                    TextureEditor();

                if (impactor == null)
                    impactor = FlightGlobals.Bodies.First(b => b.transform.name == "Chicxulub");
            }


            if (earth != null && CheckTime())
            {
                if (now == Time.POSTASTEROID)
                    RestoreCrater(earth);
                else
                    RemoveCrater(earth);

                FixBiomeMap(earth);

                if (impactor != null)
                {
                    if (now == Time.ASTEROID)
                        ShowAsteroid(impactor);
                    else
                        HideAsteroid(impactor);
                }
            }
        }


        void TextureEditor()
        {
            earth = FlightGlobals.GetHomeBody();

            // CREATE MODIFIED COLOR MAP

            MainTex = Utility.CreateReadable(Resources.FindObjectsOfTypeAll<Texture>().First(t => t.name == "KerbinScaledSpace300") as Texture2D);
            Texture2D PlateauMain = Utility.CreateReadable(Resources.FindObjectsOfTypeAll<Texture>().First(t => t.name == "StockalikeSolarSystemExpansion/Configs/Bodies/Earth/EarthData/SpoilersInside/Textures/PlateauMain") as Texture2D);
            if (MainTex != null && PlateauMain != null)
            {
                MainTex.SetPixels(2872, 913, PlateauMain.width, PlateauMain.height, PlateauMain.GetPixels());
                MainTex.name = "EarthPlateauMainTex";
                MainTex.Apply();
            }

            // CREATE MODIFIED BUMP MAP

            BumpMap = Utility.CreateReadable(Resources.FindObjectsOfTypeAll<Texture>().First(t => t.name == "StockalikeSolarSystemExpansion/Configs/Bodies/Earth/EarthData/SpoilersInside/Textures/EarthCraterBumpMap") as Texture2D);
            Texture2D PlateauBump = Utility.CreateReadable(Resources.FindObjectsOfTypeAll<Texture>().First(t => t.name == "StockalikeSolarSystemExpansion/Configs/Bodies/Earth/EarthData/SpoilersInside/Textures/PlateauBump") as Texture2D);

            if (BumpMap != null && PlateauBump != null)
            {
                BumpMap.SetPixels(2872, 913, PlateauBump.width, PlateauBump.height, PlateauBump.GetPixels());
                BumpMap.name = "EarthPlateauBumpMap";
                BumpMap.Apply();
            }

            // CREATE MODIFIED BIOME MAPS

            Texture2D CPWBiomes = Utility.CreateReadable(Resources.FindObjectsOfTypeAll<Texture>().First(t => t.name == "StockalikeSolarSystemExpansion/Configs/Bodies/Earth/EarthData/SpoilersInside/Textures/CraterPlateauWaterBiomes") as Texture2D);
            CBAttributeMapSO kerbinbiome = earth.BiomeMap;

            Texture2D CraterBiomes = new Texture2D(kerbinbiome.Width, kerbinbiome.Height);
            Texture2D PlateauBiomes = new Texture2D(kerbinbiome.Width, kerbinbiome.Height);

            Color Crater = new Color(0.6156862745098040f, 0.0745098039215686f, 0.0745098039215686f, 1);
            Color Plateau = new Color(0.8941176470588240f, 0.4823529411764710f, 0.1098039215686270f, 1);
            Color Water = new Color(0.2156862745098040f, 0.3843137254901960f, 0.6705882352941180f, 1);

            int h = CPWBiomes.height / 3;

            for (int x = 0; x < kerbinbiome.Width; x++)
            {
                for (int y = 0; y < kerbinbiome.Height; y++)
                {
                    Color Biome = kerbinbiome.GetPixelColor(x, y);
                    if (x > 2903 && x < 2904 + CPWBiomes.width && y > 969 && y < 970 + h)
                    {
                        Color color = CPWBiomes.GetPixel(x - 2904, y - 970);
                        if (color.r == 1)
                            CraterBiomes.SetPixel(x, y, Crater);
                        else
                            CraterBiomes.SetPixel(x, y, Biome);

                        color = CPWBiomes.GetPixel(x - 2904, y - 970 + h);
                        if (color.g == 1)
                            PlateauBiomes.SetPixel(x, y, Plateau);
                        else
                        {
                            color = CPWBiomes.GetPixel(x - 2904, y - 970 + h + h);
                            if (color.b == 1)
                                PlateauBiomes.SetPixel(x, y, Water);
                            else
                                PlateauBiomes.SetPixel(x, y, Biome);
                        }
                    }
                    else
                    {
                        CraterBiomes.SetPixel(x, y, Biome);
                        PlateauBiomes.SetPixel(x, y, Biome);
                    }
                }
            }

            CraterBiomeMap.CreateMap(MapSO.MapDepth.RGB, CraterBiomes);
            PlateauBiomeMap.CreateMap(MapSO.MapDepth.RGB, PlateauBiomes);
            DestroyImmediate(CraterBiomes);
            DestroyImmediate(PlateauBiomes);
        }


        void RemoveCrater(CelestialBody earth)
        {
            // CHANGE COLOR MAP

            if (MainTex != null && BumpMap != null)
            {
                earth.scaledBody.GetComponent<Renderer>().material.SetTexture("_MainTex", MainTex);
                earth.scaledBody.GetComponent<Renderer>().material.SetTexture("_BumpMap", BumpMap);
                if (OnDemandStorage.useOnDemand)
                {
                    ScaledSpaceDemand demand = earth.scaledBody.GetComponent<ScaledSpaceDemand>();
                    demand.texture = MainTex.name;
                    demand.normals = BumpMap.name;
                }
            }

            // ADD PQS MOD

            foreach (PQSMod mod in earth.GetComponentsInChildren<PQSMod>(true))
            {
                if (mod.name == "MapDecal" && mod.order == 10 && !mod.modEnabled)
                {
                    mod.modEnabled = true;
                    if (HighLogic.LoadedScene == GameScenes.FLIGHT)
                        mod.sphere.RebuildSphere();
                }
            }
        }


        void RestoreCrater(CelestialBody earth)
        {
            // Restore COLOR MAP AND BUMP MAP

            Texture MainTex = Resources.FindObjectsOfTypeAll<Texture>().First(t => t.name == "KerbinScaledSpace300");
            Texture BumpMap = Resources.FindObjectsOfTypeAll<Texture>().First(t => t.name == "StockalikeSolarSystemExpansion/Configs/Bodies/Earth/EarthData/SpoilersInside/Textures/EarthCraterBumpMap");

            if (MainTex != null && BumpMap != null)
            {
                earth.scaledBody.GetComponent<Renderer>().material.SetTexture("_MainTex", MainTex);
                earth.scaledBody.GetComponent<Renderer>().material.SetTexture("_BumpMap", BumpMap);
                if (OnDemandStorage.useOnDemand)
                {
                    ScaledSpaceDemand demand = earth.scaledBody.GetComponent<ScaledSpaceDemand>();
                    demand.texture = MainTex.name;
                    demand.normals = BumpMap.name;
                }
            }

            // REMOVE PQS MOD

            foreach (PQSMod mod in earth.GetComponentsInChildren<PQSMod>(true))
            {
                if (mod.name == "MapDecal" && mod.order == 10 && mod.modEnabled)
                {
                    mod.modEnabled = false;
                    if (HighLogic.LoadedScene == GameScenes.FLIGHT)
                        mod.sphere.RebuildSphere();
                }
            }
        }


        void ShowAsteroid(CelestialBody body)
        {
            body.Set("drawMode", OrbitRenderer.DrawMode.REDRAW_AND_RECALCULATE);
            body.Set("drawIcons", OrbitRenderer.DrawIcons.OBJ);
            body.scaledBody.SetActive(true);
            body.sphereOfInfluence = body.Radius * 3;
        }


        void HideAsteroid(CelestialBody body)
        {
            body.Set("drawMode", OrbitRenderer.DrawMode.OFF);
            body.Set("drawIcons", OrbitRenderer.DrawIcons.NONE);
            body.scaledBody.SetActive(false);
            body.sphereOfInfluence = 0;
        }


        void FixBiomeMap(CelestialBody body)
        {
            if (now == Time.POSTASTEROID)
                body.BiomeMap = CraterBiomeMap;
            else
                body.BiomeMap = PlateauBiomeMap;
        }

        bool CheckTime()
        {
            double time = Planetarium.GetUniversalTime() - Templates.epoch;
            Time then = now;

            if (time < -598122413200000)
                now = Time.PREASTEROID;
            else if (time < -598122400976110) // -598122400989261
                now = Time.ASTEROID;
            else
                now = Time.POSTASTEROID;

            return then != now;
        }
    }
}

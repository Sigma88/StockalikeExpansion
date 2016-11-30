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
    [KSPAddon(KSPAddon.Startup.SpaceCentre, true)]
    public class TextureEditor : MonoBehaviour
    {
        void Start()
        {
            ChicxulubFixer.earth = FlightGlobals.GetHomeBody();
            ChicxulubFixer.impactor = FlightGlobals.Bodies.First(b => b.transform.name == "Chicxulub");

            // CREATE MODIFIED COLOR MAP

            ChicxulubFixer.MainTex = Utility.CreateReadable(Resources.FindObjectsOfTypeAll<Texture>().First(t => t.name == "KerbinScaledSpace300") as Texture2D);
            Texture2D PlateauMain = Utility.CreateReadable(Resources.FindObjectsOfTypeAll<Texture>().First(t => t.name == "StockalikeSolarSystemExpansion/Configs/Bodies/Earth/EarthData/SpoilersInside/Textures/PlateauMain") as Texture2D);
            if (ChicxulubFixer.MainTex != null && PlateauMain != null)
            {
                ChicxulubFixer.MainTex.SetPixels(2872, 913, PlateauMain.width, PlateauMain.height, PlateauMain.GetPixels());
                ChicxulubFixer.MainTex.name = "EarthPlateauMainTex";
                ChicxulubFixer.MainTex.Apply();
            }

            // CREATE MODIFIED BUMP MAP

            ChicxulubFixer.BumpMap = Utility.CreateReadable(Resources.FindObjectsOfTypeAll<Texture>().First(t => t.name == "StockalikeSolarSystemExpansion/Configs/Bodies/Earth/EarthData/SpoilersInside/Textures/EarthCraterBumpMap") as Texture2D);
            Texture2D PlateauBump = Utility.CreateReadable(Resources.FindObjectsOfTypeAll<Texture>().First(t => t.name == "StockalikeSolarSystemExpansion/Configs/Bodies/Earth/EarthData/SpoilersInside/Textures/PlateauBump") as Texture2D);

            if (ChicxulubFixer.BumpMap != null && PlateauBump != null)
            {
                ChicxulubFixer.BumpMap.SetPixels(2872, 913, PlateauBump.width, PlateauBump.height, PlateauBump.GetPixels());
                ChicxulubFixer.BumpMap.name = "EarthPlateauBumpMap";
                ChicxulubFixer.BumpMap.Apply();
            }

            // CREATE MODIFIED BIOME MAPS

            Texture2D CPWBiomes = Utility.CreateReadable(Resources.FindObjectsOfTypeAll<Texture>().First(t => t.name == "StockalikeSolarSystemExpansion/Configs/Bodies/Earth/EarthData/SpoilersInside/Textures/CraterPlateauWaterBiomes") as Texture2D);
            CBAttributeMapSO kerbinbiome = ChicxulubFixer.earth.BiomeMap;

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

            ChicxulubFixer.CraterBiomeMap.CreateMap(MapSO.MapDepth.RGB, CraterBiomes);
            ChicxulubFixer.PlateauBiomeMap.CreateMap(MapSO.MapDepth.RGB, PlateauBiomes);
            DestroyImmediate(CraterBiomes);
            DestroyImmediate(PlateauBiomes);
        }
    }



    [KSPAddon(KSPAddon.Startup.EveryScene, false)]
    public class ChicxulubFixer : MonoBehaviour
    {
        public static CelestialBody earth = null;
        public static CelestialBody impactor = null;
        public static Texture2D MainTex = null;
        public static Texture2D BumpMap = null;
        public static CBAttributeMapSO CraterBiomeMap = ScriptableObject.CreateInstance<CBAttributeMapSO>();
        public static CBAttributeMapSO PlateauBiomeMap = ScriptableObject.CreateInstance<CBAttributeMapSO>();
        public bool Crater = true;
        public bool Asteroid = true;
        public bool FixBiomes = true;

        void Update()
        {
            if (earth != null)
            {
                double time = Planetarium.GetUniversalTime();
                double epoch = earth.orbit.epoch;
                if ((time - epoch) < -598122400989261 && Crater)
                {
                    Crater = false;
                    FixBiomes = true;
                    RemoveCrater(earth);
                }
                else if ((time - epoch) > -598122400989261 && !Crater)
                {
                    Crater = true;
                    FixBiomes = true;
                    RestoreCrater(earth);
                }
                if (FixBiomes)
                    FixBiomeMap(earth);
            }
            if (impactor != null)
            {
                double time = Planetarium.GetUniversalTime() - earth.orbit.epoch;
                if (time > -598122413200000 && time < -598122400989261)
                {
                    if (!Asteroid)
                    {
                        Asteroid = true;
                        ShowAsteroid(impactor);
                    }
                }
                else if (Asteroid)
                {
                    Asteroid = false;
                    HideAsteroid(impactor);
                }
            }
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
                    mod.sphere.RebuildSphere();
                }
            }
        }


        void ShowAsteroid(CelestialBody body)
        {
            body.orbitDriver.Renderer.drawMode = OrbitRenderer.DrawMode.REDRAW_AND_RECALCULATE;
            body.orbitDriver.Renderer.drawIcons = OrbitRenderer.DrawIcons.OBJ;
            body.scaledBody.SetActive(true);
            body.sphereOfInfluence = body.Radius * 3;
        }


        void HideAsteroid(CelestialBody body)
        {
            body.orbitDriver.Renderer.drawMode = OrbitRenderer.DrawMode.OFF;
            body.orbitDriver.Renderer.drawIcons = OrbitRenderer.DrawIcons.NONE;
            body.scaledBody.SetActive(false);
            body.sphereOfInfluence = 0;
        }


        public void FixBiomeMap(CelestialBody body)
        {
            FixBiomes = false;
            if (Crater)
                body.BiomeMap = CraterBiomeMap;
            else
                body.BiomeMap = PlateauBiomeMap;
        }
    }
}

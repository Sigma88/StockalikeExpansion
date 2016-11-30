using UnityEngine;
using Kopernicus;
using Kopernicus.Components;
using Kopernicus.Configuration;
using Kopernicus.OnDemand;
using System.Text;
using System;
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
            Texture2D PlateauMain = Utility.CreateReadable(Resources.FindObjectsOfTypeAll<Texture>().First(t => t.name == "StockalikeSolarSystemExpansion/Configs/Bodies/Earth/EarthData/SpoliersInside/Textures/PlateauMain") as Texture2D);
            if (ChicxulubFixer.MainTex != null && PlateauMain != null)
            {
                ChicxulubFixer.MainTex.SetPixels(2872, 913, PlateauMain.width, PlateauMain.height, PlateauMain.GetPixels());
                ChicxulubFixer.MainTex.name = "EarthPlateauMainTex";
                ChicxulubFixer.MainTex.Apply();
            }

            // CREATE MODIFIED BUMP MAP

            ChicxulubFixer.BumpMap = Utility.CreateReadable(Resources.FindObjectsOfTypeAll<Texture>().First(t => t.name == "StockalikeSolarSystemExpansion/Configs/Bodies/Earth/EarthData/SpoliersInside/Textures/EarthCraterBumpMap") as Texture2D);
            Texture2D PlateauBump = Utility.CreateReadable(Resources.FindObjectsOfTypeAll<Texture>().First(t => t.name == "StockalikeSolarSystemExpansion/Configs/Bodies/Earth/EarthData/SpoliersInside/Textures/PlateauBump") as Texture2D);

            if (ChicxulubFixer.BumpMap != null && PlateauBump != null)
            {
                ChicxulubFixer.BumpMap.SetPixels(2872, 913, PlateauBump.width, PlateauBump.height, PlateauBump.GetPixels());
                ChicxulubFixer.BumpMap.name = "EarthPlateauBumpMap";
                ChicxulubFixer.BumpMap.Apply();
            }

            // CREATE MODIFIED BIOME MAPS

            Texture2D CPWBiomes = Utility.CreateReadable(Resources.FindObjectsOfTypeAll<Texture>().First(t => t.name == "StockalikeSolarSystemExpansion/Configs/Bodies/Earth/EarthData/SpoliersInside/Textures/CraterPlateauWaterBiomes") as Texture2D);
            Texture2D CraterBiomes = Resources.FindObjectsOfTypeAll<CBAttributeMapSO>().First(t => t.name == "kerbin_biome").CompileRGB();
            Texture2D PlateauBiomes = Utility.CreateReadable(CraterBiomes);
            Color Crater = new Color(0.6156862745098040f, 0.0745098039215686f, 0.0745098039215686f);
            Color Plateau = new Color(0.8941176470588240f, 0.4823529411764710f, 0.1098039215686270f);
            Color Water = new Color(0.2156862745098040f, 0.3843137254901960f, 0.6705882352941180f);
            for (int x = 0; x < CPWBiomes.width; x++)
            {
                for (int y = 0; y < CPWBiomes.height; y++)
                {
                    Color color = CPWBiomes.GetPixel(x, y);
                    if (color.r == 1)
                        CraterBiomes.SetPixel(x + 2872, y + 913, Crater);
                    if (color.g == 1)
                        PlateauBiomes.SetPixel(x + 2872, y + 913, Plateau);
                    if (color.b == 1)
                        PlateauBiomes.SetPixel(x + 2872, y + 913, Water);
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
        public static CBAttributeMapSO CraterBiomeMap = null;
        public static CBAttributeMapSO PlateauBiomeMap = null;
        public bool Crater = true;
        public bool Asteroid = true;
        public bool FixBiomes = true;

        void Start()
        {
        }
        void Update()
        {
            if (earth != null)
            {
                double time = Planetarium.GetUniversalTime();
                double epoch = earth.orbit.epoch;
                if ((time - epoch) < -598122400989261 && Crater)
                {
                    Crater = false;
                    RemoveCrater(earth);
                }
                else if ((time - epoch) > -598122400989261 && !Crater)
                {
                    Crater = true;
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
            Texture BumpMap = Resources.FindObjectsOfTypeAll<Texture>().First(t => t.name == "StockalikeSolarSystemExpansion/Configs/Bodies/Earth/EarthData/SpoliersInside/Textures/EarthCraterBumpMap");

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
            if (Crater)
                body.BiomeMap = CraterBiomeMap;
            else
                body.BiomeMap = PlateauBiomeMap;
        }
    }
}

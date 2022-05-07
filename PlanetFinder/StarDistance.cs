using System.Collections.Generic;
using HarmonyLib;

namespace PlanetFinderMod
{
    public class StarDistance
    {
        public static class Patch
        {
            [HarmonyPrefix, HarmonyPatch(typeof(GameMain), "Begin")]
            public static void GameMain_Begin_Prefix()
            {
                if (_distances != null)
                {
                    _distances.Clear();
                }
                _distances = new Dictionary<int, Dictionary<int, float>>(GameMain.galaxy.starCount);
                StarDistance.InitDistances();
            }
        }

        public static Dictionary<int, Dictionary<int, float>> _distances;

        public static float DistanceFromHere(int star, int nearStar = 0)
        {
            int star2;
            if (GameMain.localPlanet != null)
            {
                star2 = GameMain.localPlanet.star.id;
            }
            else
            {
                star2 = nearStar;
            }
            if (star2 != 0)
            {
                return Distance(star, star2);
            }
            else
            {
                return -1f;
            }
        }
        public static float Distance(int star, int star2)
        {
            int key, key2;
            if (star == star2)
            {
                return 0f;
            }
            else if (star > star2)
            {
                key = star2;
                key2 = star;
            }
            else
            {
                key = star;
                key2 = star2;
            }

            _distances.TryGetValue(key, out Dictionary<int, float> dist);
            if (dist != null)
            {
                dist.TryGetValue(key2, out float val);
                return val;
            }
            return -1f;
        }

        internal static void InitDistances()
        {
            _distances.Clear();
            if (GameMain.instance.isMenuDemo)
            {
                return;
            }
            GalaxyData galaxy = GameMain.galaxy;
            for (int i = 0; i < galaxy.starCount; i++)
            {
                StarData star = galaxy.stars[i];

                Dictionary<int, float> dist = new Dictionary<int, float>(galaxy.starCount - i);

                for (int j = i + 1; j < galaxy.starCount; j++)
                {
                    float d;
                    StarData star2 = galaxy.stars[j];
                    double num = (star.uPosition - star2.uPosition).magnitude;
                    if (num < 2400000.0)
                    {
                        d = 1.0f;
                    }
                    else
                    {
                        d = (float)(num / 2400000.0);
                    }

                    dist.Add(star2.id, d);
                }
                _distances.Add(star.id, dist);

            }
        }
    }
}

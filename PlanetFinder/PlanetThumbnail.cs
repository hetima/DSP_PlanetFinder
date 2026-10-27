using System.Collections.Generic;
using UnityEngine;

namespace PlanetFinderMod
{
    /// <summary>
    /// Row icons taken from the artwork the game already ships.
    /// Planets use ThemeProto.uiIconSprite (one round planet icon per theme and style).
    /// Stars use the round-star-* sprites; those are grey glow masks, so they are tinted with the
    /// star colour and a giant is drawn larger to be told apart from a main sequence star.
    /// A thin outline ring is available behind a black hole icon.
    /// </summary>
    public static class PlanetThumbnail
    {
        private static readonly Dictionary<long, Sprite> planetCache = new Dictionary<long, Sprite>();
        private static readonly Dictionary<string, Sprite> starCache = new Dictionary<string, Sprite>();
        private static bool loggedFailure;
        private static bool loggedSummary;
        private static Sprite outline;

        public static Sprite Get(PlanetData pd)
        {
            if (pd == null || pd.theme <= 0)
            {
                return null;
            }
            int style = pd.style;
            long key = ((long)pd.theme << 8) | (long)(style & 0xff);
            if (planetCache.TryGetValue(key, out Sprite cached))
            {
                return cached;
            }

            Sprite sprite = null;
            try
            {
                ThemeProto proto = LDB.themes.Select(pd.theme);
                Sprite[] icons = proto?.uiIconSprite;
                if (icons != null && icons.Length > 0)
                {
                    if (style < 0 || style >= icons.Length)
                    {
                        style = 0;
                    }
                    sprite = icons[style] != null ? icons[style] : icons[0];
                }
                if (sprite == null)
                {
                    LogOnce("planet icon is not available for theme " + pd.theme + " style " + pd.style
                        + " (uiIconSprite=" + (icons == null ? "null" : icons.Length.ToString()) + "), using colour dots");
                }
            }
            catch (System.Exception e)
            {
                LogOnce("planet icon failed: " + e.Message);
            }
            planetCache[key] = sprite;
            return sprite;
        }

        public static Sprite GetStar(StarData sd)
        {
            if (sd == null)
            {
                return null;
            }
            string name = StarSpriteName(sd);
            if (starCache.TryGetValue(name, out Sprite cached))
            {
                return cached;
            }

            Sprite result = null;
            try
            {
                result = Resources.Load<Sprite>(name);
                if (result == null)
                {
                    //the sprite may only be referenced by a prefab; then take the loaded instance
                    foreach (Sprite candidate in Resources.FindObjectsOfTypeAll<Sprite>())
                    {
                        if (candidate != null && candidate.name == name)
                        {
                            result = candidate;
                            break;
                        }
                    }
                }
                if (result != null)
                {
                    //the star sprites are opaque glow masks with a black background; turn the brightness
                    //into alpha so the row does not show a black square around the star
                    Sprite clean = MakeAlphaSprite(result);
                    if (clean != null)
                    {
                        result = clean;
                    }
                }
            }
            catch (System.Exception e)
            {
                LogOnce("star icon failed: " + e.Message);
            }
            if (result == null)
            {
                LogOnce("star icon is not available (" + name + "), using colour dots");
            }
            starCache[name] = result;
            return result;
        }

        /// <summary>
        /// Converts an opaque glow mask (white on black) into a white sprite with the brightness as
        /// alpha, keeping the original frame so the icon size and position do not change.
        /// </summary>
        private static Sprite MakeAlphaSprite(Sprite source)
        {
            Texture texture = source != null ? source.texture : null;
            if (texture == null)
            {
                return null;
            }
            const int size = 64;
            RenderTexture rt = RenderTexture.GetTemporary(size, size, 0, RenderTextureFormat.ARGB32);
            RenderTexture previous = RenderTexture.active;
            Graphics.Blit(texture, rt);
            RenderTexture.active = rt;
            Texture2D flat = new Texture2D(size, size, TextureFormat.RGBA32, false);
            flat.ReadPixels(new Rect(0f, 0f, size, size), 0, 0);
            flat.Apply(false, false);
            Color[] pixels = flat.GetPixels();
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(rt);
            Object.Destroy(flat);

            float max = 0f;
            for (int i = 0; i < pixels.Length; i++)
            {
                float l = Mathf.Max(pixels[i].r, Mathf.Max(pixels[i].g, pixels[i].b)) * pixels[i].a;
                if (l > max)
                {
                    max = l;
                }
            }
            if (max <= 0.01f)
            {
                return null;
            }

            Texture2D icon = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color[] result = new Color[pixels.Length];
            float scale = 1.15f / max;
            for (int i = 0; i < pixels.Length; i++)
            {
                float l = Mathf.Max(pixels[i].r, Mathf.Max(pixels[i].g, pixels[i].b)) * pixels[i].a;
                result[i] = new Color(1f, 1f, 1f, Mathf.Clamp01(l * scale));
            }
            icon.SetPixels(result);
            icon.Apply(false, false);
            return Sprite.Create(icon, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
        }

        public static string StarSpriteName(StarData sd)
        {
            switch (sd.type)
            {
                case EStarType.GiantStar:
                    return "round-star-giant";
                case EStarType.WhiteDwarf:
                    return "round-star-whitedwarf";
                case EStarType.NeutronStar:
                    return "round-star-neutron";
                case EStarType.BlackHole:
                    return "round-star-blackhole";
                default:
                    return "round-star-mainseq";
            }
        }

        /// <summary>
        /// Thin white ring used as a light outline behind a black hole icon: its star colour is almost
        /// black, so without the outline the icon disappears into the dark row.
        /// </summary>
        public static Sprite OutlineSprite
        {
            get
            {
                if (outline == null)
                {
                    outline = MakeOutline();
                }
                return outline;
            }
        }

        private static Sprite MakeOutline()
        {
            const int size = 48;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x + 0.5f) / size * 2f - 1f;
                    float dy = (y + 0.5f) / size * 2f - 1f;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    //a narrow ring, no wide glow, so it reads as an outline around the icon
                    float ring = Mathf.Exp(-Mathf.Pow((d - 0.72f) / 0.055f, 2f));
                    pixels[y * size + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(ring * 0.75f));
                }
            }
            tex.SetPixels(pixels);
            tex.Apply(false, false);
            return Sprite.Create(tex, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size, 0, SpriteMeshType.FullRect);
        }

        private static void LogOnce(string message)
        {
            if (loggedFailure)
            {
                return;
            }
            loggedFailure = true;
            PLFN.LogWarning(message);
        }

    }
}

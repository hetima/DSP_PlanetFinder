using UnityEngine;
using UnityEngine.UI;

namespace PlanetFinderMod
{
    /// <summary>
    /// Row tooltip. Reverts to the native rendering path: the game's own UISimpleGeneralTip draws the
    /// frame and the orange title through SetTip(), so the appearance is the native one. The two fixes
    /// on top are only about content: the title text is written directly (the localization step added a
    /// gap between a Latin letter and Chinese) and the title is kept to a single line.
    /// </summary>
    public class RowTip : MonoBehaviour
    {
        private static RowTip instance;
        private static bool warnedPrefab;
        private static bool hookedConfig;
        private UISimpleGeneralTip tip;

        public static void Show(UIPlanetFinderListItem item, string titleText)
        {
            if (string.IsNullOrEmpty(titleText) || !PLFN.showRowTooltip.Value)
            {
                Hide();
                return;
            }
            titleText = titleText.Replace(" 型", "型").Replace(" 巨", "巨").Replace(" 恒", "恒");
            Ensure(item);
            if (instance == null)
            {
                return;
            }

            UISimpleGeneralTip.TipSettings settings = new UISimpleGeneralTip.TipSettings();
            settings.topLevel = true;
            settings.corner = 0;
            settings.width = 0;
            settings.offset = Vector2.zero;
            settings.tipTitle = titleText;
            settings.tipText = "";
            settings.tipSprite = null;

            RectTransform canvasRect = (RectTransform)instance.transform;
            instance.tip.gameObject.SetActive(true);
            instance.tip.SetTip(ref settings, canvasRect, titleText, "");

            //one line only: the native layout would reserve a body row and may wrap the title
            if (instance.tip.subTextComp != null)
            {
                instance.tip.subTextComp.gameObject.SetActive(false);
            }
            if (instance.tip.titleComp != null)
            {
                Text title = instance.tip.titleComp;
                title.text = titleText;
                title.horizontalOverflow = HorizontalWrapMode.Overflow;
                title.verticalOverflow = VerticalWrapMode.Overflow;
            }

            RectTransform rowRect = item.transform as RectTransform;
            if (rowRect != null)
            {
                Vector3[] corners = new Vector3[4];
                rowRect.GetWorldCorners(corners);
                Canvas rowCanvas = rowRect.GetComponentInParent<Canvas>();
                Camera rowCamera = null;
                if (rowCanvas != null && rowCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
                {
                    rowCamera = rowCanvas.worldCamera;
                }
                Vector2 bottomLeft = RectTransformUtility.WorldToScreenPoint(rowCamera, corners[0]);
                Vector2 topLeft = RectTransformUtility.WorldToScreenPoint(rowCamera, corners[1]);
                if (bottomLeft.x <= 1f && bottomLeft.y <= 1f)
                {
                    bottomLeft = Input.mousePosition;
                    topLeft = bottomLeft + new Vector2(0f, 24f);
                }
                Vector2 target = new Vector2(bottomLeft.x + 18f, topLeft.y + 4f);

                RectTransform tipRect = instance.tip.trans != null ? instance.tip.trans : instance.tip.transform as RectTransform;
                if (tipRect != null)
                {
                    tipRect.pivot = new Vector2(0f, 0f);
                    RectTransform parentRect = tipRect.parent as RectTransform;
                    Canvas parentCanvas = tipRect.GetComponentInParent<Canvas>();
                    Camera parentCamera = (parentCanvas != null && parentCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
                        ? parentCanvas.worldCamera
                        : null;
                    Vector3 world;
                    if (parentRect != null
                        && RectTransformUtility.ScreenPointToWorldPointInRectangle(parentRect, target, parentCamera, out world))
                    {
                        tipRect.position = world;
                    }
                    else
                    {
                        tipRect.position = new Vector3(target.x, target.y, 0f);
                    }
                }
            }
        }

        public static void Hide()
        {
            if (instance != null && instance.tip != null)
            {
                instance.tip.gameObject.SetActive(false);
            }
        }

        private static void Ensure(UIPlanetFinderListItem item)
        {
            if (instance != null)
            {
                return;
            }
            UIRoot root = UIRoot.instance;
            if (root == null)
            {
                return;
            }
            if (!hookedConfig)
            {
                hookedConfig = true;
                PLFN.showRowTooltip.SettingChanged += (sender, args) => Hide();
            }
            UISimpleGeneralTip prefab = FindTipPrefab();
            if (prefab == null)
            {
                if (!warnedPrefab)
                {
                    warnedPrefab = true;
                    PLFN.LogWarning("[tip] native tip prefab not found");
                }
                return;
            }

            GameObject go = new GameObject("PlanetFinder-RowTip", typeof(RectTransform));
            go.layer = 5;
            RowTip owner = go.AddComponent<RowTip>();
            RectTransform canvasRect = (RectTransform)go.transform;
            canvasRect.SetParent(root.transform, false);
            Canvas canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 32000;
            canvas.pixelPerfect = false;

            UISimpleGeneralTip tip = Object.Instantiate(prefab, canvasRect);
            tip.gameObject.name = "native-tip";
            tip.gameObject.layer = 5;
            tip.gameObject.SetActive(false);
            owner.tip = tip;
            instance = owner;
        }

        private static UISimpleGeneralTip FindTipPrefab()
        {
            try
            {
                UIGalaxySelect select = UIRoot.instance != null ? UIRoot.instance.galaxySelect : null;
                Transform toggle = select != null && select.sandboxToggle != null ? select.sandboxToggle.transform : null;
                Transform tipButton = toggle != null && toggle.parent != null ? toggle.parent.Find("tip-button") : null;
                UIButton button = tipButton != null ? tipButton.GetComponent<UIButton>() : null;
                if (button != null && button.tips.simpleGeneralTipPrefab != null)
                {
                    return button.tips.simpleGeneralTipPrefab;
                }
            }
            catch (System.Exception)
            {
                //fall through
            }

            try
            {
                foreach (UIButton candidate in Resources.FindObjectsOfTypeAll<UIButton>())
                {
                    if (candidate != null && candidate.tips.simpleGeneralTipPrefab != null)
                    {
                        return candidate.tips.simpleGeneralTipPrefab;
                    }
                }
            }
            catch (System.Exception)
            {
                //fall through
            }
            return null;
        }
    }
}

using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;
using UnityEngine;
using UnityEngine.UI;

namespace PlanetFinderMod
{
    // BepInEx.ConfigEntry<bool> の設定をするチェックボックス
    // CreateCheckBox() で作ったインスタンスを配置するだけ。後は何もしなくて良い
    //
    // To-Do ラベルの文字にクリック範囲を合わせるのめんどいので後回し or won't fix
    public class MyCheckBox : MonoBehaviour
    {
        public UIButton uiButton;
        public Image checkImage;
        public RectTransform rectTrans;
        public Text labelText;
        public ConfigEntry<bool> config;
        public bool interactable = true;
        private Color labelNormalColor = Color.white;

        // MyCheckBox を作って RectTransform を返す
        public static RectTransform CreateCheckBox(ConfigEntry<bool> config, string label = "", int fontSize = 17)
        {
            void ResetAnchor(RectTransform rect_)
            {
                rect_.anchorMax = Vector2.zero;
                rect_.anchorMin = Vector2.zero;
            }

            UIBuildMenu buildMenu = UIRoot.instance.uiGame.buildMenu;
            UIButton src = buildMenu.uxFacilityCheck;

            GameObject go = GameObject.Instantiate(src.gameObject);
            go.name = "my-checkbox";
            MyCheckBox cb = go.AddComponent<MyCheckBox>();
            cb.config = config;
            RectTransform rect = go.transform as RectTransform;
            cb.rectTrans = rect;
            ResetAnchor(rect);
            rect.anchoredPosition3D = new Vector3(0, 0, 0);

            cb.uiButton = go.GetComponent<UIButton>();
            cb.checkImage = go.transform.Find("checked")?.GetComponent<Image>();
            //ResetAnchor(cb.checkImage.rectTransform);

            //text
            Transform child = go.transform.Find("text");
            if (child != null)
            {
                //ResetAnchor(child as RectTransform);
                GameObject.DestroyImmediate(child.GetComponent<Localizer>());
                cb.labelText = child.GetComponent<Text>();
                cb.labelText.fontSize = fontSize;
                cb.labelNormalColor = cb.labelText.color;
                cb.SetLabelText(label);
            }

            //value
            cb.uiButton.onClick += cb.OnClick;
            cb.SettingChanged();
            config.SettingChanged += (sender, args) => {
                cb.SettingChanged();
            };

            return cb.rectTrans;
        }

        public void SetLabelText(string val)
        {
            if (labelText != null)
            {
                labelText.text = val;
                //rectTrans.sizeDelta = new Vector2(checkImage.rectTransform.sizeDelta.x + 4f + labelText.preferredWidth, rectTrans.sizeDelta.y);
            }
        }

        /// <summary>
        /// Enables or disables the check box. A disabled box cannot be clicked and is drawn dimmed.
        /// </summary>
        public void SetInteractable(bool on)
        {
            interactable = on;
            if (uiButton != null && uiButton.button != null)
            {
                uiButton.button.enabled = on;
            }
            if (checkImage != null)
            {
                checkImage.color = on ? Color.white : new Color(1f, 1f, 1f, 0.3f);
            }
            if (labelText != null)
            {
                labelText.color = on
                    ? labelNormalColor
                    : new Color(labelNormalColor.r, labelNormalColor.g, labelNormalColor.b, labelNormalColor.a * 0.35f);
            }
        }

        public void SettingChanged()
        {
            if (config.Value != checkImage.enabled)
            {
                checkImage.enabled = config.Value;
            }
        }

        public void OnClick(int obj)
        {
            if (!interactable)
            {
                return;
            }
            config.Value = !config.Value;
        }

    }
}

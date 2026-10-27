using UnityEngine;
using UnityEngine.EventSystems;

namespace PlanetFinderMod
{
    /// <summary>
    /// Invisible hover area over the icon and the name of a row.
    /// It shows the type tooltip and forwards the pointer events to the row, so the row buttons still
    /// appear on hover and a right click still opens the row menu.
    /// </summary>
    public class RowHoverForward : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        public UIPlanetFinderListItem item;

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (item == null)
            {
                return;
            }
            item.ShowRowButtons(true);
            RowTip.Show(item, item.ObjectTypeText());
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            RowTip.Hide();
            if (item != null)
            {
                item.ShowRowButtons(false);
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (item == null || eventData.button != PointerEventData.InputButton.Right)
            {
                return;
            }
            RowTip.Hide();
            item.window.ShowMenu(item);
        }
    }
}

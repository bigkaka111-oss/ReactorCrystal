using UnityEngine;
using UnityEngine.EventSystems;
public static class WorldInteraction
{
    public static bool Blocked => ReactorUI.ModalOpen || Time.timeScale <= 0f || (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject());
}


using UnityEngine;

public class LockButton : MonoBehaviour
{
    public CrystalDrive drive;

    // Метод для вызова через UnityEvent (OnClick)
    public void TryLockPistons()
    {
        if (drive == null)
        {
            Debug.LogWarning("[LockButton] drive не назначен!");
            return;
        }

        drive.LockPistons();
    }
}
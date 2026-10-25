using UnityEngine;

public class WeaponAnimationEvents : MonoBehaviour
{
    private Gun parentGun;

    void Start()
    {
        // Находим родительский скрипт оружия
        parentGun = GetComponentInParent<Gun>();
    }

    // Этот метод вызывается аниматором через Event
    public void EjectShell()
    {
        if (parentGun != null)
        {
            parentGun.OnAnimationEjectShell();
        }
    }

    // Этот метод вызывается аниматором через Event
    public void DropMagazine()
    {
        if (parentGun != null)
        {
            parentGun.OnAnimationDropMagazine();
        }
    }

    public void PlaySoundMagOut()
    {
        if (parentGun != null)
        {
            parentGun.PlaySoundMagOut();
        }
    }

    public void PlaySoundMagIn()
    {
        if (parentGun != null)
        {
            parentGun.PlaySoundMagIn();
        }
    }

    public void PlaySoundBoltPull()
    {
        if (parentGun != null)
        {
            parentGun.PlaySoundBoltPull();
        }
    }

    public void PlaySoundBoltRelease()
    {
        if (parentGun != null)
        {
            parentGun.PlaySoundBoltRelease();
        }
    }

    public void PlaySoundShellInsert()
    {
        if (parentGun != null)
        {
            parentGun.PlaySoundShellInsert();
        }
    }
}
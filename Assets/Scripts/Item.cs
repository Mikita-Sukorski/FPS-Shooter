using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Item : MonoBehaviour
{
	public ItemInfo itemInfo;
	public GameObject itemGameObject;
    public GameObject crosshairPrefab;

    public abstract void Use();
    public virtual void Reload() { }
    public virtual void AddAmmo(int amount) { }
}
using UnityEngine;

[CreateAssetMenu(menuName = "Shop/Item", fileName = "Item_")]
public class ItemData : ScriptableObject
{
    [Header("Identity")]
    public string id;               // unique key
    public string displayName;
    [TextArea] public string description;

    [Header("Visuals")]
    public Sprite icon;

    [Header("Economy")]
    public int price = 100;
    public bool stackable = false;
}

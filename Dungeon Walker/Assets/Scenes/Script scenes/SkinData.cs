using UnityEngine;

// An enum to define which character a skin belongs to.
public enum CharacterType
{
    Cat,
    Man
}

[CreateAssetMenu(fileName = "NewSkin", menuName = "Game/Skin Data")]
public class SkinData : ScriptableObject
{
    [Header("Shop & Inventory Info")]
    public string skinName;
    [TextArea] public string description;
    public Sprite icon;
    public int price;

    [Header("Gameplay Configuration")]
    public CharacterType character;
    public string spriteLibraryLabel;

    // This function creates a unique ID for saving/checking ownership.
    // e.g., "Cat_Bee" or "Man_Robot"
    public string GetUniqueID()
    {
        return character.ToString() + "_" + spriteLibraryLabel;
    }
}

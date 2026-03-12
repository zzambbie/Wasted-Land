using UnityEngine;

[CreateAssetMenu(fileName = "NewItem", menuName = "Kart/Item Data")]
public class KartItemData : ScriptableObject
{
    public string itemName;
    public Sprite icon;
    public GameObject prefab;
    public InventoryManager.ItemType itemType;

    public bool isAttackType;
    [TextArea] public string description;
}

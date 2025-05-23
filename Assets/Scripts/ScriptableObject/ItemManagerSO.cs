using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ItemManagerSO", menuName = "ScriptableObjects/ItemManagerSO", order = 1)]
public class ItemManagerSO : ScriptableObject
{
    [SerializeField] Sprite[] items;
    [SerializeField] Sprite[] boardItems;
    [SerializeField] GameObject normalItemPrefab;
    [SerializeField] GameObject bonusItemPrefab;
    [SerializeField] GameObject backgroundItemPrefab;

    public Sprite[] Items => items;
    public GameObject NormalItemPrefab => normalItemPrefab;
    public GameObject BonusItemPrefab => bonusItemPrefab;
    public GameObject BackgroundItemPrefab => backgroundItemPrefab;
    
    public Sprite GetRandomItem()
    {
        return items[Random.Range(0, items.Length)];
    }    
    public Sprite GetBonusRandomItem(Board.eMatchDirection  direction)
    {
        return boardItems[(int)direction];
    }
}
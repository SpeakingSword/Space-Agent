using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [SerializeField] private float gravityModifier = 1.0f;              // 游戏中的重力值
    [SerializeField] private int playerMaxHealth = 100;                 // 玩家的最大生命值

    public int PlayerMaxHealth { get => playerMaxHealth; set => playerMaxHealth = value; }
    public float GravityModifier { get => gravityModifier; set => gravityModifier = value; }

    // Start is called before the first frame update
    void Start()
    {
        Physics2D.gravity *= gravityModifier;
        GameObject.Find("Player").GetComponent<PlayerController>().PlayerHealth = playerMaxHealth;
    }
}

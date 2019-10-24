using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [SerializeField] private float gravityModifier = 1.0f;
    [SerializeField] private int playerMaxHealth = 100;

    public int PlayerMaxHealth { get => playerMaxHealth; set => playerMaxHealth = value; }

    // Start is called before the first frame update
    void Start()
    {
        Physics2D.gravity *= gravityModifier;
        GameObject.Find("Player").GetComponent<PlayerController>().PlayerHealth = playerMaxHealth;
    }
}

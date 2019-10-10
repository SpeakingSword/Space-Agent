using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [SerializeField] private float gravityModifier = 1.0f;

    // Start is called before the first frame update
    void Start()
    {
        Physics2D.gravity *= gravityModifier;    
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}

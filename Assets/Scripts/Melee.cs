using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Melee : MonoBehaviour
{
    [SerializeField] private float forceValue = 5.0f;
    [SerializeField] private float forceDistance = 5.0f;
    [SerializeField] private Transform hitPoint;

    public LayerMask enemyLayer;


    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.J))
        {
            HitEnemy();
        }
    }

    void HitEnemy()
    {
        Vector2 rayStartPosition = hitPoint.position;
        Vector2 rayDirection = transform.right;

        RaycastHit2D rayHits = Physics2D.Raycast(rayStartPosition, rayDirection, forceDistance, enemyLayer);
        if(rayHits.collider != null)
        {
            Vector2 forceDirection = (rayHits.transform.position - transform.position).normalized;
            rayHits.rigidbody.AddForce(forceDirection * forceValue, ForceMode2D.Impulse);
        }

    }
}

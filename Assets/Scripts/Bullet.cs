using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    [SerializeField] private float bulletForce = 10.0f;
    [SerializeField] private float speed = 20f;
    public Rigidbody2D rb;


    // Start is called before the first frame update
    void Start()
    {
        rb.velocity = transform.right * speed;
    }
    
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            Destroy(gameObject);
        }

        if (collision.gameObject.CompareTag("Enemy"))
        {
            Vector2 forceFirection = (collision.transform.position - transform.position).normalized;
            collision.rigidbody.AddForce(forceFirection * bulletForce, ForceMode2D.Impulse);
            Destroy(gameObject);
        }
    }
    
}

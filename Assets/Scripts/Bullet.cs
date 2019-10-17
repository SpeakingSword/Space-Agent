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
        // 被子弹击中的物体受到力
        // Vector2 forceFirection = (collision.transform.position - transform.position).normalized;
        // collision.rigidbody.AddForce(forceFirection * bulletForce, ForceMode2D.Impulse);
        Destroy(gameObject);
    }
    
}

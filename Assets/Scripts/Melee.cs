using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Melee : MonoBehaviour
{
    [SerializeField] private float forceValue = 5.0f;           // 近战力量的大小
    [SerializeField] private float forceDistance = 5.0f;        // 近战射线的距离
    [SerializeField] private Transform hitPoint;                // 近战射线的起始位置
    private Animator playerAnimator;

    public LayerMask enemyLayer;                                // 场景里的敌人层级

    private void Awake()
    {
        playerAnimator = GetComponent<Animator>();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Mouse1))
        {
            HitEnemy(); 
        }
    }

    // 通过射线检测前方是否有敌人，并对距离内的敌人施加力
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

        // 激活格斗动画
        playerAnimator.SetTrigger("Hit_trig");
    }
}

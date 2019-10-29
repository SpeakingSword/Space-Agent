using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Melee : MonoBehaviour
{
    [SerializeField] private float forceValue = 5.0f;           // 近战力量的大小
    [SerializeField] private float forceDistance = 5.0f;        // 近战射线的距离
    [SerializeField] private Transform hitPoint;                // 近战射线的起始位置
    private Animator playerAnimator;                            // 该角色的动画播放控制器

    public LayerMask enemyLayer;                                // 场景里的敌人层级

    private void Awake()
    {
        playerAnimator = GetComponent<Animator>();
    }

    // Update is called once per frame
    void Update()
    {
        // 如果玩家按下鼠标右键且手里没拿着东西则角色尝试近战
        if (Input.GetKeyDown(KeyCode.Mouse1) && !gameObject.GetComponent<Hold>().IsHold)
        {
            HitEnemy(); 
        }
    }

    // 通过射线检测前方是否有敌人，如果检测到敌人则对敌人施加一个相反的力
    void HitEnemy()
    {
        Vector2 rayStartPosition = hitPoint.position;               // 射线的起始位置
        Vector2 rayDirection = transform.right;                     // 射线的方向

        RaycastHit2D rayHits = Physics2D.Raycast(rayStartPosition, rayDirection, forceDistance, enemyLayer);
        if(rayHits.collider != null)
        {
            Vector2 forceDirection = (rayHits.transform.position - transform.position).normalized;
            rayHits.rigidbody.AddForce(forceDirection * forceValue, ForceMode2D.Impulse); 
        }

        // 播放角色格斗动画
        playerAnimator.SetTrigger("Hit_trig");
    }
}

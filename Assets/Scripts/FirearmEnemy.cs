using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FirearmEnemy : MonoBehaviour
{    
    public LayerMask groundLayer;               // 可在上面跳跃的层
    public Transform footPoint;                 // 脚部射线的发射点，用来探测障碍
    public Transform firePoint;                 // 子弹的发射点
    public GameObject bulletPrefab;             // 子弹实体
    public Transform[] path;                    // 巡逻的路径点数组
    public LayerMask detectedLayer;             // 被探测的玩家的层
    private FSMSystem fsm;                      // 一个FSM实例
    private GameObject player;                  // 玩家实例

    [SerializeField] private float detectedRayDistance = 15.0f;             // 探测玩家层的射线长度
    [SerializeField] private float shootRate = 0.5f;                        // 射击频率
    [SerializeField] private float patrolSpeed = 10.0f;                     // 巡逻的速度
    [SerializeField] private float runSpeed = 15.0f;                        // 追击的速度
    [SerializeField] private float jumpForce = 100.0f;                      // 跳跃用的力度
    [SerializeField] private float meleeForce = 60.0f;                      // 近战攻击的力度
    [SerializeField] private float meleeRange = 1.0f;                       // 近战攻击的距离
    private const float jumpRayDistance = 1.5f;                             // 探测障碍的射线长度
    private int health = 100;

    public float DetectedRayDistance { get => detectedRayDistance; set => detectedRayDistance = value; }
    public float ShootRate { get => shootRate; set => shootRate = value; }
    public float PatrolSpeed { get => patrolSpeed; set => patrolSpeed = value; }
    public float RunSpeed { get => runSpeed; set => runSpeed = value; }
    public float JumpForce { get => jumpForce; set => jumpForce = value; }
    public float JumpRayDistance { get { return jumpRayDistance; } }
    public float MeleeForce { get => meleeForce; set => meleeForce = value; }
    public float MeleeRange { get => meleeRange; set => meleeRange = value; }
    public int Health { get => health; set => health = value; }

    // 激活状态转换过程
    public void SetTransition(Transition t) { fsm.PerformTransition(t); }

    // 第一帧之前执行一次
    void Start()
    {
        player = GameObject.Find("Player");
        MakeFSM();
    }

    // 每帧都会执行一次
    void Update()
    {
        fsm.CurrentState.Reason(player, gameObject);
        fsm.CurrentState.Act(player, gameObject);
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        // 当受到攻击时且玩家处于该敌人的后方时交换两个路径点的值
        if(collision.gameObject.tag == "Bullet" && Vector2.Dot(transform.right, player.transform.position - transform.position) < 0)
        {
            Transform temp = path[0];
            path[0] = path[1];
            path[1] = temp;
        }    
    }

    void OnCollisionStay2D(Collision2D collision)
    {
        string collideObjTag = collision.gameObject.tag;
        switch (collideObjTag)
        {
            // 通过不断地施加力解决敌人被箱子卡住的情况
            case "Crate":
                gameObject.GetComponent<Rigidbody2D>().AddForce(new Vector2(transform.right.x * 20 * Time.deltaTime, jumpForce),
                                                                ForceMode2D.Impulse);
                break;
        }
    }

    // 判断游戏对象是否站在地上
    public bool IsOnGround()
    {
        Vector2 raysSartPosition = footPoint.transform.position;
        Vector2 rayDirection = Vector2.down;

        RaycastHit2D rayHits = Physics2D.Raycast(raysSartPosition,
                                                 rayDirection, 
                                                 1.0f,
                                                 groundLayer);
        if (rayHits.collider != null)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    // 实例化一个FSMSystem
    private void MakeFSM()
    {
        F_FollowPathState follow = new F_FollowPathState(path);
        follow.AddTransition(Transition.F_SawPlayer, StateID.F_Attack);
        follow.AddTransition(Transition.F_ReachPathPoint, StateID.F_Rest);

        F_AttackState attack = new F_AttackState();
        attack.AddTransition(Transition.F_PlayerComeClose, StateID.F_Melee);
        attack.AddTransition(Transition.F_LostPlayer, StateID.F_FollowingPath);
        attack.AddTransition(Transition.PlayerDead, StateID.F_FollowingPath);

        F_MeleeState melee = new F_MeleeState();
        melee.AddTransition(Transition.F_PlayerFallback, StateID.F_Attack);
        melee.AddTransition(Transition.PlayerDead, StateID.F_FollowingPath);

        F_RestState rest = new F_RestState();
        rest.AddTransition(Transition.F_FinishRest, StateID.F_FollowingPath);

        fsm = new FSMSystem();
        fsm.AddState(follow);
        fsm.AddState(attack);
        fsm.AddState(melee);
        fsm.AddState(rest);
    }

}

// 持枪敌人巡逻类
public class F_FollowPathState: FSMState
{
    private int currentWayPoint;
    private Transform[] waypoints;

    public F_FollowPathState(Transform[] wp)
    {
        waypoints = wp;
        currentWayPoint = 0;
        stateID = StateID.F_FollowingPath;
    }

    public override void Reason(GameObject player, GameObject npc)
    {   
        Debug.DrawRay(npc.transform.position, npc.transform.right * npc.GetComponent<FirearmEnemy>().DetectedRayDistance, Color.red);
        // 向前探测玩家的射线
        RaycastHit2D hit = Physics2D.Raycast(npc.transform.position,
                                             npc.transform.right,
                                             npc.GetComponent<FirearmEnemy>().DetectedRayDistance,
                                             npc.GetComponent<FirearmEnemy>().detectedLayer);

        Debug.DrawRay(npc.transform.position, -npc.transform.right * npc.GetComponent<FirearmEnemy>().DetectedRayDistance / 10, Color.red);
        // 向后探测玩家的射线
        RaycastHit2D hit2 = Physics2D.Raycast(npc.transform.position,
                                              -npc.transform.right,
                                              npc.GetComponent<FirearmEnemy>().DetectedRayDistance / 10,
                                              npc.GetComponent<FirearmEnemy>().detectedLayer);

        // 当探测射线探测到东西且被探测到的物体为玩家时转换为攻击状态
        if ((hit.collider != null && hit.collider.gameObject.tag == "Player") || (hit2.collider != null && hit2.collider.gameObject.tag == "Player"))
        {
            Debug.Log("Player has been spotted by firearm enemy!");
            // 转换为攻击状态
            npc.GetComponent<FirearmEnemy>().SetTransition(Transition.F_SawPlayer);
        }
    }

    public override void Act(GameObject player, GameObject npc)
    {
        // 计算移动方向
        Vector2 moveDir = new Vector2(waypoints[currentWayPoint].position.x - npc.transform.position.x, 0);

        // 当到达路径点时，更新目标点
        if (moveDir.magnitude < 1)
        {
            currentWayPoint++;
            if (currentWayPoint >= waypoints.Length)
            {
                currentWayPoint = 0;
            }

            // 转换为休息状态
            // npc.GetComponent<FirearmEnemy>().StartCoroutine(EnemyRest(npc));
        }
        else
        {
            // 如果目标点在该敌人后方，则转向
            if (Vector2.Dot(npc.transform.right, moveDir) < 0)
            {
                npc.transform.Rotate(new Vector3(0, 180, 0), Space.Self);
            }

            Debug.DrawRay(npc.GetComponent<FirearmEnemy>().footPoint.position,
                      npc.transform.right * npc.GetComponent<FirearmEnemy>().JumpRayDistance,
                      Color.green);
            // 探测前方障碍的射线
            RaycastHit2D hitObstacle = Physics2D.Raycast(npc.GetComponent<FirearmEnemy>().footPoint.position,
                                                         npc.transform.right,
                                                         npc.GetComponent<FirearmEnemy>().JumpRayDistance,
                                                         1 << LayerMask.NameToLayer("Ground") | 1 << LayerMask.NameToLayer("Crate"));

            if (hitObstacle.collider != null && npc.GetComponent<FirearmEnemy>().IsOnGround())
            {
                npc.GetComponent<Rigidbody2D>().AddForce(new Vector2(npc.transform.right.x * Time.deltaTime * 5, 
                                                                     npc.GetComponent<FirearmEnemy>().JumpForce), 
                                                                     ForceMode2D.Impulse);
            }

            // 继续往路径点移动
            if (npc.GetComponent<FirearmEnemy>().IsOnGround())
                npc.GetComponent<Rigidbody2D>().AddForce(moveDir.normalized * npc.GetComponent<FirearmEnemy>().PatrolSpeed);

        }
    }

    private IEnumerator EnemyRest(GameObject npc)
    {
        npc.GetComponent<FirearmEnemy>().SetTransition(Transition.F_ReachPathPoint);
        int restTime = Random.Range(0, 5);
        yield return new WaitForSeconds(restTime);
        npc.transform.Rotate(new Vector3(0, 180, 0), Space.Self);
        npc.GetComponent<FirearmEnemy>().SetTransition(Transition.F_FinishRest);
    }
}

// 持枪敌人攻击类
public class F_AttackState: FSMState
{
    bool isFirstAttack = true;
    float lastTime = 0.0f;
    float currentTime = 0.0f;

    public F_AttackState()
    {
        stateID = StateID.F_Attack;
    }

    public override void Reason(GameObject player, GameObject npc)
    {
        Debug.DrawRay(npc.transform.position, npc.transform.right * npc.GetComponent<FirearmEnemy>().DetectedRayDistance, Color.red);
        
        RaycastHit2D hitPlayer = Physics2D.Raycast(npc.transform.position,
                                                   npc.transform.right,
                                                   npc.GetComponent<FirearmEnemy>().DetectedRayDistance,
                                                   1 << player.layer);

        float escapeInY = Mathf.Abs(player.transform.position.y - npc.transform.position.y);
        float escapeInX = Mathf.Abs(player.transform.position.x - npc.transform.position.x);

        // 第一个括号内的条件防止玩家跳到敌人头顶而敌人却因此失去目标
        // 第二个条件是玩家在水平方向上离得足够远才能使敌人失去目标
        if ((hitPlayer.collider == null && escapeInY > 8) || escapeInX > 45)
        {
            // 转换为巡逻状态
            npc.GetComponent<FirearmEnemy>().SetTransition(Transition.F_LostPlayer);
        }

        // 如果玩家靠得太近了，则转换为近战攻击状态
        if((npc.transform.position - player.transform.position).magnitude < npc.GetComponent<FirearmEnemy>().MeleeRange)
        {
            npc.GetComponent<FirearmEnemy>().SetTransition(Transition.F_PlayerComeClose);
        }
    }

    public override void Act(GameObject player, GameObject npc)
    {
        // 在攻击状态时如果玩家在敌人后方，敌人需转向
        if (Vector2.Dot(player.transform.position - npc.transform.position, npc.transform.right) < 0)
        {
            npc.transform.Rotate(new Vector3(0, 180, 0), Space.Self);
        }

        // 开始发射子弹，有发射频率
        if (isFirstAttack)
        {
            Object.Instantiate(npc.GetComponent<FirearmEnemy>().bulletPrefab,
                                   npc.GetComponent<FirearmEnemy>().firePoint.position,
                                   npc.GetComponent<FirearmEnemy>().firePoint.rotation);
            isFirstAttack = false;
        }
        else
        {
            currentTime = Time.time;
            if (currentTime - lastTime >= npc.GetComponent<FirearmEnemy>().ShootRate)
            {
                Object.Instantiate(npc.GetComponent<FirearmEnemy>().bulletPrefab,
                                   npc.GetComponent<FirearmEnemy>().firePoint.position,
                                   npc.GetComponent<FirearmEnemy>().firePoint.rotation);
                lastTime = currentTime;
            }
        }

        // 玩家死亡则转换为巡逻状态
        if (player.GetComponent<PlayerController>().PlayerHealth <= 0)
        {
            npc.GetComponent<FirearmEnemy>().SetTransition(Transition.PlayerDead);
        }
    }

    public override void DoBeforeEntering()
    {
        isFirstAttack = true;
        lastTime = Time.time;
    }
}

// 持枪敌人近战攻击类
public class F_MeleeState: FSMState
{
    bool isFirstAttack = true;
    float lastTime = 0.0f;
    float currentTime = 0.0f;

    public F_MeleeState()
    {
        stateID = StateID.F_Melee;
    }

    public override void Reason(GameObject player, GameObject npc)
    {
        // 当玩家离开近战攻击范围则转换为远程攻击状态
        if((npc.transform.position - player.transform.position).magnitude > npc.GetComponent<FirearmEnemy>().MeleeRange)
        {
            npc.GetComponent<FirearmEnemy>().SetTransition(Transition.F_PlayerFallback);
        }
    }

    public override void Act(GameObject player, GameObject npc)
    {
        if (Vector2.Dot(player.transform.position - npc.transform.position, npc.transform.right) < 0)
        {
            npc.transform.Rotate(new Vector3(0, 180, 0), Space.Self);
        }

        // 开始攻击，有攻击频率
        if (isFirstAttack)
        {
            player.GetComponent<Rigidbody2D>().AddForce(new Vector2(player.transform.position.x - npc.transform.position.x, 1).normalized
                                                        * npc.GetComponent<FirearmEnemy>().MeleeForce,
                                                        ForceMode2D.Impulse);
            player.GetComponent<PlayerController>().PlayerHealth -= 20;
            isFirstAttack = false;
        }
        else
        {
            currentTime = Time.time;
            if (currentTime - lastTime >= 1)
            {
                player.GetComponent<Rigidbody2D>().AddForce(new Vector2(player.transform.position.x - npc.transform.position.x, 1).normalized
                                                        * npc.GetComponent<FirearmEnemy>().MeleeForce,
                                                        ForceMode2D.Impulse);
                lastTime = currentTime;
            }
        }

        if (player.GetComponent<PlayerController>().PlayerHealth <= 0)
        {
            npc.GetComponent<FirearmEnemy>().SetTransition(Transition.PlayerDead);
        }
    }

    public override void DoBeforeEntering()
    {
        isFirstAttack = true;
        lastTime = Time.time;
    }
}

// 持枪敌人休息类
public class F_RestState: FSMState
{
    public F_RestState()
    {
        stateID = StateID.F_Rest;
    }

    public override void Reason(GameObject player, GameObject npc)
    {
        
    }

    public override void Act(GameObject player, GameObject npc)
    {
        
    }
}

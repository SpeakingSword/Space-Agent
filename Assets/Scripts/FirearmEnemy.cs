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
    private AudioSource firearmEnemyAudio;      // 挂在该对象上的一个声源，用来播放音效

    [SerializeField] private float detectedRayDistance = 15.0f;             // 探测玩家层的射线长度
    [SerializeField] private float shootRate = 0.5f;                        // 射击频率
    [SerializeField] private float patrolSpeed = 10.0f;                     // 巡逻的速度
    [SerializeField] private float jumpForce = 100.0f;                      // 跳跃用的力度
    [SerializeField] private float meleeForce = 60.0f;                      // 近战攻击的力度
    [SerializeField] private float meleeRange = 1.0f;                       // 近战攻击的距离
    public AudioClip fireSound;                                             // 开火的音效
    private const float jumpRayDistance = 1.5f;                             // 探测障碍的射线长度
    private int health = 100;                                               // 生命值

    public float DetectedRayDistance { get => detectedRayDistance; set => detectedRayDistance = value; }
    public float ShootRate { get => shootRate; set => shootRate = value; }
    public float PatrolSpeed { get => patrolSpeed; set => patrolSpeed = value; }
    public float JumpForce { get => jumpForce; set => jumpForce = value; }
    public float JumpRayDistance { get { return jumpRayDistance; } }
    public float MeleeForce { get => meleeForce; set => meleeForce = value; }
    public float MeleeRange { get => meleeRange; set => meleeRange = value; }
    public int Health { get => health; set => health = value; }
    public AudioSource FirearmEnemyAudio { get => firearmEnemyAudio; set => firearmEnemyAudio = value; }

    // 激活状态转换过程
    public void SetTransition(Transition t) { fsm.PerformTransition(t); }

    // 第一帧之前执行一次
    void Start()
    {
        player = GameObject.Find("Player");
        firearmEnemyAudio = GetComponent<AudioSource>();
        MakeFSM();
    }

    // 每帧都会执行一次
    void Update()
    {
        fsm.CurrentState.Reason(player, gameObject);
        fsm.CurrentState.Act(player, gameObject);

        // 如果生命值小于等于0，则销毁
        if(health <= 0)
        {
            Destroy(gameObject);
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if(collision.gameObject.tag == "Bullet")
        {
            health -= 20;

            // 当受到攻击时且玩家处于该敌人的后方时交换两个路径点的值（相当于该敌人转向）
            if (Vector2.Dot(transform.right, player.transform.position - transform.position) < 0)
            {
                Transform temp = path[0];
                path[0] = path[1];
                path[1] = temp;
            }  
        }    
    }

    void OnCollisionStay2D(Collision2D collision)
    {
        string collideObjTag = collision.gameObject.tag;
        switch (collideObjTag)
        {
            // 通过不断地施加力解决敌人被箱子卡住的情况
            case "Crate":
                gameObject.GetComponent<Rigidbody2D>().AddForce(new Vector2(transform.right.x * 50 * Time.deltaTime, jumpForce),
                                                                ForceMode2D.Impulse);
                break;
        }
    }

    // 判断游戏对象是否站在地上
    public bool IsOnGround()
    {
        Vector2 raysSartPosition = footPoint.transform.position;        // 射线的起始点
        Vector2 rayDirection = Vector2.down;                            // 射线的方向

        RaycastHit2D rayHit = Physics2D.Raycast(raysSartPosition,
                                                rayDirection, 
                                                1.0f,
                                                groundLayer);

        // 如果射线探测到相应的层(属于地面的层)则返回true，否则返回false
        if (rayHit.collider != null)
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

// 远程敌人巡逻状态类
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
        }

        Debug.DrawRay(npc.GetComponent<FirearmEnemy>().footPoint.position,
                  npc.transform.right * npc.GetComponent<FirearmEnemy>().JumpRayDistance,
                  Color.green);
        // 探测前方障碍的射线
        RaycastHit2D hitObstacle = Physics2D.Raycast(npc.GetComponent<FirearmEnemy>().footPoint.position,
                                                     npc.transform.right,
                                                     npc.GetComponent<FirearmEnemy>().JumpRayDistance,
                                                     1 << LayerMask.NameToLayer("Ground") | 1 << LayerMask.NameToLayer("Crate"));

        // 如果探测到前方有障碍且该敌人站在地上则跳过障碍
        if (hitObstacle.collider != null && npc.GetComponent<FirearmEnemy>().IsOnGround())
        {
            npc.GetComponent<Rigidbody2D>().AddForce(new Vector2(npc.transform.right.x * Time.deltaTime * 5, 
                                                                 npc.GetComponent<FirearmEnemy>().JumpForce), 
                                                                 ForceMode2D.Impulse);
        }

        // 如果该敌人向前的方向与当前路径点的方向相反则转向
        if (Vector3.Dot(waypoints[currentWayPoint].transform.position - npc.transform.position, npc.transform.right) < 0)
        {
            npc.transform.Rotate(new Vector3(0, 180, 0));
        }

        // 继续往路径点移动
        if (npc.GetComponent<FirearmEnemy>().IsOnGround())
            npc.GetComponent<Rigidbody2D>().AddForce(moveDir.normalized * npc.GetComponent<FirearmEnemy>().PatrolSpeed);

    }
}

// 远程敌人攻击状态类
public class F_AttackState: FSMState
{
    float lastTime = 0.0f;
    float currentTime = 0.0f;

    public F_AttackState()
    {
        stateID = StateID.F_Attack;
    }

    public override void Reason(GameObject player, GameObject npc)
    {
        // 在攻击状态时如果玩家在敌人后方，敌人需转向
        if (Vector3.Dot(player.transform.position - npc.transform.position, npc.transform.right) < 0)
        {
            npc.transform.Rotate(new Vector3(0, 180, 0));
        }

        Debug.DrawRay(npc.transform.position, npc.transform.right * npc.GetComponent<FirearmEnemy>().DetectedRayDistance, Color.red);
        // 检测玩家的射线
        RaycastHit2D hitPlayer = Physics2D.Raycast(npc.transform.position,
                                                   npc.transform.right,
                                                   npc.GetComponent<FirearmEnemy>().DetectedRayDistance,
                                                   1 << player.layer);

        float escapeInY = Mathf.Abs(player.transform.position.y - npc.transform.position.y);
        float escapeInX = Mathf.Abs(player.transform.position.x - npc.transform.position.x);

        // 当射线没有探测到玩家且玩家在垂直方向上离得足够远 或者 在水平方向上离得足够远 则 该敌人确认为失去目标
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
        // 开始发射子弹，有发射频率
        currentTime = Time.time;
        if ((currentTime - lastTime) > npc.GetComponent<FirearmEnemy>().ShootRate)
        {
            // 播放开火音效
            npc.GetComponent<FirearmEnemy>().FirearmEnemyAudio.PlayOneShot(npc.GetComponent<FirearmEnemy>().fireSound, 0.2f);
            // 生成子弹实例
            Object.Instantiate(npc.GetComponent<FirearmEnemy>().bulletPrefab,
                               npc.GetComponent<FirearmEnemy>().firePoint.position,
                               npc.GetComponent<FirearmEnemy>().firePoint.rotation);
            lastTime = currentTime;
        }
        

        // 玩家死亡则转换为巡逻状态
        if (player.GetComponent<PlayerController>().PlayerHealth <= 0)
        {
            npc.GetComponent<FirearmEnemy>().SetTransition(Transition.PlayerDead);
        }
    }

    public override void DoBeforeEntering()
    {
        lastTime = Time.time;
    }
}

// 远程敌人近战攻击状态类
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
        // 在近战攻击状态时如果玩家在敌人后方，敌人需转向
        if (Vector2.Dot(player.transform.position - npc.transform.position, npc.transform.right) < 0)
        {
            npc.transform.Rotate(new Vector3(0, 180, 0));
        }

        // 开始攻击，如果是第一次攻击则直接攻击，之后的攻击按攻击频率攻击
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

        // 如果玩家死亡则转换为巡逻状态
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

// 远程敌人休息状态类 (暂不做处理)
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

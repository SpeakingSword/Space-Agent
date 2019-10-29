using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeleeEnemy : MonoBehaviour
{
    // public GameObject healthText;
    public LayerMask groundLayer;                                           // 地面层
    public Transform footPoint;                                             // 该敌人脚部位置
    public Transform[] path;                                                // 巡逻点数组
    public LayerMask detectedLayer;                                         // 玩家层
    private FSMSystem fsm;                                                  // FSM实例
    private GameObject player;                                              // 玩家实例

    [SerializeField] private float detectedRayDistance = 15.0f;             // 探测玩家射线的距离
    [SerializeField] private float attackRange = 1.0f;                      // 近战距离
    [SerializeField] private float attackForce = 60.0f;                     // 近战力度
    [SerializeField] private float patrolSpeed = 10.0f;                     // 巡逻时的移动速度
    [SerializeField] private float persueSpeed = 15.0f;                     // 追击时的移动速度
    [SerializeField] private float jumpForce = 100.0f;                      // 跳跃障碍用的力度
    private const float jumpRayDistance = 1.5f;                             // 检测障碍的射线距离
    private int health = 100;                                               // 敌人生命值

    public float DetectedRayDistance { get { return detectedRayDistance; } }
    public float AttackRange { get { return attackRange; } }
    public float AttackForce { get { return attackForce; } }
    public float PatrolSpeed { get { return patrolSpeed; } }
    public float PersueSpeed { get { return persueSpeed; } }
    public float JumpRayDistance { get { return jumpRayDistance; } }
    public float JumpForce { get { return jumpForce; } }
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

        // 如果生命值小于等于0，则销毁
        if (health <= 0)
        {
            Destroy(gameObject);
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        string collideObj = collision.gameObject.tag;
        switch (collideObj)
        {
            case "Bullet":
                health -= 20;

                // 当受到攻击时且玩家处于该敌人的后方时交换两个路径点的值（相当于该敌人转向）
                if (Vector2.Dot(-transform.right, player.transform.position - transform.position) < 0)
                {
                    Transform temp = path[0];
                    path[0] = path[1];
                    path[1] = temp;
                }
                break;
        }
        
    }

    void OnCollisionStay2D(Collision2D collision)
    {
        string collideObjTag = collision.gameObject.tag;
        switch (collideObjTag)
        {
            // 通过不断地施加力解决敌人被箱子卡住的情况
            case "Crate":
                gameObject.GetComponent<Rigidbody2D>().AddForce(new Vector2(-transform.right.x * 50 * Time.deltaTime, jumpForce),
                                                            ForceMode2D.Impulse);
                break;
        }
    }

    // 判断游戏对象是否站在地上
    public bool IsOnGround()
    {
        Vector2 raysSartPosition = footPoint.transform.position;            // 射线起始点
        Vector2 rayDirection = Vector2.down;                                // 射线方向

        RaycastHit2D rayHits = Physics2D.Raycast(raysSartPosition,
                                                 rayDirection,
                                                 1.0f,
                                                 groundLayer);

        // 如果射线探测到相应的层(属于地面的层)则返回true，否则返回false
        if (rayHits.collider != null)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    private void MakeFSM()
    {
        M_FollowPathState follow = new M_FollowPathState(path);
        follow.AddTransition(Transition.M_SawPlayer, StateID.M_ChasingPlayer);
        follow.AddTransition(Transition.M_ReachPathPoint, StateID.M_Rest);

        M_ChasePlayerState chase = new M_ChasePlayerState();
        chase.AddTransition(Transition.M_LostPlayer, StateID.M_FollowingPath);
        chase.AddTransition(Transition.M_CloseEnough, StateID.M_Attack);

        M_AttackState attack = new M_AttackState();
        attack.AddTransition(Transition.M_NotClose, StateID.M_ChasingPlayer);
        attack.AddTransition(Transition.PlayerDead, StateID.M_FollowingPath);
        
        M_RestState rest = new M_RestState();
        rest.AddTransition(Transition.M_FinishRest, StateID.M_FollowingPath);

        fsm = new FSMSystem();
        fsm.AddState(follow);
        fsm.AddState(chase);
        fsm.AddState(attack);
        fsm.AddState(rest);
    }

}

// 近战敌人巡逻状态类
public class M_FollowPathState : FSMState
{
    private int currentWayPoint;
    private Transform[] waypoints;

    public M_FollowPathState(Transform[] wp)
    {
        waypoints = wp;
        currentWayPoint = 0;
        stateID = StateID.M_FollowingPath;
    }

    public override void Reason(GameObject player, GameObject npc)
    {
        Debug.DrawRay(npc.transform.position, -npc.transform.right * npc.GetComponent<MeleeEnemy>().DetectedRayDistance, Color.red);
        // 向前探测玩家的射线
        RaycastHit2D hit = Physics2D.Raycast(npc.transform.position,
                                             -npc.transform.right,
                                             npc.GetComponent<MeleeEnemy>().DetectedRayDistance,
                                             npc.GetComponent<MeleeEnemy>().detectedLayer);

        Debug.DrawRay(npc.transform.position, npc.transform.right * npc.GetComponent<MeleeEnemy>().DetectedRayDistance / 10, Color.red);
        // 向后探测玩家的射线
        RaycastHit2D hit2 = Physics2D.Raycast(npc.transform.position,
                                              npc.transform.right,
                                              npc.GetComponent<MeleeEnemy>().DetectedRayDistance / 10,
                                              npc.GetComponent<MeleeEnemy>().detectedLayer);

        // 当探测射线探测到东西且被探测到的物体为玩家时转换为攻击状态
        if ((hit.collider != null && hit.collider.gameObject.tag == "Player") || (hit2.collider != null && hit2.collider.gameObject.tag == "Player"))
        {
            Debug.Log("Player has been spotted by melee enemy!");
            // 转换为追击状态
            npc.GetComponent<MeleeEnemy>().SetTransition(Transition.M_SawPlayer);
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

        Debug.DrawRay(npc.GetComponent<MeleeEnemy>().footPoint.position,
                  -npc.transform.right * npc.GetComponent<MeleeEnemy>().JumpRayDistance,
                  Color.green);
        // 探测前方障碍的射线
        RaycastHit2D hitObstacle = Physics2D.Raycast(npc.GetComponent<MeleeEnemy>().footPoint.position,
                                                     -npc.transform.right,
                                                     npc.GetComponent<MeleeEnemy>().JumpRayDistance,
                                                     1 << LayerMask.NameToLayer("Ground") | 1 << LayerMask.NameToLayer("Crate"));

        // 如果探测到前方有障碍且该敌人站在地上则跳过障碍
        if (hitObstacle.collider != null && npc.GetComponent<MeleeEnemy>().IsOnGround())
        {
            npc.GetComponent<Rigidbody2D>().AddForce(new Vector2(-npc.transform.right.x * Time.deltaTime * 5, 
                                                     npc.GetComponent<MeleeEnemy>().JumpForce), 
                                                     ForceMode2D.Impulse);
        }

        // 如果该敌人向前的方向与当前路径点的方向相反则转向
        if (Vector3.Dot(waypoints[currentWayPoint].transform.position - npc.transform.position, -npc.transform.right) < 0)
        {
            npc.transform.Rotate(new Vector3(0, 180, 0));
        }

        // 继续往路径点移动
        if (npc.GetComponent<MeleeEnemy>().IsOnGround())
            npc.GetComponent<Rigidbody2D>().AddForce(moveDir.normalized * npc.GetComponent<MeleeEnemy>().PatrolSpeed);

    }
}

// 近战敌人追击状态类
public class M_ChasePlayerState: FSMState
{
    public M_ChasePlayerState()
    {
        stateID = StateID.M_ChasingPlayer;
    }

    public override void Reason(GameObject player, GameObject npc)
    {
        // 在攻击状态时如果玩家在敌人后方，敌人需转向
        if (Vector3.Dot(player.transform.position - npc.transform.position, -npc.transform.right) < 0)
        {
            npc.transform.Rotate(new Vector3(0, 180, 0));
        }

        Debug.DrawRay(npc.transform.position, -npc.transform.right * npc.GetComponent<MeleeEnemy>().DetectedRayDistance, Color.red);
        // 检测玩家的射线
        RaycastHit2D hitPlayer = Physics2D.Raycast(npc.transform.position,
                                                   -npc.transform.right,
                                                   npc.GetComponent<MeleeEnemy>().DetectedRayDistance,
                                                   1 << player.layer);

        float escapeInY = Mathf.Abs(player.transform.position.y - npc.transform.position.y);
        float escapeInX = Mathf.Abs(player.transform.position.x - npc.transform.position.x);

        // 当射线没有探测到玩家且玩家在垂直方向上离得足够远 或者 在水平方向上离得足够远 则 该敌人确认为失去目标
        if (hitPlayer.collider == null && escapeInY > 8 || escapeInX > 45)
        {
            npc.GetComponent<MeleeEnemy>().SetTransition(Transition.M_LostPlayer);

        }
    }

    public override void Act(GameObject player, GameObject npc)
    {
        // 计算追击时的移动方向
        Vector2 moveDir = new Vector2(player.transform.position.x - npc.transform.position.x, 0).normalized;

        Debug.DrawRay(npc.GetComponent<MeleeEnemy>().footPoint.position,
                      -npc.transform.right * npc.GetComponent<MeleeEnemy>().JumpRayDistance,
                      Color.green);
        // 检测障碍的射线
        RaycastHit2D hitObstacle = Physics2D.Raycast(npc.GetComponent<MeleeEnemy>().footPoint.position,
                                                     -npc.transform.right,
                                                     npc.GetComponent<MeleeEnemy>().JumpRayDistance,
                                                     1 << LayerMask.NameToLayer("Ground") | 1 << LayerMask.NameToLayer("Crate"));

        // 如果探测到前方有障碍且该敌人站在地上则跳过障碍
        if (hitObstacle.collider != null && npc.GetComponent<MeleeEnemy>().IsOnGround())
        {
            npc.GetComponent<Rigidbody2D>().AddForce(new Vector2(-npc.transform.right.x * Time.deltaTime * 5, 
                                                                 npc.GetComponent<MeleeEnemy>().JumpForce), 
                                                                 ForceMode2D.Impulse);
        }

        // 向玩家位置移动
        npc.GetComponent<Rigidbody2D>().AddForce(moveDir * npc.GetComponent<MeleeEnemy>().PersueSpeed);

        // 如果足够靠近玩家则转换为攻击状态
        if((npc.transform.position - player.transform.position).magnitude < npc.GetComponent<MeleeEnemy>().AttackRange)
        {
            npc.GetComponent<MeleeEnemy>().SetTransition(Transition.M_CloseEnough);
        }

        

    }
}

// 近战敌人休息状态类 (暂不处理)
public class M_RestState: FSMState
{
    public M_RestState()
    {
        stateID = StateID.M_Rest;
    }

    public override void Reason(GameObject player, GameObject npc)
    {
        
    }

    public override void Act(GameObject player, GameObject npc)
    {
        
    }
}

// 近战敌人攻击状态类
public class M_AttackState: FSMState
{
    bool isFirstAttack = true;
    float lastTime = 0.0f;
    float currentTime = 0.0f;

    public M_AttackState()
    {
        stateID = StateID.M_Attack;
    }

    public override void Reason(GameObject player, GameObject npc)
    {
        // 当玩家离开近战攻击范围则转换为追击状态
        if ((npc.transform.position - player.transform.position).magnitude > npc.GetComponent<MeleeEnemy>().AttackRange)
        {
            npc.GetComponent<MeleeEnemy>().SetTransition(Transition.M_NotClose);
        }
    }

    public override void Act(GameObject player, GameObject npc)
    {
        // 开始攻击，如果是第一次攻击则直接攻击，之后的攻击按攻击频率攻击
        if (isFirstAttack)
        {
            player.GetComponent<Rigidbody2D>().AddForce(new Vector2(player.transform.position.x - npc.transform.position.x, 1).normalized
                                                        * npc.GetComponent<MeleeEnemy>().AttackForce, 
                                                        ForceMode2D.Impulse);
            player.GetComponent<PlayerController>().PlayerHealth -= 20;
            isFirstAttack = false;
        }
        else
        {
            currentTime = Time.time;
            if(currentTime - lastTime >= 1)
            {
                player.GetComponent<Rigidbody2D>().AddForce(new Vector2(player.transform.position.x - npc.transform.position.x, 1).normalized 
                                                            * npc.GetComponent<MeleeEnemy>().AttackForce,
                                                            ForceMode2D.Impulse);
                player.GetComponent<PlayerController>().PlayerHealth -= 20;
                lastTime = currentTime;
            }
        }

        // 如果玩家死亡则转换为巡逻状态
        if(player.GetComponent<PlayerController>().PlayerHealth <= 0)
        {
            npc.GetComponent<MeleeEnemy>().SetTransition(Transition.PlayerDead);
        }
    }

    public override void DoBeforeEntering()
    {
        isFirstAttack = true;
        lastTime = Time.time;
    }
}



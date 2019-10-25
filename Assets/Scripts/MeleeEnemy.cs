using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeleeEnemy : MonoBehaviour
{
    // public GameObject healthText;
    public LayerMask groundLayer;
    public Transform footPoint;
    public Transform[] path;
    public LayerMask detectedLayer;
    private FSMSystem fsm;
    private GameObject player;

    [SerializeField] private float detectedRayDistance = 15.0f;
    [SerializeField] private float attackRange = 1.0f;
    [SerializeField] private float attackForce = 60.0f;
    [SerializeField] private float patrolSpeed = 10.0f;
    [SerializeField] private float persueSpeed = 15.0f;
    [SerializeField] private float jumpForce = 100.0f;
    private const float jumpRayDistance = 1.5f;
    private int health = 100;

    public float DetectedRayDistance { get { return detectedRayDistance; } }
    public float AttackRange { get { return attackRange; } }
    public float AttackForce { get { return attackForce; } }
    public float PatrolSpeed { get { return patrolSpeed; } }
    public float PersueSpeed { get { return persueSpeed; } }
    public float JumpRayDistance { get { return jumpRayDistance; } }
    public float JumpForce { get { return jumpForce; } }
    public int Health { get => health; set => health = value; }

    public void SetTransition(Transition t) { fsm.PerformTransition(t); }

    // Start is called before the first frame update
    void Start()
    {
        player = GameObject.Find("Player");
        MakeFSM(); 
    }

    // Update is called once per frame
    void Update()
    {
        fsm.CurrentState.Reason(player, gameObject);
        fsm.CurrentState.Act(player, gameObject);
        if(health <= 0)
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
                if(Vector2.Dot(-transform.right, player.transform.position - transform.position) < 0)
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
            case "Crate":
                gameObject.GetComponent<Rigidbody2D>().AddForce(new Vector2(-transform.right.x * 20 * Time.deltaTime, jumpForce),
                                                            ForceMode2D.Impulse);
                break;
        }
    }

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

        RaycastHit2D hit = Physics2D.Raycast(npc.transform.position,
                                                   -npc.transform.right,
                                                   npc.GetComponent<MeleeEnemy>().DetectedRayDistance,
                                                   npc.GetComponent<MeleeEnemy>().detectedLayer);

        Debug.DrawRay(npc.transform.position, npc.transform.right * npc.GetComponent<MeleeEnemy>().DetectedRayDistance / 10, Color.red);

        RaycastHit2D hit2 = Physics2D.Raycast(npc.transform.position,
                                                   npc.transform.right,
                                                   npc.GetComponent<MeleeEnemy>().DetectedRayDistance / 10,
                                                   npc.GetComponent<MeleeEnemy>().detectedLayer);

        if ((hit.collider != null && hit.collider.gameObject.tag == "Player") || (hit2.collider != null && hit2.collider.gameObject.tag == "Player"))
        {
            Debug.Log("Player has been spotted by melee enemy!");
            // 转换为追击状态
            npc.GetComponent<MeleeEnemy>().SetTransition(Transition.M_SawPlayer);
        }
    }

    public override void Act(GameObject player, GameObject npc)
    {
        Vector2 moveDir = new Vector2(waypoints[currentWayPoint].position.x - npc.transform.position.x, 0);

        if (moveDir.magnitude < 1)
        {
            currentWayPoint++;
            if (currentWayPoint >= waypoints.Length)
            {
                currentWayPoint = 0;
            }

            // 转向
            npc.transform.Rotate(new Vector3(0, 180, 0));
            // 转换为休息状态
            // npc.GetComponent<MeleeEnemy>().StartCoroutine(EnemyRest(npc));
        }
        else
        {

            Debug.DrawRay(npc.GetComponent<MeleeEnemy>().footPoint.position,
                      -npc.transform.right * npc.GetComponent<MeleeEnemy>().JumpRayDistance,
                      Color.green);

            RaycastHit2D hitObstacle = Physics2D.Raycast(npc.GetComponent<MeleeEnemy>().footPoint.position,
                                                         -npc.transform.right,
                                                         npc.GetComponent<MeleeEnemy>().JumpRayDistance,
                                                         1 << LayerMask.NameToLayer("Ground") | 1 << LayerMask.NameToLayer("Crate"));

            if (hitObstacle.collider != null)
            {
                npc.GetComponent<Rigidbody2D>().AddForce(new Vector2(-npc.transform.right.x * Time.deltaTime * 5, 
                                                         npc.GetComponent<MeleeEnemy>().JumpForce), 
                                                         ForceMode2D.Impulse);
            }

            // 转向
            if(Vector3.Dot(waypoints[currentWayPoint].transform.position - npc.transform.position, -npc.transform.right) < 0)
            {
                npc.transform.Rotate(new Vector3(0, 180, 0));
            }

            // 继续往路径点移动
            if (npc.GetComponent<MeleeEnemy>().IsOnGround())
                npc.GetComponent<Rigidbody2D>().AddForce(moveDir.normalized * npc.GetComponent<MeleeEnemy>().PatrolSpeed);
        }
    }

    private IEnumerator EnemyRest(GameObject npc)
    {
        npc.GetComponent<MeleeEnemy>().SetTransition(Transition.M_ReachPathPoint);
        int restTime = Random.Range(0, 5);
        yield return new WaitForSeconds(restTime);
        npc.transform.Rotate(new Vector3(0, 180, 0), Space.Self);
        npc.GetComponent<MeleeEnemy>().SetTransition(Transition.M_FinishRest);
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
        // 转向
        if (Vector3.Dot(player.transform.position - npc.transform.position, -npc.transform.right) < 0)
        {
            npc.transform.Rotate(new Vector3(0, 180, 0));
        }

        Debug.DrawRay(npc.transform.position, -npc.transform.right * npc.GetComponent<MeleeEnemy>().DetectedRayDistance, Color.red);
        RaycastHit2D hitPlayer = Physics2D.Raycast(npc.transform.position,
                                                   -npc.transform.right,
                                                   npc.GetComponent<MeleeEnemy>().DetectedRayDistance,
                                                   1 << player.layer);

        float escapeInY = Mathf.Abs(player.transform.position.y - npc.transform.position.y);

        // 如果丢失玩家则转回巡逻状态
        if (hitPlayer.collider == null && escapeInY > 8)
        {
            npc.GetComponent<MeleeEnemy>().SetTransition(Transition.M_LostPlayer);

        }
    }

    public override void Act(GameObject player, GameObject npc)
    {
        Vector2 moveDir = new Vector2(player.transform.position.x - npc.transform.position.x, 0).normalized;

        Debug.DrawRay(npc.GetComponent<MeleeEnemy>().footPoint.position,
                      -npc.transform.right * npc.GetComponent<MeleeEnemy>().JumpRayDistance,
                      Color.green);

        RaycastHit2D hitObstacle = Physics2D.Raycast(npc.GetComponent<MeleeEnemy>().footPoint.position,
                                                     -npc.transform.right,
                                                     npc.GetComponent<MeleeEnemy>().JumpRayDistance,
                                                     1 << LayerMask.NameToLayer("Ground") | 1 << LayerMask.NameToLayer("Crate"));

        if (hitObstacle.collider != null && npc.GetComponent<MeleeEnemy>().IsOnGround())
        {
            npc.GetComponent<Rigidbody2D>().AddForce(new Vector2(-npc.transform.right.x * Time.deltaTime * 5, 
                                                                 npc.GetComponent<MeleeEnemy>().JumpForce), 
                                                                 ForceMode2D.Impulse);
        }

        npc.GetComponent<Rigidbody2D>().AddForce(moveDir * npc.GetComponent<MeleeEnemy>().PersueSpeed);

        if((npc.transform.position - player.transform.position).magnitude < npc.GetComponent<MeleeEnemy>().AttackRange)
        {
            npc.GetComponent<MeleeEnemy>().SetTransition(Transition.M_CloseEnough);
        }

        

    }
}

// 近战敌人休息状态类
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
        if ((npc.transform.position - player.transform.position).magnitude > npc.GetComponent<MeleeEnemy>().AttackRange)
        {
            npc.GetComponent<MeleeEnemy>().SetTransition(Transition.M_NotClose);
        }
    }

    public override void Act(GameObject player, GameObject npc)
    {
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



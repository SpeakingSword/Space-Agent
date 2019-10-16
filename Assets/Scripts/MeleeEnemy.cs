using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeleeEnemy : MonoBehaviour
{
    private GameObject player;
    public Transform footPoint;
    public LayerMask detectedLayer;
    public Transform[] path;
    private FSMSystem fsm;

    [SerializeField] private float detectedRayDistance = 15.0f;
    [SerializeField] private float attackRange = 1.0f;
    [SerializeField] private float patrolSpeed = 10.0f;
    [SerializeField] private float persueSpeed = 15.0f;
    [SerializeField] private float jumpForce = 100.0f;
    private const float jumpRayDistance = 2.5f;

    public float DetectedRayDistance { get { return detectedRayDistance; } }
    public float AttackRange { get { return attackRange; } }
    public float PatrolSpeed { get { return patrolSpeed; } }
    public float PersueSpeed { get { return persueSpeed; } }
    public float JumpRayDistance { get { return jumpRayDistance; } }
    public float JumpForce { get { return jumpForce; } }

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
    }

    private void MakeFSM()
    {
        FollowPathState follow = new FollowPathState(path);
        follow.AddTransition(Transition.SawPlayer, StateID.ChasingPlayer);
        follow.AddTransition(Transition.ReachPathPoint, StateID.Rest);

        ChasePlayerState chase = new ChasePlayerState();
        chase.AddTransition(Transition.LostPlayer, StateID.FollowingPath);
        chase.AddTransition(Transition.CloseEnough, StateID.Attack);

        AttackState attack = new AttackState();
        attack.AddTransition(Transition.NotClose, StateID.ChasingPlayer);

        RestState rest = new RestState();
        rest.AddTransition(Transition.FinishRest, StateID.FollowingPath);

        fsm = new FSMSystem();
        fsm.AddState(follow);
        fsm.AddState(chase);
        fsm.AddState(attack);
        fsm.AddState(rest);
    }
}

// 巡逻状态类
public class FollowPathState : FSMState
{
    private int currentWayPoint;
    private Transform[] waypoints;

    public FollowPathState(Transform[] wp)
    {
        waypoints = wp;
        currentWayPoint = 0;
        stateID = StateID.FollowingPath;
    }

    public override void Reason(GameObject player, GameObject npc)
    {
        Debug.DrawRay(npc.transform.position, -npc.transform.right * npc.GetComponent<MeleeEnemy>().DetectedRayDistance, Color.red);

        RaycastHit2D hitPlayer = Physics2D.Raycast(npc.transform.position,
                                                   -npc.transform.right,
                                                   npc.GetComponent<MeleeEnemy>().DetectedRayDistance,
                                                   1 << player.layer);

        if (hitPlayer.collider != null)
        {
            Debug.Log("Player has been spotted!");
            // 转换为追击状态
            npc.GetComponent<MeleeEnemy>().SetTransition(Transition.SawPlayer);
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

            // 转换为休息状态
            npc.GetComponent<MeleeEnemy>().StartCoroutine(EnemyRest(npc));
        }
        else
        {
            // 继续往路径点移动
            npc.GetComponent<Rigidbody2D>().AddForce(moveDir.normalized * npc.GetComponent<MeleeEnemy>().PatrolSpeed);
        }

        Debug.DrawRay(npc.GetComponent<MeleeEnemy>().footPoint.position, 
                      -npc.transform.right * npc.GetComponent<MeleeEnemy>().JumpRayDistance, 
                      Color.green);

        RaycastHit2D hitObstacle = Physics2D.Raycast(npc.GetComponent<MeleeEnemy>().footPoint.position,
                                                     -npc.transform.right,
                                                     npc.GetComponent<MeleeEnemy>().JumpRayDistance,
                                                     1 << LayerMask.NameToLayer("Ground") | 1 << LayerMask.NameToLayer("Crate"));

        if(hitObstacle.collider != null)
        {
            npc.GetComponent<Rigidbody2D>().AddForce(new Vector2(0, npc.GetComponent<MeleeEnemy>().JumpForce), ForceMode2D.Impulse);
        }

    }

    public IEnumerator EnemyRest(GameObject npc)
    {
        npc.GetComponent<MeleeEnemy>().SetTransition(Transition.ReachPathPoint);
        int restTime = Random.Range(0, 5);
        yield return new WaitForSeconds(restTime);
        npc.transform.Rotate(new Vector3(0, 180, 0), Space.Self);
        npc.GetComponent<MeleeEnemy>().SetTransition(Transition.FinishRest);
    }
}

// 追击状态类
public class ChasePlayerState: FSMState
{
    public ChasePlayerState()
    {
        stateID = StateID.ChasingPlayer;
    }

    public override void Reason(GameObject player, GameObject npc)
    {
        Debug.DrawRay(npc.transform.position, -npc.transform.right * npc.GetComponent<MeleeEnemy>().DetectedRayDistance, Color.red);
        RaycastHit2D hitPlayer = Physics2D.Raycast(npc.transform.position,
                                                   -npc.transform.right,
                                                   npc.GetComponent<MeleeEnemy>().DetectedRayDistance,
                                                   1 << player.layer);

        // 如果丢失玩家则转回巡逻状态
        if(hitPlayer.collider == null)
        {
            npc.GetComponent<MeleeEnemy>().SetTransition(Transition.LostPlayer);
        }
    }

    public override void Act(GameObject player, GameObject npc)
    {
        if(player.transform.position.x > npc.transform.position.x)
        {
            npc.transform.rotation = new Quaternion(0, 180, 0, 1);
        }
        else
        {
            npc.transform.rotation = new Quaternion(0, 0, 0, 1);
        }

        Vector2 moveDir = new Vector2(player.transform.position.x - npc.transform.position.x, 0).normalized;
        npc.GetComponent<Rigidbody2D>().AddForce(moveDir * npc.GetComponent<MeleeEnemy>().PersueSpeed);

        // Debug.LogFormat("The distance between player and enemy: {0}", (npc.transform.position - player.transform.position).magnitude);

        if((npc.transform.position - player.transform.position).magnitude < npc.GetComponent<MeleeEnemy>().AttackRange)
        {
            npc.GetComponent<MeleeEnemy>().SetTransition(Transition.CloseEnough);
        }

        Debug.DrawRay(npc.GetComponent<MeleeEnemy>().footPoint.position,
                      -npc.transform.right * npc.GetComponent<MeleeEnemy>().JumpRayDistance,
                      Color.green);

        RaycastHit2D hitObstacle = Physics2D.Raycast(npc.GetComponent<MeleeEnemy>().footPoint.position,
                                                     -npc.transform.right,
                                                     npc.GetComponent<MeleeEnemy>().JumpRayDistance,
                                                     1 << LayerMask.NameToLayer("Ground") | 1 << LayerMask.NameToLayer("Crate"));

        if (hitObstacle.collider != null)
        {
            npc.GetComponent<Rigidbody2D>().AddForce(new Vector2(0, npc.GetComponent<MeleeEnemy>().JumpForce), ForceMode2D.Impulse);
        }

    }
}

// 休息状态类
public class RestState: FSMState
{
    public RestState()
    {
        stateID = StateID.Rest;
    }

    public override void Reason(GameObject player, GameObject npc)
    {
        
    }

    public override void Act(GameObject player, GameObject npc)
    {
        
    }
}

// 攻击状态类
public class AttackState: FSMState
{
    public AttackState()
    {
        stateID = StateID.Attack;
    }

    public override void Reason(GameObject player, GameObject npc)
    {
        
    }

    public override void Act(GameObject player, GameObject npc)
    {
        
    }
}



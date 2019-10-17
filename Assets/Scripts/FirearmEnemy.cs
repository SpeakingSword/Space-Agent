using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FirearmEnemy : MonoBehaviour
{
    private GameObject player;
    public LayerMask groundLayer;
    public Transform footPoint;
    public Transform firePoint;
    public GameObject bulletPrefab;
    public Transform[] path;
    private FSMSystem fsm;

    [SerializeField] private float detectedRayDistance = 15.0f;
    [SerializeField] private float shootRate = 0.5f;
    [SerializeField] private float patrolSpeed = 10.0f;
    [SerializeField] private float runSpeed = 15.0f;
    [SerializeField] private float jumpForce = 100.0f;
    [SerializeField] private float meleeForce = 60.0f;
    [SerializeField] private float meleeRange = 1.0f;
    private const float jumpRayDistance = 1.5f;

    public float DetectedRayDistance { get => detectedRayDistance; set => detectedRayDistance = value; }
    public float ShootRate { get => shootRate; set => shootRate = value; }
    public float PatrolSpeed { get => patrolSpeed; set => patrolSpeed = value; }
    public float RunSpeed { get => runSpeed; set => runSpeed = value; }
    public float JumpForce { get => jumpForce; set => jumpForce = value; }
    public float JumpRayDistance { get { return jumpRayDistance; } }
    public float MeleeForce { get => meleeForce; set => meleeForce = value; }
    public float MeleeRange { get => meleeRange; set => meleeRange = value; }

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

        RaycastHit2D hitPlayer = Physics2D.Raycast(npc.transform.position,
                                                   npc.transform.right,
                                                   npc.GetComponent<FirearmEnemy>().DetectedRayDistance,
                                                   1 << player.layer);

        if (hitPlayer.collider != null)
        {
            Debug.Log("Player has been spotted by firearm enemy!");
            // 转换为攻击状态
            npc.GetComponent<FirearmEnemy>().SetTransition(Transition.F_SawPlayer);
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
            // npc.GetComponent<FirearmEnemy>().StartCoroutine(EnemyRest(npc));
        }
        else
        {
            if (Vector2.Dot(npc.transform.right, moveDir) < 0)
            {
                npc.transform.Rotate(new Vector3(0, 180, 0), Space.Self);
            }

            Debug.DrawRay(npc.GetComponent<FirearmEnemy>().footPoint.position,
                      npc.transform.right * npc.GetComponent<FirearmEnemy>().JumpRayDistance,
                      Color.green);

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

        if ((hitPlayer.collider == null && escapeInY > 10) || escapeInX > npc.GetComponent<FirearmEnemy>().DetectedRayDistance)
        {
            // 转换为巡逻状态
            npc.GetComponent<FirearmEnemy>().SetTransition(Transition.F_LostPlayer);
        }

        if((npc.transform.position - player.transform.position).magnitude < npc.GetComponent<FirearmEnemy>().MeleeRange)
        {
            npc.GetComponent<FirearmEnemy>().SetTransition(Transition.F_PlayerComeClose);
        }
    }

    public override void Act(GameObject player, GameObject npc)
    {
        if (Vector2.Dot(player.transform.position - npc.transform.position, npc.transform.right) < 0)
        {
            npc.transform.Rotate(new Vector3(0, 180, 0), Space.Self);
        }

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

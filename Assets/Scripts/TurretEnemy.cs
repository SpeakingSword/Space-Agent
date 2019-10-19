using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TurretEnemy : MonoBehaviour
{
    public LayerMask detectedLayer;
    public Transform firePoint;
    public GameObject bulletPrefab;
    private GameObject player;
    private FSMSystem fsm;
    private Quaternion originalRotation;

    [SerializeField] private float patrolTime = 3;
    [SerializeField] private float patrolSpeed = 1.0f;
    [SerializeField] private float shootRate = 0.5f;
    [SerializeField] private int horizon = 90;
    [SerializeField] private int precision = 2; 
    [SerializeField] private float rayDistance = 10.0f;

    public float PatrolSpeed { get => patrolSpeed; set => patrolSpeed = value; }
    public int Horizon { get => horizon; set => horizon = value; }
    public int Precision { get => precision; set => precision = value; }
    public float RayDistance { get => rayDistance; set => rayDistance = value; }
    public float PatrolTime { get => patrolTime; set => patrolTime = value; }
    public float ShootRate { get => shootRate; set => shootRate = value; }
    public Quaternion OriginalRotation { get => originalRotation; set => originalRotation = value; }

    public void SetTransition(Transition t) { fsm.PerformTransition(t); }

    // Start is called before the first frame update
    void Start()
    {
        player = GameObject.Find("Player");
        OriginalRotation = transform.rotation;
        MakeFSM();
    }

    // Update is called once per frame
    void Update()
    {
        fsm.CurrentState.Reason(player, gameObject);
        fsm.CurrentState.Act(player, gameObject);
    }

    void MakeFSM()
    {
        T_PatrolState patrol = new T_PatrolState();
        patrol.AddTransition(Transition.T_SawPlayer, StateID.T_Attack);

        T_AttackState attack = new T_AttackState();
        attack.AddTransition(Transition.T_LostPlayer, StateID.T_Reset);
        attack.AddTransition(Transition.PlayerDead, StateID.T_Reset);

        T_ResetState reset = new T_ResetState();
        reset.AddTransition(Transition.T_SawPlayer, StateID.T_Attack);
        reset.AddTransition(Transition.T_FinishReset, StateID.T_Patrol);

        fsm = new FSMSystem();
        fsm.AddState(patrol);
        fsm.AddState(attack);
        fsm.AddState(reset);
    }
}

// 炮塔巡逻状态类
public class T_PatrolState: FSMState
{
    int direction = 1;
    float lastTime = 0.0f;
    float currentTime = 0.0f;

    public T_PatrolState()
    {
        stateID = StateID.T_Patrol;
    }

    public override void Reason(GameObject player, GameObject npc)
    {
        float subAngle = npc.GetComponent<TurretEnemy>().Horizon / npc.GetComponent<TurretEnemy>().Precision * 2;
        for (int i = 0; i < npc.GetComponent<TurretEnemy>().Precision; i++)
        {
            
            Debug.DrawRay(npc.transform.position,
                          AfterRotate(-npc.transform.up, subAngle * (i + 1)) * npc.GetComponent<TurretEnemy>().RayDistance,
                          Color.green);
            RaycastHit2D hit = Physics2D.Raycast(npc.transform.position,
                                               AfterRotate(-npc.transform.up, subAngle * (i + 1)),
                                               npc.GetComponent<TurretEnemy>().RayDistance);
            if(hit.collider != null && hit.collider.gameObject.tag == "Player")
            {
                Debug.Log("Player have been detected by turret!");
                npc.GetComponent<TurretEnemy>().SetTransition(Transition.T_SawPlayer);
            }


            Debug.DrawRay(npc.transform.position,
                          AfterRotate(-npc.transform.up, -subAngle * (i + 1)) * npc.GetComponent<TurretEnemy>().RayDistance,
                          Color.green);
            hit = Physics2D.Raycast(npc.transform.position,
                                               AfterRotate(-npc.transform.up, -subAngle * (i + 1)),
                                               npc.GetComponent<TurretEnemy>().RayDistance);
            if (hit.collider != null && hit.collider.gameObject.tag == "Player")
            {
                Debug.Log("Player have been detected by turret!");
                npc.GetComponent<TurretEnemy>().SetTransition(Transition.T_SawPlayer);
            }
            
        }
    }

    public override void Act(GameObject player, GameObject npc)
    {
        currentTime = Time.time;
        if(currentTime - lastTime >= npc.GetComponent<TurretEnemy>().PatrolTime)
        {
            direction = -direction;
            lastTime = currentTime;
        }

        npc.transform.Rotate(Vector3.forward, npc.GetComponent<TurretEnemy>().PatrolSpeed * Time.deltaTime * direction);
    }

    Vector2 AfterRotate(Vector2 direction, float angle)
    {
        return Quaternion.Euler(0, 0, angle) * direction;
    }

    public override void DoBeforeEntering()
    {
        lastTime = Time.time;
    }
}

// 炮塔攻击状态类
public class T_AttackState: FSMState
{
    float lastTime = 0.0f;
    float currentTime = 0.0f;

    public T_AttackState()
    {
        stateID = StateID.T_Attack;
    }

    public override void Reason(GameObject player, GameObject npc)
    {
        Debug.DrawRay(npc.transform.position, 
                      -npc.transform.up * (player.transform.position - npc.transform.position).magnitude, 
                      Color.red);
        RaycastHit2D hit = Physics2D.Raycast(npc.transform.position,
                                             -npc.transform.up,
                                             (player.transform.position - npc.transform.position).magnitude,
                                             npc.GetComponent<TurretEnemy>().detectedLayer);
        if(hit.collider != null && hit.collider.gameObject.tag != "Player")
        {
            npc.GetComponent<TurretEnemy>().SetTransition(Transition.T_LostPlayer);
        }
    }

    public override void Act(GameObject player, GameObject npc)
    {
        npc.transform.up = -(player.transform.position - npc.transform.position).normalized;

        currentTime = Time.time;
        if(currentTime - lastTime >= npc.GetComponent<TurretEnemy>().ShootRate)
        {
            Object.Instantiate(npc.GetComponent<TurretEnemy>().bulletPrefab,
                           npc.GetComponent<TurretEnemy>().firePoint.position,
                           npc.transform.rotation * Quaternion.Euler(0, 0, -90));
            lastTime = currentTime;
        }

        if(player.GetComponent<PlayerController>().PlayerHealth <= 0)
        {
            npc.GetComponent<TurretEnemy>().SetTransition(Transition.PlayerDead);
        }
        
    }

    public override void DoBeforeEntering()
    {
        lastTime = Time.time;
    }
}

// 炮塔重置类
public class T_ResetState: FSMState
{
    float m_ref;

    public T_ResetState()
    {
        stateID = StateID.T_Reset;
        m_ref = Time.deltaTime;
    }

    public override void Reason(GameObject player, GameObject npc)
    {
        float subAngle = npc.GetComponent<TurretEnemy>().Horizon / npc.GetComponent<TurretEnemy>().Precision * 2;
        for (int i = 0; i < npc.GetComponent<TurretEnemy>().Precision; i++)
        {

            Debug.DrawRay(npc.transform.position,
                          AfterRotate(-npc.transform.up, subAngle * (i + 1)) * npc.GetComponent<TurretEnemy>().RayDistance,
                          Color.green);
            RaycastHit2D hit = Physics2D.Raycast(npc.transform.position,
                                               AfterRotate(-npc.transform.up, subAngle * (i + 1)),
                                               npc.GetComponent<TurretEnemy>().RayDistance);
            if (hit.collider != null && hit.collider.gameObject.tag == "Player")
            {
                Debug.Log("Player have been detected by turret!");
                npc.GetComponent<TurretEnemy>().SetTransition(Transition.T_SawPlayer);
            }


            Debug.DrawRay(npc.transform.position,
                          AfterRotate(-npc.transform.up, -subAngle * (i + 1)) * npc.GetComponent<TurretEnemy>().RayDistance,
                          Color.green);
            hit = Physics2D.Raycast(npc.transform.position,
                                               AfterRotate(-npc.transform.up, -subAngle * (i + 1)),
                                               npc.GetComponent<TurretEnemy>().RayDistance);
            if (hit.collider != null && hit.collider.gameObject.tag == "Player")
            {
                Debug.Log("Player have been detected by turret!");
                npc.GetComponent<TurretEnemy>().SetTransition(Transition.T_SawPlayer);
            }

        }
    }

    public override void Act(GameObject player, GameObject npc)
    {
        
        if(Mathf.Abs(npc.transform.rotation.z - npc.GetComponent<TurretEnemy>().OriginalRotation.z) > m_ref)
        {
            if(npc.transform.rotation.z > npc.GetComponent<TurretEnemy>().OriginalRotation.z)
            {
                npc.transform.Rotate(Vector3.forward, npc.GetComponent<TurretEnemy>().PatrolSpeed * Time.deltaTime * -1);
            }
            else if(npc.transform.rotation.z < npc.GetComponent<TurretEnemy>().OriginalRotation.z)
            {
                npc.transform.Rotate(Vector3.forward, npc.GetComponent<TurretEnemy>().PatrolSpeed * Time.deltaTime * 1);
            }
        }
        else
        {
            npc.transform.rotation = npc.GetComponent<TurretEnemy>().OriginalRotation;
            npc.GetComponent<TurretEnemy>().SetTransition(Transition.T_FinishReset);
        }
    }

    Vector2 AfterRotate(Vector2 direction, float angle)
    {
        return Quaternion.Euler(0, 0, angle) * direction;
    }
}

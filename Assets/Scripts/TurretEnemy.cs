using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TurretEnemy : MonoBehaviour
{
    public LayerMask detectedLayer;                                 // 可以被检测到的层级
    public Transform firePoint;                                     // 开火位置
    public GameObject bulletPrefab;                                 // 子弹实例
    private GameObject player;                                      // 玩家实例
    private FSMSystem fsm;                                          // FSM实例
    private AudioSource turretAudio;                                // 炮塔音效播放器
    private Quaternion originalRotation;                            // 炮塔起始位置

    [SerializeField] private float patrolTime = 3;                  // 完成半次摇摆所需的时间
    [SerializeField] private float patrolSpeed = 1.0f;              // 摇摆速度
    [SerializeField] private float shootRate = 0.5f;                // 射击频率
    [SerializeField] private int horizon = 90;                      // 检测玩家的扇形角度（范围）
    [SerializeField] private int precision = 2;                     // 检测的射线精度（同范围下精度越高，射线越多）
    [SerializeField] private float rayDistance = 10.0f;             // 射线距离
    public AudioClip fireSound;                                     // 开火音效
    private int health = 100;                                       // 生命值

    public float PatrolSpeed { get => patrolSpeed; set => patrolSpeed = value; }
    public int Horizon { get => horizon; set => horizon = value; }
    public int Precision { get => precision; set => precision = value; }
    public float RayDistance { get => rayDistance; set => rayDistance = value; }
    public float PatrolTime { get => patrolTime; set => patrolTime = value; }
    public float ShootRate { get => shootRate; set => shootRate = value; }
    public Quaternion OriginalRotation { get => originalRotation; set => originalRotation = value; }
    public int Health { get => health; set => health = value; }
    public AudioSource TurretAudio { get => turretAudio; set => turretAudio = value; }

    // 激活状态转换过程
    public void SetTransition(Transition t) { fsm.PerformTransition(t); }

    // 第一帧之前执行一次
    void Start()
    {
        player = GameObject.Find("Player");
        turretAudio = GetComponent<AudioSource>();
        OriginalRotation = transform.rotation;
        MakeFSM();
    }

    // 每帧都会执行一次
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
        attack.AddTransition(Transition.T_SawPlayer, StateID.T_Attack);

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
    int direction = 1;              // 初始摇摆的方向（逆时针）
    float lastTime = 0.0f;
    float currentTime = 0.0f;

    public T_PatrolState()
    {
        stateID = StateID.T_Patrol;
    }

    public override void Reason(GameObject player, GameObject npc)
    {
        // 发射射线，侦测到玩家则转换为攻击状态
        // 由范围和精度计算出需要发出射线的方向
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
        // 开始摇摆，经过半次摇摆后反方向继续摇摆（在逆时针和顺时针之间切换）
        currentTime = Time.time;
        if(currentTime - lastTime >= npc.GetComponent<TurretEnemy>().PatrolTime)
        {
            direction = -direction;
            lastTime = currentTime;
        }

        npc.transform.Rotate(Vector3.forward, npc.GetComponent<TurretEnemy>().PatrolSpeed * Time.deltaTime * direction);
    }

    // 计算由初始角度旋转一定角度后的方向
    Vector2 AfterRotate(Vector2 direction, float angle)
    {
        return Quaternion.Euler(0, 0, angle) * direction;
    }

    public override void DoBeforeEntering()
    {
        lastTime = Time.time;

        // 初始化摇摆方向（逆时针）
        direction = 1;
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
        // 探测玩家的射线，射线长度为炮塔与玩家之间的距离
        RaycastHit2D hit = Physics2D.Raycast(npc.transform.position,
                                             -npc.transform.up,
                                             (player.transform.position - npc.transform.position).magnitude,
                                             npc.GetComponent<TurretEnemy>().detectedLayer);

        // 如果射线探测不到玩家则确认为失去目标
        if(hit.collider != null && hit.collider.gameObject.tag != "Player")
        {
            npc.GetComponent<TurretEnemy>().SetTransition(Transition.T_LostPlayer);
        }
    }

    public override void Act(GameObject player, GameObject npc)
    {
        // 设置开火方向朝向玩家
        npc.transform.up = -(player.transform.position - npc.transform.position).normalized;

        // 开始根据射击频率射击
        currentTime = Time.time;
        if(currentTime - lastTime >= npc.GetComponent<TurretEnemy>().ShootRate)
        {
            npc.GetComponent<TurretEnemy>().TurretAudio.PlayOneShot(npc.GetComponent<TurretEnemy>().fireSound, 0.2f);
            Object.Instantiate(npc.GetComponent<TurretEnemy>().bulletPrefab,
                           npc.GetComponent<TurretEnemy>().firePoint.position,
                           npc.transform.rotation * Quaternion.Euler(0, 0, -90));
            lastTime = currentTime;
        }

        // 如果玩家死亡则转换为重置状态
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
    float m_ref;            // 最小角度差，当炮塔当前角度与初始位置的角度的差小于等于最小角度差时，
                            // 确认为完成重置

    public T_ResetState()
    {
        stateID = StateID.T_Reset;
        m_ref = Time.deltaTime;
    }

    public override void Reason(GameObject player, GameObject npc)
    {
        // 发射射线，侦测到玩家则转换为攻击状态
        // 由范围和精度计算出需要发出射线的方向
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
        // 如果当炮塔当前角度与初始位置的角度的差小于等于最小角度差时，
        // 确认为完成重置，否则继续重置
        if (Mathf.Abs(npc.transform.rotation.z - npc.GetComponent<TurretEnemy>().OriginalRotation.z) > m_ref)
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

    // 计算由初始角度旋转一定角度后的方向
    Vector2 AfterRotate(Vector2 direction, float angle)
    {
        return Quaternion.Euler(0, 0, angle) * direction;
    }
}

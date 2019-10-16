using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeleeEnemyController : MonoBehaviour
{
    private GameObject player;
    public Transform[] path;
    private FSMSystem fsm;

    [SerializeField] private float detectedRayDistance = 15.0f;
    [SerializeField] private float patrolSpeed = 10.0f;

    public float DetectedRayDistance { get { return detectedRayDistance; } }
    public float PatrolSpeed { get { return patrolSpeed; } }

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

        ChasePlayerState chase = new ChasePlayerState();
        chase.AddTransition(Transition.LostPlayer, StateID.FollowingPath);

        fsm = new FSMSystem();
        fsm.AddState(follow);
        fsm.AddState(chase);
    }
}

public class FollowPathState: FSMState
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
        Debug.DrawRay(npc.transform.position,
                      -npc.transform.right * npc.GetComponent<MeleeEnemyController>().DetectedRayDistance,
                      Color.red);

        RaycastHit2D hit = Physics2D.Raycast(npc.transform.position,
                                            -npc.transform.right,
                                            npc.GetComponent<MeleeEnemyController>().DetectedRayDistance,
                                            1<<player.layer);

        if(hit.collider != null)
        {   
            Debug.Log("Player has been spotted!");
        }
        
    }

    public override void Act(GameObject player, GameObject npc)
    {
        Vector2 vel = npc.GetComponent<Rigidbody2D>().velocity;
        Vector2 moveDir = waypoints[currentWayPoint].position - npc.transform.position;

        if(moveDir.magnitude < 1)
        {
            currentWayPoint++;
            if(currentWayPoint >= waypoints.Length)
            {
                currentWayPoint = 0;
            }

            npc.transform.Rotate(new Vector3(0, 180, 0), Space.Self);
        }
        else
        {
            vel = moveDir.normalized * npc.GetComponent<MeleeEnemyController>().PatrolSpeed;
        }

        npc.GetComponent<Rigidbody2D>().velocity = vel;
    }
}

public class ChasePlayerState: FSMState
{
    public ChasePlayerState()
    {
        stateID = StateID.ChasingPlayer;
    }

    public override void Reason(GameObject player, GameObject npc)
    {
        
    }

    public override void Act(GameObject player, GameObject npc)
    {
        
    }
}



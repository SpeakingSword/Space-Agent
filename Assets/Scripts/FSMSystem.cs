using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 定义状态之间的转换
/// </summary>
public enum Transition
{
    NullTransition = 0,
    SawPlayer,
    LostPlayer,
    ReachPathPoint,
    FinishRest,
    CloseEnough,
    NotClose,
    PlayerDead,
}

/// <summary>
/// 定义需要的状态
/// </summary>
public enum StateID
{
    NullStateID = 0,
    FollowingPath,
    ChasingPlayer,
    Rest,
    Attack,
}

/// <summary>
/// 状态类
/// </summary>
public abstract class FSMState
{
    protected Dictionary<Transition, StateID> map = new Dictionary<Transition, StateID>();
    protected StateID stateID;
    public StateID ID { get { return stateID; } }

    /// <summary>
    /// 添加转换
    /// </summary>
    public void AddTransition(Transition trans, StateID id)
    {
        // 检查各个参数是否合法
        if(trans == Transition.NullTransition)
        {
            Debug.LogError("FSMState ERROR: NullTransition is not allowed for a real transition");
            return;
        }

        if(id == StateID.NullStateID)
        {
            Debug.LogError("FSMState ERROR: NullStateID is not allowed for a real ID");
            return;
        }

        // 检查要添加的转换是否已存在
        if (map.ContainsKey(trans))
        {
            Debug.LogError("FSMState ERROR: State " + stateID.ToString() + " already has transition " + trans.ToString() +
                           "Impossible to assign to another state");
            return;
        }

        map.Add(trans, id);
    }

    /// <summary>
    /// 删除状态的转换
    /// </summary>
    public void DeleteTransition(Transition trans)
    {
        if(trans == Transition.NullTransition)
        {
            Debug.LogError("FSMState ERROR: NullTransition is not allowed");
            return;
        }

        if (map.ContainsKey(trans))
        {
            map.Remove(trans);
            return;
        }

        Debug.LogError("FSMState ERROR: Transition " + trans.ToString() + " passed to " + stateID.ToString() +
                       " was not on the state's transition list");
    }

    /// <summary>
    /// 获得转换对应的状态
    /// </summary>
    public StateID GetOutputState(Transition trans)
    {
        if (map.ContainsKey(trans))
        {
            return map[trans];
        }

        return StateID.NullStateID;
    }

    /// <summary>
    /// 进入新状态之前要做的处理
    /// </summary>
    public virtual void DoBeforeEntering()
    {

    }

    /// <summary>
    /// 退出当前状态前要做的处理
    /// </summary>
    public virtual void DoBeforeLeaving()
    {

    }

    /// <summary>
    /// 判断是否需要转换到另一个状态
    /// </summary>
    public abstract void Reason(GameObject player, GameObject npc);

    /// <summary>
    /// 游戏对象在当前状态下的行为
    /// </summary>
    public abstract void Act(GameObject player, GameObject npc);
}

/// <summary>
/// 有限状态机系统类
/// </summary>
public class FSMSystem
{
    private List<FSMState> states;

    private StateID currentStateID;
    public StateID CurrentStateID { get { return currentStateID; } }

    private FSMState currentState;
    public FSMState CurrentState { get { return currentState; } }

    public FSMSystem()
    {
        states = new List<FSMState>();
    }

    /// <summary>
    /// 给当前系统添加新状态
    /// </summary>
    public void AddState(FSMState s)
    {
        if(s == null)
        {
            Debug.LogError("FSM ERROR: Null reference is not allowed");
        }

        if(states.Count == 0)
        {
            states.Add(s);
            currentState = s;
            currentStateID = s.ID;
            return;
        }

        foreach(FSMState state in states)
        {
            if(state.ID == s.ID)
            {
                Debug.LogError("FSM ERROR: Impossible to add state " + s.ID.ToString() +
                              " because state has already been added");
                return;
            }
        }

        states.Add(s);
    }

    /// <summary>
    /// 删除系统中的某个状态
    /// </summary>
    public void DeleteState(StateID id)
    {
        if(id == StateID.NullStateID)
        {
            Debug.LogError("FSM ERROR: NullStateID is not allowed for a real state");
            return;
        }

        foreach(FSMState state in states)
        {
            if(state.ID == id)
            {
                states.Remove(state);
                return;
            }
        }

        Debug.LogError("FSM ERROR: Impossible to delete state " + id.ToString() +
                       ". It was not on the list of states");
    }

    /// <summary>
    /// 开始转换状态
    /// </summary>
    public void PerformTransition(Transition trans)
    {
        if(trans == Transition.NullTransition)
        {
            Debug.LogError("FSM ERROR: NullTransition is not allowed for a real transition");
            return;
        }

        StateID id = currentState.GetOutputState(trans);
        if(id == StateID.NullStateID)
        {
            Debug.LogError("FSM ERROR: State " + currentStateID.ToString() + " does not have a target state " +
                           " for transition " + trans.ToString());
            return;
        }

        currentStateID = id;
        foreach(FSMState state in states)
        {
            if(state.ID == currentStateID)
            {
                currentState.DoBeforeLeaving();

                currentState = state;

                currentState.DoBeforeEntering();
                break;
            }
        }
    }
}

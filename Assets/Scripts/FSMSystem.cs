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

    M_SawPlayer,                    // 近战敌人发现玩家
    M_LostPlayer,                   // 近战敌人丢失玩家
    M_ReachPathPoint,               // 近战敌人到达目标路径点
    M_FinishRest,                   // 近战敌人完成休息
    M_CloseEnough,                  // 近战敌人靠近玩家
    M_NotClose,                     // 近战敌人没有靠近玩家

    PlayerDead,                     // 玩家死亡

    F_SawPlayer,                    // 远程敌人发现玩家
    F_LostPlayer,                   // 远程敌人丢失玩家
    F_ReachPathPoint,               // 远程敌人到达目标路径点
    F_FinishRest,                   // 远程敌人完成休息
    F_PlayerComeClose,              // 远程敌人被玩家靠近
    F_PlayerFallback,               // 远程敌人开始撤退

    T_SawPlayer,                    // 炮塔发现玩家
    T_LostPlayer,                   // 炮塔丢失玩家
    T_FinishReset,                  // 炮塔完成重置
}

/// <summary>
/// 定义需要的状态
/// </summary>
public enum StateID
{
    NullStateID = 0,

    M_FollowingPath,                // 近战敌人巡逻状态 
    M_ChasingPlayer,                // 近战敌人追击状态
    M_Rest,                         // 近战敌人休息状态
    M_Attack,                       // 近战敌人攻击状态

    F_FollowingPath,                // 远程敌人巡逻状态
    F_Attack,                       // 远程敌人攻击状态
    F_Melee,                        // 远程敌人近战攻击状态
    F_Rest,                         // 远程敌人休息状态

    T_Patrol,                       // 炮塔巡逻（摇摆）状态
    T_Attack,                       // 炮塔攻击状态
    T_Reset,                        // 炮塔重置状态
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

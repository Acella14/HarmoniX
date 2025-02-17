using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using BlackboardSystem;


[RequireComponent(typeof(UnityEngine.AI.NavMeshAgent))]
//[RequireComponent(typeof(Animator))]
public class BasicEnemy : MonoBehaviour
{
    [SerializeField] List<Transform> waypoints = new();

    UnityEngine.AI.NavMeshAgent agent;
    //AnimatorController animations;
    BehaviourTree tree;

    //[SerializeField] BlackboardData blackboardData;
    //readonly Blackboard blackboard = new Blackboard();
    //BlackboardKey someKey;

    void Awake() {
        agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
        //animations = GetComponent<AnimatorController>();

        tree = new BehaviourTree("BasicEnemy");
        tree.AddChild(new Leaf("Patrol", new PatrolStrategy(transform, agent, waypoints)));
    }

    void Update() {
        //animations.SetSpeed(agent.velocity.magnitude);
        tree.Process();
    }
}

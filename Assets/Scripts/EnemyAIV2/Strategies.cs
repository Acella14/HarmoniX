using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.AI;

public interface IStrategy {
    Node.Status Process();
    void Reset() {
        // Noop
    }
}

public class ActionStrategy : IStrategy {
    readonly Action doSomething;

    public ActionStrategy(Action doSomething) {
        this.doSomething = doSomething;
    }

    public Node.Status Process() {
        doSomething();
        return Node.Status.Success;
    }
}

public class Condition : IStrategy {
    readonly Func<bool> predicate;

    public Condition(Func<bool> predicate) {
        this.predicate = predicate;
    }

    public Node.Status Process() => predicate() ? Node.Status.Success : Node.Status.Failure;
}

public class PatrolStrategy : IStrategy {
    readonly Transform entity;
    readonly NavMeshAgent agent;
    readonly List<Transform> patrolPoints;
    readonly float patrolSpeed;
    int currentIndex;
    bool isPathCalculated;

    public PatrolStrategy(Transform entity, NavMeshAgent agent, List<Transform> patrolPoints, float patrolSpeed = 2f) {
        this.entity = entity;
        this.agent = agent;
        this.patrolPoints = patrolPoints;
        this.patrolSpeed = patrolSpeed;
    }

    public Node.Status Process() {
        if (currentIndex == patrolPoints.Count) return Node.Status.Success;

        var target = patrolPoints[currentIndex];
        agent.SetDestination(target.position);

        if (isPathCalculated && agent.remainingDistance < 0.1f) {
            currentIndex++;
            isPathCalculated = false;
        }

        if (agent.pathPending) {
            isPathCalculated = true;
        }

        return Node.Status.Running;
    }

    public void Reset() => currentIndex = 0;
}

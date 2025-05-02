using System;
using System.Collections.Generic;
using UnityEngine;
using UnityServiceLocator;


namespace BlackboardSystem 
{
    public class BlackboardController : MonoBehaviour
    {
        [SerializeField] BlackboardData blackboardData;
        readonly Blackboard blackboard = new Blackboard();
        readonly Arbiter arbiter = new Arbiter();

        void Awake() {
            if (ServiceLocator.Global.TryGet<BlackboardController>(out var _)) {
                Destroy(gameObject);
                return;
            }

            ServiceLocator.Global.Register(this);
            DontDestroyOnLoad(gameObject);

            blackboardData.SetValuesOnBlackboard(blackboard);
            blackboard.Debug();
        }

        public Blackboard GetBlackboard() => blackboard;

        public void RegisterExpert(IExpert expert) => arbiter.RegisterExpert(expert);
        public void DeregisterExpert(IExpert expert) => arbiter.DeregisterExpert(expert);

        void Update() {
            // Execute all agreed actions from the currrent iteration
            foreach (var action in arbiter.BlackboardIteration(blackboard)) {
                action();
            }

            if (Input.GetKeyDown(KeyCode.B)) { // Press "B" to dump the blackboard state.
                blackboard.Debug();
            }
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Assets.Scripts;

namespace Assets.Scripts
{
    public class Vehicle : MonoBehaviour
    {
        public float speed = 5f; // default speed
        public RoadNode currentRoadNode;
        public RoadNode destinationRoadNode;
        private Vector3 targetPosition;
        
        void Start()
        {
            // Initialize the vehicle's position to its current node's position
            if(currentRoadNode != null)
            {
                transform.position = currentRoadNode.transform.position;
            }
        }
        
        void Update()
        {
            // If the vehicle has a destination, move towards it
            if(destinationRoadNode != null)
            {
                targetPosition = destinationRoadNode.transform.position;
                MoveTowardsTarget();
            }
        }

        private void MoveTowardsTarget()
        {
            Vector3 moveDirection = (targetPosition - transform.position).normalized;
            transform.position += moveDirection * speed * Time.deltaTime;

            // Check if the vehicle has reached its target
            float distanceToTarget = Vector3.Distance(transform.position, targetPosition);
            if(distanceToTarget <= 0.1f) // near enough to be considered at the target
            {
                ArriveAtNode();
            }
        }

        private void ArriveAtNode()
        {
            currentRoadNode = destinationRoadNode;
            destinationRoadNode = null;
        }

        public void SetDestination(RoadNode newDestination)
        {
            destinationRoadNode = newDestination;
        }
    }
}
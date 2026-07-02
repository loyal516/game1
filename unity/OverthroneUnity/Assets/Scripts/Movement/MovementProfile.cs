using System;
using UnityEngine;

namespace Overthrone
{
    [Serializable]
    public sealed class MovementProfile
    {
        public MovementState state = MovementState.Neutral;
        public bool canMove = true;
        public bool canSprint = true;
        public float walkSpeed = 4.5f;
        public float runSpeed = 7.2f;
        public float acceleration = 18f;
        public float turnSharpness = 16f;
        public float noiseRadius = 0f;
        public float footstepInterval = 0.42f;

        public float MaxSpeed(bool sprinting)
        {
            if (!canMove)
            {
                return 0f;
            }

            return sprinting && canSprint ? runSpeed : walkSpeed;
        }
    }
}

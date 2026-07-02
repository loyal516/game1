using UnityEngine;

namespace Overthrone
{
    [RequireComponent(typeof(Animator))]
    [RequireComponent(typeof(PlayerMotor))]
    public sealed class PlayerAnimationDriver : MonoBehaviour
    {
        private static readonly int MoveSpeedHash = Animator.StringToHash("MoveSpeed");
        private static readonly int IsMovingHash = Animator.StringToHash("IsMoving");
        private static readonly int IsSprintingHash = Animator.StringToHash("IsSprinting");
        private static readonly int IsGroundedHash = Animator.StringToHash("IsGrounded");
        private static readonly int MovementStateHash = Animator.StringToHash("MovementState");

        [SerializeField] private float dampTime = 0.08f;

        private Animator animator;
        private PlayerMotor motor;

        private void Awake()
        {
            animator = GetComponent<Animator>();
            motor = GetComponent<PlayerMotor>();
        }

        private void Update()
        {
            var profile = motor.CurrentProfile;
            var runSpeed = Mathf.Max(0.01f, profile.runSpeed);
            var normalizedSpeed = Mathf.Clamp01(motor.CurrentHorizontalSpeed / runSpeed);

            animator.SetFloat(MoveSpeedHash, normalizedSpeed, dampTime, Time.deltaTime);
            animator.SetBool(IsMovingHash, motor.IsMoving);
            animator.SetBool(IsSprintingHash, motor.IsSprinting);
            animator.SetBool(IsGroundedHash, motor.IsGrounded);
            animator.SetInteger(MovementStateHash, (int)motor.State);
        }
    }
}

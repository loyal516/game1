using System.Reflection;
using NUnit.Framework;
using Overthrone;
using UnityEngine;

public sealed class CaptureInteractionTests
{
    private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
    private const float SlimeDurationSeconds = 3f;

    [Test]
    public void TackleRequiresAttackerOrKingWithEnoughStamina()
    {
        Assert.IsFalse(CaptureInteractionRules.CanTackle(MovementState.Neutral, CaptureStatus.Free, 100f));
        Assert.IsFalse(CaptureInteractionRules.CanTackle(MovementState.Attacker, CaptureStatus.Free, 29f));
        Assert.IsFalse(CaptureInteractionRules.CanTackle(MovementState.King, CaptureStatus.Held, 100f));
        Assert.IsTrue(CaptureInteractionRules.CanTackle(MovementState.Attacker, CaptureStatus.Free, 30f));
        Assert.IsTrue(CaptureInteractionRules.CanTackle(MovementState.King, CaptureStatus.Free, 100f));
    }

    [Test]
    public void TackleTargetMustBeEnemyFreeOrHolding()
    {
        Assert.IsTrue(CaptureInteractionRules.CanTackleTarget(TeamId.Blue, TeamId.Red, CaptureStatus.Free));
        Assert.IsTrue(CaptureInteractionRules.CanTackleTarget(TeamId.Blue, TeamId.Red, CaptureStatus.Holding));
        Assert.IsFalse(CaptureInteractionRules.CanTackleTarget(TeamId.Blue, TeamId.Blue, CaptureStatus.Holding));
        Assert.IsFalse(CaptureInteractionRules.CanTackleTarget(TeamId.None, TeamId.Red, CaptureStatus.Holding));
        Assert.IsFalse(CaptureInteractionRules.CanTackleTarget(TeamId.Blue, TeamId.None, CaptureStatus.Holding));
        Assert.IsFalse(CaptureInteractionRules.CanTackleTarget(TeamId.Blue, TeamId.Red, CaptureStatus.Held));
        Assert.IsFalse(CaptureInteractionRules.CanTackleTarget(TeamId.Blue, TeamId.Red, CaptureStatus.Captured));
    }

    [Test]
    public void SlimeEscapeRequiresHeldStatusAndUnusedCharge()
    {
        Assert.IsFalse(CaptureInteractionRules.CanSlimeEscape(CaptureStatus.Free, false));
        Assert.IsFalse(CaptureInteractionRules.CanSlimeEscape(CaptureStatus.Holding, false));
        Assert.IsFalse(CaptureInteractionRules.CanSlimeEscape(CaptureStatus.Captured, false));
        Assert.IsFalse(CaptureInteractionRules.CanSlimeEscape(CaptureStatus.Held, true));
        Assert.IsTrue(CaptureInteractionRules.CanSlimeEscape(CaptureStatus.Held, false));
    }

    [Test]
    public void FinalCaptureRequiresSeparateHolder()
    {
        Assert.IsFalse(CaptureInteractionRules.CanFinalCapture(MovementState.King, CaptureStatus.Holding, CaptureStatus.Held, true));
        Assert.IsTrue(CaptureInteractionRules.CanFinalCapture(MovementState.King, CaptureStatus.Free, CaptureStatus.Held, false));
    }

    [Test]
    public void HoldSetsHolderAndTargetMovementStatesThenReleaseRestoresThem()
    {
        var holder = CreateAgent("Holder", TeamId.Blue, MovementState.Attacker);
        var target = CreateAgent("Target", TeamId.Red, MovementState.Neutral);

        try
        {
            Assert.IsTrue(holder.Agent.TryHold(target.Agent));

            Assert.AreEqual(CaptureStatus.Holding, holder.Agent.Status);
            Assert.AreEqual(CaptureStatus.Held, target.Agent.Status);
            Assert.AreEqual(MovementState.Holding, holder.StateController.PersistentState);
            Assert.AreEqual(MovementState.Held, target.StateController.PersistentState);

            holder.Agent.ReleaseHold();

            Assert.AreEqual(CaptureStatus.Free, holder.Agent.Status);
            Assert.AreEqual(CaptureStatus.Free, target.Agent.Status);
            Assert.AreEqual(MovementState.Attacker, holder.StateController.PersistentState);
            Assert.AreEqual(MovementState.Neutral, target.StateController.PersistentState);
        }
        finally
        {
            Object.DestroyImmediate(holder.GameObject);
            Object.DestroyImmediate(target.GameObject);
        }
    }

    [Test]
    public void NeutralPlayerCannotCreateHoldDirectly()
    {
        var holder = CreateAgent("Neutral Holder", TeamId.Blue, MovementState.Neutral);
        var target = CreateAgent("Neutral Hold Target", TeamId.Red, MovementState.Neutral);

        try
        {
            Assert.IsFalse(holder.Agent.TryHold(target.Agent));
            Assert.AreEqual(CaptureStatus.Free, holder.Agent.Status);
            Assert.AreEqual(CaptureStatus.Free, target.Agent.Status);
        }
        finally
        {
            Object.DestroyImmediate(holder.GameObject);
            Object.DestroyImmediate(target.GameObject);
        }
    }

    [Test]
    public void KingCannotCompleteCaptureAfterHoldingTarget()
    {
        var king = CreateAgent("King", TeamId.Blue, MovementState.King);
        var target = CreateAgent("Held Target", TeamId.Red, MovementState.Neutral);

        try
        {
            Assert.IsTrue(king.Agent.TryHold(target.Agent));
            Assert.IsFalse(king.Agent.CompleteCapture(target.Agent));

            Assert.AreEqual(CaptureStatus.Holding, king.Agent.Status);
            Assert.AreEqual(CaptureStatus.Held, target.Agent.Status);
            Assert.AreEqual(king.Agent, target.Agent.HeldBy);
            Assert.AreEqual(target.Agent, king.Agent.HeldTarget);
            Assert.AreEqual(MovementState.Holding, king.StateController.PersistentState);
            Assert.AreEqual(MovementState.Held, target.StateController.PersistentState);
            Assert.IsFalse(king.Agent.IsReleaseStunned);
        }
        finally
        {
            Object.DestroyImmediate(king.GameObject);
            Object.DestroyImmediate(target.GameObject);
        }
    }

    [Test]
    public void KingCanCompleteCaptureTargetHeldByAlly()
    {
        var holder = CreateAgent("Ally Holder", TeamId.Blue, MovementState.Attacker);
        var king = CreateAgent("Ally King", TeamId.Blue, MovementState.King);
        var target = CreateAgent("Held Enemy", TeamId.Red, MovementState.Neutral);

        try
        {
            Assert.IsTrue(holder.Agent.TryHold(target.Agent));
            Assert.IsTrue(king.Agent.CompleteCapture(target.Agent));

            Assert.AreEqual(CaptureStatus.Free, holder.Agent.Status);
            Assert.AreEqual(CaptureStatus.Free, king.Agent.Status);
            Assert.AreEqual(CaptureStatus.Captured, target.Agent.Status);
            Assert.IsNull(holder.Agent.HeldTarget);
            Assert.IsNull(target.Agent.HeldBy);
            Assert.AreEqual(MovementState.Attacker, holder.StateController.PersistentState);
            Assert.AreEqual(MovementState.King, king.StateController.PersistentState);
            Assert.AreEqual(MovementState.Captured, target.StateController.PersistentState);
            Assert.AreEqual(1, king.Agent.Team.FinalCaptureCount);
        }
        finally
        {
            Object.DestroyImmediate(holder.GameObject);
            Object.DestroyImmediate(king.GameObject);
            Object.DestroyImmediate(target.GameObject);
        }
    }

    [Test]
    public void RescueNearbyReleaseStunsHolder()
    {
        var holder = CreateAgent("Holder", TeamId.Blue, MovementState.Attacker);
        var target = CreateAgent("Target", TeamId.Red, MovementState.Neutral);
        var rescuer = CreateAgent("Rescuer", TeamId.Red, MovementState.Neutral);
        var systemObject = new GameObject("Capture System");
        var captureSystem = systemObject.AddComponent<LocalCaptureSystem>();
        captureSystem.Configure(rescuer.Agent, new[] { holder.Agent, target.Agent, rescuer.Agent });

        try
        {
            Assert.IsTrue(holder.Agent.TryHold(target.Agent));
            Assert.IsTrue(captureSystem.TryRescueNearby(rescuer.Agent));

            Assert.AreEqual(CaptureStatus.Free, holder.Agent.Status);
            Assert.AreEqual(CaptureStatus.Free, target.Agent.Status);
            Assert.AreEqual(MovementState.Attacker, holder.StateController.PersistentState);
            Assert.AreEqual(MovementState.Neutral, target.StateController.PersistentState);
            Assert.AreEqual(MovementState.Holding, holder.StateController.CurrentState);
            Assert.IsTrue(holder.Agent.IsReleaseStunned);
        }
        finally
        {
            Object.DestroyImmediate(systemObject);
            Object.DestroyImmediate(holder.GameObject);
            Object.DestroyImmediate(target.GameObject);
            Object.DestroyImmediate(rescuer.GameObject);
        }
    }

    [Test]
    public void TackleInterruptsNearestEnemyHolderAndStunsHolder()
    {
        var attacker = CreateAgent("Interrupting Attacker", TeamId.Blue, MovementState.Attacker);
        var enemyHolder = CreateAgent("Enemy Holder", TeamId.Red, MovementState.Attacker);
        var heldTarget = CreateAgent("Held Ally", TeamId.Blue, MovementState.Neutral);
        var fartherFreeEnemy = CreateAgent("Farther Free Enemy", TeamId.Red, MovementState.Neutral);
        var systemObject = new GameObject("Capture System");
        var captureSystem = systemObject.AddComponent<LocalCaptureSystem>();

        attacker.GameObject.transform.position = Vector3.zero;
        enemyHolder.GameObject.transform.position = Vector3.forward * 1.5f;
        heldTarget.GameObject.transform.position = Vector3.forward * 1.75f;
        fartherFreeEnemy.GameObject.transform.position = Vector3.forward * 2.5f;
        captureSystem.Configure(
            attacker.Agent,
            new[] { attacker.Agent, enemyHolder.Agent, heldTarget.Agent, fartherFreeEnemy.Agent }
        );

        try
        {
            Assert.IsTrue(enemyHolder.Agent.TryHold(heldTarget.Agent));
            Assert.IsTrue(captureSystem.TryTackle(attacker.Agent));

            Assert.AreEqual(CaptureStatus.Free, attacker.Agent.Status);
            Assert.AreEqual(CaptureStatus.Free, enemyHolder.Agent.Status);
            Assert.AreEqual(CaptureStatus.Free, heldTarget.Agent.Status);
            Assert.AreEqual(CaptureStatus.Free, fartherFreeEnemy.Agent.Status);
            Assert.IsNull(attacker.Agent.HeldTarget);
            Assert.IsNull(enemyHolder.Agent.HeldTarget);
            Assert.IsNull(heldTarget.Agent.HeldBy);
            Assert.AreEqual(MovementState.Attacker, enemyHolder.StateController.PersistentState);
            Assert.AreEqual(MovementState.Neutral, heldTarget.StateController.PersistentState);
            Assert.AreEqual(MovementState.Holding, enemyHolder.StateController.CurrentState);
            Assert.IsTrue(enemyHolder.Agent.IsReleaseStunned);
            Assert.AreEqual(70f, attacker.GameObject.GetComponent<PlayerMotor>().currentStamina);
        }
        finally
        {
            Object.DestroyImmediate(systemObject);
            Object.DestroyImmediate(attacker.GameObject);
            Object.DestroyImmediate(enemyHolder.GameObject);
            Object.DestroyImmediate(heldTarget.GameObject);
            Object.DestroyImmediate(fartherFreeEnemy.GameObject);
        }
    }

    [Test]
    public void TackleMissWithoutTargetSpendsStaminaStartsCooldownAndStuns()
    {
        var attacker = CreateAgent("Missing Attacker", TeamId.Blue, MovementState.Attacker);
        var target = CreateAgent("Out Of Range Target", TeamId.Red, MovementState.Neutral);
        var captureSystem = attacker.GameObject.AddComponent<LocalCaptureSystem>();

        attacker.GameObject.transform.position = Vector3.zero;
        target.GameObject.transform.position = Vector3.forward;
        captureSystem.Configure(attacker.Agent, new[] { attacker.Agent });

        try
        {
            Assert.IsFalse(captureSystem.TryTackle(attacker.Agent));
            Assert.AreEqual(70f, attacker.GameObject.GetComponent<PlayerMotor>().currentStamina);
            Assert.AreEqual(MovementState.Holding, attacker.StateController.CurrentState);
            Assert.IsTrue(attacker.Agent.IsInteractionStunned);
            Assert.IsFalse(attacker.Agent.IsReleaseStunned);

            captureSystem.Configure(attacker.Agent, new[] { attacker.Agent, target.Agent });
            AdvanceTimedState(attacker.StateController, CaptureInteractionRules.TackleMissStunSeconds);
            captureSystem.Tick(CaptureInteractionRules.TackleMissStunSeconds);

            Assert.IsFalse(attacker.Agent.IsInteractionStunned);
            Assert.IsFalse(
                captureSystem.TryTackle(attacker.Agent),
                "The 2s tackle cooldown should still block the same local system after the 0.5s miss stun expires."
            );
            Assert.AreEqual(
                70f,
                attacker.GameObject.GetComponent<PlayerMotor>().currentStamina,
                "A cooldown-blocked retackle must not spend another tackle cost."
            );

            captureSystem.Tick(CaptureInteractionRules.TackleCooldownSeconds - CaptureInteractionRules.TackleMissStunSeconds);
            Assert.IsTrue(captureSystem.TryTackle(attacker.Agent));
            Assert.AreEqual(CaptureStatus.Holding, attacker.Agent.Status);
            Assert.AreEqual(CaptureStatus.Held, target.Agent.Status);
        }
        finally
        {
            Object.DestroyImmediate(captureSystem);
            Object.DestroyImmediate(attacker.GameObject);
            Object.DestroyImmediate(target.GameObject);
        }
    }

    [Test]
    public void TackleHitboxCanFindColliderTargetOutsideConfiguredAgentList()
    {
        var attacker = CreateAgent("Hitbox Attacker", TeamId.Blue, MovementState.Attacker);
        var target = CreateAgent("Hitbox Target", TeamId.Red, MovementState.Neutral);
        var captureSystem = attacker.GameObject.AddComponent<LocalCaptureSystem>();

        attacker.GameObject.AddComponent<TackleHitbox>();
        attacker.GameObject.transform.position = Vector3.zero;
        target.GameObject.transform.position = Vector3.forward;
        captureSystem.Configure(attacker.Agent, new[] { attacker.Agent });

        try
        {
            Assert.IsTrue(captureSystem.TryTackle(attacker.Agent));
            Assert.AreEqual(CaptureStatus.Holding, attacker.Agent.Status);
            Assert.AreEqual(CaptureStatus.Held, target.Agent.Status);
        }
        finally
        {
            Object.DestroyImmediate(captureSystem);
            Object.DestroyImmediate(attacker.GameObject);
            Object.DestroyImmediate(target.GameObject);
        }
    }

    [Test]
    public void TackleHitboxStillRejectsTargetsOutsideForwardCone()
    {
        var attacker = CreateAgent("Hitbox Cone Attacker", TeamId.Blue, MovementState.Attacker);
        var target = CreateAgent("Hitbox Behind Target", TeamId.Red, MovementState.Neutral);
        var captureSystem = attacker.GameObject.AddComponent<LocalCaptureSystem>();

        attacker.GameObject.AddComponent<TackleHitbox>();
        attacker.GameObject.transform.position = Vector3.zero;
        target.GameObject.transform.position = Vector3.back;
        captureSystem.Configure(attacker.Agent, new[] { attacker.Agent });

        try
        {
            Assert.IsFalse(captureSystem.TryTackle(attacker.Agent));
            Assert.AreEqual(CaptureStatus.Free, attacker.Agent.Status);
            Assert.AreEqual(CaptureStatus.Free, target.Agent.Status);
            Assert.AreEqual(70f, attacker.GameObject.GetComponent<PlayerMotor>().currentStamina);
            Assert.IsTrue(attacker.Agent.IsInteractionStunned);
        }
        finally
        {
            Object.DestroyImmediate(captureSystem);
            Object.DestroyImmediate(attacker.GameObject);
            Object.DestroyImmediate(target.GameObject);
        }
    }

    [Test]
    public void TackleHitboxMissDoesNotFallBackToConfiguredAgentList()
    {
        var attacker = CreateAgent("Hitbox No Fallback Attacker", TeamId.Blue, MovementState.Attacker);
        var target = CreateAgent("Hitbox No Collider Target", TeamId.Red, MovementState.Neutral);
        var captureSystem = attacker.GameObject.AddComponent<LocalCaptureSystem>();

        attacker.GameObject.AddComponent<TackleHitbox>();
        target.GameObject.GetComponent<CharacterController>().enabled = false;
        attacker.GameObject.transform.position = Vector3.zero;
        target.GameObject.transform.position = Vector3.forward;
        captureSystem.Configure(attacker.Agent, new[] { attacker.Agent, target.Agent });

        try
        {
            Assert.IsFalse(captureSystem.TryTackle(attacker.Agent));
            Assert.AreEqual(CaptureStatus.Free, attacker.Agent.Status);
            Assert.AreEqual(CaptureStatus.Free, target.Agent.Status);
            Assert.AreEqual(70f, attacker.GameObject.GetComponent<PlayerMotor>().currentStamina);
            Assert.IsTrue(attacker.Agent.IsInteractionStunned);
        }
        finally
        {
            Object.DestroyImmediate(captureSystem);
            Object.DestroyImmediate(attacker.GameObject);
            Object.DestroyImmediate(target.GameObject);
        }
    }

    [Test]
    public void TackleMissCooldownBlocksFreshRetackleAfterHalfSecondBoundary()
    {
        var attacker = CreateAgent("Boundary Attacker", TeamId.Blue, MovementState.Attacker);
        var target = CreateAgent("Boundary Target", TeamId.Red, MovementState.Neutral);
        var missSystemObject = new GameObject("Miss Capture System");
        var freshSystemObject = new GameObject("Fresh Capture System");
        var missSystem = missSystemObject.AddComponent<LocalCaptureSystem>();
        var freshSystem = freshSystemObject.AddComponent<LocalCaptureSystem>();

        attacker.GameObject.transform.position = Vector3.zero;
        target.GameObject.transform.position = Vector3.forward * (CaptureInteractionRules.TackleRange + 1f);
        missSystem.Configure(attacker.Agent, new[] { attacker.Agent, target.Agent });
        freshSystem.Configure(attacker.Agent, new[] { attacker.Agent, target.Agent });

        try
        {
            Assert.IsFalse(missSystem.TryTackle(attacker.Agent));

            target.GameObject.transform.position = Vector3.forward;
            AdvanceTimedState(attacker.StateController, CaptureInteractionRules.TackleMissStunSeconds - 0.001f);
            freshSystem.Tick(CaptureInteractionRules.TackleMissStunSeconds - 0.001f);
            Assert.IsFalse(
                freshSystem.TryTackle(attacker.Agent),
                "A miss-stunned attacker must not retackle just before the 0.5s boundary."
            );
            Assert.AreEqual(
                70f,
                attacker.GameObject.GetComponent<PlayerMotor>().currentStamina,
                "A stun-blocked retackle must not spend another tackle cost."
            );

            AdvanceTimedState(attacker.StateController, 0.002f);
            freshSystem.Tick(0.002f);
            Assert.IsFalse(
                freshSystem.TryTackle(attacker.Agent),
                "A fresh local system must still honor the attacker's 2s tackle cooldown after miss stun expires."
            );
            Assert.AreEqual(
                70f,
                attacker.GameObject.GetComponent<PlayerMotor>().currentStamina,
                "A cooldown-blocked retackle must not spend another tackle cost."
            );

            freshSystem.Tick(CaptureInteractionRules.TackleCooldownSeconds - CaptureInteractionRules.TackleMissStunSeconds);
            Assert.IsTrue(freshSystem.TryTackle(attacker.Agent));
            Assert.AreEqual(40f, attacker.GameObject.GetComponent<PlayerMotor>().currentStamina);
            Assert.AreEqual(CaptureStatus.Holding, attacker.Agent.Status);
            Assert.AreEqual(CaptureStatus.Held, target.Agent.Status);
        }
        finally
        {
            Object.DestroyImmediate(missSystemObject);
            Object.DestroyImmediate(freshSystemObject);
            Object.DestroyImmediate(attacker.GameObject);
            Object.DestroyImmediate(target.GameObject);
        }
    }

    [Test]
    public void DuplicateLocalCaptureSystemsDoNotAccelerateTackleCooldown()
    {
        var attacker = CreateAgent("Duplicate System Attacker", TeamId.Blue, MovementState.Attacker);
        var target = CreateAgent("Duplicate System Target", TeamId.Red, MovementState.Neutral);
        var primarySystemObject = new GameObject("Primary Capture System");
        var duplicateSystemObject = new GameObject("Duplicate Capture System");
        var primarySystem = primarySystemObject.AddComponent<LocalCaptureSystem>();
        var duplicateSystem = duplicateSystemObject.AddComponent<LocalCaptureSystem>();

        attacker.GameObject.transform.position = Vector3.zero;
        target.GameObject.transform.position = Vector3.forward * (CaptureInteractionRules.TackleRange + 1f);
        primarySystem.Configure(attacker.Agent, new[] { attacker.Agent, target.Agent });
        duplicateSystem.Configure(attacker.Agent, new[] { attacker.Agent, target.Agent });

        try
        {
            Assert.IsFalse(primarySystem.TryTackle(attacker.Agent));

            target.GameObject.transform.position = Vector3.forward;
            AdvanceTimedState(attacker.StateController, CaptureInteractionRules.TackleMissStunSeconds);
            primarySystem.Tick(CaptureInteractionRules.TackleMissStunSeconds);
            duplicateSystem.Tick(CaptureInteractionRules.TackleMissStunSeconds);
            primarySystem.Tick(1f);
            duplicateSystem.Tick(1f);

            Assert.IsFalse(
                duplicateSystem.TryTackle(attacker.Agent),
                "Duplicate local systems must not tick the same attacker's tackle cooldown twice."
            );
            Assert.AreEqual(
                70f,
                attacker.GameObject.GetComponent<PlayerMotor>().currentStamina,
                "A duplicate-system cooldown block must not spend another tackle cost."
            );

            primarySystem.Tick(CaptureInteractionRules.TackleCooldownSeconds - CaptureInteractionRules.TackleMissStunSeconds - 1f);
            Assert.IsTrue(primarySystem.TryTackle(attacker.Agent));
            Assert.AreEqual(40f, attacker.GameObject.GetComponent<PlayerMotor>().currentStamina);
        }
        finally
        {
            Object.DestroyImmediate(primarySystemObject);
            Object.DestroyImmediate(duplicateSystemObject);
            Object.DestroyImmediate(attacker.GameObject);
            Object.DestroyImmediate(target.GameObject);
        }
    }

    [Test]
    public void PlayerCaptureAgentRequiresMotorForStaminaContract()
    {
        var gameObject = new GameObject("Required Motor Agent");

        try
        {
            gameObject.AddComponent<LocalPlayerTeam>();
            gameObject.AddComponent<PlayerCaptureAgent>();

            Assert.IsNotNull(gameObject.GetComponent<PlayerMotor>());
        }
        finally
        {
            Object.DestroyImmediate(gameObject);
        }
    }

    [Test]
    public void TackleMissesSameTeamHolderAndAppliesAttemptPenalty()
    {
        var attacker = CreateAgent("Friendly Attacker", TeamId.Blue, MovementState.Attacker);
        var friendlyHolder = CreateAgent("Friendly Holder", TeamId.Blue, MovementState.Attacker);
        var heldEnemy = CreateAgent("Held Enemy", TeamId.Red, MovementState.Neutral);
        var systemObject = new GameObject("Capture System");
        var captureSystem = systemObject.AddComponent<LocalCaptureSystem>();

        attacker.GameObject.transform.position = Vector3.zero;
        friendlyHolder.GameObject.transform.position = Vector3.forward;
        heldEnemy.GameObject.transform.position = Vector3.forward * 1.25f;
        captureSystem.Configure(attacker.Agent, new[] { attacker.Agent, friendlyHolder.Agent, heldEnemy.Agent });

        try
        {
            Assert.IsTrue(friendlyHolder.Agent.TryHold(heldEnemy.Agent));
            Assert.IsFalse(captureSystem.TryTackle(attacker.Agent));

            Assert.AreEqual(CaptureStatus.Free, attacker.Agent.Status);
            Assert.AreEqual(CaptureStatus.Holding, friendlyHolder.Agent.Status);
            Assert.AreEqual(CaptureStatus.Held, heldEnemy.Agent.Status);
            Assert.AreEqual(friendlyHolder.Agent, heldEnemy.Agent.HeldBy);
            Assert.AreEqual(heldEnemy.Agent, friendlyHolder.Agent.HeldTarget);
            Assert.AreEqual(70f, attacker.GameObject.GetComponent<PlayerMotor>().currentStamina);
            Assert.AreEqual(MovementState.Holding, attacker.StateController.CurrentState);
            Assert.IsTrue(attacker.Agent.IsInteractionStunned);
            Assert.IsFalse(attacker.Agent.IsReleaseStunned);
        }
        finally
        {
            Object.DestroyImmediate(systemObject);
            Object.DestroyImmediate(attacker.GameObject);
            Object.DestroyImmediate(friendlyHolder.GameObject);
            Object.DestroyImmediate(heldEnemy.GameObject);
        }
    }

    [Test]
    public void InterruptedHolderCannotRetackleUntilReleaseStunExpires()
    {
        var attacker = CreateAgent("Interrupting Attacker", TeamId.Blue, MovementState.Attacker);
        var enemyHolder = CreateAgent("Interrupted Holder", TeamId.Red, MovementState.Attacker);
        var heldTarget = CreateAgent("Held Ally", TeamId.Blue, MovementState.Neutral);
        var nextTarget = CreateAgent("Next Target", TeamId.Blue, MovementState.Neutral);
        var attackerSystemObject = new GameObject("Attacker Capture System");
        var attackerSystem = attackerSystemObject.AddComponent<LocalCaptureSystem>();
        var holderSystem = enemyHolder.GameObject.AddComponent<LocalCaptureSystem>();

        attacker.GameObject.transform.position = Vector3.zero;
        enemyHolder.GameObject.transform.position = Vector3.forward;
        heldTarget.GameObject.transform.position = Vector3.forward * 1.25f;
        nextTarget.GameObject.transform.position = enemyHolder.GameObject.transform.position + Vector3.forward;
        attackerSystem.Configure(attacker.Agent, new[] { attacker.Agent, enemyHolder.Agent, heldTarget.Agent });
        holderSystem.Configure(enemyHolder.Agent, new[] { nextTarget.Agent });

        try
        {
            Assert.IsTrue(enemyHolder.Agent.TryHold(heldTarget.Agent));
            Assert.IsTrue(attackerSystem.TryTackle(attacker.Agent));

            AdvanceTimedState(enemyHolder.StateController, CaptureInteractionRules.HolderReleaseStunSeconds - 0.001f);
            Assert.IsFalse(
                holderSystem.TryTackle(enemyHolder.Agent),
                "An interrupted holder must not retackle during the 1s release stun window."
            );
            Assert.AreEqual(CaptureStatus.Free, enemyHolder.Agent.Status);
            Assert.AreEqual(CaptureStatus.Free, nextTarget.Agent.Status);

            AdvanceTimedState(enemyHolder.StateController, 0.002f);
            Assert.IsTrue(
                holderSystem.TryTackle(enemyHolder.Agent),
                "An interrupted holder should regain tackle authority after the 1s release stun expires."
            );
            Assert.AreEqual(CaptureStatus.Holding, enemyHolder.Agent.Status);
            Assert.AreEqual(CaptureStatus.Held, nextTarget.Agent.Status);
        }
        finally
        {
            Object.DestroyImmediate(attackerSystemObject);
            Object.DestroyImmediate(holderSystem);
            Object.DestroyImmediate(attacker.GameObject);
            Object.DestroyImmediate(enemyHolder.GameObject);
            Object.DestroyImmediate(heldTarget.GameObject);
            Object.DestroyImmediate(nextTarget.GameObject);
        }
    }

    [Test]
    public void TryHoldRejectsCaptorOrTargetWithoutTeam()
    {
        var noTeamHolder = CreateAgent("No Team Holder", TeamId.None, MovementState.Attacker);
        var redTarget = CreateAgent("Red Target", TeamId.Red, MovementState.Neutral);
        var blueHolder = CreateAgent("Blue Holder", TeamId.Blue, MovementState.Attacker);
        var noTeamTarget = CreateAgent("No Team Target", TeamId.None, MovementState.Neutral);

        try
        {
            Assert.IsFalse(noTeamHolder.Agent.TryHold(redTarget.Agent));
            Assert.IsFalse(blueHolder.Agent.TryHold(noTeamTarget.Agent));
            Assert.AreEqual(CaptureStatus.Free, noTeamHolder.Agent.Status);
            Assert.AreEqual(CaptureStatus.Free, redTarget.Agent.Status);
            Assert.AreEqual(CaptureStatus.Free, blueHolder.Agent.Status);
            Assert.AreEqual(CaptureStatus.Free, noTeamTarget.Agent.Status);
        }
        finally
        {
            Object.DestroyImmediate(noTeamHolder.GameObject);
            Object.DestroyImmediate(redTarget.GameObject);
            Object.DestroyImmediate(blueHolder.GameObject);
            Object.DestroyImmediate(noTeamTarget.GameObject);
        }
    }

    [Test]
    public void CompleteCaptureRejectsCaptorOrTargetWithoutTeam()
    {
        var king = CreateAgent("No Team Guard King", TeamId.Blue, MovementState.King);
        var holder = CreateAgent("No Team Guard Holder", TeamId.Blue, MovementState.Attacker);
        var target = CreateAgent("No Team Guard Target", TeamId.Red, MovementState.Neutral);

        try
        {
            Assert.IsTrue(holder.Agent.TryHold(target.Agent));

            king.Agent.Team.Configure(TeamId.None);
            Assert.IsFalse(king.Agent.CompleteCapture(target.Agent));
            Assert.AreEqual(CaptureStatus.Holding, holder.Agent.Status);
            Assert.AreEqual(CaptureStatus.Held, target.Agent.Status);

            king.Agent.Team.Configure(TeamId.Blue);
            target.Agent.Team.Configure(TeamId.None);
            Assert.IsFalse(king.Agent.CompleteCapture(target.Agent));
            Assert.AreEqual(CaptureStatus.Holding, holder.Agent.Status);
            Assert.AreEqual(CaptureStatus.Held, target.Agent.Status);
        }
        finally
        {
            Object.DestroyImmediate(king.GameObject);
            Object.DestroyImmediate(holder.GameObject);
            Object.DestroyImmediate(target.GameObject);
        }
    }

    [Test]
    public void FinalCaptureTargetWithoutTeamDoesNotBuildHoldProgress()
    {
        var king = CreateAgent("No Team Progress King", TeamId.Blue, MovementState.King);
        var holder = CreateAgent("No Team Progress Holder", TeamId.Blue, MovementState.Attacker);
        var target = CreateAgent("No Team Progress Target", TeamId.Red, MovementState.Neutral);
        var captureSystem = king.GameObject.AddComponent<LocalCaptureSystem>();

        king.GameObject.transform.position = Vector3.zero;
        holder.GameObject.transform.position = Vector3.forward * 0.5f;
        target.GameObject.transform.position = Vector3.forward;
        captureSystem.Configure(king.Agent, new[] { king.Agent, holder.Agent, target.Agent });

        try
        {
            Assert.IsTrue(holder.Agent.TryHold(target.Agent));

            target.Agent.Team.Configure(TeamId.None);
            Assert.IsFalse(captureSystem.TickFinalCapture(king.Agent, CaptureInteractionRules.CaptureHoldSeconds * 0.5f));
            Assert.AreEqual(0f, captureSystem.CaptureHoldProgress01);

            target.Agent.Team.Configure(TeamId.Red);
            king.Agent.Team.Configure(TeamId.None);
            Assert.IsFalse(captureSystem.TickFinalCapture(king.Agent, CaptureInteractionRules.CaptureHoldSeconds * 0.5f));
            Assert.AreEqual(0f, captureSystem.CaptureHoldProgress01);
        }
        finally
        {
            Object.DestroyImmediate(captureSystem);
            Object.DestroyImmediate(king.GameObject);
            Object.DestroyImmediate(holder.GameObject);
            Object.DestroyImmediate(target.GameObject);
        }
    }

    [Test]
    public void OwnHeldTargetDoesNotBuildFinalCaptureProgress()
    {
        var king = CreateAgent("Own Hold Progress King", TeamId.Blue, MovementState.King);
        var target = CreateAgent("Own Hold Progress Target", TeamId.Red, MovementState.Neutral);
        var captureSystem = king.GameObject.AddComponent<LocalCaptureSystem>();

        king.GameObject.transform.position = Vector3.zero;
        target.GameObject.transform.position = Vector3.forward;
        captureSystem.Configure(king.Agent, new[] { king.Agent, target.Agent });

        try
        {
            Assert.IsTrue(king.Agent.TryHold(target.Agent));
            Assert.IsFalse(captureSystem.TickFinalCapture(king.Agent, CaptureInteractionRules.CaptureHoldSeconds));
            Assert.AreEqual(0f, captureSystem.CaptureHoldProgress01);
            Assert.AreEqual(CaptureStatus.Holding, king.Agent.Status);
            Assert.AreEqual(CaptureStatus.Held, target.Agent.Status);
        }
        finally
        {
            Object.DestroyImmediate(captureSystem);
            Object.DestroyImmediate(king.GameObject);
            Object.DestroyImmediate(target.GameObject);
        }
    }

    [Test]
    public void AttackerCannotCompleteFinalCapture()
    {
        var attacker = CreateAgent("Attacker", TeamId.Blue, MovementState.Attacker);
        var target = CreateAgent("Held Target", TeamId.Red, MovementState.Neutral);

        try
        {
            Assert.IsTrue(attacker.Agent.TryHold(target.Agent));
            Assert.IsFalse(attacker.Agent.CompleteCapture(target.Agent));
            Assert.AreEqual(CaptureStatus.Held, target.Agent.Status);
        }
        finally
        {
            Object.DestroyImmediate(attacker.GameObject);
            Object.DestroyImmediate(target.GameObject);
        }
    }

    [Test]
    public void HeldTargetConsumesOnlyOneSlimeEscapePerLocalMatch()
    {
        var holder = CreateAgent("Holder", TeamId.Blue, MovementState.Attacker);
        var target = CreateAgent("One Time Slime Target", TeamId.Red, MovementState.Neutral);

        try
        {
            Assert.IsTrue(holder.Agent.TryHold(target.Agent));

            PressSlime(target.StateController);

            AssertReleasedBySlimeEscape(holder, target);
            Assert.AreEqual(
                MovementState.Slime,
                target.StateController.CurrentState,
                "The first held-slime use should still enter Slime after breaking the hold."
            );

            AdvanceTimedState(target.StateController, SlimeDurationSeconds);
            AdvanceTimedState(holder.StateController, CaptureInteractionRules.HolderReleaseStunSeconds + 0.01f);
            Assert.AreEqual(MovementState.Neutral, target.StateController.CurrentState);

            Assert.IsTrue(holder.Agent.TryHold(target.Agent));

            PressSlime(target.StateController);

            Assert.AreEqual(
                CaptureStatus.Holding,
                holder.Agent.Status,
                "A target must not get a second held-slime escape in the same local match."
            );
            Assert.AreEqual(CaptureStatus.Held, target.Agent.Status);
            Assert.AreEqual(holder.Agent, target.Agent.HeldBy);
            Assert.AreEqual(target.Agent, holder.Agent.HeldTarget);
            Assert.AreEqual(
                MovementState.Held,
                target.StateController.CurrentState,
                "After the escape charge is consumed, slime input while held must not override the held movement lock."
            );
        }
        finally
        {
            Object.DestroyImmediate(holder.GameObject);
            Object.DestroyImmediate(target.GameObject);
        }
    }

    [Test]
    public void HolderStaysStunnedForExactlyOneSecondAfterSlimeEscape()
    {
        var holder = CreateAgent("Stunned Holder", TeamId.Blue, MovementState.Attacker);
        var escapedTarget = CreateAgent("Escaping Target", TeamId.Red, MovementState.Neutral);
        var nextTarget = CreateAgent("Next Target", TeamId.Red, MovementState.Neutral);
        var captureSystem = holder.GameObject.AddComponent<LocalCaptureSystem>();

        holder.GameObject.transform.position = Vector3.zero;
        escapedTarget.GameObject.transform.position = Vector3.forward;
        nextTarget.GameObject.transform.position = Vector3.forward;
        captureSystem.Configure(holder.Agent, new[] { escapedTarget.Agent, nextTarget.Agent });

        try
        {
            Assert.IsTrue(holder.Agent.TryHold(escapedTarget.Agent));

            PressSlime(escapedTarget.StateController);
            AssertReleasedBySlimeEscape(holder, escapedTarget);

            escapedTarget.GameObject.transform.position = Vector3.right * 10f;

            AdvanceTimedState(holder.StateController, CaptureInteractionRules.HolderReleaseStunSeconds - 0.001f);
            Assert.IsFalse(
                captureSystem.TryTackle(holder.Agent),
                "The holder must still be stunned just before the 1.0s boundary after a slime escape."
            );
            Assert.AreEqual(CaptureStatus.Free, holder.Agent.Status);
            Assert.AreEqual(CaptureStatus.Free, nextTarget.Agent.Status);

            AdvanceTimedState(holder.StateController, 0.002f);
            Assert.IsTrue(
                captureSystem.TryTackle(holder.Agent),
                "The holder should regain capture authority once the 1.0s stun expires."
            );
            Assert.AreEqual(CaptureStatus.Holding, holder.Agent.Status);
            Assert.AreEqual(CaptureStatus.Held, nextTarget.Agent.Status);
        }
        finally
        {
            Object.DestroyImmediate(captureSystem);
            Object.DestroyImmediate(holder.GameObject);
            Object.DestroyImmediate(escapedTarget.GameObject);
            Object.DestroyImmediate(nextTarget.GameObject);
        }
    }

    [Test]
    public void HolderReleaseStunSurvivesPersistentStateRefreshBeforeOneSecond()
    {
        var holder = CreateAgent("State Refreshed Holder", TeamId.Blue, MovementState.Attacker);
        var escapedTarget = CreateAgent("Escaping Target", TeamId.Red, MovementState.Neutral);
        var nextTarget = CreateAgent("Next Target", TeamId.Red, MovementState.Neutral);
        var captureSystem = holder.GameObject.AddComponent<LocalCaptureSystem>();

        holder.GameObject.transform.position = Vector3.zero;
        escapedTarget.GameObject.transform.position = Vector3.forward;
        nextTarget.GameObject.transform.position = Vector3.forward;
        captureSystem.Configure(holder.Agent, new[] { escapedTarget.Agent, nextTarget.Agent });

        try
        {
            Assert.IsTrue(holder.Agent.TryHold(escapedTarget.Agent));

            PressSlime(escapedTarget.StateController);
            AssertReleasedBySlimeEscape(holder, escapedTarget);
            Assert.IsTrue(holder.Agent.IsReleaseStunned);

            escapedTarget.GameObject.transform.position = Vector3.right * 10f;
            holder.StateController.SetPersistentState(MovementState.Attacker);

            Assert.IsTrue(
                holder.Agent.IsReleaseStunned,
                "Refreshing the holder's persistent match state before 1s must not cancel the mandated release stun."
            );
            Assert.IsFalse(
                captureSystem.TryTackle(holder.Agent),
                "A match-state refresh must not let the holder immediately retackle during the 1s stun window."
            );

            AdvanceTimedState(holder.StateController, CaptureInteractionRules.HolderReleaseStunSeconds);
            Assert.IsTrue(captureSystem.TryTackle(holder.Agent));
        }
        finally
        {
            Object.DestroyImmediate(captureSystem);
            Object.DestroyImmediate(holder.GameObject);
            Object.DestroyImmediate(escapedTarget.GameObject);
            Object.DestroyImmediate(nextTarget.GameObject);
        }
    }

    private static AgentFixture CreateAgent(string name, TeamId team, MovementState state)
    {
        var gameObject = new GameObject(name);
        gameObject.AddComponent<CharacterController>();
        gameObject.AddComponent<PlayerInputReader>();
        gameObject.AddComponent<PlayerMotor>();
        var teamComponent = gameObject.AddComponent<LocalPlayerTeam>();
        teamComponent.Configure(team);
        var stateController = gameObject.AddComponent<PlayerStateController>();
        var agent = gameObject.AddComponent<PlayerCaptureAgent>();
        stateController.SetPersistentState(state);
        agent.Configure(stateController);
        return new AgentFixture(gameObject, stateController, agent);
    }

    private static void AssertReleasedBySlimeEscape(AgentFixture holder, AgentFixture target)
    {
        Assert.AreEqual(
            CaptureStatus.Free,
            holder.Agent.Status,
            "A held target's first slime use must immediately release the holder."
        );
        Assert.AreEqual(
            CaptureStatus.Free,
            target.Agent.Status,
            "A held target's first slime use must immediately release the target."
        );
        Assert.IsNull(holder.Agent.HeldTarget);
        Assert.IsNull(target.Agent.HeldBy);
        Assert.IsTrue(target.Agent.HasUsedSlimeEscape);
        Assert.AreEqual(MovementState.Attacker, holder.StateController.PersistentState);
        Assert.AreEqual(MovementState.Neutral, target.StateController.PersistentState);
    }

    private static void PressSlime(PlayerStateController stateController)
    {
        var method = stateController.GetType().GetMethod("HandleSlimePressed", PrivateInstance);
        Assert.IsNotNull(
            method,
            "PlayerStateController.HandleSlimePressed should stay deterministic for held-slime capture tests."
        );
        method.Invoke(stateController, null);
    }

    private static void AdvanceTimedState(PlayerStateController stateController, float seconds)
    {
        var method = stateController.GetType().GetMethod("TickTimedState", PrivateInstance);
        Assert.IsNotNull(
            method,
            "PlayerStateController.TickTimedState should remain available for deterministic EditMode timing tests."
        );
        method.Invoke(stateController, new object[] { seconds });
    }

    private readonly struct AgentFixture
    {
        public AgentFixture(GameObject gameObject, PlayerStateController stateController, PlayerCaptureAgent agent)
        {
            GameObject = gameObject;
            StateController = stateController;
            Agent = agent;
        }

        public GameObject GameObject { get; }
        public PlayerStateController StateController { get; }
        public PlayerCaptureAgent Agent { get; }
    }
}

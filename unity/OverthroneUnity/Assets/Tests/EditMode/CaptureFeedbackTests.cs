using System.Collections.Generic;
using NUnit.Framework;
using Overthrone;
using UnityEngine;

public sealed class CaptureFeedbackTests
{
    [Test]
    public void TackleHitAndMissEmitCaptureFeedback()
    {
        var attacker = CreateAgent("Feedback Attacker", TeamId.Blue, MovementState.Attacker);
        var target = CreateAgent("Feedback Target", TeamId.Red, MovementState.Neutral);
        var captureSystem = attacker.GameObject.AddComponent<LocalCaptureSystem>();
        var events = new List<CaptureFeedbackEvent>();

        attacker.GameObject.transform.position = Vector3.zero;
        target.GameObject.transform.position = Vector3.forward;
        captureSystem.Configure(attacker.Agent, new[] { attacker.Agent, target.Agent });

        try
        {
            CaptureFeedbackSystem.FeedbackEmitted += events.Add;

            Assert.IsTrue(captureSystem.TryTackle(attacker.Agent));
            Assert.AreEqual(CaptureFeedbackType.TackleHit, events[0].Type);
            Assert.AreEqual(attacker.GameObject, events[0].Source);
            Assert.AreEqual(target.GameObject, events[0].Target);

            attacker.Agent.ReleaseHold();
            AdvanceTimedState(attacker.StateController, CaptureInteractionRules.TackleCooldownSeconds);
            captureSystem.Tick(CaptureInteractionRules.TackleCooldownSeconds);
            target.GameObject.transform.position = Vector3.forward * (CaptureInteractionRules.TackleRange + 2f);

            Assert.IsFalse(captureSystem.TryTackle(attacker.Agent));
            Assert.AreEqual(CaptureFeedbackType.TackleMiss, events[1].Type);
            Assert.AreEqual(attacker.GameObject, events[1].Source);
            Assert.IsNull(events[1].Target);
        }
        finally
        {
            CaptureFeedbackSystem.FeedbackEmitted -= events.Add;
            Object.DestroyImmediate(captureSystem);
            Object.DestroyImmediate(attacker.GameObject);
            Object.DestroyImmediate(target.GameObject);
        }
    }

    [Test]
    public void RescueFinalCaptureAndSlimeEscapeEmitSpecificFeedback()
    {
        var king = CreateAgent("Feedback King", TeamId.Blue, MovementState.King);
        var target = CreateAgent("Feedback Target", TeamId.Red, MovementState.Neutral);
        var rescuer = CreateAgent("Feedback Rescuer", TeamId.Red, MovementState.Neutral);
        var events = new List<CaptureFeedbackEvent>();
        var rescueSystemObject = new GameObject("Feedback Rescue System");
        var rescueSystem = rescueSystemObject.AddComponent<LocalCaptureSystem>();

        king.GameObject.transform.position = Vector3.zero;
        target.GameObject.transform.position = Vector3.forward;
        rescuer.GameObject.transform.position = Vector3.forward * 1.2f;
        rescueSystem.Configure(rescuer.Agent, new[] { king.Agent, target.Agent, rescuer.Agent });

        try
        {
            CaptureFeedbackSystem.FeedbackEmitted += events.Add;

            Assert.IsTrue(king.Agent.TryHold(target.Agent));
            Assert.IsTrue(rescueSystem.TryRescueNearby(rescuer.Agent));
            Assert.AreEqual(CaptureFeedbackType.Rescue, events[0].Type);
            Assert.AreEqual(rescuer.GameObject, events[0].Source);
            Assert.AreEqual(target.GameObject, events[0].Target);

            AdvanceTimedState(king.StateController, CaptureInteractionRules.HolderReleaseStunSeconds);
            Assert.IsTrue(king.Agent.TryHold(target.Agent));
            Assert.IsTrue(target.Agent.TrySlimeEscape());
            Assert.AreEqual(CaptureFeedbackType.SlimeEscape, events[1].Type);
            Assert.AreEqual(target.GameObject, events[1].Source);
            Assert.AreEqual(king.GameObject, events[1].Target);

            AdvanceTimedState(king.StateController, CaptureInteractionRules.HolderReleaseStunSeconds);
            Assert.IsTrue(king.Agent.TryHold(target.Agent));
            var captureSystem = king.GameObject.AddComponent<LocalCaptureSystem>();
            captureSystem.Configure(king.Agent, new[] { king.Agent, target.Agent });
            Assert.IsTrue(captureSystem.TickFinalCapture(king.Agent, CaptureInteractionRules.CaptureHoldSeconds));
            Assert.AreEqual(CaptureFeedbackType.FinalCapture, events[2].Type);
            Assert.AreEqual(king.GameObject, events[2].Source);
            Assert.AreEqual(target.GameObject, events[2].Target);
            Object.DestroyImmediate(captureSystem);
        }
        finally
        {
            CaptureFeedbackSystem.FeedbackEmitted -= events.Add;
            Object.DestroyImmediate(rescueSystemObject);
            Object.DestroyImmediate(king.GameObject);
            Object.DestroyImmediate(target.GameObject);
            Object.DestroyImmediate(rescuer.GameObject);
        }
    }

    [Test]
    public void CaptureFeedbackControllerCreatesParticleEffectForEvent()
    {
        var feedbackObject = new GameObject("Feedback Controller");
        var controller = feedbackObject.AddComponent<CaptureFeedbackController>();

        try
        {
            var feedback = new CaptureFeedbackEvent(
                CaptureFeedbackType.FinalCapture,
                feedbackObject,
                null,
                new Vector3(1f, 2f, 3f)
            );

            controller.PlayFeedback(feedback);

            Assert.AreEqual(1, controller.PlayedFeedbackCount);
            Assert.AreEqual(CaptureFeedbackType.FinalCapture, controller.LastFeedback.Type);
            Assert.IsNotNull(controller.LastEffect);
            Assert.IsNotNull(controller.LastEffect.GetComponent<ParticleSystem>());
            Assert.AreEqual(feedback.Position, controller.LastEffect.transform.position);
        }
        finally
        {
            if (controller.LastEffect != null)
            {
                Object.DestroyImmediate(controller.LastEffect);
            }

            Object.DestroyImmediate(feedbackObject);
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

    private static void AdvanceTimedState(PlayerStateController stateController, float seconds)
    {
        var method = stateController.GetType().GetMethod(
            "TickTimedState",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic
        );
        Assert.IsNotNull(method);
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

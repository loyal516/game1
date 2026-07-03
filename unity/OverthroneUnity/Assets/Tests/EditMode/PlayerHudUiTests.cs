using System.Reflection;
using NUnit.Framework;
using Overthrone;
using UnityEngine;
using UnityEngine.UI;

public sealed class PlayerHudUiTests
{
    [Test]
    public void PlayerHudShowsTeamStatusRailFromMatchParticipants()
    {
        var blue = CreateAgent("Blue King", TeamId.Blue, MovementState.King);
        var red = CreateAgent("Red Held", TeamId.Red, MovementState.Neutral);
        var managerObject = new GameObject("Hud Match Manager");
        var hudObject = new GameObject("Hud");
        var statusObject = new GameObject("Status Text", typeof(RectTransform), typeof(Text));
        var railObject = new GameObject("Team Rail Text", typeof(RectTransform), typeof(Text));

        try
        {
            Assert.IsTrue(blue.Agent.TryHold(red.Agent));

            var manager = managerObject.AddComponent<LocalMatchManager>();
            manager.Configure(
                System.Array.Empty<CapturePoint>(),
                new[] { blue.Team, red.Team }
            );

            var railText = railObject.GetComponent<Text>();
            var hud = hudObject.AddComponent<PlayerHud>();
            hud.Configure(
                blue.Motor,
                null,
                statusObject.GetComponent<Text>(),
                localMatchManager: manager,
                teamStatusRailText: railText
            );

            hud.Refresh();

            Assert.IsTrue(railText.gameObject.activeSelf);
            StringAssert.Contains("Team Status", railText.text);
            StringAssert.Contains("Blue", railText.text);
            StringAssert.Contains("Blue King", railText.text);
            StringAssert.Contains("King", railText.text);
            StringAssert.Contains("Red", railText.text);
            StringAssert.Contains("Red Held", railText.text);
            StringAssert.Contains("Held", railText.text);
        }
        finally
        {
            Object.DestroyImmediate(railObject);
            Object.DestroyImmediate(statusObject);
            Object.DestroyImmediate(hudObject);
            Object.DestroyImmediate(managerObject);
            Object.DestroyImmediate(blue.GameObject);
            Object.DestroyImmediate(red.GameObject);
        }
    }

    [Test]
    public void PlayerHudShowsTeamStatusIconsForActiveParticipantStatesAndHidesStaleSlots()
    {
        var king = CreateAgent("Icon Blue King", TeamId.Blue, MovementState.King);
        var holder = CreateAgent("Icon Red Holder", TeamId.Red, MovementState.King);
        var held = CreateAgent("Icon Blue Held", TeamId.Blue, MovementState.Neutral);
        var captureHelper = CreateAgent("Icon Blue Capture Helper", TeamId.Blue, MovementState.Attacker);
        var captured = CreateAgent("Icon Red Captured", TeamId.Red, MovementState.Neutral);
        var managerObject = new GameObject("Icon Match Manager");
        var hudObject = new GameObject("Icon Hud");
        var statusObject = new GameObject("Icon Status Text", typeof(RectTransform), typeof(Text));
        var iconObjects = new[]
        {
            new GameObject("King Status Icon", typeof(RectTransform), typeof(Image)),
            new GameObject("Holding Status Icon", typeof(RectTransform), typeof(Image)),
            new GameObject("Held Status Icon", typeof(RectTransform), typeof(Image)),
            new GameObject("Captured Status Icon", typeof(RectTransform), typeof(Image)),
            new GameObject("Stale Status Icon", typeof(RectTransform), typeof(Image))
        };

        try
        {
            Assert.IsTrue(holder.Agent.TryHold(held.Agent));
            Assert.IsTrue(captureHelper.Agent.TryHold(captured.Agent));
            Assert.IsTrue(king.Agent.CompleteCapture(captured.Agent));

            var manager = managerObject.AddComponent<LocalMatchManager>();
            manager.Configure(
                System.Array.Empty<CapturePoint>(),
                new[] { king.Team, holder.Team, held.Team, captured.Team }
            );

            var icons = new Image[iconObjects.Length];
            for (var i = 0; i < iconObjects.Length; i++)
            {
                icons[i] = iconObjects[i].GetComponent<Image>();
                icons[i].gameObject.SetActive(true);
                icons[i].color = Color.magenta;
            }

            var hud = hudObject.AddComponent<PlayerHud>();
            hud.Configure(
                king.Motor,
                null,
                statusObject.GetComponent<Text>(),
                localMatchManager: manager,
                playerCaptureAgent: king.Agent,
                teamStatusIconImages: icons
            );

            hud.Refresh();

            AssertTeamStatusIconActive(icons[0], "King");
            AssertTeamStatusIconActive(icons[1], "Holding");
            AssertTeamStatusIconActive(icons[2], "Held");
            AssertTeamStatusIconActive(icons[3], "Captured");
            Assert.IsFalse(icons[4].gameObject.activeSelf);

            AssertDistinctIconRender(icons[0], icons[1], "King", "Holding");
            AssertDistinctIconRender(icons[0], icons[2], "King", "Held");
            AssertDistinctIconRender(icons[0], icons[3], "King", "Captured");
            AssertDistinctIconRender(icons[1], icons[2], "Holding", "Held");
            AssertDistinctIconRender(icons[1], icons[3], "Holding", "Captured");
            AssertDistinctIconRender(icons[2], icons[3], "Held", "Captured");
        }
        finally
        {
            foreach (var iconObject in iconObjects)
            {
                Object.DestroyImmediate(iconObject);
            }

            Object.DestroyImmediate(statusObject);
            Object.DestroyImmediate(hudObject);
            Object.DestroyImmediate(managerObject);
            Object.DestroyImmediate(captured.GameObject);
            Object.DestroyImmediate(captureHelper.GameObject);
            Object.DestroyImmediate(held.GameObject);
            Object.DestroyImmediate(holder.GameObject);
            Object.DestroyImmediate(king.GameObject);
        }
    }

    [Test]
    public void PlayerHudUpdatesCaptureProgressRingFromLocalCaptureSystem()
    {
        var king = CreateAgent("Ring King", TeamId.Blue, MovementState.King);
        var holder = CreateAgent("Ring Holder", TeamId.Blue, MovementState.Attacker);
        var target = CreateAgent("Ring Target", TeamId.Red, MovementState.Neutral);
        var systemObject = new GameObject("Ring Capture System");
        var hudObject = new GameObject("Hud");
        var statusObject = new GameObject("Status Text", typeof(RectTransform), typeof(Text));
        var ringObject = new GameObject("Capture Ring", typeof(RectTransform), typeof(Image));

        king.GameObject.transform.position = Vector3.zero;
        holder.GameObject.transform.position = Vector3.forward * 0.5f;
        target.GameObject.transform.position = Vector3.forward;

        try
        {
            var captureSystem = systemObject.AddComponent<LocalCaptureSystem>();
            captureSystem.Configure(king.Agent, new[] { king.Agent, holder.Agent, target.Agent });
            Assert.IsTrue(holder.Agent.TryHold(target.Agent));
            Assert.IsFalse(captureSystem.TickFinalCapture(king.Agent, CaptureInteractionRules.CaptureHoldSeconds * 0.5f));

            var ring = ringObject.GetComponent<Image>();
            var hud = hudObject.AddComponent<PlayerHud>();
            hud.Configure(
                king.Motor,
                null,
                statusObject.GetComponent<Text>(),
                captureHoldProgressRing: ring,
                localCaptureSystem: captureSystem
            );

            hud.Refresh();

            Assert.IsTrue(ring.gameObject.activeSelf);
            Assert.AreEqual(0.5f, ring.fillAmount, 0.0001f);
        }
        finally
        {
            Object.DestroyImmediate(ringObject);
            Object.DestroyImmediate(statusObject);
            Object.DestroyImmediate(hudObject);
            Object.DestroyImmediate(systemObject);
            Object.DestroyImmediate(king.GameObject);
            Object.DestroyImmediate(holder.GameObject);
            Object.DestroyImmediate(target.GameObject);
        }
    }

    [Test]
    public void PlayerHudUpdatesMinimapMarkersFromGddVisibilityRules()
    {
        var local = CreateAgent("Local Blue", TeamId.Blue, MovementState.Neutral);
        var ally = CreateAgent("Ally Blue", TeamId.Blue, MovementState.Neutral);
        var enemyNeutral = CreateAgent("Enemy Neutral", TeamId.Red, MovementState.Neutral);
        var enemyAttacker = CreateAgent("Enemy Attacker", TeamId.Red, MovementState.Attacker);
        var enemyKing = CreateAgent("Enemy King", TeamId.Red, MovementState.King);
        var pointObject = new GameObject("Capture Point A");
        var managerObject = new GameObject("Hud Match Manager");
        var hudObject = new GameObject("Hud");
        var statusObject = new GameObject("Status Text", typeof(RectTransform), typeof(Text));
        var minimapObject = new GameObject("Minimap", typeof(RectTransform), typeof(Image));
        var selfMarkerObject = new GameObject("Self Marker", typeof(RectTransform), typeof(Image));
        var pointMarkerObject = new GameObject("Point Marker", typeof(RectTransform), typeof(Image));
        var markerObjects = new[]
        {
            new GameObject("Local Participant Marker", typeof(RectTransform), typeof(Image)),
            new GameObject("Ally Marker", typeof(RectTransform), typeof(Image)),
            new GameObject("Enemy Neutral Marker", typeof(RectTransform), typeof(Image)),
            new GameObject("Enemy Attacker Marker", typeof(RectTransform), typeof(Image)),
            new GameObject("Enemy King Marker", typeof(RectTransform), typeof(Image))
        };

        local.GameObject.transform.position = Vector3.zero;
        ally.GameObject.transform.position = new Vector3(8f, 0f, 0f);
        enemyNeutral.GameObject.transform.position = new Vector3(-8f, 0f, 0f);
        enemyAttacker.GameObject.transform.position = new Vector3(-16f, 0f, 0f);
        enemyKing.GameObject.transform.position = new Vector3(0f, 0f, -16f);
        pointObject.transform.position = new Vector3(0f, 0f, 16f);

        try
        {
            var point = pointObject.AddComponent<CapturePoint>();
            point.Configure("A", 5f);
            var manager = managerObject.AddComponent<LocalMatchManager>();
            manager.Configure(
                new[] { point },
                new[] { local.Team, ally.Team, enemyNeutral.Team, enemyAttacker.Team, enemyKing.Team }
            );

            var minimapRoot = minimapObject.GetComponent<RectTransform>();
            minimapRoot.sizeDelta = new Vector2(200f, 200f);
            var selfMarker = selfMarkerObject.GetComponent<Image>();
            selfMarker.transform.SetParent(minimapRoot, false);
            var pointMarker = pointMarkerObject.GetComponent<Image>();
            pointMarker.transform.SetParent(minimapRoot, false);
            var participantMarkers = new Image[markerObjects.Length];
            for (var i = 0; i < markerObjects.Length; i++)
            {
                participantMarkers[i] = markerObjects[i].GetComponent<Image>();
                participantMarkers[i].transform.SetParent(minimapRoot, false);
            }

            var hud = hudObject.AddComponent<PlayerHud>();
            hud.Configure(
                local.Motor,
                null,
                statusObject.GetComponent<Text>(),
                objectives: new[] { point },
                localMatchManager: manager,
                playerCaptureAgent: local.Agent,
                minimapContainer: minimapRoot,
                localMinimapSelfMarker: selfMarker,
                participantMinimapMarkerImages: participantMarkers,
                capturePointMinimapMarkerImages: new[] { pointMarker },
                minimapRadius: 16f
            );

            hud.Refresh();

            Assert.IsTrue(selfMarker.gameObject.activeSelf);
            AssertAnchored(selfMarker.rectTransform, 0f, 0f);
            Assert.IsFalse(participantMarkers[0].gameObject.activeSelf);
            Assert.IsTrue(participantMarkers[1].gameObject.activeSelf);
            AssertAnchored(participantMarkers[1].rectTransform, 50f, 0f);
            Assert.IsFalse(participantMarkers[2].gameObject.activeSelf);
            Assert.IsTrue(participantMarkers[3].gameObject.activeSelf);
            AssertAnchored(participantMarkers[3].rectTransform, -100f, 0f);
            Assert.IsTrue(participantMarkers[4].gameObject.activeSelf);
            AssertAnchored(participantMarkers[4].rectTransform, 0f, -100f);
            Assert.IsTrue(pointMarker.gameObject.activeSelf);
            AssertAnchored(pointMarker.rectTransform, 0f, 100f);
        }
        finally
        {
            foreach (var markerObject in markerObjects)
            {
                Object.DestroyImmediate(markerObject);
            }

            Object.DestroyImmediate(pointMarkerObject);
            Object.DestroyImmediate(selfMarkerObject);
            Object.DestroyImmediate(minimapObject);
            Object.DestroyImmediate(statusObject);
            Object.DestroyImmediate(hudObject);
            Object.DestroyImmediate(managerObject);
            Object.DestroyImmediate(pointObject);
            Object.DestroyImmediate(local.GameObject);
            Object.DestroyImmediate(ally.GameObject);
            Object.DestroyImmediate(enemyNeutral.GameObject);
            Object.DestroyImmediate(enemyAttacker.GameObject);
            Object.DestroyImmediate(enemyKing.GameObject);
        }
    }

    [Test]
    public void PlayerHudShowsLocalPingMarkerAndLogUntilPingExpires()
    {
        var local = CreateAgent("Ping Local", TeamId.Blue, MovementState.Neutral);
        var pingSystemObject = new GameObject("Ping System");
        var hudObject = new GameObject("Ping Hud");
        var statusObject = new GameObject("Ping Status Text", typeof(RectTransform), typeof(Text));
        var minimapObject = new GameObject("Ping Minimap", typeof(RectTransform), typeof(Image));
        var pingMarkerObject = new GameObject("Ping Marker", typeof(RectTransform), typeof(Image));
        var pingLogObject = new GameObject("Ping Log", typeof(RectTransform), typeof(Text));

        try
        {
            var pingSystem = pingSystemObject.AddComponent<LocalPingSystem>();
            pingSystem.SubmitPing(new LocalPingEvent(
                LocalPingType.Objective,
                TeamId.Blue,
                new Vector3(8f, 0f, 0f),
                "Point B",
                6f
            ));

            var minimapRoot = minimapObject.GetComponent<RectTransform>();
            minimapRoot.sizeDelta = new Vector2(200f, 200f);
            var pingMarker = pingMarkerObject.GetComponent<Image>();
            pingMarker.transform.SetParent(minimapRoot, false);
            var pingLog = pingLogObject.GetComponent<Text>();

            var hud = hudObject.AddComponent<PlayerHud>();
            hud.Configure(
                local.Motor,
                null,
                statusObject.GetComponent<Text>(),
                playerCaptureAgent: local.Agent,
                minimapContainer: minimapRoot,
                localPingSystem: pingSystem,
                minimapPingMarker: pingMarker,
                localPingLogText: pingLog
            );

            hud.Refresh();

            Assert.IsTrue(pingMarker.gameObject.activeSelf);
            AssertAnchored(pingMarker.rectTransform, 50f, 0f);
            AssertColor(new Color(1f, 0.84f, 0.18f, 0.98f), pingMarker.color);
            Assert.IsTrue(pingLog.gameObject.activeSelf);
            StringAssert.Contains("PING", pingLog.text);
            StringAssert.Contains("Point B", pingLog.text);
            StringAssert.Contains("6s", pingLog.text);

            pingSystem.Tick(6f);
            hud.Refresh();

            Assert.IsFalse(pingMarker.gameObject.activeSelf);
            Assert.IsFalse(pingLog.gameObject.activeSelf);
            Assert.AreEqual(string.Empty, pingLog.text);
        }
        finally
        {
            Object.DestroyImmediate(pingLogObject);
            Object.DestroyImmediate(pingMarkerObject);
            Object.DestroyImmediate(minimapObject);
            Object.DestroyImmediate(statusObject);
            Object.DestroyImmediate(hudObject);
            Object.DestroyImmediate(pingSystemObject);
            Object.DestroyImmediate(local.GameObject);
        }
    }

    [Test]
    public void PlayerHudPingLogAccumulatesResponsesAndShowsLatestResponder()
    {
        var local = CreateAgent("Ping Response Local", TeamId.Blue, MovementState.Neutral);
        var pingSystemObject = new GameObject("Ping Response System");
        var hudObject = new GameObject("Ping Response Hud");
        var statusObject = new GameObject("Ping Response Status Text", typeof(RectTransform), typeof(Text));
        var pingLogObject = new GameObject("Ping Response Log", typeof(RectTransform), typeof(Text));

        try
        {
            var pingSystem = pingSystemObject.AddComponent<LocalPingSystem>();
            pingSystem.SubmitPing(new LocalPingEvent(
                LocalPingType.Objective,
                TeamId.Blue,
                new Vector3(8f, 0f, 0f),
                "Point B",
                6f
            ));

            Assert.IsTrue(pingSystem.SubmitResponse("Blue Runner", LocalPingType.Attention, TeamId.Blue));
            Assert.IsTrue(pingSystem.SubmitResponse("Blue Anchor", LocalPingType.Defend, TeamId.Blue));
            Assert.IsTrue(pingSystem.SubmitResponse("Blue Medic", LocalPingType.Help, TeamId.Blue));
            Assert.IsTrue(pingSystem.SubmitResponse("Blue Caller", LocalPingType.Objective, TeamId.Blue));

            var pingLog = pingLogObject.GetComponent<Text>();
            var hud = hudObject.AddComponent<PlayerHud>();
            hud.Configure(
                local.Motor,
                null,
                statusObject.GetComponent<Text>(),
                localPingSystem: pingSystem,
                localPingLogText: pingLog
            );

            hud.Refresh();

            Assert.AreEqual(4, pingSystem.ResponseCount);
            Assert.AreEqual("Blue Caller", pingSystem.LatestResponse.ResponderName);
            Assert.AreEqual(LocalPingType.Objective, pingSystem.LatestResponse.Type);
            Assert.IsTrue(pingLog.gameObject.activeSelf);
            StringAssert.Contains("PING", pingLog.text);
            StringAssert.Contains("Point B", pingLog.text);
            StringAssert.Contains("Blue Runner: Going", pingLog.text);
            StringAssert.Contains("Blue Anchor: Defend", pingLog.text);
            StringAssert.Contains("Blue Medic: Help", pingLog.text);
            StringAssert.Contains("Blue Caller: Capture", pingLog.text);
            StringAssert.Contains("LATEST  Blue Caller: Capture", pingLog.text);
        }
        finally
        {
            Object.DestroyImmediate(pingLogObject);
            Object.DestroyImmediate(statusObject);
            Object.DestroyImmediate(hudObject);
            Object.DestroyImmediate(pingSystemObject);
            Object.DestroyImmediate(local.GameObject);
        }
    }

    [Test]
    public void LocalPingSystemClearsResponsesWhenNewPingStartsOrExpires()
    {
        var pingSystemObject = new GameObject("Ping Response Clearing System");

        try
        {
            var pingSystem = pingSystemObject.AddComponent<LocalPingSystem>();

            Assert.IsFalse(pingSystem.SubmitResponse("Blue Early", LocalPingType.Attention, TeamId.Blue));
            Assert.AreEqual(0, pingSystem.ResponseCount);

            pingSystem.SubmitPing(new LocalPingEvent(
                LocalPingType.Attention,
                TeamId.Blue,
                Vector3.forward,
                "Attention",
                6f
            ));
            Assert.IsTrue(pingSystem.SubmitResponse("Blue First", LocalPingType.Attention, TeamId.Blue));
            StringAssert.Contains("Blue First: Going", pingSystem.BuildVisibleLog());

            pingSystem.SubmitPing(new LocalPingEvent(
                LocalPingType.Objective,
                TeamId.Blue,
                Vector3.right,
                "Point C",
                6f
            ));

            Assert.AreEqual(0, pingSystem.ResponseCount);
            Assert.IsFalse(pingSystem.BuildVisibleLog().Contains("Blue First"));

            Assert.IsTrue(pingSystem.SubmitResponse("Blue Second", LocalPingType.Defend, TeamId.Blue));
            pingSystem.Tick(6f);

            Assert.IsFalse(pingSystem.HasActivePing);
            Assert.AreEqual(0, pingSystem.ResponseCount);
            Assert.AreEqual(string.Empty, pingSystem.BuildVisibleLog());
            Assert.IsFalse(pingSystem.SubmitResponse("Blue Late", LocalPingType.Help, TeamId.Blue));
        }
        finally
        {
            Object.DestroyImmediate(pingSystemObject);
        }
    }

    [Test]
    public void LocalPingSystemPrioritizesEnemyThreatThenObjective()
    {
        var local = CreateAgent("Context Ping Local", TeamId.Blue, MovementState.Neutral);
        var enemyAttacker = CreateAgent("Context Enemy Attacker", TeamId.Red, MovementState.Attacker);
        var pointObject = new GameObject("Context Point A");
        var pingSystemObject = new GameObject("Context Ping System");

        local.GameObject.transform.position = Vector3.zero;
        enemyAttacker.GameObject.transform.position = new Vector3(3f, 0f, 0f);
        pointObject.transform.position = new Vector3(1f, 0f, 0f);

        try
        {
            var point = pointObject.AddComponent<CapturePoint>();
            point.Configure("A", 5f);
            var pingSystem = pingSystemObject.AddComponent<LocalPingSystem>();
            pingSystem.Configure(
                null,
                local.Agent,
                new[] { point },
                new[] { local.Team, enemyAttacker.Team }
            );

            var enemyPing = pingSystem.SubmitContextPing();

            Assert.AreEqual(LocalPingType.Enemy, enemyPing.Type);
            Assert.AreEqual("Enemy Attacker", enemyPing.Label);
            Assert.AreEqual(enemyAttacker.GameObject.transform.position, enemyPing.Position);

            enemyAttacker.StateController.SetPersistentState(MovementState.Neutral);
            var objectivePing = pingSystem.SubmitContextPing();

            Assert.AreEqual(LocalPingType.Objective, objectivePing.Type);
            Assert.AreEqual("Point A", objectivePing.Label);
            Assert.AreEqual(pointObject.transform.position, objectivePing.Position);
        }
        finally
        {
            Object.DestroyImmediate(pingSystemObject);
            Object.DestroyImmediate(pointObject);
            Object.DestroyImmediate(enemyAttacker.GameObject);
            Object.DestroyImmediate(local.GameObject);
        }
    }

    [Test]
    public void LocalPingSystemShowsWheelThenSubmitsSelectedPingOnRelease()
    {
        var local = CreateAgent("Wheel Ping Local", TeamId.Blue, MovementState.Neutral);
        var pointObject = new GameObject("Wheel Point C");
        var pingSystemObject = new GameObject("Wheel Ping System");
        var hudObject = new GameObject("Wheel Ping Hud");
        var statusObject = new GameObject("Wheel Ping Status", typeof(RectTransform), typeof(Text));
        var pingWheelObject = new GameObject("Wheel Ping Text", typeof(RectTransform), typeof(Text));
        var pingLogObject = new GameObject("Wheel Ping Log", typeof(RectTransform), typeof(Text));

        local.GameObject.transform.position = Vector3.zero;
        local.GameObject.transform.rotation = Quaternion.LookRotation(Vector3.forward, Vector3.up);
        pointObject.transform.position = new Vector3(4f, 0f, 2f);

        try
        {
            var point = pointObject.AddComponent<CapturePoint>();
            point.Configure("C", 5f);
            var pingSystem = pingSystemObject.AddComponent<LocalPingSystem>();
            pingSystem.Configure(
                null,
                local.Agent,
                new[] { point },
                new[] { local.Team }
            );

            var pingWheelText = pingWheelObject.GetComponent<Text>();
            var pingLogText = pingLogObject.GetComponent<Text>();
            var hud = hudObject.AddComponent<PlayerHud>();
            hud.Configure(
                local.Motor,
                null,
                statusObject.GetComponent<Text>(),
                localPingSystem: pingSystem,
                localPingLogText: pingLogText,
                localPingWheelText: pingWheelText
            );

            pingSystem.TickPingInput(0f, pressed: true, held: true, released: false, selection: Vector2.zero);
            pingSystem.TickPingInput(0.3f, pressed: false, held: true, released: false, selection: Vector2.left);
            hud.Refresh();

            Assert.IsTrue(pingSystem.IsWheelOpen);
            Assert.IsTrue(pingWheelText.gameObject.activeSelf);
            StringAssert.Contains("PING WHEEL", pingWheelText.text);
            StringAssert.Contains("Defend", pingWheelText.text);
            Assert.IsFalse(pingLogText.gameObject.activeSelf);

            pingSystem.TickPingInput(0f, pressed: false, held: false, released: true, selection: Vector2.left);
            var ping = pingSystem.CurrentPing;
            hud.Refresh();

            Assert.AreEqual(LocalPingType.Defend, ping.Type);
            Assert.AreEqual("Defend", ping.Label);
            Assert.IsFalse(pingSystem.IsWheelOpen);
            Assert.IsFalse(pingWheelText.gameObject.activeSelf);
            Assert.IsTrue(pingLogText.gameObject.activeSelf);
            StringAssert.Contains("Defend", pingLogText.text);
        }
        finally
        {
            Object.DestroyImmediate(pingLogObject);
            Object.DestroyImmediate(pingWheelObject);
            Object.DestroyImmediate(statusObject);
            Object.DestroyImmediate(hudObject);
            Object.DestroyImmediate(pingSystemObject);
            Object.DestroyImmediate(pointObject);
            Object.DestroyImmediate(local.GameObject);
        }
    }

    [Test]
    public void PlayerHudShowsHeldAndRescueRingsForCaptureStates()
    {
        var heldLocal = CreateAgent("Held Local", TeamId.Blue, MovementState.Neutral);
        var localHolder = CreateAgent("Local Holder", TeamId.Red, MovementState.King);
        var rescuer = CreateAgent("Rescuer", TeamId.Blue, MovementState.Neutral);
        var heldAlly = CreateAgent("Held Ally", TeamId.Blue, MovementState.Neutral);
        var allyHolder = CreateAgent("Ally Holder", TeamId.Red, MovementState.King);
        var heldManagerObject = new GameObject("Held Match Manager");
        var rescueManagerObject = new GameObject("Rescue Match Manager");
        var heldHudObject = new GameObject("Held Hud");
        var rescueHudObject = new GameObject("Rescue Hud");
        var heldStatusObject = new GameObject("Held Status Text", typeof(RectTransform), typeof(Text));
        var rescueStatusObject = new GameObject("Rescue Status Text", typeof(RectTransform), typeof(Text));
        var heldRingObject = new GameObject("Held Ring", typeof(RectTransform), typeof(Image));
        var rescueRingObject = new GameObject("Rescue Ring", typeof(RectTransform), typeof(Image));
        var heldScenarioRescueRingObject = new GameObject("Held Scenario Rescue Ring", typeof(RectTransform), typeof(Image));
        var rescueScenarioHeldRingObject = new GameObject("Rescue Scenario Held Ring", typeof(RectTransform), typeof(Image));

        rescuer.GameObject.transform.position = Vector3.zero;
        heldAlly.GameObject.transform.position = Vector3.forward * CaptureInteractionRules.RescueRange;

        try
        {
            Assert.IsTrue(localHolder.Agent.TryHold(heldLocal.Agent));
            Assert.IsTrue(allyHolder.Agent.TryHold(heldAlly.Agent));

            var heldManager = heldManagerObject.AddComponent<LocalMatchManager>();
            heldManager.Configure(System.Array.Empty<CapturePoint>(), new[] { heldLocal.Team, localHolder.Team });
            var rescueManager = rescueManagerObject.AddComponent<LocalMatchManager>();
            rescueManager.Configure(System.Array.Empty<CapturePoint>(), new[] { rescuer.Team, heldAlly.Team, allyHolder.Team });

            var heldRing = heldRingObject.GetComponent<Image>();
            var heldScenarioRescueRing = heldScenarioRescueRingObject.GetComponent<Image>();
            var heldHud = heldHudObject.AddComponent<PlayerHud>();
            heldHud.Configure(
                heldLocal.Motor,
                null,
                heldStatusObject.GetComponent<Text>(),
                localMatchManager: heldManager,
                playerCaptureAgent: heldLocal.Agent,
                heldRing: heldRing,
                rescueRing: heldScenarioRescueRing
            );

            heldHud.Refresh();

            Assert.IsTrue(heldRing.gameObject.activeSelf);
            Assert.AreEqual(1f, heldRing.fillAmount, 0.0001f);
            Assert.IsFalse(heldScenarioRescueRing.gameObject.activeSelf);

            var rescueRing = rescueRingObject.GetComponent<Image>();
            var rescueScenarioHeldRing = rescueScenarioHeldRingObject.GetComponent<Image>();
            var rescueHud = rescueHudObject.AddComponent<PlayerHud>();
            rescueHud.Configure(
                rescuer.Motor,
                null,
                rescueStatusObject.GetComponent<Text>(),
                localMatchManager: rescueManager,
                playerCaptureAgent: rescuer.Agent,
                heldRing: rescueScenarioHeldRing,
                rescueRing: rescueRing
            );

            rescueHud.Refresh();

            Assert.IsFalse(rescueScenarioHeldRing.gameObject.activeSelf);
            Assert.IsTrue(rescueRing.gameObject.activeSelf);
            Assert.AreEqual(1f, rescueRing.fillAmount, 0.0001f);
        }
        finally
        {
            Object.DestroyImmediate(rescueScenarioHeldRingObject);
            Object.DestroyImmediate(heldScenarioRescueRingObject);
            Object.DestroyImmediate(rescueRingObject);
            Object.DestroyImmediate(heldRingObject);
            Object.DestroyImmediate(rescueStatusObject);
            Object.DestroyImmediate(heldStatusObject);
            Object.DestroyImmediate(rescueHudObject);
            Object.DestroyImmediate(heldHudObject);
            Object.DestroyImmediate(rescueManagerObject);
            Object.DestroyImmediate(heldManagerObject);
            Object.DestroyImmediate(allyHolder.GameObject);
            Object.DestroyImmediate(heldAlly.GameObject);
            Object.DestroyImmediate(rescuer.GameObject);
            Object.DestroyImmediate(localHolder.GameObject);
            Object.DestroyImmediate(heldLocal.GameObject);
        }
    }

    [Test]
    public void PlayerHudObjectivePanelRowsMirrorCapturePointStateAndHideUnusedRows()
    {
        var local = CreateAgent("Objective Hud Local", TeamId.Blue, MovementState.Neutral);
        var capturingPointObject = new GameObject("Capture Point A");
        var heldPointObject = new GameObject("Capture Point B");
        var hudObject = new GameObject("Objective Hud");
        var statusObject = new GameObject("Objective Status Text", typeof(RectTransform), typeof(Text));
        var rows = CreateObjectivePanelRows(3);
        var capturingBlue = CreateObjectiveOccupant("Point A Blue", TeamId.Blue);
        var heldRedOccupants = new[]
        {
            CreateObjectiveOccupant("Point B Red 1", TeamId.Red),
            CreateObjectiveOccupant("Point B Red 2", TeamId.Red),
            CreateObjectiveOccupant("Point B Red 3", TeamId.Red)
        };

        try
        {
            var capturingPoint = capturingPointObject.AddComponent<CapturePoint>();
            capturingPoint.Configure("A", 5f);
            EnterObjective(capturingPoint, capturingBlue);
            capturingPoint.Tick(10f);

            var heldPoint = heldPointObject.AddComponent<CapturePoint>();
            heldPoint.Configure("B", 5f);
            foreach (var occupant in heldRedOccupants)
            {
                EnterObjective(heldPoint, occupant);
            }

            heldPoint.Tick(10f);
            foreach (var occupant in heldRedOccupants)
            {
                ExitObjective(heldPoint, occupant);
            }

            heldPoint.Tick(0f);

            var hud = hudObject.AddComponent<PlayerHud>();
            hud.Configure(
                local.Motor,
                null,
                statusObject.GetComponent<Text>(),
                objectives: new[] { capturingPoint, heldPoint },
                objectivePanelRowBackgrounds: rows.RowBackgrounds,
                objectivePanelPointIdTexts: rows.PointIdTexts,
                objectivePanelOwnerIndicators: rows.OwnerIndicators,
                objectivePanelProgressFills: rows.ProgressFills,
                objectivePanelStateTexts: rows.StateTexts,
                objectivePanelOccupantCountTexts: rows.OccupantCountTexts
            );

            hud.Refresh();

            AssertObjectiveRowVisible(rows, 0, true);
            AssertObjectiveRowVisible(rows, 1, true);
            AssertObjectiveRowVisible(rows, 2, false);

            Assert.AreEqual("A", rows.PointIdTexts[0].text);
            Assert.AreEqual(0.5f, rows.ProgressFills[0].fillAmount, 0.0001f);
            AssertColor(ExpectedBlueColor(), rows.ProgressFills[0].color);
            Assert.AreEqual("Capturing Blue", rows.StateTexts[0].text);
            StringAssert.Contains("Blue 1", rows.OccupantCountTexts[0].text);
            StringAssert.Contains("Red 0", rows.OccupantCountTexts[0].text);

            Assert.AreEqual("B", rows.PointIdTexts[1].text);
            AssertColor(ExpectedRedColor(), rows.OwnerIndicators[1].color);
            Assert.AreEqual(1f, rows.ProgressFills[1].fillAmount, 0.0001f);
            AssertColor(ExpectedRedColor(), rows.ProgressFills[1].color);
            Assert.AreEqual("Held", rows.StateTexts[1].text);
            StringAssert.Contains("Blue 0", rows.OccupantCountTexts[1].text);
            StringAssert.Contains("Red 0", rows.OccupantCountTexts[1].text);
        }
        finally
        {
            foreach (var occupant in heldRedOccupants)
            {
                Object.DestroyImmediate(occupant.gameObject);
            }

            Object.DestroyImmediate(capturingBlue.gameObject);
            rows.Destroy();
            Object.DestroyImmediate(statusObject);
            Object.DestroyImmediate(hudObject);
            Object.DestroyImmediate(heldPointObject);
            Object.DestroyImmediate(capturingPointObject);
            Object.DestroyImmediate(local.GameObject);
        }
    }

    [Test]
    public void PlayerHudObjectivePanelRowsHideStaleRowsWhenObjectivesShrink()
    {
        var local = CreateAgent("Objective Shrink Local", TeamId.Blue, MovementState.Neutral);
        var firstPointObject = new GameObject("Capture Point Shrink A");
        var secondPointObject = new GameObject("Capture Point Shrink B");
        var hudObject = new GameObject("Objective Shrink Hud");
        var statusObject = new GameObject("Objective Shrink Status Text", typeof(RectTransform), typeof(Text));
        var rows = CreateObjectivePanelRows(3);

        try
        {
            var firstPoint = firstPointObject.AddComponent<CapturePoint>();
            firstPoint.Configure("A", 5f);
            var secondPoint = secondPointObject.AddComponent<CapturePoint>();
            secondPoint.Configure("B", 5f);

            var hud = hudObject.AddComponent<PlayerHud>();
            hud.Configure(
                local.Motor,
                null,
                statusObject.GetComponent<Text>(),
                objectives: new[] { firstPoint, secondPoint },
                objectivePanelRowBackgrounds: rows.RowBackgrounds,
                objectivePanelPointIdTexts: rows.PointIdTexts,
                objectivePanelOwnerIndicators: rows.OwnerIndicators,
                objectivePanelProgressFills: rows.ProgressFills,
                objectivePanelStateTexts: rows.StateTexts,
                objectivePanelOccupantCountTexts: rows.OccupantCountTexts
            );

            hud.Refresh();

            AssertObjectiveRowVisible(rows, 0, true);
            AssertObjectiveRowVisible(rows, 1, true);
            AssertObjectiveRowVisible(rows, 2, false);

            hud.SetCapturePoints(new[] { firstPoint });
            hud.Refresh();

            AssertObjectiveRowVisible(rows, 0, true);
            AssertObjectiveRowVisible(rows, 1, false);
            AssertObjectiveRowVisible(rows, 2, false);

            hud.SetCapturePoints(null);
            hud.Refresh();

            AssertObjectiveRowVisible(rows, 0, false);
            AssertObjectiveRowVisible(rows, 1, false);
            AssertObjectiveRowVisible(rows, 2, false);
        }
        finally
        {
            rows.Destroy();
            Object.DestroyImmediate(statusObject);
            Object.DestroyImmediate(hudObject);
            Object.DestroyImmediate(secondPointObject);
            Object.DestroyImmediate(firstPointObject);
            Object.DestroyImmediate(local.GameObject);
        }
    }

    private static AgentFixture CreateAgent(string name, TeamId team, MovementState state)
    {
        var gameObject = new GameObject(name);
        gameObject.AddComponent<CharacterController>();
        gameObject.AddComponent<PlayerInputReader>();
        var motor = gameObject.AddComponent<PlayerMotor>();
        var teamComponent = gameObject.AddComponent<LocalPlayerTeam>();
        teamComponent.Configure(team);
        var stateController = gameObject.AddComponent<PlayerStateController>();
        var agent = gameObject.AddComponent<PlayerCaptureAgent>();
        stateController.SetPersistentState(state);
        agent.Configure(stateController);
        return new AgentFixture(gameObject, motor, teamComponent, stateController, agent);
    }

    private static LocalPlayerTeam CreateObjectiveOccupant(string name, TeamId team)
    {
        var gameObject = new GameObject(name, typeof(BoxCollider));
        var teamComponent = gameObject.AddComponent<LocalPlayerTeam>();
        teamComponent.Configure(team);
        return teamComponent;
    }

    private static ObjectivePanelRowsFixture CreateObjectivePanelRows(int count)
    {
        var rowBackgrounds = new Image[count];
        var pointIdTexts = new Text[count];
        var ownerIndicators = new Image[count];
        var progressFills = new Image[count];
        var stateTexts = new Text[count];
        var occupantCountTexts = new Text[count];

        for (var i = 0; i < count; i++)
        {
            var rowObject = new GameObject($"Objective Row {i}", typeof(RectTransform), typeof(Image));
            rowBackgrounds[i] = rowObject.GetComponent<Image>();
            pointIdTexts[i] = CreateRowText($"Objective Row {i} Point Id", rowObject.transform);
            ownerIndicators[i] = CreateRowImage($"Objective Row {i} Owner", rowObject.transform);
            progressFills[i] = CreateRowImage($"Objective Row {i} Progress", rowObject.transform);
            stateTexts[i] = CreateRowText($"Objective Row {i} State", rowObject.transform);
            occupantCountTexts[i] = CreateRowText($"Objective Row {i} Occupants", rowObject.transform);
        }

        return new ObjectivePanelRowsFixture(
            rowBackgrounds,
            pointIdTexts,
            ownerIndicators,
            progressFills,
            stateTexts,
            occupantCountTexts
        );
    }

    private static Text CreateRowText(string name, Transform parent)
    {
        var gameObject = new GameObject(name, typeof(RectTransform), typeof(Text));
        gameObject.transform.SetParent(parent, false);
        return gameObject.GetComponent<Text>();
    }

    private static Image CreateRowImage(string name, Transform parent)
    {
        var gameObject = new GameObject(name, typeof(RectTransform), typeof(Image));
        gameObject.transform.SetParent(parent, false);
        return gameObject.GetComponent<Image>();
    }

    private static void EnterObjective(CapturePoint point, LocalPlayerTeam occupant)
    {
        InvokeObjectiveTrigger(point, "OnTriggerEnter", occupant.GetComponent<Collider>());
    }

    private static void ExitObjective(CapturePoint point, LocalPlayerTeam occupant)
    {
        InvokeObjectiveTrigger(point, "OnTriggerExit", occupant.GetComponent<Collider>());
    }

    private static void InvokeObjectiveTrigger(CapturePoint point, string methodName, Collider collider)
    {
        var method = typeof(CapturePoint).GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(method, $"{methodName} should remain available for EditMode objective seeding.");
        method.Invoke(point, new object[] { collider });
    }

    private static void AssertAnchored(RectTransform rectTransform, float expectedX, float expectedY)
    {
        Assert.AreEqual(expectedX, rectTransform.anchoredPosition.x, 0.001f);
        Assert.AreEqual(expectedY, rectTransform.anchoredPosition.y, 0.001f);
    }

    private static void AssertObjectiveRowVisible(ObjectivePanelRowsFixture rows, int index, bool expected)
    {
        Assert.AreEqual(expected, rows.RowBackgrounds[index].gameObject.activeSelf);
        Assert.AreEqual(expected, rows.PointIdTexts[index].gameObject.activeInHierarchy);
        Assert.AreEqual(expected, rows.OwnerIndicators[index].gameObject.activeInHierarchy);
        Assert.AreEqual(expected, rows.ProgressFills[index].gameObject.activeInHierarchy);
        Assert.AreEqual(expected, rows.StateTexts[index].gameObject.activeInHierarchy);
        Assert.AreEqual(expected, rows.OccupantCountTexts[index].gameObject.activeInHierarchy);
    }

    private static void AssertTeamStatusIconActive(Image icon, string statusName)
    {
        Assert.IsTrue(icon.gameObject.activeSelf, $"{statusName} icon should be active.");
        Assert.IsNotNull(icon.sprite, $"{statusName} icon should assign a sprite.");
        Assert.IsFalse(string.IsNullOrWhiteSpace(icon.sprite.name), $"{statusName} icon sprite should be named.");
        Assert.AreNotEqual(Color.magenta, icon.color, $"{statusName} icon should overwrite stale slot color.");
    }

    private static void AssertDistinctIconRender(Image first, Image second, string firstName, string secondName)
    {
        Assert.AreNotEqual(
            first.sprite.name,
            second.sprite.name,
            $"{firstName} and {secondName} should not reuse the same sprite."
        );
        Assert.AreNotEqual(
            first.color,
            second.color,
            $"{firstName} and {secondName} should not reuse the same color."
        );
    }

    private static void AssertColor(Color expected, Color actual)
    {
        Assert.AreEqual(expected.r, actual.r, 0.0001f);
        Assert.AreEqual(expected.g, actual.g, 0.0001f);
        Assert.AreEqual(expected.b, actual.b, 0.0001f);
        Assert.AreEqual(expected.a, actual.a, 0.0001f);
    }

    private static Color ExpectedBlueColor()
    {
        return new Color(0.25f, 0.64f, 1f, 0.95f);
    }

    private static Color ExpectedRedColor()
    {
        return new Color(1f, 0.18f, 0.24f, 0.95f);
    }

    private readonly struct AgentFixture
    {
        public AgentFixture(
            GameObject gameObject,
            PlayerMotor motor,
            LocalPlayerTeam team,
            PlayerStateController stateController,
            PlayerCaptureAgent agent)
        {
            GameObject = gameObject;
            Motor = motor;
            Team = team;
            StateController = stateController;
            Agent = agent;
        }

        public GameObject GameObject { get; }
        public PlayerMotor Motor { get; }
        public LocalPlayerTeam Team { get; }
        public PlayerStateController StateController { get; }
        public PlayerCaptureAgent Agent { get; }
    }

    private readonly struct ObjectivePanelRowsFixture
    {
        public ObjectivePanelRowsFixture(
            Image[] rowBackgrounds,
            Text[] pointIdTexts,
            Image[] ownerIndicators,
            Image[] progressFills,
            Text[] stateTexts,
            Text[] occupantCountTexts)
        {
            RowBackgrounds = rowBackgrounds;
            PointIdTexts = pointIdTexts;
            OwnerIndicators = ownerIndicators;
            ProgressFills = progressFills;
            StateTexts = stateTexts;
            OccupantCountTexts = occupantCountTexts;
        }

        public Image[] RowBackgrounds { get; }
        public Text[] PointIdTexts { get; }
        public Image[] OwnerIndicators { get; }
        public Image[] ProgressFills { get; }
        public Text[] StateTexts { get; }
        public Text[] OccupantCountTexts { get; }

        public void Destroy()
        {
            foreach (var rowBackground in RowBackgrounds)
            {
                Object.DestroyImmediate(rowBackground.gameObject);
            }
        }
    }
}

using System.Collections.Generic;
using Overthrone;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class OverthroneUnityBootstrap
{
    private const string ProfilePath = "Assets/Profiles/DefaultMovementProfiles.asset";
    private const string InputPath = "Assets/Input/OverthroneControls.inputactions";
    private const string ControllerPath = "Assets/Animations/Controllers/Player.controller";
    private const string ScenePath = "Assets/Scenes/Prototype.unity";
    private const string PlayerPrefabPath = "Assets/Prefabs/Player.prefab";
    private const string AiPrefabPath = "Assets/Prefabs/HearingAI.prefab";

    [MenuItem("Overthrone/Bootstrap Prototype Scene")]
    public static void BootstrapPrototypeScene()
    {
        EnsureFolders();

        var profiles = CreateMovementProfiles();
        var controller = CreateAnimatorController();
        var inputActions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputPath);
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        CreateLighting();
        CreateGround();
        var rosterSlots = LocalRosterBuilder.CreateDefaultThreeVsThree();
        var player = CreatePlayer(profiles, controller, inputActions, rosterSlots[0]);
        var rosterObjects = CreateRosterParticipants(profiles, rosterSlots, player);
        var capturePoints = CreateCapturePoints();
        var matchParticipants = GetRosterTeams(rosterObjects);
        var blueDefenderSpawn = CreateDefenderSpawnMarker("Blue Defender Re-entry Spawn", new Vector3(-6.5f, 0.05f, -6.5f), TeamId.Blue);
        var redDefenderSpawn = CreateDefenderSpawnMarker("Red Defender Re-entry Spawn", new Vector3(6.5f, 0.05f, -6.5f), TeamId.Red);
        var matchManager = CreateLocalMatchManager(capturePoints, matchParticipants);
        var captureAgents = GetRosterCaptureAgents(rosterObjects);
        ConfigureLocalSpectatorCamera(player.GetComponentInChildren<LocalSpectatorCamera>(), captureAgents[0], captureAgents, player.GetComponent<PlayerInputReader>());
        var deadChannel = CreateLocalDeadChannel();
        var localCaptureSystem = CreateLocalCaptureSystem(player.GetComponent<PlayerCaptureAgent>(), captureAgents, deadChannel);
        var localPingSystem = CreateLocalPingSystem(player.GetComponent<PlayerInputReader>(), player.GetComponent<PlayerCaptureAgent>(), capturePoints, matchParticipants);
        CreateCaptureFeedbackController();
        CreatePrototypeHud(
            player.GetComponent<PlayerMotor>(),
            player.GetComponent<PlayerCaptureAgent>(),
            player.GetComponentInChildren<LocalSpectatorCamera>(),
            capturePoints,
            matchManager,
            localCaptureSystem,
            deadChannel,
            localPingSystem,
            matchParticipants,
            blueDefenderSpawn,
            redDefenderSpawn
        );
        var ai = CreateHearingAi();

        PrefabUtility.SaveAsPrefabAsset(player, PlayerPrefabPath);
        PrefabUtility.SaveAsPrefabAsset(ai, AiPrefabPath);

        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Overthrone Unity prototype scene bootstrapped.");
    }

    private static void EnsureFolders()
    {
        var folders = new[]
        {
            "Assets/Animations",
            "Assets/Animations/Clips",
            "Assets/Animations/Controllers",
            "Assets/Art",
            "Assets/Art/Materials",
            "Assets/Audio",
            "Assets/Input",
            "Assets/Prefabs",
            "Assets/Profiles",
            "Assets/Scenes"
        };

        foreach (var folder in folders)
        {
            if (!AssetDatabase.IsValidFolder(folder))
            {
                var parent = System.IO.Path.GetDirectoryName(folder)?.Replace("\\", "/") ?? "Assets";
                var child = System.IO.Path.GetFileName(folder);
                AssetDatabase.CreateFolder(parent, child);
            }
        }
    }

    private static MovementProfileSet CreateMovementProfiles()
    {
        var profileSet = AssetDatabase.LoadAssetAtPath<MovementProfileSet>(ProfilePath);
        if (profileSet == null)
        {
            profileSet = ScriptableObject.CreateInstance<MovementProfileSet>();
            AssetDatabase.CreateAsset(profileSet, ProfilePath);
        }

        profileSet.SetProfiles(new List<MovementProfile>
        {
            new MovementProfile { state = MovementState.Neutral, canMove = true, canSprint = true, walkSpeed = 4.5f, runSpeed = 7.2f, noiseRadius = 6f, footstepInterval = 0.44f },
            new MovementProfile { state = MovementState.Attacker, canMove = true, canSprint = true, walkSpeed = 4.8f, runSpeed = 7.6f, noiseRadius = 8f, footstepInterval = 0.38f },
            new MovementProfile { state = MovementState.King, canMove = true, canSprint = true, walkSpeed = 5.0f, runSpeed = 7.9f, noiseRadius = 9.5f, footstepInterval = 0.36f },
            new MovementProfile { state = MovementState.Held, canMove = false, canSprint = false, walkSpeed = 0f, runSpeed = 0f, noiseRadius = 0f, footstepInterval = 1f },
            new MovementProfile { state = MovementState.Captured, canMove = false, canSprint = false, walkSpeed = 0f, runSpeed = 0f, noiseRadius = 0f, footstepInterval = 1f },
            new MovementProfile { state = MovementState.Slime, canMove = true, canSprint = false, walkSpeed = 6.3f, runSpeed = 6.3f, noiseRadius = 3f, footstepInterval = 0.52f },
            new MovementProfile { state = MovementState.Holding, canMove = false, canSprint = false, walkSpeed = 0f, runSpeed = 0f, noiseRadius = 0f, footstepInterval = 1f }
        });
        EditorUtility.SetDirty(profileSet);
        return profileSet;
    }

    private static AnimatorController CreateAnimatorController()
    {
        var idle = CreateClip("Assets/Animations/Clips/Idle.anim");
        var walk = CreateClip("Assets/Animations/Clips/Walk.anim");
        var run = CreateClip("Assets/Animations/Clips/Run.anim");

        if (AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath) != null)
        {
            AssetDatabase.DeleteAsset(ControllerPath);
        }

        var controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
        controller.AddParameter("MoveSpeed", AnimatorControllerParameterType.Float);
        controller.AddParameter("IsMoving", AnimatorControllerParameterType.Bool);
        controller.AddParameter("IsSprinting", AnimatorControllerParameterType.Bool);
        controller.AddParameter("IsGrounded", AnimatorControllerParameterType.Bool);
        controller.AddParameter("MovementState", AnimatorControllerParameterType.Int);

        var stateMachine = controller.layers[0].stateMachine;
        stateMachine.states = System.Array.Empty<ChildAnimatorState>();
        var locomotionState = stateMachine.AddState("Locomotion");
        var blendTree = new BlendTree
        {
            name = "IdleWalkRun Blend Tree",
            blendType = BlendTreeType.Simple1D,
            blendParameter = "MoveSpeed",
            useAutomaticThresholds = false
        };
        AssetDatabase.AddObjectToAsset(blendTree, controller);
        blendTree.AddChild(idle, 0f);
        blendTree.AddChild(walk, 0.45f);
        blendTree.AddChild(run, 1f);
        locomotionState.motion = blendTree;
        stateMachine.defaultState = locomotionState;

        AssetDatabase.SaveAssets();
        return controller;
    }

    private static AnimationClip CreateClip(string path)
    {
        var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if (clip != null)
        {
            return clip;
        }

        clip = new AnimationClip
        {
            frameRate = 30f
        };
        AssetDatabase.CreateAsset(clip, path);
        return clip;
    }

    private static void CreateLighting()
    {
        var light = new GameObject("Sun");
        var directional = light.AddComponent<Light>();
        directional.type = LightType.Directional;
        directional.intensity = 1.2f;
        light.transform.rotation = Quaternion.Euler(50f, -35f, 0f);
        RenderSettings.ambientLight = new Color(0.72f, 0.86f, 0.95f);
    }

    private static void CreateGround()
    {
        var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Moonlit Garden Blockout";
        ground.transform.localScale = new Vector3(8f, 1f, 8f);
        var renderer = ground.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = CreateMaterial("Assets/Art/Materials/GardenGrass.mat", new Color(0.46f, 0.78f, 0.34f));
    }

    private static GameObject CreatePlayer(
        MovementProfileSet profiles,
        AnimatorController controller,
        InputActionAsset inputActions,
        LocalRosterSlot slot)
    {
        var player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        player.name = slot.DisplayName;
        player.transform.position = slot.SpawnPosition;
        Object.DestroyImmediate(player.GetComponent<CapsuleCollider>());
        player.GetComponent<MeshRenderer>().sharedMaterial = CreateMaterial("Assets/Art/Materials/PlayerBlue.mat", new Color(0.22f, 0.56f, 1f));

        var characterController = player.AddComponent<CharacterController>();
        characterController.height = 1.8f;
        characterController.radius = 0.36f;
        characterController.center = new Vector3(0f, 0.9f, 0f);

        var input = player.AddComponent<PlayerInputReader>();
        input.Configure(inputActions);
        var team = player.AddComponent<LocalPlayerTeam>();
        team.Configure(slot.Team);
        team.ConfigureKingPriority(0, 0f, 100 - slot.TeamIndex);

        var cameraPivot = new GameObject("CameraPivot").transform;
        cameraPivot.SetParent(player.transform);
        cameraPivot.localPosition = new Vector3(0f, 1.35f, 0f);
        cameraPivot.localRotation = Quaternion.Euler(18f, 0f, 0f);

        var cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";
        cameraObject.transform.SetParent(cameraPivot);
        cameraObject.transform.localPosition = new Vector3(0f, 1.25f, -4.8f);
        cameraObject.transform.localRotation = Quaternion.identity;
        var camera = cameraObject.AddComponent<Camera>();
        camera.fieldOfView = 76f;
        cameraObject.AddComponent<AudioListener>();
        cameraObject.AddComponent<LocalSpectatorCamera>();

        var motor = player.AddComponent<PlayerMotor>();
        motor.cameraPivot = cameraPivot;
        motor.movementProfiles = profiles;
        player.AddComponent<PlayerStateController>();
        player.AddComponent<PlayerCaptureAgent>();
        player.AddComponent<TackleHitbox>();

        var animator = player.AddComponent<Animator>();
        animator.runtimeAnimatorController = controller;
        player.AddComponent<PlayerAnimationDriver>();

        var audioSource = player.AddComponent<AudioSource>();
        audioSource.spatialBlend = 1f;
        player.AddComponent<PlayerNoiseEmitter>();
        return player;
    }

    private static GameObject[] CreateRosterParticipants(
        MovementProfileSet profiles,
        LocalRosterSlot[] rosterSlots,
        GameObject localPlayer)
    {
        var roster = new GameObject[rosterSlots.Length];
        for (var index = 0; index < rosterSlots.Length; index++)
        {
            var slot = rosterSlots[index];
            roster[index] = slot.IsLocalPlayer ? localPlayer : CreateRosterNpc(profiles, slot);
        }

        return roster;
    }

    private static GameObject CreateRosterNpc(MovementProfileSet profiles, LocalRosterSlot slot)
    {
        var target = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        target.name = slot.DisplayName;
        target.transform.position = slot.SpawnPosition;
        target.GetComponent<MeshRenderer>().sharedMaterial = CreateMaterial(
            slot.Team == TeamId.Blue ? "Assets/Art/Materials/PlayerBlue.mat" : "Assets/Art/Materials/AIRed.mat",
            slot.Team == TeamId.Blue ? new Color(0.22f, 0.56f, 1f) : new Color(1f, 0.28f, 0.38f)
        );

        var characterController = target.AddComponent<CharacterController>();
        characterController.height = 1.8f;
        characterController.radius = 0.36f;
        characterController.center = new Vector3(0f, 0.9f, 0f);

        target.AddComponent<PlayerInputReader>();
        var motor = target.AddComponent<PlayerMotor>();
        motor.lockCursorOnStart = false;
        motor.lockCursorOnClick = false;
        motor.movementProfiles = profiles;
        target.AddComponent<PlayerStateController>();
        var team = target.AddComponent<LocalPlayerTeam>();
        team.Configure(slot.Team);
        team.ConfigureKingPriority(0, 0f, 100 - slot.TeamIndex);
        target.AddComponent<PlayerCaptureAgent>();
        target.AddComponent<TackleHitbox>();
        return target;
    }

    private static LocalPlayerTeam[] GetRosterTeams(GameObject[] rosterObjects)
    {
        var teams = new LocalPlayerTeam[rosterObjects.Length];
        for (var index = 0; index < rosterObjects.Length; index++)
        {
            teams[index] = rosterObjects[index].GetComponent<LocalPlayerTeam>();
        }

        return teams;
    }

    private static PlayerCaptureAgent[] GetRosterCaptureAgents(GameObject[] rosterObjects)
    {
        var agents = new PlayerCaptureAgent[rosterObjects.Length];
        for (var index = 0; index < rosterObjects.Length; index++)
        {
            agents[index] = rosterObjects[index].GetComponent<PlayerCaptureAgent>();
        }

        return agents;
    }

    private static CapturePoint[] CreateCapturePoints()
    {
        var captureMaterial = CreateMaterial("Assets/Art/Materials/CapturePointNeutral.mat", new Color(0.2f, 0.78f, 0.96f, 0.45f));
        return new[]
        {
            CreateCapturePoint("A", new Vector3(0f, 0.05f, 0f), captureMaterial),
            CreateCapturePoint("B", new Vector3(-8f, 0.05f, 7f), captureMaterial),
            CreateCapturePoint("C", new Vector3(8f, 0.05f, 7f), captureMaterial)
        };
    }

    private static CapturePoint CreateCapturePoint(string pointId, Vector3 position, Material material)
    {
        var pointObject = new GameObject($"Capture Point {pointId}");
        pointObject.transform.position = position;
        var capturePoint = pointObject.AddComponent<CapturePoint>();
        capturePoint.Configure(pointId, 5f);

        var radiusVisual = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        radiusVisual.name = $"Capture Point {pointId} Radius";
        radiusVisual.transform.SetParent(pointObject.transform, false);
        radiusVisual.transform.localPosition = Vector3.zero;
        radiusVisual.transform.localScale = new Vector3(10f, 0.04f, 10f);
        Object.DestroyImmediate(radiusVisual.GetComponent<Collider>());
        radiusVisual.GetComponent<MeshRenderer>().sharedMaterial = material;

        var pillar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        pillar.name = $"Capture Point {pointId} Pillar";
        pillar.transform.SetParent(pointObject.transform, false);
        pillar.transform.localPosition = new Vector3(0f, 1.6f, 0f);
        pillar.transform.localScale = new Vector3(0.35f, 1.6f, 0.35f);
        Object.DestroyImmediate(pillar.GetComponent<Collider>());
        pillar.GetComponent<MeshRenderer>().sharedMaterial = material;

        var labelObject = new GameObject($"Capture Point {pointId} Label");
        labelObject.transform.SetParent(pointObject.transform, false);
        labelObject.transform.localPosition = new Vector3(0f, 3.4f, 0f);
        var label = labelObject.AddComponent<TextMesh>();
        label.text = pointId;
        label.fontSize = 72;
        label.characterSize = 0.08f;
        label.anchor = TextAnchor.MiddleCenter;
        label.alignment = TextAlignment.Center;
        label.color = Color.white;
        return capturePoint;
    }

    private static LocalMatchManager CreateLocalMatchManager(CapturePoint[] capturePoints, LocalPlayerTeam[] participants)
    {
        var matchManagerObject = new GameObject("Local Match Manager");
        var matchManager = matchManagerObject.AddComponent<LocalMatchManager>();
        matchManager.Configure(capturePoints, participants);
        return matchManager;
    }

    private static Transform CreateDefenderSpawnMarker(string name, Vector3 position, TeamId team)
    {
        var spawnObject = new GameObject(name);
        spawnObject.transform.position = position;

        var marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        marker.name = $"{name} Marker";
        marker.transform.SetParent(spawnObject.transform, false);
        marker.transform.localPosition = Vector3.zero;
        marker.transform.localScale = new Vector3(1.2f, 0.06f, 1.2f);
        Object.DestroyImmediate(marker.GetComponent<Collider>());
        marker.GetComponent<MeshRenderer>().sharedMaterial = CreateMaterial(
            team == TeamId.Blue ? "Assets/Art/Materials/BlueReentrySpawn.mat" : "Assets/Art/Materials/RedReentrySpawn.mat",
            team == TeamId.Blue ? new Color(0.24f, 0.64f, 1f, 0.55f) : new Color(1f, 0.28f, 0.38f, 0.55f)
        );

        var labelObject = new GameObject($"{name} Label");
        labelObject.transform.SetParent(spawnObject.transform, false);
        labelObject.transform.localPosition = new Vector3(0f, 0.4f, 0f);
        var label = labelObject.AddComponent<TextMesh>();
        label.text = "RE-ENTRY";
        label.fontSize = 36;
        label.characterSize = 0.06f;
        label.anchor = TextAnchor.MiddleCenter;
        label.alignment = TextAlignment.Center;
        label.color = Color.white;
        return spawnObject.transform;
    }

    private static LocalDeadChannel CreateLocalDeadChannel()
    {
        var deadChannelObject = new GameObject("Local Dead Channel");
        return deadChannelObject.AddComponent<LocalDeadChannel>();
    }

    private static LocalCaptureSystem CreateLocalCaptureSystem(PlayerCaptureAgent player, PlayerCaptureAgent[] captureAgents, LocalDeadChannel deadChannel)
    {
        var captureSystemObject = new GameObject("Local Capture System");
        var captureSystem = captureSystemObject.AddComponent<LocalCaptureSystem>();
        captureSystem.Configure(player, captureAgents, deadChannel);
        return captureSystem;
    }

    private static LocalPingSystem CreateLocalPingSystem(
        PlayerInputReader inputReader,
        PlayerCaptureAgent localPlayer,
        CapturePoint[] capturePoints,
        LocalPlayerTeam[] matchParticipants)
    {
        var pingSystemObject = new GameObject("Local Ping System");
        var pingSystem = pingSystemObject.AddComponent<LocalPingSystem>();
        pingSystem.Configure(inputReader, localPlayer, capturePoints, matchParticipants);
        return pingSystem;
    }

    private static void ConfigureLocalSpectatorCamera(
        LocalSpectatorCamera spectatorCamera,
        PlayerCaptureAgent localPlayer,
        PlayerCaptureAgent[] captureAgents,
        PlayerInputReader inputReader)
    {
        if (spectatorCamera != null)
        {
            spectatorCamera.Configure(localPlayer, captureAgents, inputReader);
        }
    }

    private static void CreateCaptureFeedbackController()
    {
        var feedbackObject = new GameObject("Capture Feedback Controller");
        feedbackObject.AddComponent<AudioSource>();
        feedbackObject.AddComponent<CaptureFeedbackController>();
    }

    private static void CreatePrototypeHud(
        PlayerMotor playerMotor,
        PlayerCaptureAgent playerCaptureAgent,
        LocalSpectatorCamera spectatorCamera,
        CapturePoint[] capturePoints,
        LocalMatchManager matchManager,
        LocalCaptureSystem captureSystem,
        LocalDeadChannel deadChannel,
        LocalPingSystem pingSystem,
        LocalPlayerTeam[] matchParticipants,
        Transform blueDefenderSpawn,
        Transform redDefenderSpawn)
    {
        var hud = new GameObject("Prototype HUD");
        var canvas = hud.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 20;

        var scaler = hud.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        hud.AddComponent<GraphicRaycaster>();

        var root = hud.GetComponent<RectTransform>();
        var flowOverlay = CreateUiImage("Match Flow Overlay", root, new Color(0f, 0f, 0f, 0f));
        flowOverlay.gameObject.SetActive(false);
        SetRect(flowOverlay.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        var crosshairVertical = CreateUiImage("Crosshair Vertical", root, new Color(1f, 1f, 1f, 0.92f));
        SetRect(crosshairVertical.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(2f, 18f), Vector2.zero);

        var crosshairHorizontal = CreateUiImage("Crosshair Horizontal", root, new Color(1f, 1f, 1f, 0.92f));
        SetRect(crosshairHorizontal.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(18f, 2f), Vector2.zero);

        var staminaBackground = CreateUiImage("Stamina Background", root, new Color(0.08f, 0.1f, 0.12f, 0.72f));
        SetRect(staminaBackground.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(320f, 18f), new Vector2(0f, 58f));

        var staminaFill = CreateUiImage("Stamina Fill", staminaBackground.rectTransform, new Color(0.24f, 0.86f, 0.52f, 0.95f));
        staminaFill.type = Image.Type.Filled;
        staminaFill.fillMethod = Image.FillMethod.Horizontal;
        staminaFill.fillOrigin = (int)Image.OriginHorizontal.Left;
        staminaFill.fillAmount = 1f;
        SetRect(staminaFill.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        var statusText = CreateUiText("Status Text", root, 18, TextAnchor.MiddleCenter);
        SetRect(statusText.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(560f, 28f), new Vector2(0f, 88f));

        var objectiveText = CreateUiText("Objective Text", root, 16, TextAnchor.UpperLeft);
        objectiveText.horizontalOverflow = HorizontalWrapMode.Wrap;
        objectiveText.verticalOverflow = VerticalWrapMode.Overflow;
        SetRect(objectiveText.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(430f, 150f), new Vector2(-245f, 120f));

        var objectivePanelBackground = CreateUiImage("Objective Panel Background", root, new Color(0.04f, 0.06f, 0.07f, 0.78f));
        SetRect(objectivePanelBackground.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(430f, 220f), new Vector2(-245f, -120f));
        var objectivePanelTitle = CreateUiText("Objective Panel Title", objectivePanelBackground.rectTransform, 17, TextAnchor.MiddleLeft);
        objectivePanelTitle.text = "Capture Points";
        SetRect(objectivePanelTitle.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(394f, 28f), new Vector2(0f, 88f));
        var objectivePanelRows = CreateObjectivePanelRows(
            objectivePanelBackground.rectTransform,
            capturePoints.Length,
            out var objectivePanelPointIdTexts,
            out var objectivePanelOwnerIndicators,
            out var objectivePanelProgressFills,
            out var objectivePanelStateTexts,
            out var objectivePanelOccupantCountTexts
        );

        var minimapBackground = CreateUiImage("Minimap Background", root, new Color(0.04f, 0.06f, 0.07f, 0.78f));
        SetRect(minimapBackground.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(188f, 188f), new Vector2(-118f, -118f));
        var minimapBorder = CreateUiImage("Minimap North Line", minimapBackground.rectTransform, new Color(1f, 1f, 1f, 0.28f));
        SetRect(minimapBorder.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(188f, 2f), new Vector2(0f, -1f));
        var localMinimapMarker = CreateMinimapMarker("Minimap Self Marker", minimapBackground.rectTransform, new Color(1f, 1f, 1f, 0.96f), new Vector2(12f, 12f));
        var participantMinimapMarkers = CreateMinimapMarkers("Minimap Player Marker", minimapBackground.rectTransform, matchParticipants.Length, new Vector2(9f, 9f));
        var capturePointMinimapMarkers = CreateMinimapMarkers("Minimap Point Marker", minimapBackground.rectTransform, capturePoints.Length, new Vector2(12f, 12f));
        var pingMinimapMarker = CreateMinimapMarker("Minimap Ping Marker", minimapBackground.rectTransform, new Color(1f, 0.84f, 0.18f, 0.98f), new Vector2(16f, 16f));

        var teamRailText = CreateUiText("Team Status Rail", root, 16, TextAnchor.UpperLeft);
        teamRailText.horizontalOverflow = HorizontalWrapMode.Wrap;
        teamRailText.verticalOverflow = VerticalWrapMode.Overflow;
        SetRect(teamRailText.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(360f, 180f), new Vector2(205f, 110f));

        var pingLogText = CreateUiText("Ping Log Text", root, 18, TextAnchor.MiddleCenter);
        pingLogText.horizontalOverflow = HorizontalWrapMode.Wrap;
        pingLogText.gameObject.SetActive(false);
        SetRect(pingLogText.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(420f, 30f), new Vector2(0f, 124f));

        var pingWheelText = CreateUiText("Ping Wheel Text", root, 18, TextAnchor.MiddleCenter);
        pingWheelText.horizontalOverflow = HorizontalWrapMode.Wrap;
        pingWheelText.verticalOverflow = VerticalWrapMode.Overflow;
        pingWheelText.gameObject.SetActive(false);
        SetRect(pingWheelText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(520f, 76f), new Vector2(0f, -8f));

        var captureRingBackground = CreateUiImage("Capture Progress Ring Background", root, new Color(0.05f, 0.06f, 0.07f, 0.6f));
        captureRingBackground.type = Image.Type.Filled;
        captureRingBackground.fillMethod = Image.FillMethod.Radial360;
        captureRingBackground.fillOrigin = (int)Image.Origin360.Top;
        captureRingBackground.fillAmount = 1f;
        SetRect(captureRingBackground.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(74f, 74f), new Vector2(0f, -92f));

        var captureRingFill = CreateUiImage("Capture Progress Ring Fill", root, new Color(1f, 0.9f, 0.18f, 0.92f));
        captureRingFill.type = Image.Type.Filled;
        captureRingFill.fillMethod = Image.FillMethod.Radial360;
        captureRingFill.fillOrigin = (int)Image.Origin360.Top;
        captureRingFill.fillClockwise = true;
        captureRingFill.fillAmount = 0f;
        captureRingFill.gameObject.SetActive(false);
        SetRect(captureRingFill.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(74f, 74f), new Vector2(0f, -92f));

        var heldStatusRing = CreateUiImage("Held Status Ring", root, new Color(1f, 0.18f, 0.24f, 0.72f));
        heldStatusRing.type = Image.Type.Filled;
        heldStatusRing.fillMethod = Image.FillMethod.Radial360;
        heldStatusRing.fillOrigin = (int)Image.Origin360.Top;
        heldStatusRing.fillAmount = 0f;
        heldStatusRing.gameObject.SetActive(false);
        SetRect(heldStatusRing.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(96f, 96f), new Vector2(0f, -92f));

        var rescueOpportunityRing = CreateUiImage("Rescue Opportunity Ring", root, new Color(0.18f, 0.95f, 0.46f, 0.72f));
        rescueOpportunityRing.type = Image.Type.Filled;
        rescueOpportunityRing.fillMethod = Image.FillMethod.Radial360;
        rescueOpportunityRing.fillOrigin = (int)Image.Origin360.Top;
        rescueOpportunityRing.fillAmount = 0f;
        rescueOpportunityRing.gameObject.SetActive(false);
        SetRect(rescueOpportunityRing.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(106f, 106f), new Vector2(0f, -92f));

        var spectatorText = CreateUiText("Spectator Text", root, 20, TextAnchor.UpperCenter);
        spectatorText.horizontalOverflow = HorizontalWrapMode.Wrap;
        spectatorText.verticalOverflow = VerticalWrapMode.Overflow;
        spectatorText.gameObject.SetActive(false);
        SetRect(spectatorText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(620f, 92f), new Vector2(0f, -86f));

        var flowBannerText = CreateUiText("Match Flow Banner", root, 34, TextAnchor.MiddleCenter);
        flowBannerText.horizontalOverflow = HorizontalWrapMode.Wrap;
        flowBannerText.verticalOverflow = VerticalWrapMode.Overflow;
        flowBannerText.gameObject.SetActive(false);
        SetRect(flowBannerText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(760f, 132f), new Vector2(0f, 154f));

        var hudComponent = hud.AddComponent<PlayerHud>();
        hudComponent.Configure(
            playerMotor,
            staminaFill,
            statusText,
            objectiveText,
            capturePoints,
            matchManager,
            playerCaptureAgent,
            spectatorCamera,
            spectatorText,
            teamRailText,
            captureRingFill,
            captureSystem,
            deadChannel,
            minimapBackground.rectTransform,
            localMinimapMarker,
            participantMinimapMarkers,
            capturePointMinimapMarkers,
            16f,
            heldStatusRing,
            rescueOpportunityRing,
            objectivePanelRows,
            objectivePanelPointIdTexts,
            objectivePanelOwnerIndicators,
            objectivePanelProgressFills,
            objectivePanelStateTexts,
            objectivePanelOccupantCountTexts,
            pingSystem,
            pingMinimapMarker,
            pingLogText,
            pingWheelText
        );

        var flowPresenter = hud.AddComponent<LocalMatchFlowPresenter>();
        flowPresenter.Configure(matchManager, flowBannerText, flowOverlay, matchParticipants, blueDefenderSpawn, redDefenderSpawn);
    }

    private static GameObject CreateHearingAi()
    {
        var ai = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        ai.name = "Hearing AI";
        ai.transform.position = new Vector3(5.5f, 1f, -1.5f);
        ai.GetComponent<MeshRenderer>().sharedMaterial = CreateMaterial("Assets/Art/Materials/AIRed.mat", new Color(1f, 0.28f, 0.38f));
        ai.AddComponent<AIHearingSensor>();
        return ai;
    }

    private static Material CreateMaterial(string path, Color color)
    {
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material != null)
        {
            return material;
        }

        material = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"))
        {
            color = color
        };
        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    private static Image CreateUiImage(string name, Transform parent, Color color)
    {
        var gameObject = new GameObject(name, typeof(RectTransform), typeof(Image));
        gameObject.transform.SetParent(parent, false);
        var image = gameObject.GetComponent<Image>();
        image.color = color;
        return image;
    }

    private static Image CreateMinimapMarker(string name, Transform parent, Color color, Vector2 size)
    {
        var marker = CreateUiImage(name, parent, color);
        SetRect(marker.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), size, Vector2.zero);
        marker.gameObject.SetActive(false);
        return marker;
    }

    private static Image[] CreateMinimapMarkers(string prefix, Transform parent, int count, Vector2 size)
    {
        var markers = new Image[count];
        for (var i = 0; i < markers.Length; i++)
        {
            markers[i] = CreateMinimapMarker($"{prefix} {i + 1}", parent, Color.white, size);
        }

        return markers;
    }

    private static Image[] CreateObjectivePanelRows(
        Transform parent,
        int count,
        out Text[] pointIdTexts,
        out Image[] ownerIndicators,
        out Image[] progressFills,
        out Text[] stateTexts,
        out Text[] occupantCountTexts)
    {
        var rows = new Image[count];
        pointIdTexts = new Text[count];
        ownerIndicators = new Image[count];
        progressFills = new Image[count];
        stateTexts = new Text[count];
        occupantCountTexts = new Text[count];

        for (var i = 0; i < count; i++)
        {
            var pointLabel = ((char)('A' + i)).ToString();
            var row = CreateUiImage($"Objective Point {pointLabel} Row", parent, new Color(0.08f, 0.1f, 0.12f, 0.76f));
            rows[i] = row;
            SetRect(row.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(394f, 48f), new Vector2(0f, 40f - i * 56f));

            pointIdTexts[i] = CreateUiText($"Objective Point {pointLabel} Id", row.rectTransform, 18, TextAnchor.MiddleCenter);
            pointIdTexts[i].text = pointLabel;
            SetRect(pointIdTexts[i].rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(42f, 30f), new Vector2(-172f, 0f));

            ownerIndicators[i] = CreateUiImage($"Objective Point {pointLabel} Owner", row.rectTransform, new Color(0.82f, 0.88f, 0.92f, 0.86f));
            SetRect(ownerIndicators[i].rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(12f, 30f), new Vector2(-140f, 0f));

            var progressBackground = CreateUiImage($"Objective Point {pointLabel} Progress Background", row.rectTransform, new Color(0f, 0f, 0f, 0.42f));
            SetRect(progressBackground.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(116f, 12f), new Vector2(-72f, 0f));

            progressFills[i] = CreateUiImage($"Objective Point {pointLabel} Progress Fill", progressBackground.rectTransform, new Color(0.82f, 0.88f, 0.92f, 0.86f));
            progressFills[i].type = Image.Type.Filled;
            progressFills[i].fillMethod = Image.FillMethod.Horizontal;
            progressFills[i].fillOrigin = (int)Image.OriginHorizontal.Left;
            progressFills[i].fillAmount = 0f;
            SetRect(progressFills[i].rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            stateTexts[i] = CreateUiText($"Objective Point {pointLabel} State", row.rectTransform, 14, TextAnchor.MiddleLeft);
            stateTexts[i].horizontalOverflow = HorizontalWrapMode.Wrap;
            SetRect(stateTexts[i].rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(136f, 36f), new Vector2(34f, 0f));

            occupantCountTexts[i] = CreateUiText($"Objective Point {pointLabel} Counts", row.rectTransform, 14, TextAnchor.MiddleRight);
            SetRect(occupantCountTexts[i].rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(96f, 30f), new Vector2(144f, 0f));
        }

        return rows;
    }

    private static Text CreateUiText(string name, Transform parent, int fontSize, TextAnchor alignment)
    {
        var gameObject = new GameObject(name, typeof(RectTransform), typeof(Text));
        gameObject.transform.SetParent(parent, false);
        var text = gameObject.GetComponent<Text>();
        text.font = LoadDefaultFont(fontSize);
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = Color.white;
        return text;
    }

    private static Font LoadDefaultFont(int fontSize)
    {
        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        return font != null ? font : Font.CreateDynamicFontFromOSFont("Arial", fontSize);
    }

    private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 sizeDelta, Vector2 anchoredPosition)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = sizeDelta;
        rect.anchoredPosition = anchoredPosition;
    }
}

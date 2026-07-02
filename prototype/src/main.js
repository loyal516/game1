import * as THREE from "three";
import "./styles.css";
import { GAME_PHASES, LEVEL_OBSTACLES, MAP_BOUNDS, OverthroneGame, PLAYER_STATES, RULES, TEAMS } from "./game.js";

const app = document.querySelector("#app");
const game = new OverthroneGame();

const state = {
  lastTime: performance.now(),
  yaw: Math.PI * 0.25,
  pitch: -0.22,
  moveX: 0,
  moveZ: 0,
  keys: new Set(),
  pointerLocked: false,
  mouseLookFallback: false,
  showScoreboard: false,
  captureHeld: false,
  transient: {
    useSlime: false,
    tackle: false
  }
};

const renderer = new THREE.WebGLRenderer({
  antialias: true,
  powerPreference: "high-performance",
  preserveDrawingBuffer: true
});
renderer.setPixelRatio(Math.min(window.devicePixelRatio, 2));
renderer.setSize(window.innerWidth, window.innerHeight);
renderer.shadowMap.enabled = true;
renderer.outputColorSpace = THREE.SRGBColorSpace;

const scene = new THREE.Scene();
scene.background = new THREE.Color(0xbde7ff);
scene.fog = new THREE.Fog(0xbde7ff, 48, 118);

const camera = new THREE.PerspectiveCamera(76, window.innerWidth / window.innerHeight, 0.1, 180);

const world = new THREE.Group();
scene.add(world);

const playerMeshes = new Map();
const captureMeshes = new Map();

const ui = document.createElement("div");
ui.className = "ui-root";
app.append(renderer.domElement, ui);
let lastUiKey = "";
let lastPlayingHudRefreshAt = 0;
const PLAYING_HUD_REFRESH_MS = 125;

const materials = {
  grass: new THREE.MeshStandardMaterial({ color: 0x8fd36f, roughness: 0.9 }),
  path: new THREE.MeshStandardMaterial({ color: 0xf1d891, roughness: 0.82 }),
  water: new THREE.MeshStandardMaterial({ color: 0x69c8ff, emissive: 0x124c7a, emissiveIntensity: 0.18, roughness: 0.35 }),
  wall: new THREE.MeshStandardMaterial({ color: 0x8bbf68, roughness: 0.8 }),
  rail: new THREE.MeshStandardMaterial({ color: 0xf8e7a2, roughness: 0.6 }),
  obstacle: new THREE.MeshStandardMaterial({ color: 0xd6ad6e, roughness: 0.82 }),
  wood: new THREE.MeshStandardMaterial({ color: 0x9a6840, roughness: 0.85 }),
  roof: new THREE.MeshStandardMaterial({ color: 0xff8ba0, roughness: 0.72 }),
  leaf: new THREE.MeshStandardMaterial({ color: 0x55b96d, roughness: 0.8 }),
  flower: new THREE.MeshStandardMaterial({ color: 0xffd166, roughness: 0.72 }),
  stone: new THREE.MeshStandardMaterial({ color: 0xc7cbd1, roughness: 0.88 }),
  skin: new THREE.MeshStandardMaterial({ color: 0xffcfaa, roughness: 0.62 }),
  eye: new THREE.MeshStandardMaterial({ color: 0x1d2733, roughness: 0.45 }),
  cheek: new THREE.MeshStandardMaterial({ color: 0xff91a7, roughness: 0.62 }),
  shadow: new THREE.MeshBasicMaterial({ color: 0x29452b, transparent: true, opacity: 0.22 }),
  blue: new THREE.MeshStandardMaterial({ color: TEAMS.blue.color, roughness: 0.52 }),
  red: new THREE.MeshStandardMaterial({ color: TEAMS.red.color, roughness: 0.52 }),
  neutral: new THREE.MeshStandardMaterial({ color: 0xb8c2b0, roughness: 0.75 }),
  king: new THREE.MeshStandardMaterial({ color: 0xf6c85f, emissive: 0x4f3300, emissiveIntensity: 0.2 }),
  slime: new THREE.MeshStandardMaterial({ color: 0x62d87d, emissive: 0x104b23, emissiveIntensity: 0.3, transparent: true, opacity: 0.82 }),
  held: new THREE.MeshStandardMaterial({ color: 0xefe6c8, roughness: 0.7 }),
  captured: new THREE.MeshStandardMaterial({ color: 0x40464a, transparent: true, opacity: 0.45 }),
  progress: new THREE.MeshBasicMaterial({ color: 0xffffff, transparent: true, opacity: 0.18 })
};

buildStaticWorld();
renderUi();

window.__OVERTHRONE_DEBUG__ = {
  getSnapshot: () => game.getSnapshot(),
  getViewState: () => ({
    yaw: state.yaw,
    pitch: state.pitch,
    pointerLocked: state.pointerLocked,
    mouseLookActive: isMouseLookActive(),
    fallbackLook: state.mouseLookFallback,
    fov: camera.fov
  }),
  simulateMouseLook: (movementX, movementY) => {
    applyMouseLook(movementX, movementY);
    return window.__OVERTHRONE_DEBUG__.getViewState();
  },
  samplePixels: () => {
    const gl = renderer.getContext();
    const canvas = renderer.domElement;
    const points = [
      [0.5, 0.5],
      [0.25, 0.5],
      [0.75, 0.5],
      [0.5, 0.25],
      [0.5, 0.75]
    ];
    const colors = [];
    for (const [rx, ry] of points) {
      const pixel = new Uint8Array(4);
      const x = Math.floor(canvas.width * rx);
      const y = Math.floor(canvas.height * ry);
      gl.readPixels(x, y, 1, 1, gl.RGBA, gl.UNSIGNED_BYTE, pixel);
      colors.push(Array.from(pixel));
    }
    return {
      width: canvas.width,
      height: canvas.height,
      colors
    };
  }
};

function buildStaticWorld() {
  const hemi = new THREE.HemisphereLight(0xfff8d6, 0x6f9f68, 1.55);
  scene.add(hemi);

  const sun = new THREE.DirectionalLight(0xfff2d2, 1.7);
  sun.position.set(-18, 32, -16);
  sun.castShadow = true;
  sun.shadow.mapSize.width = 2048;
  sun.shadow.mapSize.height = 2048;
  scene.add(sun);

  const floor = new THREE.Mesh(new THREE.BoxGeometry(76, 0.7, 61), materials.grass);
  floor.position.set(0, -0.38, 1.5);
  floor.receiveShadow = true;
  world.add(floor);

  addPath(0, -7, 64, 4.8, 0);
  addPath(0, 8, 44, 4.2, Math.PI / 2);
  addPath(-18, 4, 27, 3.4, -0.62);
  addPath(18, 4, 27, 3.4, 0.62);

  addBox(0, 1, MAP_BOUNDS.minZ - 1, 76, 2, 1.4, materials.wall);
  addBox(0, 1, MAP_BOUNDS.maxZ + 1, 76, 2, 1.4, materials.wall);
  addBox(MAP_BOUNDS.minX - 1, 1, 1, 1.4, 2, 61, materials.wall);
  addBox(MAP_BOUNDS.maxX + 1, 1, 1, 1.4, 2, 61, materials.wall);

  for (const obstacle of LEVEL_OBSTACLES) {
    addObstacle(obstacle);
  }

  addFountain(0, -1);
  addDecorations();

  for (const point of game.capturePoints.length ? game.capturePoints : []) {
    createCaptureMesh(point);
  }
}

function addPath(x, z, w, d, rotation) {
  const path = new THREE.Mesh(new THREE.BoxGeometry(w, 0.08, d), materials.path);
  path.position.set(x, 0.03, z);
  path.rotation.y = rotation;
  path.receiveShadow = true;
  world.add(path);
  return path;
}

function addBox(x, y, z, w, h, d, material) {
  const mesh = new THREE.Mesh(new THREE.BoxGeometry(w, h, d), material);
  mesh.position.set(x, y, z);
  mesh.castShadow = true;
  mesh.receiveShadow = true;
  world.add(mesh);
  return mesh;
}

function addObstacle({ kind, x, y, z, w, h, d }) {
  if (kind === "stall") {
    addBox(x, y * 0.75, z, w, h * 0.65, d, materials.wood);
    const roof = new THREE.Mesh(new THREE.ConeGeometry(w * 0.62, 1.8, 4), materials.roof);
    roof.position.set(x, y + h * 0.55, z);
    roof.rotation.y = Math.PI / 4;
    roof.castShadow = true;
    world.add(roof);
    addLantern(x - w * 0.36, z + d * 0.65);
    addLantern(x + w * 0.36, z + d * 0.65);
    return;
  }

  if (kind === "hedge") {
    addBox(x, y, z, w, h, d, materials.wall);
    for (let i = -2; i <= 2; i += 1) {
      addTree(x + i * 0.4, z - d * 0.4, 0.78);
      addTree(x + i * 0.4, z + d * 0.4, 0.78);
    }
    return;
  }

  if (kind === "tower") {
    const base = new THREE.Mesh(new THREE.CylinderGeometry(w * 0.42, w * 0.5, h, 8), materials.obstacle);
    base.position.set(x, y, z);
    base.castShadow = true;
    base.receiveShadow = true;
    world.add(base);
    const cap = new THREE.Mesh(new THREE.ConeGeometry(w * 0.6, 2.2, 8), materials.roof);
    cap.position.set(x, y + h * 0.65, z);
    cap.castShadow = true;
    world.add(cap);
    return;
  }

  if (kind === "column") {
    const column = new THREE.Mesh(new THREE.CylinderGeometry(w * 0.3, w * 0.38, h, 12), materials.stone);
    column.position.set(x, y, z);
    column.castShadow = true;
    column.receiveShadow = true;
    world.add(column);
    addMushroom(x + 1.4, z - 1.2);
    return;
  }

  addBox(x, y, z, w, h, d, materials.obstacle);
}

function addFountain(x, z) {
  const group = new THREE.Group();
  group.position.set(x, 0.08, z);

  const bowl = new THREE.Mesh(new THREE.CylinderGeometry(4, 4.4, 0.55, 32), materials.stone);
  bowl.position.y = 0.28;
  const water = new THREE.Mesh(new THREE.CylinderGeometry(3.4, 3.4, 0.08, 32), materials.water);
  water.position.y = 0.6;
  const moon = new THREE.Mesh(new THREE.SphereGeometry(0.65, 16, 12), materials.rail);
  moon.position.set(0, 2.4, 0);

  group.add(bowl, water, moon);
  world.add(group);
}

function addDecorations() {
  const treeSpots = [
    [-30, -19], [-27, 20], [-12, 22], [13, 23], [29, -18], [30, 20],
    [-31, 3], [31, 5], [-17, -4], [18, -5], [-5, -23], [8, -23]
  ];
  for (const [x, z] of treeSpots) {
    addTree(x, z, 1);
  }

  for (let i = 0; i < 22; i += 1) {
    const x = -31 + ((i * 7) % 62);
    const z = -22 + ((i * 11) % 47);
    addFlower(x, z);
  }

  addCloud(-16, 10, 8);
  addCloud(19, 16, 7);
}

function addTree(x, z, scale = 1) {
  const trunk = new THREE.Mesh(new THREE.CylinderGeometry(0.26 * scale, 0.34 * scale, 1.5 * scale, 8), materials.wood);
  trunk.position.set(x, 0.75 * scale, z);
  trunk.castShadow = true;
  const top = new THREE.Mesh(new THREE.SphereGeometry(1.15 * scale, 12, 10), materials.leaf);
  top.position.set(x, 1.8 * scale, z);
  top.scale.y = 1.12;
  top.castShadow = true;
  world.add(trunk, top);
}

function addMushroom(x, z) {
  const stem = new THREE.Mesh(new THREE.CylinderGeometry(0.18, 0.24, 0.8, 8), materials.rail);
  stem.position.set(x, 0.4, z);
  const cap = new THREE.Mesh(new THREE.SphereGeometry(0.62, 12, 8), materials.roof);
  cap.position.set(x, 0.9, z);
  cap.scale.y = 0.45;
  world.add(stem, cap);
}

function addFlower(x, z) {
  const flower = new THREE.Mesh(new THREE.SphereGeometry(0.18, 8, 6), materials.flower);
  flower.position.set(x, 0.18, z);
  world.add(flower);
}

function addLantern(x, z) {
  const post = new THREE.Mesh(new THREE.CylinderGeometry(0.08, 0.08, 1.7, 8), materials.wood);
  post.position.set(x, 0.85, z);
  const light = new THREE.Mesh(new THREE.SphereGeometry(0.25, 10, 8), materials.flower);
  light.position.set(x, 1.72, z);
  world.add(post, light);
}

function addCloud(x, z, size) {
  const cloud = new THREE.Group();
  cloud.position.set(x, 6.4, z);
  for (let i = 0; i < 4; i += 1) {
    const puff = new THREE.Mesh(new THREE.SphereGeometry(size * (0.13 + i * 0.015), 12, 8), materials.rail);
    puff.position.set((i - 1.5) * size * 0.18, Math.sin(i) * 0.15, 0);
    puff.scale.y = 0.58;
    cloud.add(puff);
  }
  world.add(cloud);
}

function resetDynamicWorld() {
  for (const mesh of playerMeshes.values()) {
    world.remove(mesh);
  }
  playerMeshes.clear();

  for (const group of captureMeshes.values()) {
    world.remove(group);
  }
  captureMeshes.clear();

  for (const point of game.capturePoints) {
    createCaptureMesh(point);
  }
}

function createCaptureMesh(point) {
  const group = new THREE.Group();
  group.position.set(point.x, 0.08, point.z);

  const ring = new THREE.Mesh(
    new THREE.CylinderGeometry(RULES.captureRadius, RULES.captureRadius, 0.12, 56, 1, true),
    new THREE.MeshBasicMaterial({ color: 0xcfd8bd, transparent: true, opacity: 0.2, side: THREE.DoubleSide })
  );
  ring.position.y = 0.08;

  const pillar = new THREE.Mesh(new THREE.CylinderGeometry(0.35, 0.55, 5.2, 18), materials.neutral.clone());
  pillar.position.y = 2.6;
  pillar.castShadow = true;

  const label = makeSprite(point.id);
  label.position.set(0, 6, 0);

  group.add(ring, pillar, label);
  group.userData = { ring, pillar, label };
  captureMeshes.set(point.id, group);
  world.add(group);
}

function makeSprite(text) {
  const canvas = document.createElement("canvas");
  canvas.width = 128;
  canvas.height = 128;
  const context = canvas.getContext("2d");
  context.fillStyle = "#f4f0da";
  context.font = "700 72px ui-sans-serif";
  context.textAlign = "center";
  context.textBaseline = "middle";
  context.fillText(text, 64, 68);
  const texture = new THREE.CanvasTexture(canvas);
  const material = new THREE.SpriteMaterial({ map: texture, transparent: true });
  const sprite = new THREE.Sprite(material);
  sprite.scale.set(3.3, 3.3, 1);
  return sprite;
}

function createPlayerMesh(player) {
  const group = new THREE.Group();
  const shadow = new THREE.Mesh(new THREE.CircleGeometry(0.78, 24), materials.shadow);
  shadow.rotation.x = -Math.PI / 2;
  shadow.position.y = 0.03;

  const leftFoot = new THREE.Mesh(new THREE.SphereGeometry(0.28, 10, 8), materials[player.team]);
  leftFoot.position.set(-0.28, 0.22, 0.12);
  leftFoot.scale.set(1, 0.55, 1.2);
  const rightFoot = leftFoot.clone();
  rightFoot.position.x = 0.28;

  const body = new THREE.Mesh(new THREE.SphereGeometry(0.74, 16, 12), materials[player.team]);
  body.position.y = 0.9;
  body.scale.set(0.9, 1.05, 0.75);
  body.castShadow = true;

  const head = new THREE.Mesh(new THREE.SphereGeometry(0.56, 16, 12), materials.skin);
  head.position.y = 1.62;
  head.castShadow = true;

  const hair = new THREE.Mesh(new THREE.SphereGeometry(0.58, 12, 8), materials[player.team]);
  hair.position.set(0, 1.84, -0.02);
  hair.scale.set(1, 0.42, 0.9);
  hair.castShadow = true;

  const leftEye = new THREE.Mesh(new THREE.SphereGeometry(0.06, 8, 6), materials.eye);
  leftEye.position.set(-0.18, 1.64, 0.49);
  const rightEye = leftEye.clone();
  rightEye.position.x = 0.18;

  const leftCheek = new THREE.Mesh(new THREE.SphereGeometry(0.07, 8, 6), materials.cheek);
  leftCheek.position.set(-0.32, 1.53, 0.47);
  leftCheek.scale.y = 0.55;
  const rightCheek = leftCheek.clone();
  rightCheek.position.x = 0.32;

  const scarf = new THREE.Mesh(new THREE.TorusGeometry(0.48, 0.055, 8, 28), materials.rail);
  scarf.position.y = 1.25;
  scarf.rotation.x = Math.PI / 2;

  const crown = new THREE.Mesh(new THREE.ConeGeometry(0.45, 0.42, 5), materials.king);
  crown.position.y = 2.27;
  crown.visible = false;

  const heldRing = new THREE.Mesh(new THREE.TorusGeometry(0.86, 0.055, 8, 32), materials.held);
  heldRing.position.y = 0.25;
  heldRing.rotation.x = Math.PI / 2;
  heldRing.visible = false;

  group.add(shadow, leftFoot, rightFoot, body, head, hair, leftEye, rightEye, leftCheek, rightCheek, scarf, crown, heldRing);
  group.userData = { body, head, hair, scarf, crown, heldRing, leftFoot, rightFoot };
  world.add(group);
  playerMeshes.set(player.id, group);
  return group;
}

function updateWorld() {
  for (const player of game.players) {
    const mesh = playerMeshes.get(player.id) ?? createPlayerMesh(player);
    mesh.position.set(player.x, 0, player.z);
    mesh.rotation.y = player.yaw;
    mesh.visible = true;

    const { body, head, hair, scarf, crown, heldRing, leftFoot, rightFoot } = mesh.userData;
    body.material =
      player.state === PLAYER_STATES.captured
        ? materials.captured
        : player.slimeTimer > 0
          ? materials.slime
          : player.state === PLAYER_STATES.held
            ? materials.held
            : materials[player.team];
    hair.material = player.slimeTimer > 0 ? materials.slime : materials[player.team];
    scarf.material = player.state === PLAYER_STATES.king ? materials.king : materials.rail;
    head.material = player.state === PLAYER_STATES.captured ? materials.captured : materials.skin;
    leftFoot.material = body.material;
    rightFoot.material = body.material;
    crown.visible = player.state === PLAYER_STATES.king;
    heldRing.visible = player.state === PLAYER_STATES.held;
    const bob = Math.sin(performance.now() / 130 + player.x) * 0.045;
    mesh.position.y = player.state === PLAYER_STATES.captured ? 0.05 : bob;
    mesh.scale.setScalar(player.slimeTimer > 0 ? 0.78 : 1);
  }

  for (const point of game.capturePoints) {
    const mesh = captureMeshes.get(point.id);
    if (!mesh) {
      continue;
    }
    const ownerColor = point.owner ? TEAMS[point.owner].color : point.progressTeam ? TEAMS[point.progressTeam].color : 0xcfd8bd;
    const pillarMaterial = mesh.userData.pillar.material;
    pillarMaterial.color.setHex(ownerColor);
    pillarMaterial.emissive.setHex(ownerColor);
    pillarMaterial.emissiveIntensity = point.owner ? 0.22 : 0.08;
    mesh.userData.ring.material.color.setHex(ownerColor);
    mesh.userData.ring.material.opacity = point.contested ? 0.48 : 0.2 + point.progress / 260;
    mesh.scale.setScalar(point.contested ? 1.04 + Math.sin(performance.now() / 80) * 0.02 : 1);
  }
}

function updateCamera() {
  const player = game.localPlayer;
  if (!player) {
    camera.position.set(0, 32, 34);
    camera.lookAt(0, 0, 0);
    return;
  }

  const base = new THREE.Vector3(player.x, 1.2, player.z);
  const viewForward = new THREE.Vector3(Math.sin(state.yaw), 0, Math.cos(state.yaw));
  const distance = player.slimeTimer > 0 ? 12.5 : 11;
  const height = THREE.MathUtils.lerp(3.8, 8.4, THREE.MathUtils.clamp((state.pitch + 0.75) / 1.35, 0, 1));
  const target = base.clone().add(new THREE.Vector3(0, player.state === PLAYER_STATES.held ? 1.1 : 1.8, 0));
  const desired = target.clone().add(viewForward.clone().multiplyScalar(-distance)).add(new THREE.Vector3(0, height, 0));

  camera.position.lerp(desired, 0.18);
  camera.lookAt(target.clone().add(viewForward.multiplyScalar(1.6)));
  camera.fov = player.slimeTimer > 0 ? 82 : 76;
  camera.updateProjectionMatrix();
}

function renderUi() {
  const snapshot = game.getSnapshot();
  const phase = snapshot.phase;
  const uiKey =
    phase === GAME_PHASES.playing
      ? `${phase}:${isMouseLookActive()}:${state.pointerLocked}:${state.showScoreboard}`
      : `${phase}:${snapshot.selectedTeam}:${snapshot.aiCount}:${snapshot.matchWinner ?? ""}:${snapshot.matchReason}`;

  if (uiKey === lastUiKey) {
    if (phase === GAME_PHASES.playing) {
      refreshPlayingHud(snapshot);
    }
    return;
  }
  lastUiKey = uiKey;

  if (phase === GAME_PHASES.menu) {
    ui.innerHTML = `
      <main class="screen screen-menu">
        <section class="menu-panel">
          <p class="eyebrow">Overthrone Prototype</p>
          <h1>왕좌를 먼저 쥐어</h1>
          <p class="lead">점령하면 사냥꾼, 빼앗기면 도망자. 지금은 싱글플레이 AI 테스트 빌드야.</p>
          <button class="primary" data-action="start-flow">프로토타입 시작</button>
          <div class="status-row">
            <span>Build 0.1</span>
            <span>Local</span>
            <span>AI enabled</span>
          </div>
        </section>
      </main>
    `;
    return;
  }

  if (phase === GAME_PHASES.room) {
    ui.innerHTML = `
      <main class="screen">
        <section class="setup-panel">
          <p class="eyebrow">Room</p>
          <h2>달빛 정원 테스트 룸</h2>
          <div class="room-grid">
            <div>
              <span class="meta-label">Mode</span>
              <strong>Classic Solo Sim</strong>
            </div>
            <div>
              <span class="meta-label">Map</span>
              <strong>Moonlit Garden</strong>
            </div>
            <div>
              <span class="meta-label">Rule</span>
              <strong>3 Capture Points</strong>
            </div>
          </div>
          <button class="primary" data-action="make-room">방 만들기</button>
        </section>
      </main>
    `;
    return;
  }

  if (phase === GAME_PHASES.faction) {
    ui.innerHTML = `
      <main class="screen">
        <section class="setup-panel setup-wide">
          <p class="eyebrow">Faction</p>
          <h2>진영과 AI 밀도를 정해</h2>
          <div class="choice-grid">
            ${teamCard("blue", snapshot.selectedTeam)}
            ${teamCard("red", snapshot.selectedTeam)}
          </div>
          <div class="segmented" role="group" aria-label="AI 플레이어 수">
            ${[3, 5, 7].map((count) => `<button class="${snapshot.aiCount === count ? "selected" : ""}" data-ai="${count}">AI ${count}명</button>`).join("")}
          </div>
          <button class="primary" data-action="start-match">게임 시작</button>
        </section>
      </main>
    `;
    return;
  }

  if (phase === GAME_PHASES.result) {
    const resultColor = snapshot.matchWinner ? TEAMS[snapshot.matchWinner].css : "#f4f0da";
    const resultTitle = snapshot.matchWinner ? `${TEAMS[snapshot.matchWinner].label} 승리` : "무승부";
    ui.innerHTML = `
      <main class="screen">
        <section class="setup-panel">
          <p class="eyebrow">Result</p>
          <h2 style="color:${resultColor}">${resultTitle}</h2>
          <p class="lead">${snapshot.matchReason}</p>
          ${scoreboard(snapshot)}
          <button class="primary" data-action="restart">다시 시작</button>
        </section>
      </main>
    `;
    return;
  }

  ui.innerHTML = `
    <section class="hud" data-testid="hud">
      <div class="topbar">
        <div class="brand">Overthrone</div>
        <div class="timer" data-ui="timer">${formatTime(snapshot.timeLeft)}</div>
        <div class="team-objective">${TEAMS[snapshot.selectedTeam].objective}</div>
      </div>
      <div class="capture-strip" data-ui="capture-strip">
        ${snapshot.capturePoints.map(capturePill).join("")}
      </div>
      <div class="left-panel">
        <div data-ui="local-status">
          ${localStatus(snapshot)}
        </div>
        <div class="event-log" data-ui="event-log">
          ${snapshot.events.map((event) => `<p>${event.message}</p>`).join("")}
        </div>
      </div>
      <div class="right-panel" data-ui="right-panel">
        ${minimap(snapshot)}
        ${teamPanel(snapshot)}
      </div>
      <div class="reticle"></div>
      ${actionBar(snapshot)}
      <div class="control-ribbon" data-ui="control-ribbon">
        ${controlRibbon()}
      </div>
      ${isMouseLookActive() ? "" : `<button class="pointer-card" data-action="lock-pointer">클릭해서 마우스 조작 시작</button>`}
      ${state.showScoreboard ? `<aside class="scoreboard">${scoreboard(snapshot)}</aside>` : ""}
    </section>
  `;
  refreshPlayingHud(snapshot, true);
}

function refreshPlayingHud(snapshot, force = false) {
  const now = performance.now();
  if (!force && now - lastPlayingHudRefreshAt < PLAYING_HUD_REFRESH_MS) {
    return;
  }
  lastPlayingHudRefreshAt = now;

  setText("[data-ui='timer']", formatTime(snapshot.timeLeft));
  setHtml("[data-ui='capture-strip']", snapshot.capturePoints.map(capturePill).join(""));
  setHtml("[data-ui='local-status']", localStatus(snapshot));
  setHtml("[data-ui='event-log']", snapshot.events.map((event) => `<p>${event.message}</p>`).join(""));
  setHtml("[data-ui='right-panel']", `${minimap(snapshot)}${teamPanel(snapshot)}`);
  setHtml("[data-ui='action-bar']", actionBarButtons(snapshot));
  setHtml("[data-ui='control-ribbon']", controlRibbon());
}

function setText(selector, text) {
  const element = ui.querySelector(selector);
  if (element && element.textContent !== text) {
    element.textContent = text;
  }
}

function setHtml(selector, html) {
  const element = ui.querySelector(selector);
  if (element && element.innerHTML !== html) {
    element.innerHTML = html;
  }
}

function controlRibbon() {
  const lookActive = isMouseLookActive();
  return `
    <span>${lookActive ? "마우스 시점" : "클릭 1회"}</span> ${lookActive ? "활성화" : "조작 시작"}
    <span>WASD / 방향키</span> 시점 기준 이동
    <span>R</span> 슬라임
    <span>E</span> 덮치기
    <span>F 길게</span> 포획
  `;
}

function teamCard(teamId, selectedTeam) {
  const team = TEAMS[teamId];
  return `
    <button class="team-card ${selectedTeam === teamId ? "selected" : ""}" data-team="${teamId}" aria-label="${team.label} 진영">
      <span class="team-swatch" style="background:${team.css}"></span>
      <strong>${team.label} 진영</strong>
      <small>${team.objective}</small>
    </button>
  `;
}

function capturePill(point) {
  const color = point.owner ? TEAMS[point.owner].css : point.progressTeam ? TEAMS[point.progressTeam].css : "#b9c1ad";
  return `
    <div class="capture-pill" style="--point-color:${color}">
      <span>${point.id}</span>
      <strong>${point.owner ? TEAMS[point.owner].label : point.contested ? "경쟁중" : "중립"}</strong>
      <div class="bar"><i style="width:${Math.round(point.progress)}%"></i></div>
    </div>
  `;
}

function localStatus(snapshot) {
  const player = snapshot.localPlayer;
  if (!player) {
    return "";
  }
  const stateLabel = {
    neutral: "중립",
    attacker: "공격자",
    king: "왕",
    held: "붙들림",
    captured: "포획됨"
  }[player.state];
  const slimeReady = player.slimeCooldown <= 0 && player.stamina >= RULES.slimeCost;
  const captureCharge = player.captureCharge > 0 ? Math.round((player.captureCharge / RULES.captureHoldSeconds) * 100) : 0;

  return `
    <div class="status-card">
      <div class="status-title">
        <span style="background:${TEAMS[player.team].css}"></span>
        <strong>${stateLabel}</strong>
      </div>
      <label>Stamina <i>${Math.round(player.stamina)}</i></label>
      <div class="meter"><b style="width:${Math.round(player.stamina)}%"></b></div>
      <label>Slime <i>${player.slimeTimer > 0 ? `${player.slimeTimer.toFixed(1)}s` : slimeReady ? "Ready" : `${player.slimeCooldown.toFixed(1)}s`}</i></label>
      <div class="meter slime"><b style="width:${player.slimeTimer > 0 ? (player.slimeTimer / RULES.slimeDuration) * 100 : slimeReady ? 100 : Math.max(0, 100 - (player.slimeCooldown / RULES.slimeCooldown) * 100)}%"></b></div>
      ${captureCharge ? `<label>Capture <i>${captureCharge}%</i></label><div class="meter capture"><b style="width:${captureCharge}%"></b></div>` : ""}
    </div>
  `;
}

function actionBar(snapshot) {
  return `
    <div class="action-bar" data-testid="action-bar" data-ui="action-bar">
      ${actionBarButtons(snapshot)}
    </div>
  `;
}

function actionBarButtons(snapshot) {
  const player = snapshot.localPlayer;
  if (!player) {
    return "";
  }

  const slimeLocked = player.stamina < RULES.slimeCost || player.slimeCooldown > 0 || player.state === PLAYER_STATES.captured;
  const tackleLocked =
    player.tackleCooldown > 0 ||
    player.stamina < RULES.tackleCost ||
    (player.state !== PLAYER_STATES.attacker && player.state !== PLAYER_STATES.king);
  const captureLocked = player.state !== PLAYER_STATES.king || !player.holdingId;
  const captureProgress = Math.min(100, Math.round((player.captureCharge / RULES.captureHoldSeconds) * 100));

  return `
    ${actionButton({
      key: "R",
      title: "슬라임",
      detail: player.slimeTimer > 0 ? `${player.slimeTimer.toFixed(1)}s 변신중` : slimeLocked ? `${Math.max(0, player.slimeCooldown).toFixed(1)}s` : "탈출/가속",
      action: "use-slime",
      locked: slimeLocked && player.slimeTimer <= 0,
      meter: player.slimeTimer > 0 ? (player.slimeTimer / RULES.slimeDuration) * 100 : slimeLocked ? Math.max(0, 100 - (player.slimeCooldown / RULES.slimeCooldown) * 100) : 100
    })}
    ${actionButton({
      key: "E",
      title: "덮치기",
      detail: tackleLocked ? player.state === PLAYER_STATES.neutral ? "공격자/왕 전용" : `${player.tackleCooldown.toFixed(1)}s` : "붙들기",
      action: "tackle",
      locked: tackleLocked,
      meter: tackleLocked ? Math.max(0, 100 - (player.tackleCooldown / RULES.tackleCooldown) * 100) : 100
    })}
    ${actionButton({
      key: "F",
      title: "포획",
      detail: captureLocked ? "붙든 대상 필요" : `${captureProgress}%`,
      action: "capture",
      locked: captureLocked,
      hold: true,
      meter: captureProgress
    })}
  `;
}

function actionButton({ key, title, detail, action, locked, hold = false, meter }) {
  return `
    <button class="action-button ${locked ? "locked" : "ready"}" ${hold ? `data-hold-action="${action}"` : `data-action="${action}"`} aria-label="${title}">
      <span class="keycap">${key}</span>
      <strong>${title}</strong>
      <small>${detail}</small>
      <i style="width:${Math.round(meter)}%"></i>
    </button>
  `;
}

function minimap(snapshot) {
  const toX = (value) => ((value - MAP_BOUNDS.minX) / (MAP_BOUNDS.maxX - MAP_BOUNDS.minX)) * 100;
  const toZ = (value) => ((value - MAP_BOUNDS.minZ) / (MAP_BOUNDS.maxZ - MAP_BOUNDS.minZ)) * 100;
  const players = snapshot.players
    .filter((player) => player.state !== PLAYER_STATES.captured)
    .map((player) => {
      const localClass = player.isLocal ? " local" : "";
      return `<i class="mini-player${localClass}" style="--x:${toX(player.x)}%;--z:${toZ(player.z)}%;--color:${TEAMS[player.team].css}"></i>`;
    })
    .join("");
  const points = snapshot.capturePoints
    .map((point) => {
      const color = point.owner ? TEAMS[point.owner].css : point.progressTeam ? TEAMS[point.progressTeam].css : "#f6d27a";
      return `<b class="mini-point" style="--x:${toX(point.x)}%;--z:${toZ(point.z)}%;--color:${color}">${point.id}</b>`;
    })
    .join("");

  return `
    <div class="minimap" data-testid="minimap" aria-label="미니맵">
      <div class="minimap-head">
        <strong>Map</strong>
        <small>${snapshot.players.length}명</small>
      </div>
      <div class="minimap-field">
        <span class="mini-lane horizontal"></span>
        <span class="mini-lane vertical"></span>
        ${points}
        ${players}
      </div>
    </div>
  `;
}

function teamPanel(snapshot) {
  return `
    <div class="team-panel">
      ${Object.values(TEAMS).map((team) => {
        const pressure = snapshot.teamPressure[team.id];
        const alive = snapshot.players.filter((player) => player.team === team.id && player.state !== PLAYER_STATES.captured).length;
        const role = pressure.role === "ruling" ? "왕권" : pressure.role === "pressing" ? "압박" : "은신";
        return `
          <div class="team-line">
            <span style="background:${team.css}"></span>
            <strong>${team.label}</strong>
            <small>${role} · ${pressure.owned}/3 · ${alive}명</small>
          </div>
        `;
      }).join("")}
    </div>
  `;
}

function scoreboard(snapshot) {
  return `
    <div class="board">
      ${snapshot.players
        .slice()
        .sort((a, b) => a.team.localeCompare(b.team) || b.captureScore - a.captureScore)
        .map((player) => `
          <div class="board-row">
            <span style="background:${TEAMS[player.team].css}"></span>
            <strong>${player.name}</strong>
            <small>${player.state}</small>
            <em>${player.captureScore} 포획 / ${player.rescueScore} 구출</em>
          </div>
        `)
        .join("")}
    </div>
  `;
}

function formatTime(seconds) {
  const mins = Math.floor(seconds / 60);
  const secs = Math.floor(seconds % 60);
  return `${mins}:${secs.toString().padStart(2, "0")}`;
}

function tick(now) {
  const dt = Math.min(0.05, (now - state.lastTime) / 1000);
  state.lastTime = now;

  updateInputAxes();
  game.update(dt, {
    moveX: state.moveX,
    moveZ: state.moveZ,
    sprint: state.keys.has("ShiftLeft") || state.keys.has("ShiftRight"),
    yaw: state.yaw,
    useSlime: state.transient.useSlime,
    tackle: state.transient.tackle,
    capture: state.keys.has("KeyF") || state.captureHeld
  });

  state.transient.useSlime = false;
  state.transient.tackle = false;

  updateWorld();
  updateCamera();
  renderer.render(scene, camera);
  renderUi();

  requestAnimationFrame(tick);
}

function updateInputAxes() {
  const left = state.keys.has("KeyA") || state.keys.has("ArrowLeft") ? -1 : 0;
  const right = state.keys.has("KeyD") || state.keys.has("ArrowRight") ? 1 : 0;
  const forward = state.keys.has("KeyW") || state.keys.has("ArrowUp") ? 1 : 0;
  const backward = state.keys.has("KeyS") || state.keys.has("ArrowDown") ? -1 : 0;
  state.moveX = left + right;
  state.moveZ = forward + backward;
}

document.addEventListener("keydown", (event) => {
  state.keys.add(event.code);
  if (event.code === "Escape") {
    state.mouseLookFallback = false;
    lastUiKey = "";
  }
  if (event.code === "KeyR") {
    state.transient.useSlime = true;
  }
  if (event.code === "KeyE") {
    state.transient.tackle = true;
  }
  if (event.code === "Tab") {
    event.preventDefault();
    state.showScoreboard = true;
  }
  if (["ArrowUp", "ArrowDown", "ArrowLeft", "ArrowRight", "Space"].includes(event.code)) {
    event.preventDefault();
  }
});

document.addEventListener("keyup", (event) => {
  state.keys.delete(event.code);
  if (event.code === "Tab") {
    state.showScoreboard = false;
  }
});

document.addEventListener("mousemove", (event) => {
  if (!isMouseLookActive()) {
    return;
  }
  applyMouseLook(event.movementX, event.movementY);
});

function applyMouseLook(movementX, movementY) {
  state.yaw -= movementX * 0.0024;
  state.pitch = Math.max(-0.75, Math.min(0.6, state.pitch - movementY * 0.0018));
}

function isMouseLookActive() {
  return state.pointerLocked || state.mouseLookFallback;
}

function startMouseLook() {
  if (isMouseLookActive()) {
    return;
  }

  try {
    const lockResult = renderer.domElement.requestPointerLock();
    if (lockResult && typeof lockResult.catch === "function") {
      lockResult.catch(enableMouseLookFallback);
    }
  } catch {
    enableMouseLookFallback();
    return;
  }

  window.setTimeout(() => {
    if (!state.pointerLocked && document.pointerLockElement !== renderer.domElement) {
      enableMouseLookFallback();
    }
  }, 180);
}

function enableMouseLookFallback() {
  if (state.pointerLocked) {
    return;
  }
  state.mouseLookFallback = true;
  lastUiKey = "";
}

document.addEventListener("pointerlockchange", () => {
  state.pointerLocked = document.pointerLockElement === renderer.domElement;
  if (state.pointerLocked) {
    state.mouseLookFallback = false;
  }
  lastUiKey = "";
});

document.addEventListener("pointerlockerror", () => {
  enableMouseLookFallback();
});

renderer.domElement.addEventListener("click", () => {
  if (game.phase === GAME_PHASES.playing && !isMouseLookActive()) {
    startMouseLook();
  }
});

ui.addEventListener("click", (event) => {
  const target = event.target.closest("button");
  if (!target) {
    return;
  }

  const action = target.dataset.action;
  if (action === "start-flow") {
    game.openRoom();
  }
  if (action === "make-room") {
    game.openFactionSelect();
  }
  if (target.dataset.team) {
    game.setTeam(target.dataset.team);
  }
  if (target.dataset.ai) {
    game.setAiCount(Number(target.dataset.ai));
  }
  if (action === "start-match") {
    game.startMatch();
    resetDynamicWorld();
    state.yaw = game.localPlayer?.yaw ?? state.yaw;
    state.pitch = -0.22;
    state.captureHeld = false;
    state.mouseLookFallback = false;
    lastUiKey = "";
  }
  if (action === "restart") {
    game.phase = GAME_PHASES.menu;
    state.mouseLookFallback = false;
    lastUiKey = "";
  }
  if (action === "use-slime") {
    state.transient.useSlime = true;
  }
  if (action === "tackle") {
    state.transient.tackle = true;
  }
  if (action === "lock-pointer") {
    startMouseLook();
  }
});

ui.addEventListener("pointerdown", (event) => {
  const target = event.target.closest("[data-hold-action='capture']");
  if (target) {
    state.captureHeld = true;
  }
});

window.addEventListener("pointerup", () => {
  state.captureHeld = false;
});

window.addEventListener("resize", () => {
  renderer.setSize(window.innerWidth, window.innerHeight);
  camera.aspect = window.innerWidth / window.innerHeight;
  camera.updateProjectionMatrix();
});

requestAnimationFrame(tick);

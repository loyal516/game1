export const TEAMS = {
  blue: {
    id: "blue",
    label: "Blue",
    color: 0x4aa8ff,
    css: "#4aa8ff",
    spawn: { x: -23, z: -17 },
    objective: "거점을 되찾고 붙들린 적을 왕으로 포획"
  },
  red: {
    id: "red",
    label: "Red",
    color: 0xff5f7a,
    css: "#ff5f7a",
    spawn: { x: 23, z: 17 },
    objective: "왕권을 끊고 끝까지 살아남기"
  }
};

export const PLAYER_STATES = {
  neutral: "neutral",
  attacker: "attacker",
  king: "king",
  held: "held",
  captured: "captured"
};

export const GAME_PHASES = {
  menu: "menu",
  room: "room",
  faction: "faction",
  playing: "playing",
  result: "result"
};

export const CAPTURE_POINTS = [
  { id: "A", label: "Sky Garden", x: -18, z: 12, type: "garden" },
  { id: "B", label: "Moon Fountain", x: 0, z: -1, type: "fountain" },
  { id: "C", label: "Candy Gate", x: 18, z: 12, type: "market" }
];

export const MAP_BOUNDS = {
  minX: -36,
  maxX: 36,
  minZ: -27,
  maxZ: 30
};

export const LEVEL_OBSTACLES = [
  { id: "blue-market", kind: "stall", x: -13, y: 1.15, z: -11, w: 9, h: 2.3, d: 3 },
  { id: "red-market", kind: "stall", x: 13, y: 1.15, z: -12, w: 9, h: 2.3, d: 3 },
  { id: "west-hedge", kind: "hedge", x: -23, y: 1.0, z: 1, w: 4, h: 2, d: 11 },
  { id: "east-hedge", kind: "hedge", x: 23, y: 1.0, z: 1, w: 4, h: 2, d: 11 },
  { id: "north-kiosk", kind: "tower", x: 0, y: 1.6, z: 16, w: 9, h: 3.2, d: 4 },
  { id: "moon-column-left", kind: "column", x: -6, y: 2.4, z: 5, w: 3, h: 4.8, d: 3 },
  { id: "moon-column-right", kind: "column", x: 6, y: 2.4, z: 5, w: 3, h: 4.8, d: 3 },
  { id: "south-bridge", kind: "bridge", x: 0, y: 0.65, z: -20, w: 28, h: 1.3, d: 2.5 }
];

export const RULES = {
  matchSeconds: 420,
  playerRadius: 0.65,
  captureRadius: 5,
  captureRates: {
    one: 5,
    two: 8,
    many: 12,
    decay: 3
  },
  fullCaptureHoldSeconds: 30,
  baseSpeed: 7,
  sprintSpeed: 10,
  kingSpeedBonus: 1.1,
  slimeSpeedBonus: 1.4,
  slimeDuration: 3,
  slimeCooldown: 15,
  slimeCost: 50,
  sprintDrain: 14,
  staminaRecoverWalk: 8,
  staminaRecoverIdle: 18,
  tackleCost: 30,
  tackleCooldown: 2,
  tackleRange: 3.2,
  kingTackleRange: 3.8,
  tackleAngleCos: Math.cos(Math.PI / 6),
  holdDistance: 2.0,
  captureRange: 2.4,
  captureHoldSeconds: 1.5,
  rescueRange: 1.7,
  aiThinkSeconds: 0.35,
  aiCaptureSeconds: 1.2,
  stateGraceSeconds: 5
};

export const MOVEMENT_PROFILES = {
  [PLAYER_STATES.neutral]: {
    canMove: true,
    canSprint: true,
    speedMultiplier: 1
  },
  [PLAYER_STATES.attacker]: {
    canMove: true,
    canSprint: true,
    speedMultiplier: 1.05
  },
  [PLAYER_STATES.king]: {
    canMove: true,
    canSprint: true,
    speedMultiplier: RULES.kingSpeedBonus
  },
  [PLAYER_STATES.held]: {
    canMove: false,
    canSprint: false,
    speedMultiplier: 0
  },
  [PLAYER_STATES.captured]: {
    canMove: false,
    canSprint: false,
    speedMultiplier: 0
  }
};

const rand = (min, max) => min + Math.random() * (max - min);
const clamp = (value, min, max) => Math.max(min, Math.min(max, value));
const distSq = (a, b) => {
  const dx = a.x - b.x;
  const dz = a.z - b.z;
  return dx * dx + dz * dz;
};
const dist = (a, b) => Math.sqrt(distSq(a, b));
const normalize = (x, z) => {
  const length = Math.hypot(x, z);
  if (length < 0.0001) {
    return { x: 0, z: 0 };
  }
  return { x: x / length, z: z / length };
};

export class OverthroneGame {
  constructor() {
    this.phase = GAME_PHASES.menu;
    this.selectedTeam = "blue";
    this.aiCount = 5;
    this.players = [];
    this.capturePoints = [];
    this.localPlayerId = "player";
    this.timeLeft = RULES.matchSeconds;
    this.fullCaptureTeam = null;
    this.fullCaptureTimer = 0;
    this.matchWinner = null;
    this.matchReason = "";
    this.events = [];
    this.teamPressure = {
      blue: { owned: 0, role: "hiding", hadCrownControl: false },
      red: { owned: 0, role: "hiding", hadCrownControl: false }
    };
  }

  openRoom() {
    this.phase = GAME_PHASES.room;
  }

  openFactionSelect() {
    this.phase = GAME_PHASES.faction;
  }

  setTeam(teamId) {
    this.selectedTeam = teamId;
  }

  setAiCount(count) {
    this.aiCount = count;
  }

  startMatch() {
    this.phase = GAME_PHASES.playing;
    this.matchWinner = null;
    this.matchReason = "";
    this.timeLeft = RULES.matchSeconds;
    this.fullCaptureTeam = null;
    this.fullCaptureTimer = 0;
    this.events = [];
    this.teamPressure = {
      blue: { owned: 0, role: "hiding", hadCrownControl: false },
      red: { owned: 0, role: "hiding", hadCrownControl: false }
    };
    this.capturePoints = CAPTURE_POINTS.map((point) => ({
      ...point,
      owner: null,
      progressTeam: null,
      progress: 0,
      contested: false
    }));

    const enemyTeam = this.selectedTeam === "blue" ? "red" : "blue";
    this.players = [
      this.createPlayer({
        id: this.localPlayerId,
        name: "You",
        team: this.selectedTeam,
        isLocal: true,
        index: 0
      })
    ];

    for (let i = 0; i < this.aiCount; i += 1) {
      const team = i % 2 === 0 ? enemyTeam : this.selectedTeam;
      this.players.push(
        this.createPlayer({
          id: `ai-${i + 1}`,
          name: `${team === "blue" ? "B" : "R"}-AI ${i + 1}`,
          team,
          isLocal: false,
          index: i + 1
        })
      );
    }

    this.assignInitialKings();
    this.recomputeTeamPressure();
    this.pushEvent("왕좌가 열렸다. 점령전 시작!");
  }

  createPlayer({ id, name, team, isLocal, index }) {
    const spawn = TEAMS[team].spawn;
    const side = team === "blue" ? 1 : -1;
    return {
      id,
      name,
      team,
      isLocal,
      x: spawn.x + rand(-3, 3) + (index % 3) * side,
      z: spawn.z + rand(-3, 3),
      yaw: team === "blue" ? Math.PI * 0.2 : Math.PI * 1.2,
      state: PLAYER_STATES.neutral,
      stateBeforeHeld: null,
      heldBy: null,
      holdingId: null,
      capturedBy: null,
      stamina: 100,
      slimeTimer: 0,
      slimeCooldown: 0,
      usedHeldEscape: false,
      tackleCooldown: 0,
      attackerGraceTimer: 0,
      captureCharge: 0,
      captureScore: 0,
      captureContribution: 0,
      rescueScore: 0,
      aiThink: rand(0, RULES.aiThinkSeconds),
      aiTarget: null,
      aiCaptureTimer: 0,
      slowTimer: 0
    };
  }

  assignInitialKings() {
    for (const team of Object.keys(TEAMS)) {
      const candidate = this.players.find((player) => player.team === team);
      if (candidate) {
        candidate.state = PLAYER_STATES.king;
      }
    }
  }

  get localPlayer() {
    return this.players.find((player) => player.id === this.localPlayerId);
  }

  update(dt, input) {
    if (this.phase !== GAME_PHASES.playing || this.matchWinner) {
      return;
    }

    this.timeLeft = Math.max(0, this.timeLeft - dt);
    this.updateTimers(dt);
    this.updateLocalPlayer(dt, input);
    this.updateAiPlayers(dt);
    this.updateRescues();
    this.updateCapturePoints(dt);
    this.recomputeTeamPressure();
    this.updateVictory(dt);
  }

  updateTimers(dt) {
    for (const player of this.players) {
      player.tackleCooldown = Math.max(0, player.tackleCooldown - dt);
      player.attackerGraceTimer = Math.max(0, player.attackerGraceTimer - dt);
      player.slimeCooldown = Math.max(0, player.slimeCooldown - dt);
      player.slowTimer = Math.max(0, player.slowTimer - dt);

      if (player.slimeTimer > 0) {
        player.slimeTimer = Math.max(0, player.slimeTimer - dt);
        if (player.slimeTimer === 0 && player.isLocal) {
          this.pushEvent("슬라임 변신 종료");
        }
      }
    }
  }

  updateLocalPlayer(dt, input) {
    const player = this.localPlayer;
    if (!player || player.state === PLAYER_STATES.captured) {
      return;
    }

    player.yaw = input.yaw ?? player.yaw;

    if (input.useSlime) {
      this.tryUseSlime(player);
    }

    if (input.tackle) {
      this.tryTackle(player);
    }

    if (player.state === PLAYER_STATES.held) {
      player.captureCharge = 0;
      return;
    }

    if (player.holdingId) {
      if (input.capture) {
        this.updateManualCapture(player, dt);
      } else {
        player.captureCharge = 0;
      }
      player.stamina = Math.min(100, player.stamina + RULES.staminaRecoverIdle * dt);
      return;
    }

    const movementProfile = this.getMovementProfile(player);
    if (!movementProfile.canMove) {
      player.captureCharge = 0;
      return;
    }

    const move = normalize(input.moveX, input.moveZ);
    const isMoving = Math.abs(move.x) + Math.abs(move.z) > 0.01;
    const wantsSprint = input.sprint && movementProfile.canSprint && isMoving && player.stamina > 1;
    const speed = (wantsSprint ? RULES.sprintSpeed : RULES.baseSpeed) * movementProfile.speedMultiplier;

    const sin = Math.sin(player.yaw);
    const cos = Math.cos(player.yaw);
    const worldX = move.z * sin - move.x * cos;
    const worldZ = move.z * cos + move.x * sin;

    this.movePlayerWithCollision(player, worldX * speed * dt, worldZ * speed * dt);

    if (wantsSprint) {
      player.stamina = Math.max(0, player.stamina - RULES.sprintDrain * dt);
    } else {
      const recover = isMoving ? RULES.staminaRecoverWalk : RULES.staminaRecoverIdle;
      player.stamina = Math.min(100, player.stamina + recover * dt);
    }

    if (input.capture) {
      this.updateManualCapture(player, dt);
    } else {
      player.captureCharge = 0;
    }
  }

  getMovementProfile(player) {
    const profile = MOVEMENT_PROFILES[player.state] ?? MOVEMENT_PROFILES[PLAYER_STATES.neutral];
    const speedMultiplier =
      profile.speedMultiplier *
      (player.slimeTimer > 0 ? RULES.slimeSpeedBonus : 1) *
      (player.slowTimer > 0 ? 0.65 : 1);

    return {
      ...profile,
      canMove: profile.canMove && !player.holdingId,
      canSprint: profile.canSprint && player.slimeTimer <= 0,
      speedMultiplier
    };
  }

  updateManualCapture(player, dt) {
    if (player.state !== PLAYER_STATES.king) {
      player.captureCharge = 0;
      return;
    }

    const target = this.players.find(
      (other) =>
        other.team !== player.team &&
        other.state === PLAYER_STATES.held &&
        dist(player, other) <= RULES.captureRange
    );

    if (!target) {
      player.captureCharge = 0;
      return;
    }

    player.captureCharge += dt;
    if (player.captureCharge >= RULES.captureHoldSeconds) {
      this.capturePlayer(target, player);
      player.captureCharge = 0;
    }
  }

  updateAiPlayers(dt) {
    for (const player of this.players) {
      if (player.isLocal || player.state === PLAYER_STATES.captured) {
        continue;
      }

      if (player.state === PLAYER_STATES.held) {
        player.aiCaptureTimer = 0;
        continue;
      }

      if (player.holdingId) {
        if (player.state === PLAYER_STATES.king) {
          this.updateAiFinalCapture(player, dt);
        }
        this.recoverAiStamina(player, dt, false);
        continue;
      }

      player.aiThink -= dt;
      if (player.aiThink <= 0) {
        player.aiThink = RULES.aiThinkSeconds + rand(-0.08, 0.08);
        player.aiTarget = this.pickAiTarget(player);
      }

      const beforeX = player.x;
      const beforeZ = player.z;
      const isSprinting = player.aiTarget && (player.aiTarget.kind === "chase" || player.aiTarget.kind === "finish");
      this.moveAiTowardTarget(player, dt);
      const moved = Math.hypot(player.x - beforeX, player.z - beforeZ) > 0.01;
      this.updateAiStamina(player, dt, moved, isSprinting);

      if ((player.state === PLAYER_STATES.attacker || player.state === PLAYER_STATES.king) && this.findTackleTarget(player)) {
        this.tryTackle(player);
      }

      if (player.state === PLAYER_STATES.king) {
        this.updateAiFinalCapture(player, dt);
      }
    }
  }

  recoverAiStamina(player, dt, isMoving) {
    const recover = isMoving ? RULES.staminaRecoverWalk : RULES.staminaRecoverIdle;
    player.stamina = Math.min(100, player.stamina + recover * dt);
  }

  updateAiStamina(player, dt, isMoving, isSprinting) {
    if (isMoving && isSprinting && player.stamina > 0) {
      player.stamina = Math.max(0, player.stamina - RULES.sprintDrain * dt);
      return;
    }

    this.recoverAiStamina(player, dt, isMoving);
  }

  pickAiTarget(player) {
    const heldAlly = this.players.find(
      (other) => other.team === player.team && other.state === PLAYER_STATES.held
    );
    if (heldAlly) {
      return { kind: "rescue", x: heldAlly.x, z: heldAlly.z, id: heldAlly.id };
    }

    const heldEnemy = this.players.find(
      (other) => other.team !== player.team && other.state === PLAYER_STATES.held
    );
    if (player.state === PLAYER_STATES.king && heldEnemy) {
      return { kind: "finish", x: heldEnemy.x, z: heldEnemy.z, id: heldEnemy.id };
    }

    const activeEnemy = this.closestEnemy(player);
    const pressure = this.teamPressure[player.team];
    if ((player.state === PLAYER_STATES.attacker || player.state === PLAYER_STATES.king) && activeEnemy) {
      return { kind: "chase", x: activeEnemy.x, z: activeEnemy.z, id: activeEnemy.id };
    }

    const desired = [...this.capturePoints]
      .sort((a, b) => {
        const aScore = (a.owner === player.team ? 2 : 0) + dist(player, a) * 0.04;
        const bScore = (b.owner === player.team ? 2 : 0) + dist(player, b) * 0.04;
        return aScore - bScore;
      })
      .find((point) => point.owner !== player.team || pressure.owned < 2);

    if (desired) {
      return {
        kind: "capture",
        x: desired.x + rand(-2, 2),
        z: desired.z + rand(-2, 2),
        id: desired.id
      };
    }

    if (activeEnemy) {
      return { kind: "patrol", x: activeEnemy.x, z: activeEnemy.z, id: activeEnemy.id };
    }

    return { kind: "idle", x: player.x + rand(-8, 8), z: player.z + rand(-8, 8) };
  }

  moveAiTowardTarget(player, dt) {
    const target = player.aiTarget;
    if (!target) {
      return;
    }

    if (target.id && (target.kind === "chase" || target.kind === "finish" || target.kind === "rescue")) {
      const liveTarget = this.players.find((candidate) => candidate.id === target.id);
      if (liveTarget) {
        target.x = liveTarget.x;
        target.z = liveTarget.z;
      }
    }

    const toTarget = normalize(target.x - player.x, target.z - player.z);
    if (Math.abs(toTarget.x) + Math.abs(toTarget.z) < 0.01) {
      return;
    }

    const movementProfile = this.getMovementProfile(player);
    if (!movementProfile.canMove) {
      return;
    }

    let speed = RULES.baseSpeed * 0.82 * movementProfile.speedMultiplier;
    if ((target.kind === "chase" || target.kind === "finish") && player.stamina > 0) {
      speed = RULES.sprintSpeed * 0.9 * movementProfile.speedMultiplier;
    }

    player.yaw = Math.atan2(toTarget.x, toTarget.z);
    this.movePlayerWithCollision(player, toTarget.x * speed * dt, toTarget.z * speed * dt);
  }

  movePlayerWithCollision(player, deltaX, deltaZ) {
    const stepCount = Math.max(1, Math.ceil(Math.max(Math.abs(deltaX), Math.abs(deltaZ)) / (RULES.playerRadius * 0.5)));
    const stepX = deltaX / stepCount;
    const stepZ = deltaZ / stepCount;

    for (let i = 0; i < stepCount; i += 1) {
      this.movePlayerCollisionStep(player, stepX, stepZ);
    }
  }

  movePlayerCollisionStep(player, deltaX, deltaZ) {
    const nextX = clamp(
      player.x + deltaX,
      MAP_BOUNDS.minX + RULES.playerRadius,
      MAP_BOUNDS.maxX - RULES.playerRadius
    );
    if (!this.collidesWithObstacle(nextX, player.z)) {
      player.x = nextX;
    }

    const nextZ = clamp(
      player.z + deltaZ,
      MAP_BOUNDS.minZ + RULES.playerRadius,
      MAP_BOUNDS.maxZ - RULES.playerRadius
    );
    if (!this.collidesWithObstacle(player.x, nextZ)) {
      player.z = nextZ;
    }
  }

  collidesWithObstacle(x, z) {
    return LEVEL_OBSTACLES.some((obstacle) => {
      const halfW = obstacle.w / 2;
      const halfD = obstacle.d / 2;
      return (
        x + RULES.playerRadius > obstacle.x - halfW &&
        x - RULES.playerRadius < obstacle.x + halfW &&
        z + RULES.playerRadius > obstacle.z - halfD &&
        z - RULES.playerRadius < obstacle.z + halfD
      );
    });
  }

  updateAiFinalCapture(player, dt) {
    const target = this.players.find(
      (other) =>
        other.team !== player.team &&
        other.state === PLAYER_STATES.held &&
        dist(player, other) <= RULES.captureRange
    );

    if (!target) {
      player.aiCaptureTimer = 0;
      return;
    }

    player.aiCaptureTimer += dt;
    if (player.aiCaptureTimer >= RULES.aiCaptureSeconds) {
      this.capturePlayer(target, player);
      player.aiCaptureTimer = 0;
    }
  }

  updateRescues() {
    const heldPlayers = this.players.filter((player) => player.state === PLAYER_STATES.held);
    for (const held of heldPlayers) {
      const rescuer = this.players.find(
        (candidate) =>
          candidate.team === held.team &&
          candidate.id !== held.id &&
          candidate.state !== PLAYER_STATES.held &&
          candidate.state !== PLAYER_STATES.captured &&
          dist(candidate, held) <= RULES.rescueRange
      );

      if (rescuer) {
        rescuer.rescueScore += 1;
        this.releaseHeldPlayer(held, `${rescuer.name} 구출`);
      }
    }
  }

  updateCapturePoints(dt) {
    for (const point of this.capturePoints) {
      const nearby = this.players.filter(
        (player) =>
          player.state !== PLAYER_STATES.captured &&
          player.state !== PLAYER_STATES.held &&
          dist(player, point) <= RULES.captureRadius
      );
      const blueCount = nearby.filter((player) => player.team === "blue").length;
      const redCount = nearby.filter((player) => player.team === "red").length;

      point.contested = blueCount > 0 && redCount > 0;

      if (point.contested) {
        continue;
      }

      const capturingTeam = blueCount > 0 ? "blue" : redCount > 0 ? "red" : null;
      if (!capturingTeam) {
        if (!point.owner) {
          point.progress = Math.max(0, point.progress - RULES.captureRates.decay * dt);
          if (point.progress === 0) {
            point.progressTeam = null;
          }
        }
        continue;
      }

      const count = capturingTeam === "blue" ? blueCount : redCount;
      const rate = count >= 3 ? RULES.captureRates.many : count === 2 ? RULES.captureRates.two : RULES.captureRates.one;

      if (point.progressTeam !== capturingTeam) {
        point.progressTeam = capturingTeam;
        point.progress = point.owner === capturingTeam ? 100 : Math.max(0, 100 - point.progress);
      }

      if (point.owner === capturingTeam) {
        point.progress = 100;
      } else {
        point.progress = Math.min(100, point.progress + rate * dt);
        for (const player of nearby) {
          if (player.team === capturingTeam) {
            player.captureContribution += rate * dt * 0.1;
          }
        }
        if (point.progress >= 100) {
          const oldOwner = point.owner;
          point.owner = capturingTeam;
          point.progress = 100;
          this.pushEvent(`${TEAMS[capturingTeam].label} ${point.id} 점령 - 공수전환!`);
          if (oldOwner && oldOwner !== capturingTeam) {
            this.pushEvent(`${TEAMS[oldOwner].label} 왕권 압박`);
          }
        }
      }
    }
  }

  recomputeTeamPressure() {
    for (const team of Object.keys(TEAMS)) {
      const owned = this.capturePoints.filter((point) => point.owner === team).length;
      const pressure = this.teamPressure[team];
      const hadCrownControl = pressure.hadCrownControl || owned >= 2;
      const role = owned >= 2 ? "ruling" : owned === 1 ? "pressing" : "hiding";
      this.teamPressure[team] = { owned, role, hadCrownControl };

      if (owned >= 2) {
        this.ensureKing(team);
        this.setTeamAttackers(team, true);
      } else {
        if (hadCrownControl) {
          this.demoteKingIfNeeded(team);
        }
        this.setTeamAttackers(team, owned === 1);
      }
    }
  }

  ensureKing(team) {
    const existing = this.players.find((player) => player.team === team && player.state === PLAYER_STATES.king);
    if (existing) {
      return;
    }

    const candidates = this.players
      .filter((player) => player.team === team && player.state !== PLAYER_STATES.captured && player.state !== PLAYER_STATES.held)
      .sort((a, b) => b.captureScore - a.captureScore || b.captureContribution - a.captureContribution);

    if (candidates[0]) {
      candidates[0].state = PLAYER_STATES.king;
      this.pushEvent(`${TEAMS[team].label} 새 왕 즉위: ${candidates[0].name}`);
    }
  }

  demoteKingIfNeeded(team) {
    for (const player of this.players) {
      if (player.team === team && player.state === PLAYER_STATES.king) {
        player.state = PLAYER_STATES.attacker;
        this.pushEvent(`${TEAMS[team].label} 왕좌 상실`);
      }
    }
  }

  setTeamAttackers(team, canAttack) {
    for (const player of this.players) {
      if (player.team !== team || player.state === PLAYER_STATES.king || player.state === PLAYER_STATES.held || player.state === PLAYER_STATES.captured) {
        continue;
      }

      const nearOwnedPoint = this.capturePoints.some(
        (point) => point.owner === team && dist(player, point) <= RULES.captureRadius + 2
      );
      if (canAttack && nearOwnedPoint) {
        player.state = PLAYER_STATES.attacker;
        player.attackerGraceTimer = RULES.stateGraceSeconds;
      } else if (canAttack && player.state === PLAYER_STATES.attacker && player.attackerGraceTimer > 0) {
        player.state = PLAYER_STATES.attacker;
      } else {
        player.state = PLAYER_STATES.neutral;
        player.attackerGraceTimer = 0;
      }
    }
  }

  updateVictory(dt) {
    for (const team of Object.keys(TEAMS)) {
      const enemyTeam = team === "blue" ? "red" : "blue";
      const enemyAlive = this.players.some(
        (player) => player.team === enemyTeam && player.state !== PLAYER_STATES.captured
      );
      if (!enemyAlive) {
        this.finishMatch(team, "상대 전원 포획");
        return;
      }
    }

    const teamWithAll = Object.keys(TEAMS).find((team) => this.capturePoints.every((point) => point.owner === team));
    if (teamWithAll) {
      if (this.fullCaptureTeam !== teamWithAll) {
        this.fullCaptureTeam = teamWithAll;
        this.fullCaptureTimer = RULES.fullCaptureHoldSeconds;
        this.pushEvent(`${TEAMS[teamWithAll].label} 완전 점령 카운트다운`);
      } else {
        this.fullCaptureTimer = Math.max(0, this.fullCaptureTimer - dt);
        if (this.fullCaptureTimer <= 0) {
          this.finishMatch(teamWithAll, "모든 거점 30초 유지");
          return;
        }
      }
    } else {
      this.fullCaptureTeam = null;
      this.fullCaptureTimer = 0;
    }

    if (this.timeLeft <= 0) {
      const blueAlive = this.players.filter((player) => player.team === "blue" && player.state !== PLAYER_STATES.captured).length;
      const redAlive = this.players.filter((player) => player.team === "red" && player.state !== PLAYER_STATES.captured).length;
      const blueOwned = this.capturePoints.filter((point) => point.owner === "blue").length;
      const redOwned = this.capturePoints.filter((point) => point.owner === "red").length;
      let winner = null;
      if (blueOwned > redOwned) {
        winner = "blue";
      } else if (redOwned > blueOwned) {
        winner = "red";
      } else if (blueAlive > redAlive) {
        winner = "blue";
      } else if (redAlive > blueAlive) {
        winner = "red";
      }
      this.finishMatch(winner, "시간 종료 판정");
    }
  }

  finishMatch(team, reason) {
    this.matchWinner = team;
    this.matchReason = reason;
    this.phase = GAME_PHASES.result;
    this.pushEvent(team ? `${TEAMS[team].label} 승리 - ${reason}` : `무승부 - ${reason}`);
  }

  tryUseSlime(player) {
    if (player.slimeCooldown > 0 || player.stamina < RULES.slimeCost || player.state === PLAYER_STATES.captured) {
      return false;
    }

    if (player.state === PLAYER_STATES.held) {
      if (player.usedHeldEscape) {
        return false;
      }
      player.usedHeldEscape = true;
      this.releaseHeldPlayer(player, "슬라임 탈출");
    }

    player.stamina -= RULES.slimeCost;
    player.slimeTimer = RULES.slimeDuration;
    player.slimeCooldown = RULES.slimeCooldown;
    if (player.isLocal) {
      this.pushEvent("슬라임 변신!");
    }
    return true;
  }

  tryTackle(player) {
    if (
      player.tackleCooldown > 0 ||
      player.holdingId ||
      player.stamina < RULES.tackleCost ||
      (player.state !== PLAYER_STATES.attacker && player.state !== PLAYER_STATES.king)
    ) {
      return false;
    }

    const target = this.findTackleTarget(player);

    player.stamina -= RULES.tackleCost;
    player.tackleCooldown = RULES.tackleCooldown;

    if (target) {
      if (target.holdingId) {
        const heldByTarget = this.players.find((other) => other.id === target.holdingId);
        if (heldByTarget) {
          this.releaseHeldPlayer(heldByTarget, "붙드는 자 피격");
        }
      }
      this.snapHeldTarget(player, target);
      target.stateBeforeHeld = target.state;
      target.state = PLAYER_STATES.held;
      target.heldBy = player.id;
      player.holdingId = target.id;
      this.pushEvent(`${player.name} 덮치기 성공: ${target.name}`);
      return true;
    }

    return false;
  }

  findTackleTarget(player) {
    const range = player.state === PLAYER_STATES.king ? RULES.kingTackleRange : RULES.tackleRange;
    const forward = { x: Math.sin(player.yaw), z: Math.cos(player.yaw) };
    return this.players.find((other) => {
      if (other.team === player.team || other.state === PLAYER_STATES.captured || other.state === PLAYER_STATES.held) {
        return false;
      }
      const dx = other.x - player.x;
      const dz = other.z - player.z;
      const distance = Math.hypot(dx, dz);
      if (distance > range) {
        return false;
      }
      const dir = normalize(dx, dz);
      return forward.x * dir.x + forward.z * dir.z >= RULES.tackleAngleCos;
    });
  }

  snapHeldTarget(holder, target) {
    const dx = target.x - holder.x;
    const dz = target.z - holder.z;
    const currentDistance = Math.hypot(dx, dz);
    if (currentDistance <= RULES.holdDistance) {
      return;
    }
    const dir = normalize(dx, dz);
    target.x = holder.x + dir.x * RULES.holdDistance;
    target.z = holder.z + dir.z * RULES.holdDistance;
  }

  releaseHeldPlayer(held, reason) {
    const holder = this.players.find((player) => player.id === held.heldBy);
    if (holder) {
      holder.holdingId = null;
      holder.slowTimer = 1;
    }
    held.heldBy = null;
    const restoredState = held.stateBeforeHeld ?? PLAYER_STATES.neutral;
    let nextState = restoredState;
    if (restoredState === PLAYER_STATES.king) {
      const owned = this.capturePoints.filter((point) => point.owner === held.team).length;
      if (owned < 2) {
        nextState = PLAYER_STATES.neutral;
      }
      for (const player of this.players) {
        if (player.id !== held.id && player.team === held.team && player.state === PLAYER_STATES.king) {
          player.state = PLAYER_STATES.attacker;
        }
      }
    }
    held.state = nextState;
    held.stateBeforeHeld = null;
    held.captureCharge = 0;
    this.pushEvent(`${held.name} 해방 - ${reason}`);
  }

  capturePlayer(target, king) {
    const holder = this.players.find((player) => player.id === target.heldBy);
    if (holder) {
      holder.holdingId = null;
    }
    target.state = PLAYER_STATES.captured;
    target.stateBeforeHeld = null;
    target.capturedBy = king.id;
    target.heldBy = null;
    king.captureScore += 1;
    this.pushEvent(`${king.name} 포획 완료: ${target.name}`);
  }

  closestEnemy(player) {
    return this.players
      .filter((other) => other.team !== player.team && other.state !== PLAYER_STATES.captured && other.state !== PLAYER_STATES.held)
      .sort((a, b) => distSq(player, a) - distSq(player, b))[0];
  }

  pushEvent(message) {
    this.events.unshift({
      id: `${Date.now()}-${Math.random()}`,
      time: RULES.matchSeconds - this.timeLeft,
      message
    });
    this.events = this.events.slice(0, 6);
  }

  getSnapshot() {
    return {
      phase: this.phase,
      selectedTeam: this.selectedTeam,
      aiCount: this.aiCount,
      timeLeft: this.timeLeft,
      fullCaptureTeam: this.fullCaptureTeam,
      fullCaptureTimer: this.fullCaptureTimer,
      matchWinner: this.matchWinner,
      matchReason: this.matchReason,
      localPlayer: this.localPlayer ? { ...this.localPlayer } : null,
      players: this.players.map((player) => ({ ...player })),
      capturePoints: this.capturePoints.map((point) => ({ ...point })),
      teamPressure: JSON.parse(JSON.stringify(this.teamPressure)),
      events: [...this.events]
    };
  }
}

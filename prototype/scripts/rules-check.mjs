import { LEVEL_OBSTACLES, OverthroneGame, PLAYER_STATES, RULES } from "../src/game.js";

checkRestartResetsTeamPressure();
checkReleaseRestoresPreviousRole();
checkHolderCannotTackleSecondTarget();
checkHolderCannotMoveWhileHolding();
checkHittingHolderReleasesHeldTarget();
checkHeldKingReleaseDoesNotDuplicateKings();
checkReleasedKingDoesNotRestoreWithoutCrownControl();
checkAttackerGraceWindow();
checkTimeoutDrawDoesNotAwardBlue();
checkVisibleObstacleBlocksLocalAndAiMovement();
checkLocalMovementFollowsViewYaw();
checkLocalStrafeMatchesCameraRight();
checkMovementProfilesByState();
checkSlimeMovementDoesNotStackSprint();
checkKingTackleLeavesTargetCapturable();
checkAiDoesNotSpendTackleWithoutTarget();
checkAiChaseConsumesStamina();

console.log("rules-check ok");

function checkRestartResetsTeamPressure() {
  const game = new OverthroneGame();
  game.startMatch();
  game.teamPressure.blue.hadCrownControl = true;
  game.startMatch();

  const kings = game.players.filter((player) => player.state === PLAYER_STATES.king);
  assert(kings.some((player) => player.team === "blue"), "blue initial king missing after restart");
  assert(kings.some((player) => player.team === "red"), "red initial king missing after restart");
}

function checkReleaseRestoresPreviousRole() {
  const game = new OverthroneGame();
  game.startMatch();
  const king = game.players.find((player) => player.team === "blue");
  const holder = game.players.find((player) => player.team === "red");

  for (const point of game.capturePoints.slice(0, 2)) {
    point.owner = "blue";
  }
  king.stateBeforeHeld = king.state;
  king.state = PLAYER_STATES.held;
  king.heldBy = holder.id;
  game.releaseHeldPlayer(king, "test");

  assert(king.state === PLAYER_STATES.king, `expected released king to stay king, got ${king.state}`);
}

function checkHolderCannotTackleSecondTarget() {
  const game = new OverthroneGame();
  game.setAiCount(3);
  game.startMatch();

  const holder = game.players.find((player) => player.team === "blue");
  const targets = game.players.filter((player) => player.team === "red");

  holder.state = PLAYER_STATES.king;
  holder.stamina = 100;
  holder.tackleCooldown = 0;
  holder.x = 0;
  holder.z = 0;
  holder.yaw = 0;

  targets[0].x = 0;
  targets[0].z = 2;
  targets[0].state = PLAYER_STATES.neutral;
  targets[1].x = 0;
  targets[1].z = 2.5;
  targets[1].state = PLAYER_STATES.neutral;

  assert(game.tryTackle(holder), "first tackle should succeed");
  holder.tackleCooldown = 0;
  holder.stamina = 100;
  assert(!game.tryTackle(holder), "second tackle should be blocked while holding");

  const heldTargets = targets.filter((target) => target.state === PLAYER_STATES.held);
  assert(heldTargets.length === 1, `expected one held target, got ${heldTargets.length}`);
}

function checkHolderCannotMoveWhileHolding() {
  const game = new OverthroneGame();
  game.setAiCount(3);
  game.startMatch();

  const holder = game.players.find((player) => player.team === "blue");
  const target = game.players.find((player) => player.team === "red");

  holder.state = PLAYER_STATES.king;
  holder.stamina = 100;
  holder.tackleCooldown = 0;
  holder.x = 0;
  holder.z = 0;
  holder.yaw = 0;
  target.state = PLAYER_STATES.neutral;
  target.x = 0;
  target.z = 2;

  assert(game.tryTackle(holder), "tackle before hold-move check should succeed");
  game.updateLocalPlayer(1, {
    yaw: 0,
    moveX: 0,
    moveZ: 1,
    sprint: false,
    useSlime: false,
    tackle: false,
    capture: false
  });

  assert(holder.z === 0, `holder should not move while holding, got z=${holder.z}`);
}

function checkHittingHolderReleasesHeldTarget() {
  const game = new OverthroneGame();
  game.setAiCount(3);
  game.startMatch();

  const holder = game.players.find((player) => player.team === "blue");
  const target = game.players.find((player) => player.team === "red");
  const interrupter = game.players.find((player) => player.team === "red" && player.id !== target.id);

  holder.state = PLAYER_STATES.king;
  holder.stamina = 100;
  holder.tackleCooldown = 0;
  holder.x = 0;
  holder.z = 0;
  holder.yaw = 0;
  target.state = PLAYER_STATES.neutral;
  target.x = 0;
  target.z = 2;
  assert(game.tryTackle(holder), "initial hold should succeed");

  interrupter.state = PLAYER_STATES.attacker;
  interrupter.stamina = 100;
  interrupter.tackleCooldown = 0;
  interrupter.x = 0;
  interrupter.z = -2;
  interrupter.yaw = 0;
  assert(game.tryTackle(interrupter), "interrupter should hit holder");

  assert(holder.state === PLAYER_STATES.held, `holder should become held, got ${holder.state}`);
  assert(target.state !== PLAYER_STATES.held, "target should be released when holder is hit");
  assert(holder.holdingId === null, "holder should no longer hold old target");
}

function checkHeldKingReleaseDoesNotDuplicateKings() {
  const game = new OverthroneGame();
  game.setAiCount(5);
  game.startMatch();

  for (const point of game.capturePoints.slice(0, 2)) {
    point.owner = "blue";
  }
  game.recomputeTeamPressure();

  const originalKing = game.players.find((player) => player.team === "blue" && player.state === PLAYER_STATES.king);
  const holder = game.players.find((player) => player.team === "red");
  originalKing.stateBeforeHeld = originalKing.state;
  originalKing.state = PLAYER_STATES.held;
  originalKing.heldBy = holder.id;
  holder.holdingId = originalKing.id;

  game.recomputeTeamPressure();
  assert(game.players.filter((player) => player.team === "blue" && player.state === PLAYER_STATES.king).length === 1, "successor king should exist while original king is held");

  game.releaseHeldPlayer(originalKing, "test rescue");
  const kings = game.players.filter((player) => player.team === "blue" && player.state === PLAYER_STATES.king);
  assert(kings.length === 1, `expected exactly one blue king after release, got ${kings.length}`);
}

function checkReleasedKingDoesNotRestoreWithoutCrownControl() {
  const game = new OverthroneGame();
  game.setAiCount(5);
  game.startMatch();

  for (const point of game.capturePoints.slice(0, 2)) {
    point.owner = "blue";
  }
  game.recomputeTeamPressure();

  const originalKing = game.players.find((player) => player.team === "blue" && player.state === PLAYER_STATES.king);
  const holder = game.players.find((player) => player.team === "red");
  originalKing.stateBeforeHeld = originalKing.state;
  originalKing.state = PLAYER_STATES.held;
  originalKing.heldBy = holder.id;
  holder.holdingId = originalKing.id;

  for (const point of game.capturePoints) {
    point.owner = null;
  }
  game.recomputeTeamPressure();
  game.releaseHeldPlayer(originalKing, "test rescue after crown loss");

  assert(originalKing.state !== PLAYER_STATES.king, "released king should not regain crown after team lost crown control");
}

function checkAttackerGraceWindow() {
  const game = new OverthroneGame();
  game.setAiCount(5);
  game.startMatch();

  const attacker = game.players.find((player) => player.team === "blue" && player.state !== PLAYER_STATES.king);
  const point = game.capturePoints[0];
  point.owner = "blue";
  attacker.x = point.x;
  attacker.z = point.z;
  game.recomputeTeamPressure();
  assert(attacker.state === PLAYER_STATES.attacker, `expected near owned point to grant attacker, got ${attacker.state}`);

  attacker.x = point.x + RULES.captureRadius + 3;
  attacker.z = point.z;
  game.recomputeTeamPressure();
  assert(attacker.state === PLAYER_STATES.attacker, "attacker should remain during grace window");

  attacker.attackerGraceTimer = 0;
  game.recomputeTeamPressure();
  assert(attacker.state === PLAYER_STATES.neutral, "attacker should drop to neutral after grace expires");
}

function checkTimeoutDrawDoesNotAwardBlue() {
  const game = new OverthroneGame();
  game.setAiCount(3);
  game.startMatch();

  for (const point of game.capturePoints) {
    point.owner = null;
  }
  game.timeLeft = 0;
  game.updateVictory(0.016);

  assert(game.matchWinner === null, `expected timeout draw, got ${game.matchWinner}`);
  assert(game.phase === "result", `expected result phase after timeout draw, got ${game.phase}`);
}

function checkVisibleObstacleBlocksLocalAndAiMovement() {
  const game = new OverthroneGame();
  game.setAiCount(3);
  game.startMatch();

  const obstacle = LEVEL_OBSTACLES.find((item) => item.id === "south-bridge");
  const local = game.localPlayer;
  local.x = obstacle.x;
  local.z = obstacle.z - obstacle.d / 2 - RULES.playerRadius - 0.2;
  local.yaw = 0;

  for (let i = 0; i < 20; i += 1) {
    game.updateLocalPlayer(0.05, {
      yaw: 0,
      moveX: 0,
      moveZ: 1,
      sprint: false,
      useSlime: false,
      tackle: false,
      capture: false
    });
  }

  assert(
    local.z <= obstacle.z - obstacle.d / 2 - RULES.playerRadius,
    `local player crossed visible obstacle, z=${local.z}`
  );

  const ai = game.players.find((player) => !player.isLocal);
  ai.x = obstacle.x;
  ai.z = obstacle.z - obstacle.d / 2 - RULES.playerRadius - 0.2;
  game.movePlayerWithCollision(ai, 0, 10);

  assert(
    ai.z <= obstacle.z - obstacle.d / 2 - RULES.playerRadius,
    `AI player crossed visible obstacle, z=${ai.z}`
  );
}

function checkLocalMovementFollowsViewYaw() {
  const game = new OverthroneGame();
  game.setAiCount(3);
  game.startMatch();

  const local = game.localPlayer;
  local.x = 0;
  local.z = -8;
  local.yaw = Math.PI / 2;

  game.updateLocalPlayer(0.25, {
    yaw: Math.PI / 2,
    moveX: 0,
    moveZ: 1,
    sprint: false,
    useSlime: false,
    tackle: false,
    capture: false
  });

  assert(local.x > 1, `forward movement should follow camera yaw on x axis, got x=${local.x}`);
  assert(Math.abs(local.z + 8) < 0.2, `forward movement should not drift on z axis at 90deg yaw, got z=${local.z}`);
}

function checkLocalStrafeMatchesCameraRight() {
  const game = new OverthroneGame();
  game.setAiCount(3);
  game.startMatch();

  const local = game.localPlayer;
  local.x = 0;
  local.z = -8;
  local.yaw = 0;

  game.updateLocalPlayer(0.25, {
    yaw: 0,
    moveX: 1,
    moveZ: 0,
    sprint: false,
    useSlime: false,
    tackle: false,
    capture: false
  });

  assert(local.x < -1, `right strafe should move toward camera-right at yaw 0, got x=${local.x}`);
  assert(Math.abs(local.z + 8) < 0.2, `right strafe should not drift forward at yaw 0, got z=${local.z}`);
}

function checkMovementProfilesByState() {
  const neutralDistance = measureLocalMovementDistance(PLAYER_STATES.neutral);
  const attackerDistance = measureLocalMovementDistance(PLAYER_STATES.attacker);
  const kingDistance = measureLocalMovementDistance(PLAYER_STATES.king);
  const heldDistance = measureLocalMovementDistance(PLAYER_STATES.held);
  const capturedDistance = measureLocalMovementDistance(PLAYER_STATES.captured);

  assert(attackerDistance > neutralDistance, `attacker should move faster than neutral, got ${attackerDistance} <= ${neutralDistance}`);
  assert(kingDistance > attackerDistance, `king should move faster than attacker, got ${kingDistance} <= ${attackerDistance}`);
  assert(heldDistance === 0, `held player should not move, got ${heldDistance}`);
  assert(capturedDistance === 0, `captured player should not move, got ${capturedDistance}`);
}

function measureLocalMovementDistance(state) {
  const game = new OverthroneGame();
  game.setAiCount(3);
  game.startMatch();

  const local = game.localPlayer;
  local.x = 0;
  local.z = -8;
  local.yaw = 0;
  local.state = state;

  game.updateLocalPlayer(0.25, {
    yaw: 0,
    moveX: 0,
    moveZ: 1,
    sprint: false,
    useSlime: false,
    tackle: false,
    capture: false
  });

  return Number(Math.hypot(local.x, local.z + 8).toFixed(4));
}

function checkSlimeMovementDoesNotStackSprint() {
  const game = new OverthroneGame();
  game.setAiCount(3);
  game.startMatch();

  const local = game.localPlayer;
  local.x = 0;
  local.z = -8;
  local.yaw = 0;
  local.state = PLAYER_STATES.neutral;
  local.slimeTimer = 1;
  local.stamina = 100;

  game.updateLocalPlayer(0.25, {
    yaw: 0,
    moveX: 0,
    moveZ: 1,
    sprint: true,
    useSlime: false,
    tackle: false,
    capture: false
  });

  const moved = Math.hypot(local.x, local.z + 8);
  assert(moved < 3, `slime movement should not stack sprint speed, moved=${moved}`);
}

function checkKingTackleLeavesTargetCapturable() {
  const game = new OverthroneGame();
  game.setAiCount(3);
  game.startMatch();

  const king = game.localPlayer;
  const target = game.players.find((player) => player.team !== king.team);
  king.state = PLAYER_STATES.king;
  king.x = 0;
  king.z = 0;
  king.yaw = 0;
  king.stamina = 100;
  king.tackleCooldown = 0;
  target.state = PLAYER_STATES.neutral;
  target.x = 0;
  target.z = RULES.kingTackleRange - 0.1;

  assert(game.tryTackle(king), "king tackle at outer range should succeed");
  const heldDistance = Math.hypot(king.x - target.x, king.z - target.z);
  assert(heldDistance <= RULES.captureRange, `held target should be capturable, distance=${heldDistance}`);
}

function checkAiDoesNotSpendTackleWithoutTarget() {
  const game = new OverthroneGame();
  game.setAiCount(3);
  game.startMatch();

  const ai = game.players.find((player) => !player.isLocal && player.team === "red");
  const safeCorner = { x: 30, z: 25 };
  ai.state = PLAYER_STATES.king;
  ai.x = safeCorner.x;
  ai.z = safeCorner.z;
  ai.yaw = 0;
  ai.stamina = 100;
  ai.tackleCooldown = 0;
  ai.aiThink = 999;
  ai.aiTarget = { kind: "idle", x: safeCorner.x, z: safeCorner.z };

  for (let i = 0; i < 80; i += 1) {
    game.updateTimers(0.5);
    game.updateAiPlayers(0.5);
    ai.state = PLAYER_STATES.king;
  }

  assert(ai.stamina >= RULES.tackleCost, `AI should retain tackle stamina without a target, got ${ai.stamina}`);
}

function checkAiChaseConsumesStamina() {
  const game = new OverthroneGame();
  game.setAiCount(3);
  game.startMatch();

  const ai = game.players.find((player) => !player.isLocal && player.team === "red");
  ai.state = PLAYER_STATES.attacker;
  ai.x = -12;
  ai.z = 0;
  ai.stamina = 100;
  ai.aiThink = 999;
  ai.aiTarget = { kind: "chase", x: 12, z: 0 };

  game.updateAiPlayers(0.5);

  assert(ai.stamina < 100, `AI chase movement should consume stamina, got ${ai.stamina}`);
}

function assert(condition, message) {
  if (!condition) {
    throw new Error(message);
  }
}

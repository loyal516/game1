import { chromium } from "playwright";
import { mkdir } from "node:fs/promises";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const url = process.env.OVERTHRONE_BASE_URL ?? "http://127.0.0.1:5173";
const root = resolve(dirname(fileURLToPath(import.meta.url)), "../..");
const artifactDir = resolve(root, "prototype/artifacts");

const browser = await chromium.launch({ headless: true });
try {
  await mkdir(artifactDir, { recursive: true });
  const results = [];
  results.push(await runCase({ name: "desktop", viewport: { width: 1366, height: 768 } }));
  results.push(await runCase({ name: "mobile", viewport: { width: 390, height: 844 } }));

  console.log(JSON.stringify({ ok: true, results }, null, 2));
} finally {
  await browser.close();
}

async function runCase({ name, viewport }) {
  const page = await browser.newPage({ viewport });
  await page.goto(url, { waitUntil: "networkidle" });
  await page.getByRole("button", { name: "프로토타입 시작" }).click();
  await page.getByRole("button", { name: "방 만들기" }).click();
  await page.getByRole("button", { name: "Blue 진영" }).click();
  await page.getByRole("button", { name: "AI 5명" }).click();
  await page.getByRole("button", { name: "게임 시작" }).click();

  await page.waitForSelector("canvas");
  await page.waitForSelector("[data-testid='action-bar']");
  await page.waitForSelector("[data-testid='minimap']", { state: "attached" });
  await page.waitForTimeout(1200);
  const pointerButtonStable = await page.evaluate(async () => {
    const button = document.querySelector("button[data-action='lock-pointer']");
    await new Promise((resolve) => setTimeout(resolve, 350));
    return Boolean(button && button.isConnected && button === document.querySelector("button[data-action='lock-pointer']"));
  });
  if (!pointerButtonStable) {
    throw new Error(`pointer lock button detached before click for ${name}`);
  }
  await page.getByRole("button", { name: "클릭해서 마우스 조작 시작" }).click();
  const beforeLook = await page.evaluate(() => window.__OVERTHRONE_DEBUG__.getViewState());
  await page.waitForFunction(() => window.__OVERTHRONE_DEBUG__?.getViewState?.().mouseLookActive);
  const activeLook = await page.evaluate(() => window.__OVERTHRONE_DEBUG__.getViewState());
  await page.mouse.move(Math.round(viewport.width * 0.45), Math.round(viewport.height * 0.45));
  await page.mouse.move(Math.round(viewport.width * 0.65), Math.round(viewport.height * 0.38));
  await page.waitForTimeout(100);
  const afterLook = await page.evaluate(() => window.__OVERTHRONE_DEBUG__.getViewState());
  const beforeMove = await snapshot(page);
  await page.keyboard.down("w");
  await page.waitForTimeout(700);
  await page.keyboard.up("w");
  const afterMove = await snapshot(page);

  await page.screenshot({ path: resolve(artifactDir, `${name}.png`), fullPage: true });
  await page.close();

  const dx = afterMove.localPlayer.x - beforeMove.localPlayer.x;
  const dz = afterMove.localPlayer.z - beforeMove.localPlayer.z;
  const moved = Math.hypot(dx, dz);
  const yawDelta = Math.abs(afterLook.yaw - beforeLook.yaw);
  const pitchDelta = Math.abs(afterLook.pitch - beforeLook.pitch);
  const lookDelta = Math.hypot(yawDelta, pitchDelta);
  const uniqueColors = new Set(afterMove.pixelSample.colors.map((color) => color.join(","))).size;
  const brightPixels = afterMove.pixelSample.colors.filter(([r, g, b, a]) => a > 0 && r + g + b > 45).length;

  const result = {
    name,
    viewport,
    phase: afterMove.phase,
    playerCount: afterMove.players.length,
    capturePoints: afterMove.capturePoints.length,
    playerState: afterMove.localPlayer.state,
    moved: Number(moved.toFixed(3)),
    view: {
      yawDelta: Number(yawDelta.toFixed(3)),
      pitchDelta: Number(pitchDelta.toFixed(3)),
      lookDelta: Number(lookDelta.toFixed(3)),
      active: activeLook.mouseLookActive,
      pointerLocked: activeLook.pointerLocked,
      fallbackLook: activeLook.fallbackLook,
      fov: afterMove.view.fov
    },
    hud: {
      actionBar: afterMove.actionBarText,
      minimap: afterMove.hasMinimap,
      pointerButtonStable
    },
    canvas: {
      width: afterMove.canvas.width,
      height: afterMove.canvas.height,
      uniqueColors,
      brightPixels
    }
  };

  if (
    result.phase !== "playing" ||
    result.playerCount < 4 ||
    result.capturePoints !== 3 ||
    result.canvas.width < viewport.width ||
    result.canvas.height < viewport.height ||
    result.canvas.uniqueColors < 2 ||
    result.canvas.brightPixels < 2 ||
    result.moved < 0.2 ||
    !result.view.active ||
    result.view.lookDelta < 0.2 ||
    !result.hud.actionBar.includes("슬라임") ||
    !result.hud.minimap
  ) {
    throw new Error(`smoke check failed for ${name}: ${JSON.stringify(result)}`);
  }

  return result;
}

async function snapshot(page) {
  return page.evaluate(() => {
    const canvas = document.querySelector("canvas");
    if (!canvas) {
      throw new Error("canvas-missing");
    }

    const rect = canvas.getBoundingClientRect();
    const hud = document.querySelector("[data-testid='hud']");
    const actionBar = document.querySelector("[data-testid='action-bar']");
    const minimap = document.querySelector("[data-testid='minimap']");
    const state = window.__OVERTHRONE_DEBUG__?.getSnapshot?.();
    const view = window.__OVERTHRONE_DEBUG__?.getViewState?.();
    const pixelSample = window.__OVERTHRONE_DEBUG__?.samplePixels?.();

    if (!hud || !actionBar || !minimap || !state || !view || !pixelSample) {
      throw new Error("debug-surface-missing");
    }

    return {
      ...state,
      view,
      pointerLocked: document.pointerLockElement === canvas,
      actionBarText: actionBar.textContent ?? "",
      hasMinimap: Boolean(minimap),
      canvas: {
        width: rect.width,
        height: rect.height
      },
      pixelSample
    };
  });
}

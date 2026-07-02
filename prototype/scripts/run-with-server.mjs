import { spawn } from "node:child_process";
import { createServer } from "node:net";

const port = await findFreePort();
const url = `http://127.0.0.1:${port}`;
const viteBin = process.platform === "win32" ? "npx.cmd" : "npx";

const server = spawn(viteBin, ["vite", "--host", "127.0.0.1", "--port", String(port), "--strictPort"], {
  stdio: ["ignore", "pipe", "pipe"]
});

let serverOutput = "";
server.stdout.on("data", (chunk) => {
  serverOutput += chunk.toString();
});
server.stderr.on("data", (chunk) => {
  serverOutput += chunk.toString();
});

try {
  await waitForServer();
  await runSmokeCheck();
} finally {
  server.kill("SIGTERM");
}

async function runSmokeCheck() {
  await new Promise((resolve, reject) => {
    const smoke = spawn(process.execPath, ["prototype/scripts/smoke-check.mjs"], {
      stdio: "inherit",
      env: {
        ...process.env,
        OVERTHRONE_BASE_URL: url
      }
    });
    smoke.on("exit", (code) => {
      if (code === 0) {
        resolve();
      } else {
        reject(new Error(`smoke-check exited with code ${code}`));
      }
    });
  });
}

async function waitForServer() {
  const started = Date.now();
  while (Date.now() - started < 15_000) {
    if (await isServerReady()) {
      return;
    }
    await new Promise((resolve) => setTimeout(resolve, 250));
  }
  throw new Error(`Vite server did not start in time.\n${serverOutput}`);
}

async function isServerReady() {
  try {
    const response = await fetch(url);
    return response.ok;
  } catch {
    return false;
  }
}

async function findFreePort() {
  return new Promise((resolve, reject) => {
    const probe = createServer();
    probe.unref();
    probe.on("error", reject);
    probe.listen(0, "127.0.0.1", () => {
      const address = probe.address();
      probe.close(() => {
        if (address && typeof address === "object") {
          resolve(address.port);
        } else {
          reject(new Error("Could not allocate a verification port"));
        }
      });
    });
  });
}

const canvas = document.getElementById("track");
const context = canvas.getContext("2d");
const scoreEl = document.getElementById("score");
const speedEl = document.getElementById("speed");
const bestEl = document.getElementById("best");
const overlay = document.getElementById("overlay");
const statusEl = document.getElementById("status");
const startButton = document.getElementById("startButton");

const leftButton = document.getElementById("left");
const rightButton = document.getElementById("right");
const brakeButton = document.getElementById("brake");

const state = {
  running: false,
  score: 0,
  speed: 4,
  maxSpeed: 12,
  trackOffset: 0,
  laneCount: 3,
  laneWidth: canvas.width / 3,
  car: {
    lane: 1,
    x: canvas.width / 2,
    y: canvas.height - 120,
    width: 60,
    height: 110,
  },
  obstacles: [],
  stars: [],
  tick: 0,
  best: 0,
  braking: false,
};

const controls = {
  left: false,
  right: false,
};

const COLORS = {
  road: "#0d0f1a",
  lane: "rgba(123, 141, 255, 0.35)",
  edge: "#4b4d79",
  car: "#ff4f6d",
  obstacle: "#ffd166",
  glow: "rgba(255, 79, 109, 0.4)",
};

function resetGame() {
  state.score = 0;
  state.speed = 4;
  state.trackOffset = 0;
  state.obstacles = [];
  state.tick = 0;
  state.car.lane = 1;
  state.car.x = laneToX(state.car.lane);
  state.braking = false;
  spawnStars();
}

function spawnStars() {
  state.stars = Array.from({ length: 40 }, () => ({
    x: Math.random() * canvas.width,
    y: Math.random() * canvas.height,
    radius: Math.random() * 1.8 + 0.6,
    alpha: Math.random() * 0.7 + 0.2,
  }));
}

function laneToX(lane) {
  return lane * state.laneWidth + state.laneWidth / 2;
}

function spawnObstacle() {
  const lane = Math.floor(Math.random() * state.laneCount);
  const width = 70;
  const height = 120;
  state.obstacles.push({
    lane,
    x: laneToX(lane),
    y: -height,
    width,
    height,
    speed: state.speed + Math.random() * 2,
  });
}

function updateControls() {
  if (controls.left) {
    state.car.lane = Math.max(0, state.car.lane - 1);
    controls.left = false;
  }
  if (controls.right) {
    state.car.lane = Math.min(state.laneCount - 1, state.car.lane + 1);
    controls.right = false;
  }
  state.car.x = laneToX(state.car.lane);
}

function updateObstacles() {
  state.obstacles.forEach((obstacle) => {
    obstacle.y += obstacle.speed;
  });
  state.obstacles = state.obstacles.filter((obstacle) => obstacle.y < canvas.height + 200);
}

function checkCollisions() {
  return state.obstacles.some((obstacle) => {
    const dx = Math.abs(state.car.x - obstacle.x);
    const dy = Math.abs(state.car.y - obstacle.y);
    return dx < (state.car.width + obstacle.width) / 2 - 10 &&
      dy < (state.car.height + obstacle.height) / 2 - 10;
  });
}

function updateScore() {
  state.score += state.speed * 0.4;
  if (state.speed < state.maxSpeed) {
    state.speed += 0.0025;
  }
}

function drawBackground() {
  context.fillStyle = "#0a0b16";
  context.fillRect(0, 0, canvas.width, canvas.height);

  state.stars.forEach((star) => {
    context.fillStyle = `rgba(200, 210, 255, ${star.alpha})`;
    context.beginPath();
    context.arc(star.x, star.y, star.radius, 0, Math.PI * 2);
    context.fill();
  });
}

function drawRoad() {
  context.fillStyle = COLORS.road;
  context.fillRect(80, 0, canvas.width - 160, canvas.height);

  context.strokeStyle = COLORS.edge;
  context.lineWidth = 6;
  context.strokeRect(80, 0, canvas.width - 160, canvas.height);

  context.strokeStyle = COLORS.lane;
  context.lineWidth = 4;
  const dashLength = 32;
  const gap = 26;
  const offset = state.trackOffset % (dashLength + gap);

  for (let lane = 1; lane < state.laneCount; lane += 1) {
    const x = lane * state.laneWidth;
    context.setLineDash([dashLength, gap]);
    context.lineDashOffset = -offset;
    context.beginPath();
    context.moveTo(x, -canvas.height);
    context.lineTo(x, canvas.height * 2);
    context.stroke();
  }
  context.setLineDash([]);
}

function drawCar() {
  context.save();
  context.translate(state.car.x, state.car.y);
  context.fillStyle = COLORS.glow;
  context.beginPath();
  context.ellipse(0, 30, state.car.width / 1.2, state.car.height / 1.1, 0, 0, Math.PI * 2);
  context.fill();

  context.fillStyle = COLORS.car;
  context.beginPath();
  context.roundRect(-state.car.width / 2, -state.car.height / 2, state.car.width, state.car.height, 18);
  context.fill();

  context.fillStyle = "rgba(255, 255, 255, 0.85)";
  context.fillRect(-state.car.width / 3, -state.car.height / 4, state.car.width / 1.5, state.car.height / 3);
  context.restore();
}

function drawObstacle(obstacle) {
  context.save();
  context.translate(obstacle.x, obstacle.y);
  context.fillStyle = "rgba(255, 209, 102, 0.3)";
  context.beginPath();
  context.ellipse(0, 30, obstacle.width / 1.1, obstacle.height / 1.2, 0, 0, Math.PI * 2);
  context.fill();

  context.fillStyle = COLORS.obstacle;
  context.beginPath();
  context.roundRect(-obstacle.width / 2, -obstacle.height / 2, obstacle.width, obstacle.height, 18);
  context.fill();
  context.restore();
}

function render() {
  drawBackground();
  drawRoad();
  state.obstacles.forEach(drawObstacle);
  drawCar();
}

function update() {
  if (!state.running) {
    render();
    return;
  }

  state.tick += 1;
  state.trackOffset += state.speed * 3.5;
  updateControls();
  updateObstacles();
  updateScore();

  if (state.tick % 70 === 0) {
    spawnObstacle();
  }

  if (state.braking) {
    state.speed = Math.max(2.5, state.speed - 0.06);
  }

  render();

  if (checkCollisions()) {
    endGame();
  }

  updateHud();
  requestAnimationFrame(update);
}

function updateHud() {
  scoreEl.textContent = Math.floor(state.score);
  speedEl.textContent = state.speed.toFixed(1);
  bestEl.textContent = state.best;
}

function startGame() {
  if (state.running) {
    return;
  }
  overlay.classList.add("hidden");
  state.running = true;
  resetGame();
  updateHud();
  requestAnimationFrame(update);
}

function endGame() {
  state.running = false;
  state.best = Math.max(state.best, Math.floor(state.score));
  statusEl.textContent = "Crashed! Press Space to Retry";
  startButton.textContent = "Race Again";
  overlay.classList.remove("hidden");
}

function handleKeydown(event) {
  if (event.repeat) {
    return;
  }
  if (event.code === "ArrowLeft" || event.code === "KeyA") {
    controls.left = true;
  }
  if (event.code === "ArrowRight" || event.code === "KeyD") {
    controls.right = true;
  }
  if (event.code === "Space") {
    startGame();
  }
  if (event.code === "ArrowDown" || event.code === "KeyS") {
    state.braking = true;
  }
}

function handleKeyup(event) {
  if (event.code === "ArrowDown" || event.code === "KeyS") {
    state.braking = false;
  }
}

leftButton.addEventListener("click", () => {
  controls.left = true;
});

rightButton.addEventListener("click", () => {
  controls.right = true;
});

brakeButton.addEventListener("mousedown", () => {
  state.braking = true;
});

brakeButton.addEventListener("mouseup", () => {
  state.braking = false;
});

brakeButton.addEventListener("touchstart", () => {
  state.braking = true;
});

brakeButton.addEventListener("touchend", () => {
  state.braking = false;
});

startButton.addEventListener("click", startGame);

window.addEventListener("keydown", handleKeydown);
window.addEventListener("keyup", handleKeyup);

spawnStars();
render();

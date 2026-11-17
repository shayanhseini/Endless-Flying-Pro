// Camera-preview drawing: skeleton, the two wrists that fly the drone (the wings), and the
// player frame that turns green on lock. Draws in RAW image coordinates; mirror
// the canvas with CSS (transform: scaleX(-1)) together with the <video>.

import { SKELETON, L_SHOULDER, R_SHOULDER, L_WRIST, R_WRIST, COUNT } from './core/landmarks.js';

const HAND_COLORS = { held: '#34d399', free: '#f2c14e' };
const SIDE_COLORS = { '-1': '#3aa8ff', '0': '#34d399', '1': '#ff6b6b' };
const VERT_COLORS = { '1': '#a78bfa', '-1': '#fb923c' };
export const FRAME_COLORS = { searching: 'rgba(255,255,255,0.85)', detected: '#f2c14e', locked: '#34d399' };

/**
 * @param state { held, side, vert } — from the C# drone core: wings up (green) or
 *              not (gold), roll zone -1/0/+1 and pitch zone -1/0/+1
 */
export function drawOverlay(ctx, landmarks, state, { minVisibility = 0.5, handVisibility = 0.35, clear = true, ghost = false, light = false } = {}) {
  const { width: w, height: h } = ctx.canvas;
  if (clear) ctx.clearRect(0, 0, w, h);
  if (!landmarks) return;
  if (ghost) {
    // a body the model sees but that is not the player
    ctx.save();
    ctx.lineWidth = Math.max(2, Math.min(w, h) * 0.006);
    ctx.strokeStyle = 'rgba(200,200,200,0.6)';
    ctx.setLineDash([6, 6]);
    for (const [a, b] of SKELETON) {
      const pa = landmarks[a], pb = landmarks[b];
      if (!pa || !pb || (pa.visibility ?? 1) < minVisibility || (pb.visibility ?? 1) < minVisibility) continue;
      ctx.beginPath(); ctx.moveTo(pa.x * w, pa.y * h); ctx.lineTo(pb.x * w, pb.y * h); ctx.stroke();
    }
    ctx.restore();
    return;
  }
  const P = (i, min = minVisibility) => landmarks[i] && (landmarks[i].visibility ?? 1) >= min ? landmarks[i] : null;
  const scale = Math.min(w, h);

  ctx.lineCap = 'round';
  ctx.lineWidth = Math.max(2, scale * 0.008);
  ctx.strokeStyle = 'rgba(255,255,255,0.55)';
  for (const [a, b] of light ? [] : SKELETON) {
    const pa = P(a, a === L_WRIST || a === R_WRIST ? handVisibility : minVisibility);
    const pb = P(b, b === L_WRIST || b === R_WRIST ? handVisibility : minVisibility);
    if (!pa || !pb) continue;
    ctx.beginPath();
    ctx.moveTo(pa.x * w, pa.y * h);
    ctx.lineTo(pb.x * w, pb.y * h);
    ctx.stroke();
  }

  // what the game reads: the two wrists (the wings) against the shoulder line
  const color = state.held ? HAND_COLORS.held : HAND_COLORS.free;
  const lw = P(L_WRIST, handVisibility), rw = P(R_WRIST, handVisibility);
  const ls = P(L_SHOULDER), rs = P(R_SHOULDER);
  if (ls && rs && state.held) {
    // the shoulder line, extended: up / down is measured from it
    const dx = ls.x - rs.x, dy = ls.y - rs.y;
    ctx.lineWidth = Math.max(2, scale * 0.005);
    ctx.setLineDash([8, 8]);
    ctx.strokeStyle = state.vert ? VERT_COLORS[String(state.vert)] : 'rgba(255,255,255,0.45)';
    ctx.beginPath();
    ctx.moveTo((rs.x - dx * 1.6) * w, (rs.y - dy * 1.6) * h);
    ctx.lineTo((ls.x + dx * 1.6) * w, (ls.y + dy * 1.6) * h);
    ctx.stroke();
    ctx.setLineDash([]);
  }
  if (lw && rw && state.held) {
    ctx.lineWidth = Math.max(4, scale * 0.014);
    ctx.strokeStyle = state.side ? SIDE_COLORS[String(state.side)] : state.vert ? VERT_COLORS[String(state.vert)] : color;
    ctx.beginPath();
    ctx.moveTo(lw.x * w, lw.y * h);
    ctx.lineTo(rw.x * w, rw.y * h);
    ctx.stroke();
  }
  ctx.fillStyle = color;
  for (const p of [lw, rw]) {
    if (!p) continue;
    ctx.beginPath();
    ctx.arc(p.x * w, p.y * h, Math.max(5, scale * 0.024), 0, Math.PI * 2);
    ctx.fill();
  }
  ctx.fillStyle = 'rgba(255,255,255,0.8)';
  for (const p of [P(L_SHOULDER), P(R_SHOULDER)]) {
    if (!p) continue;
    ctx.beginPath();
    ctx.arc(p.x * w, p.y * h, Math.max(3, scale * 0.01), 0, Math.PI * 2);
    ctx.fill();
  }
}

/** Smoothed box around the visible body (0..1 image coords), or null. */
export function bodyBox(landmarks, prev, { minVisibility = 0.5, pad = 0.14, smooth = 0.35 } = {}) {
  if (!landmarks) return null;
  let x0 = 1, y0 = 1, x1 = 0, y1 = 0, n = 0;
  for (let i = 0; i < COUNT; i++) {
    const p = landmarks[i];
    if (!p || (p.visibility ?? 1) < minVisibility) continue;
    if (p.x < -0.1 || p.x > 1.1 || p.y < -0.1 || p.y > 1.1) continue;
    x0 = Math.min(x0, p.x); x1 = Math.max(x1, p.x);
    y0 = Math.min(y0, p.y); y1 = Math.max(y1, p.y);
    n++;
  }
  if (n < 4) return null;
  const bw = x1 - x0, bh = y1 - y0;
  const box = {
    x0: Math.max(0.01, x0 - bw * pad), x1: Math.min(0.99, x1 + bw * pad),
    y0: Math.max(0.01, y0 - bh * pad * 0.8), y1: Math.min(0.99, y1 + bh * pad * 0.5),
  };
  if (!prev) return box;
  for (const k of ['x0', 'x1', 'y0', 'y1']) box[k] = prev[k] + (box[k] - prev[k]) * smooth;
  return box;
}

/**
 * Scanner-style corner frame around the player.
 * @param phase 'searching' | 'detected' | 'locked'
 * @param progress 0..1 lock progress (drawn along the frame while 'detected')
 * @param pulse 0..1 extra glow right after locking
 */
export function drawBodyFrame(ctx, box, phase, progress = 0, pulse = 0) {
  if (!box) return;
  const { width: w, height: h } = ctx.canvas;
  const x = box.x0 * w, y = box.y0 * h, bw = (box.x1 - box.x0) * w, bh = (box.y1 - box.y0) * h;
  const s = Math.min(w, h);
  const color = FRAME_COLORS[phase] || FRAME_COLORS.searching;
  const len = Math.min(bw, bh) * 0.22;
  ctx.save();
  ctx.lineCap = 'round';
  ctx.lineJoin = 'round';
  ctx.strokeStyle = color;
  ctx.lineWidth = Math.max(3, s * (phase === 'locked' ? 0.014 : 0.009));
  if (phase === 'locked') {
    ctx.shadowColor = color;
    ctx.shadowBlur = s * (0.03 + 0.05 * pulse);
    ctx.fillStyle = `rgba(52, 211, 153, ${0.10 + 0.12 * pulse})`;
    ctx.fillRect(x, y, bw, bh);
  }
  // four corner brackets
  const corners = [[x, y, 1, 1], [x + bw, y, -1, 1], [x, y + bh, 1, -1], [x + bw, y + bh, -1, -1]];
  ctx.beginPath();
  for (const [cx, cy, dx, dy] of corners) {
    ctx.moveTo(cx + dx * len, cy);
    ctx.lineTo(cx, cy);
    ctx.lineTo(cx, cy + dy * len);
  }
  ctx.stroke();
  // lock progress along the bottom edge
  if (phase === 'detected' && progress > 0) {
    ctx.shadowBlur = 0;
    ctx.strokeStyle = FRAME_COLORS.locked;
    ctx.lineWidth = Math.max(4, s * 0.012);
    ctx.beginPath();
    ctx.moveTo(x + len, y + bh);
    ctx.lineTo(x + len + (bw - 2 * len) * Math.min(1, progress), y + bh);
    ctx.stroke();
  }
  ctx.restore();
}

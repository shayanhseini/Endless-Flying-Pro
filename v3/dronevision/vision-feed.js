// VisionFeed — camera + MediaPipe pose model, nothing else.
//
// Emits the raw pose landmarks of every camera frame. It contains NO game
// logic: the wings gesture, the lock and the keys live in the C# core inside
// Unity (compiled to WebAssembly).
//
// Performance (2026-11-18 rewrite for hosted builds, see pose.js for the measurements):
//   · the model runs in a Web Worker on the CPU (desktops): the game's main thread and
//     its GPU never wait for it; one frame in flight, always the newest
//   · model 'auto': lite on phones and on PCs with ≤ 4 CPU threads, full (as the PC app) on
//     the others, dropping to lite by itself when too slow (_adapt: < 18 FPS AND model
//     > 40 ms for 5 s); 640×360 camera (bigger inputs measured the same time and landmarks)
//   · setPaused(true) stops inference entirely (menus, results) while keeping the camera
//
// Accuracy in crowds and bad light (image-cues.js): adaptive shadow lift (back-light),
// and a clothing-colour signature per person (`sigs`) for the C# crowd tracker.
// Removed on the web: the crop around the player (Tasks models jump off the hands on a
// crop, measured) and camera-sensor exposure control (was never switched on).

import { openCamera, listCameras, watchCameras, stopStream, cameraSupport, CameraError, CAMERA_ERROR } from './camera.js';
import { createPoseDetector, TASKS_VISION_VERSION } from './pose.js';

export { TASKS_VISION_VERSION, CAMERA_ERROR };

export const FEED_STATUS = Object.freeze({
  IDLE: 'idle',
  LOADING: 'loading',
  RUNNING: 'running',
  MODEL_FAILED: 'model-failed',   // model / WebAssembly files could not be loaded
  ...CAMERA_ERROR,
});

const STORE_KEY = 'dronevision.camera';
const store = {
  get() { try { return localStorage.getItem(STORE_KEY); } catch { return null; } },
  set(v) { try { localStorage.setItem(STORE_KEY, v); } catch { /* private mode */ } },
};

export class VisionFeed {
  /**
   * @param o.model       'auto' (default: lite on phones / ≤ 4 threads, else full) | 'full' | 'lite'
   * @param o.delegate    'auto' (default: CPU on desktops, GPU/CPU probe on phones) | 'GPU' | 'CPU'
   * @param o.numPoses    1 (default, like the Python app): MediaPipe follows the one person it
   *                    found and never looks for a second, so a neighbour is not even seen.
   *                    2–3 make it search the whole frame every frame (measured 50–75% slower
   *                    with one person in view) and hand the neighbour to the tracker.
   * @param o.adaptive    drop to the lite model when FPS is low (default true)
   * @param o.worker      'auto' (default): run the model in a Web Worker when supported
   * @param o.maxFps      inference rate cap (default 30)
   * @param o.exposure    adaptive shadow lift in software (default true)
   * @param o.width/height/fps  requested camera format (default 640×360@30)
   * @param o.assetsBase  vendor folder URL (default: ../vendor/ next to src/)
   * @param o.video       existing <video> to use (default: a hidden one is created)
   */
  constructor(o = {}) {
    this.opts = {
      model: 'auto', delegate: 'auto', numPoses: 1, adaptive: true, worker: 'auto', maxFps: 30,
      exposure: true,
      width: 640, height: 360, fps: 30,
      assetsBase: new URL('../vendor/', import.meta.url).href,
      ...o,
    };
    this.video = o.video || createHiddenVideo();
    this.detector = null;
    this.stream = null;
    this.source = 'none';      // 'camera' | 'video' | 'simulator'
    this.cameras = [];
    this.cameraId = null;
    this.cameraLabel = '';
    this.poses = [];           // raw poses of the last processed frame (full-frame coords)
    this.sigs = [];            // clothing-colour signature [r,g,b] per pose, or null
    this.aspect = 4 / 3;
    this.frameSeq = 0;
    this.frameT = 0;
    this.paused = false;
    this.runStatus = FEED_STATUS.RUNNING;
    this._handlers = {};
    this._running = false;
    this._loopToken = 0;
    this._lastFrameT = 0;
    this._lastSent = 0;
    this._slowSince = 0;
    this._startedAt = 0;
    this._busy = false;
    this._dirty = false;
    this._wakeLock = null;
    this._objectUrl = null;
    this._recovering = false;
    this._unwatch = watchCameras(() => this.refreshCameras());
    this._onVisibility = () => this._visibilityChanged();
    document.addEventListener('visibilitychange', this._onVisibility);
    this.status = FEED_STATUS.IDLE;
    this.stats = { fps: 0, inferMs: 0, latencyMs: 0, model: '', delegate: '', runner: '', resolution: '', gain: 1, lum: 0, focus: false };
  }

  // ---- events --------------------------------------------------------------
  on(event, fn) { (this._handlers[event] ||= new Set()).add(fn); return () => this.off(event, fn); }
  off(event, fn) { this._handlers[event]?.delete(fn); }
  _emit(event, arg) { this._handlers[event]?.forEach(fn => { try { fn(arg); } catch (e) { console.error(e); } }); }

  _setStatus(status) {
    if (status === this.status) return;
    this.status = status;
    this._emit('status', status);
  }

  // ---- cameras ---------------------------------------------------------------
  async refreshCameras() {
    try {
      this.cameras = await listCameras();
    } catch {
      this.cameras = [];
    }
    this._emit('cameras', this.cameras);
    return this.cameras;
  }

  /**
   * Start with the camera. Uses the remembered camera if it is still plugged in,
   * otherwise the front camera. Loads the pose model in parallel with the camera.
   */
  async start({ deviceId } = {}) {
    this._setStatus(FEED_STATUS.LOADING);
    const unsupported = cameraSupport();
    if (unsupported) {
      this._setStatus(unsupported);
      this._emit('error', new CameraError(unsupported));
      return false;
    }
    const model = this._ensureDetector();
    try {
      await this.refreshCameras();
      const remembered = deviceId || store.get();
      const known = remembered && this.cameras.some(c => c.id === remembered) ? remembered : null;
      await this._openCamera(known);
      await model;
    } catch (err) {
      this._fail(err);
      return false;
    }
    this._run();
    return true;
  }

  async useCamera(deviceId) {
    this._setStatus(FEED_STATUS.LOADING);
    try {
      await this._openCamera(deviceId);
      await this._ensureDetector();
    } catch (err) {
      this._fail(err);
      return false;
    }
    this._run();
    return true;
  }

  // A USB camera that drops out (loose cable, hub reset) is reopened
  // automatically: the same one if it comes back, otherwise any camera.
  async _recoverCamera() {
    if (this._recovering) return;
    this._recovering = true;
    this._setStatus(FEED_STATUS.LOADING);
    const id = this.cameraId;
    try {
      for (let attempt = 0; attempt < 5 && this.source === 'camera'; attempt++) {
        await new Promise(r => setTimeout(r, 600 + attempt * 400));
        await this.refreshCameras();
        const same = id && this.cameras.some(c => c.id === id);
        try {
          await this._openCamera(same ? id : null);
          this._run();
          return;
        } catch { /* try again */ }
      }
      this._fail(new CameraError(CAMERA_ERROR.NOT_FOUND));
    } finally {
      this._recovering = false;
    }
  }

  async _openCamera(deviceId) {
    this._stopSource();
    stopStream(this.stream);
    this._revokeObjectUrl();
    this.video.removeAttribute('src');
    const cam = await openCamera(this.video, {
      deviceId, width: this.opts.width, height: this.opts.height, fps: this.opts.fps,
    });
    this.stream = cam.stream;
    this.source = 'camera';
    this.cameraId = cam.settings.deviceId || deviceId || null;
    this.cameraLabel = cam.label;
    if (this.cameraId) store.set(this.cameraId);
    this.stats.resolution = `${this.video.videoWidth}×${this.video.videoHeight}`;
    await this.refreshCameras(); // labels become available after permission
    cam.track?.addEventListener('ended', () => {
      if (this.source === 'camera' && this.stream === cam.stream) this._recoverCamera();
    });
  }

  /** Test with a recorded video (e.g. booth footage) instead of the camera. */
  async useVideoFile(file) {
    this._setStatus(FEED_STATUS.LOADING);
    this._stopSource();
    stopStream(this.stream);
    this.stream = null;
    this.video.srcObject = null;
    this._revokeObjectUrl();
    this._objectUrl = URL.createObjectURL(file);
    this.video.src = this._objectUrl;
    this.video.loop = true;
    this.video.muted = true;
    await this.video.play().catch(() => {});
    if (!this.video.videoWidth) await new Promise(r => this.video.addEventListener('loadedmetadata', r, { once: true }));
    this.source = 'video';
    this.cameraLabel = file.name;
    this.stats.resolution = `${this.video.videoWidth}×${this.video.videoHeight}`;
    try {
      await this._ensureDetector();
    } catch (err) {
      this._fail(err);
      return false;
    }
    this._run();
    return true;
  }

  /** Hook for subclasses with extra sources (the test page's simulator). */
  _stopSource() {}

  _revokeObjectUrl() {
    if (this._objectUrl) URL.revokeObjectURL(this._objectUrl);
    this._objectUrl = null;
  }

  // Phones: the player never touches the screen, so keep it awake while playing,
  // and resume the camera when the page comes back to the foreground.
  async _keepAwake(on) {
    try {
      if (on && !this._wakeLock && document.visibilityState === 'visible' && navigator.wakeLock) {
        this._wakeLock = await navigator.wakeLock.request('screen');
        this._wakeLock.addEventListener('release', () => { this._wakeLock = null; });
      } else if (!on && this._wakeLock) {
        await this._wakeLock.release();
        this._wakeLock = null;
      }
    } catch { /* not supported / denied: harmless */ }
  }

  _visibilityChanged() {
    if (document.visibilityState !== 'visible' || !this._running) return;
    if (this.source === 'camera' || this.source === 'video') {
      this.video.play().catch(() => {});
      this._keepAwake(true);
    }
  }

  /** Stop/resume inference (camera stays open). Use it in menus to save battery and GPU. */
  setPaused(paused) {
    this.paused = !!paused;
  }

  async _ensureDetector() {
    if (this.detector) return this.detector;
    if (!this._detectorPromise) {
      this._detectorPromise = createPoseDetector({
        model: this.opts.model, delegate: this.opts.delegate, worker: this.opts.worker,
        numPoses: this.opts.numPoses, assetsBase: this.opts.assetsBase,
      }).then(d => {
        this.detector = d;
        // the worker may switch GPU -> CPU by itself if the GPU fails at run time
        d.onModelChange = () => this._modelChanged();
        this._modelChanged();
        return d;
      }).catch(err => {
        this._detectorPromise = null;
        const e = err instanceof Error ? err : new Error(String(err));
        e.modelFailed = true;
        throw e;
      });
    }
    return this._detectorPromise;
  }

  _modelChanged() {
    const d = this.detector;
    if (!d) return;
    Object.assign(this.stats, { model: d.model, delegate: d.delegate, runner: d.mode });
    this._emit('model', { model: d.model, delegate: d.delegate, source: d.modelSource, runner: d.mode });
  }

  async setModel(model) {
    this.opts.model = model;
    if (!this.detector) return;
    await this.detector.setModel(model);
    this._slowSince = 0;
    this._modelChanged();
  }

  async setNumPoses(n) {
    this.opts.numPoses = n;
    if (this.detector) await this.detector.setNumPoses(n);
  }

  _fail(err) {
    const code = err instanceof CameraError ? err.code : err && err.modelFailed ? FEED_STATUS.MODEL_FAILED : FEED_STATUS.FAILED;
    console.error('[dronevision]', err);
    this._running = false;
    this._setStatus(code);
    this._keepAwake(false);
    this._emit('error', err);
  }

  // ---- frame loop --------------------------------------------------------------
  _run() {
    const token = ++this._loopToken;
    this._running = true;
    this._startedAt = performance.now();
    this._lastFrameT = 0;
    this._busy = false;
    this._dirty = false;
    if (this.detector) Object.assign(this.stats, { model: this.detector.model, delegate: this.detector.delegate, runner: this.detector.mode });
    if (this.source === 'camera') this._keepAwake(true);
    this._setStatus(this.runStatus);
    const v = this.video;
    if ('requestVideoFrameCallback' in HTMLVideoElement.prototype) {
      // exactly one inference per new camera frame: no duplicates, no waiting
      const tick = (now, meta) => {
        if (token !== this._loopToken) return;
        this._frame(now, meta.captureTime || meta.presentationTime || now);
        v.requestVideoFrameCallback(tick);
      };
      v.requestVideoFrameCallback(tick);
    } else {
      let lastTime = -1;
      const tick = now => {
        if (token !== this._loopToken) return;
        if (v.currentTime !== lastTime) {
          lastTime = v.currentTime;
          this._frame(now, now);
        }
        requestAnimationFrame(tick);
      };
      requestAnimationFrame(tick);
    }
  }

  _frame(now, captureT) {
    const v = this.video;
    if (this.paused || !this.detector || !v.videoWidth || v.readyState < 2) return;
    const t = performance.now();
    if (t - this._lastSent < 1000 / this.opts.maxFps - 4) return;
    if (this.detector.async) {
      // one frame in flight; while the worker is busy only the NEWEST frame waits
      if (this._busy) this._dirty = true;
      else this._sendFrame(captureT);
      return;
    }
    this._lastSent = t;
    let r;
    try {
      r = this.detector.detect(v, t, null, this.opts.exposure);
    } catch (err) {
      console.error('[dronevision] detect failed', err);
      return;
    }
    this.stats.inferMs = ema(this.stats.inferMs, performance.now() - t, 0.1);
    this.stats.gain = r.gain;
    this.stats.lum = r.lum;
    this._deliver(r.poses, r.sigs, v.videoWidth / v.videoHeight, t, captureT);
    this._adapt(t);
  }

  async _sendFrame(captureT) {
    const token = this._loopToken;
    const v = this.video;
    this._busy = true;
    try {
      const t0 = performance.now();
      this._lastSent = t0;
      const w = v.videoWidth, h = v.videoHeight;
      // never more than 512 px on the long side (the model works at 256 anyway)
      const k = Math.min(1, 512 / Math.max(w, h));
      const bitmap = await createImageBitmap(v,
        k < 1 ? { resizeWidth: Math.round(w * k), resizeHeight: Math.round(h * k), resizeQuality: 'low' } : undefined);
      if (token !== this._loopToken || !this.detector) { bitmap.close(); return; }
      const r = await this.detector.detect(bitmap, t0, null, this.opts.exposure);
      if (token !== this._loopToken) return;
      this.stats.inferMs = ema(this.stats.inferMs, r.inferMs, 0.1);
      this.stats.gain = r.gain;
      this.stats.lum = r.lum;
      this._deliver(r.poses, r.sigs, w / h, t0, captureT);
      this._adapt(t0);
    } catch (err) {
      if (token === this._loopToken) console.warn('[dronevision] frame skipped:', err.message || err);
    } finally {
      this._busy = false;
      if (this._dirty && token === this._loopToken && !this.paused) {
        this._dirty = false;
        this._sendFrame(performance.now());
      }
    }
  }

  _deliver(poses, sigs, aspect, t, captureT) {
    if (this._lastFrameT) {
      const dt = t - this._lastFrameT;
      if (dt > 0) this.stats.fps = ema(this.stats.fps, 1000 / dt, 0.1);
    }
    this._lastFrameT = t;
    this.stats.latencyMs = ema(this.stats.latencyMs, Math.max(0, performance.now() - captureT), 0.1);
    this.poses = poses;
    this.sigs = sigs || [];
    this.aspect = aspect;
    this.frameT = t;
    this.frameSeq = (this.frameSeq + 1) % 16777216;
    this.stats.focus = false;
    this._onPoses(poses, this.sigs, aspect, t);
  }

  /** Override to consume frames. */
  _onPoses(poses, sigs, aspect, t) {
    this._emit('poses', { poses, sigs, aspect, t });
  }

  /** Kept for API compatibility: the web build never crops (see the header). */
  setFocus() {}

  // Lower the model when the device cannot keep up (like the Python app's fallback).
  _adapt(now) {
    if (!this.opts.adaptive || !this.detector || this.detector.model === 'lite') return;   // only 'full' can drop
    if (now - this._startedAt < 3000) return;
    // as the PC app: slow AND the model itself is slow (a slow camera alone is not the model's fault)
    if (this.stats.fps > 0 && this.stats.fps < 18 && this.stats.inferMs > 40) {
      if (!this._slowSince) this._slowSince = now;
      if (now - this._slowSince > 5000 && !this._swapping) {
        this._swapping = true;
        const next = 'lite';
        console.info(`[dronevision] ${this.stats.fps.toFixed(1)} fps: switching to ${next} model`);
        this.setModel(next).finally(() => { this._swapping = false; this._startedAt = performance.now(); });
      }
    } else {
      this._slowSince = 0;
    }
  }

  stop() {
    this._loopToken++;
    this._running = false;
    this._stopSource();
    stopStream(this.stream);
    this.stream = null;
    this._revokeObjectUrl();
    this._keepAwake(false);
    this.source = 'none';
    this._setStatus(FEED_STATUS.IDLE);
  }

  dispose() {
    this.stop();
    this._unwatch();
    document.removeEventListener('visibilitychange', this._onVisibility);
    this.detector?.close();
    this.detector = null;
  }
}

function ema(prev, value, a) {
  return prev ? prev + (value - prev) * a : value;
}

function createHiddenVideo() {
  const v = document.createElement('video');
  v.muted = true;
  v.playsInline = true;
  v.setAttribute('playsinline', '');
  v.style.cssText = 'position:fixed;width:1px;height:1px;opacity:0;pointer-events:none;left:-10px;top:-10px';
  document.body.appendChild(v);
  return v;
}

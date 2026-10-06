import * as THREE from 'three';
import { FBXLoader } from 'three/addons/loaders/FBXLoader.js';

export type GameMode = 'menu' | 'playing' | 'paused' | 'gameover';

export type PowerupId = 'magnet' | 'shield' | 'speed' | 'fly' | 'double';

export type ActivePowerup = {
  id: PowerupId;
  label: string;
  remaining: number;
  color: string;
};

export type GameStats = {
  score: number;
  coins: number;
  distance: number;
  best: number;
  activePowerups: ActivePowerup[];
};

export type GameCallbacks = {
  onModeChange: (mode: GameMode) => void;
  onStats: (stats: GameStats) => void;
  onAssetWarning: (message: string | null) => void;
};

export type RunnerGameControls = {
  start: () => void;
  pause: () => void;
  resume: () => void;
  restart: () => void;
  left: () => void;
  right: () => void;
  jump: () => void;
  setSoundEnabled: (enabled: boolean) => void;
};

type EntityKind = 'coin' | 'gem' | 'powerup' | 'obstacle';

type Entity = {
  group: THREE.Group;
  kind: EntityKind;
  lane: number;
  z: number;
  y: number;
  type?: PowerupId | 'log' | 'rock' | 'stump';
  spent: boolean;
  phase: number;
  obstacleHeight?: number;
};

type PowerupState = {
  label: string;
  color: string;
  remaining: number;
};

const LANE_WIDTH = 2.5;
const LANES = [-LANE_WIDTH, 0, LANE_WIDTH];
const START_SPEED = 11.2;
const MAX_SPEED = 23;
const ROW_SPACING = 14;
const SPAWN_HORIZON = -82;
const BEST_KEY = 'jungle-dash-best-v1';
const BASE_PATH = import.meta.env.BASE_URL;

const POWERUP_META: Record<PowerupId, { label: string; color: string; duration: number }> = {
  magnet: { label: 'MAGNET', color: '#8be9fd', duration: 8 },
  shield: { label: 'PELINDUNG', color: '#a5c8ff', duration: 9 },
  speed: { label: 'CEPAT', color: '#ffb84e', duration: 7 },
  fly: { label: 'TERBANG', color: '#d5a8ff', duration: 8 },
  double: { label: 'SKOR 2X', color: '#f8e66b', duration: 10 },
};

const POWERUP_ORDER: PowerupId[] = ['magnet', 'shield', 'speed', 'fly', 'double'];

const rand = (min: number, max: number) => min + Math.random() * (max - min);
const pick = <T,>(items: T[]) => items[Math.floor(Math.random() * items.length)];
const clamp = (value: number, min: number, max: number) => Math.max(min, Math.min(max, value));

export class RunnerGame implements RunnerGameControls {
  private readonly host: HTMLElement;
  private readonly callbacks: GameCallbacks;
  private readonly scene = new THREE.Scene();
  private readonly camera = new THREE.PerspectiveCamera(58, 1, 0.1, 180);
  private renderer: THREE.WebGLRenderer | null = null;
  private fallbackCanvas: HTMLCanvasElement | null = null;
  private fallbackContext: CanvasRenderingContext2D | null = null;
  private readonly loader = new THREE.TextureLoader();
  private readonly fbxLoader = new FBXLoader();
  private readonly entities: Entity[] = [];
  private readonly trees: THREE.Group[] = [];
  private readonly laneMarkers: THREE.Group[] = [];
  private readonly runnerRoot = new THREE.Group();
  private readonly runnerVisual = new THREE.Group();
  private readonly fallbackLegs: THREE.Group[] = [];
  private readonly fallbackArms: THREE.Group[] = [];
  private readonly powerups = new Map<PowerupId, PowerupState>();
  private touchStart: { pointerId: number; x: number; y: number } | null = null;
  private mode: GameMode = 'menu';
  private soundEnabled = true;
  private animationFrame = 0;
  private lastFrameAt = performance.now();
  private disposed = false;
  private lastStatsAt = 0;
  private speed = START_SPEED;
  private distance = 0;
  private coins = 0;
  private score = 0;
  private pickupPoints = 0;
  private scoreAccumulator = 0;
  private best = this.readBest();
  private laneIndex = 1;
  private runnerX = 0;
  private jumpHeight = 0;
  private verticalSpeed = 0;
  private grounded = true;
  private spawnHeadZ = SPAWN_HORIZON;
  private previousSingleFreeLane = 1;
  private previousRowWasSingle = false;
  private lastSoundContext: AudioContext | null = null;
  private pathTexture: THREE.Texture | null = null;
  private treeTexture: THREE.Texture | null = null;
  private palmTexture: THREE.Texture | null = null;
  private mixer: THREE.AnimationMixer | null = null;
  private runAction: THREE.AnimationAction | null = null;
  private jumpAction: THREE.AnimationAction | null = null;
  private activeAction: THREE.AnimationAction | null = null;
  private shieldMesh: THREE.Mesh | null = null;
  private markerMaterial = new THREE.MeshStandardMaterial({ color: '#c9a96c', roughness: 0.9 });
  private readonly onResize = () => this.resize();

  constructor(host: HTMLElement, callbacks: GameCallbacks) {
    this.host = host;
    this.callbacks = callbacks;
    this.initializeRenderer();

    this.camera.position.set(0, 5.1, 9.8);
    this.camera.lookAt(0, 1.3, -10);
    this.configureWorld();
    this.createGround();
    this.createTrackDetails();
    this.createScenery();
    this.createRunner();
    this.loadUnityAssets();
    this.populatePreview();

    window.addEventListener('resize', this.onResize);
    this.callbacks.onModeChange(this.mode);
    this.emitStats(true);
    this.animationFrame = window.requestAnimationFrame(this.frame);
  }

  start = () => {
    this.resetRun();
    this.setMode('playing');
  };

  pause = () => {
    if (this.mode === 'playing') this.setMode('paused');
  };

  resume = () => {
    if (this.mode === 'paused') {
      this.lastFrameAt = performance.now();
      this.setMode('playing');
    }
  };

  restart = () => {
    this.resetRun();
    this.setMode('playing');
  };

  left = () => {
    if (this.mode === 'playing') this.laneIndex = Math.max(0, this.laneIndex - 1);
  };

  right = () => {
    if (this.mode === 'playing') this.laneIndex = Math.min(2, this.laneIndex + 1);
  };

  jump = () => {
    if (this.mode !== 'playing' || !this.grounded) return;
    this.grounded = false;
    this.verticalSpeed = 9.3;
    this.jumpHeight = 0.03;
    this.playAnimation('jump');
    this.playTone('jump');
  };

  setSoundEnabled = (enabled: boolean) => {
    this.soundEnabled = enabled;
  };

  destroy() {
    this.disposed = true;
    window.cancelAnimationFrame(this.animationFrame);
    window.removeEventListener('resize', this.onResize);
    this.renderer?.dispose();
    this.renderer?.domElement.removeEventListener('pointerdown', this.onPointerDown);
    this.renderer?.domElement.removeEventListener('pointerup', this.onPointerUp);
    this.renderer?.domElement.removeEventListener('pointercancel', this.onPointerCancel);
    this.renderer?.domElement.remove();
    if (this.fallbackCanvas) {
      this.fallbackCanvas.removeEventListener('pointerdown', this.onPointerDown);
      this.fallbackCanvas.removeEventListener('pointerup', this.onPointerUp);
      this.fallbackCanvas.removeEventListener('pointercancel', this.onPointerCancel);
    }
    this.fallbackCanvas?.remove();
    this.scene.traverse((object) => {
      if (object instanceof THREE.Mesh || object instanceof THREE.Line) {
        object.geometry.dispose();
        const materials = Array.isArray(object.material) ? object.material : [object.material];
        materials.forEach((material) => material.dispose());
      }
    });
    this.lastSoundContext?.close().catch(() => undefined);
  }

  private initializeRenderer() {
    const webglCanvas = document.createElement('canvas');
    try {
      const context = webglCanvas.getContext('webgl2', {
        antialias: true,
        alpha: false,
        powerPreference: 'high-performance',
      });
      if (context) {
        this.renderer = new THREE.WebGLRenderer({
          canvas: webglCanvas,
          context,
          antialias: true,
          alpha: false,
          powerPreference: 'high-performance',
        });
      }
    } catch (error) {
      console.warn('WebGL2 tidak tersedia; menggunakan render Canvas 2D.', error);
    }

    if (this.renderer) {
      this.renderer.setPixelRatio(Math.min(window.devicePixelRatio || 1, 1.65));
      this.renderer.setSize(this.host.clientWidth || window.innerWidth, this.host.clientHeight || window.innerHeight);
      this.renderer.outputColorSpace = THREE.SRGBColorSpace;
      this.renderer.toneMapping = THREE.ACESFilmicToneMapping;
      this.renderer.toneMappingExposure = 1.14;
      this.renderer.shadowMap.enabled = true;
      this.renderer.shadowMap.type = THREE.PCFShadowMap;
      this.renderer.domElement.className = 'jungle-canvas';
      this.renderer.domElement.setAttribute('aria-label', 'Arena 3D Jungle Dash');
      this.renderer.domElement.style.touchAction = 'none';
      this.host.appendChild(this.renderer.domElement);
      this.attachSwipeHandlers(this.renderer.domElement);
      return;
    }

    this.fallbackCanvas = document.createElement('canvas');
    this.fallbackCanvas.className = 'jungle-canvas';
    this.fallbackCanvas.setAttribute('aria-label', 'Arena Jungle Dash dalam mode grafis ringan');
    this.fallbackCanvas.style.touchAction = 'none';
    this.fallbackContext = this.fallbackCanvas.getContext('2d');
    if (!this.fallbackContext) {
      throw new Error('Browser ini tidak menyediakan WebGL2 maupun Canvas 2D.');
    }
    this.host.appendChild(this.fallbackCanvas);
    this.attachSwipeHandlers(this.fallbackCanvas);
    this.callbacks.onAssetWarning('Mode grafis ringan aktif karena WebGL2 tidak tersedia di browser ini.');
    this.resize();
  }

  private attachSwipeHandlers(canvas: HTMLCanvasElement) {
    canvas.addEventListener('pointerdown', this.onPointerDown);
    canvas.addEventListener('pointerup', this.onPointerUp);
    canvas.addEventListener('pointercancel', this.onPointerCancel);
  }

  private readonly onPointerDown = (event: PointerEvent) => {
    if (this.mode !== 'playing' || event.pointerType === 'mouse') return;
    this.touchStart = { pointerId: event.pointerId, x: event.clientX, y: event.clientY };
    event.preventDefault();
    (event.currentTarget as HTMLCanvasElement).setPointerCapture(event.pointerId);
  };

  private readonly onPointerUp = (event: PointerEvent) => {
    const start = this.touchStart;
    this.touchStart = null;
    if (this.mode !== 'playing' || !start || start.pointerId !== event.pointerId) return;
    event.preventDefault();
    const dx = event.clientX - start.x;
    const dy = event.clientY - start.y;
    if (Math.max(Math.abs(dx), Math.abs(dy)) < 30) return;
    if (Math.abs(dx) > Math.abs(dy)) {
      if (dx < 0) this.left();
      else this.right();
    } else if (dy < 0) {
      this.jump();
    }
  };

  private readonly onPointerCancel = () => {
    this.touchStart = null;
  };

  private configureWorld() {
    const sky = new THREE.Color('#a9d78b');
    this.scene.background = sky;
    this.scene.fog = new THREE.Fog(sky, 38, 128);

    const hemisphere = new THREE.HemisphereLight('#eaffce', '#365334', 2.1);
    this.scene.add(hemisphere);

    const sun = new THREE.DirectionalLight('#fff2bf', 3.15);
    sun.position.set(-12, 18, 5);
    sun.castShadow = true;
    sun.shadow.mapSize.set(1024, 1024);
    sun.shadow.camera.left = -22;
    sun.shadow.camera.right = 22;
    sun.shadow.camera.top = 28;
    sun.shadow.camera.bottom = -14;
    this.scene.add(sun);

    const warmFill = new THREE.PointLight('#f1b86a', 3.5, 42, 1.7);
    warmFill.position.set(-10, 7, -30);
    this.scene.add(warmFill);

    const sunDisc = new THREE.Mesh(
      new THREE.SphereGeometry(3.3, 20, 14),
      new THREE.MeshBasicMaterial({ color: '#fff0ad' }),
    );
    sunDisc.position.set(-15, 17, -92);
    this.scene.add(sunDisc);
  }

  private createGround() {
    const grassMaterial = new THREE.MeshStandardMaterial({ color: '#668b47', roughness: 1 });
    const grass = new THREE.Mesh(new THREE.PlaneGeometry(100, 200), grassMaterial);
    grass.rotation.x = -Math.PI / 2;
    grass.position.set(0, -0.32, -70);
    grass.receiveShadow = true;
    this.scene.add(grass);

    const pathMaterial = new THREE.MeshStandardMaterial({ color: '#a28b60', roughness: 0.95 });
    const path = new THREE.Mesh(new THREE.PlaneGeometry(8.5, 180), pathMaterial);
    path.rotation.x = -Math.PI / 2;
    path.position.set(0, -0.16, -70);
    path.receiveShadow = true;
    this.scene.add(path);

    this.loader.load(
      `${BASE_PATH}assets/unity/MudRocky.png`,
      (texture) => {
        if (this.disposed) {
          texture.dispose();
          return;
        }
        texture.colorSpace = THREE.SRGBColorSpace;
        texture.wrapS = THREE.RepeatWrapping;
        texture.wrapT = THREE.RepeatWrapping;
        texture.repeat.set(2, 22);
        texture.anisotropy = Math.min(8, this.renderer?.capabilities.getMaxAnisotropy() ?? 4);
        pathMaterial.map = texture;
        pathMaterial.color.set('#d3c59a');
        pathMaterial.needsUpdate = true;
        this.pathTexture = texture;
      },
      undefined,
      (error) => {
        console.warn('Tekstur jalur Unity tidak dapat dimuat; menggunakan material jalur bawaan.', error);
      },
    );

    this.loader.load(`${BASE_PATH}assets/unity/GrassHill.png`, (texture) => {
      if (this.disposed) {
        texture.dispose();
        return;
      }
      texture.colorSpace = THREE.SRGBColorSpace;
      texture.wrapS = THREE.RepeatWrapping;
      texture.wrapT = THREE.RepeatWrapping;
      texture.repeat.set(12, 24);
      grassMaterial.map = texture;
      grassMaterial.color.set('#9dbb69');
      grassMaterial.needsUpdate = true;
    });
  }

  private createTrackDetails() {
    for (let i = 0; i < 14; i += 1) {
      const marker = new THREE.Group();
      for (const x of [-1.36, 1.36]) {
        const dash = new THREE.Mesh(new THREE.BoxGeometry(0.07, 0.025, 3.1), this.markerMaterial);
        dash.position.set(x, -0.135, 0);
        marker.add(dash);
      }
      marker.position.z = -102 + i * 9;
      this.scene.add(marker);
      this.laneMarkers.push(marker);
    }
  }

  private createScenery() {
    const foliage = new THREE.MeshStandardMaterial({ color: '#34764a', roughness: 0.92 });
    const lightFoliage = new THREE.MeshStandardMaterial({ color: '#72a948', roughness: 0.9 });
    const bark = new THREE.MeshStandardMaterial({ color: '#65492e', roughness: 1 });

    for (let i = 0; i < 42; i += 1) {
      const side = i % 2 === 0 ? -1 : 1;
      const column = Math.floor(i / 2);
      const x = side * rand(7.2, 14.8);
      const z = -114 + column * 5.7 + rand(-2.6, 2.6);
      const tree = new THREE.Group();
      const palm = Math.random() < 0.3;
      tree.position.set(x, 0, z);
      tree.scale.setScalar(rand(0.72, 1.35));
      tree.userData.species = palm ? 'palm' : 'broadleaf';
      tree.userData.baseRotation = rand(0, Math.PI * 2);
      tree.rotation.y = tree.userData.baseRotation;

      const trunkHeight = palm ? rand(6.1, 8.2) : rand(4.4, 6.2);
      const trunk = new THREE.Mesh(
        new THREE.CylinderGeometry(palm ? 0.22 : 0.34, palm ? 0.42 : 0.55, trunkHeight, 7),
        bark,
      );
      trunk.position.y = trunkHeight / 2;
      trunk.castShadow = true;
      tree.add(trunk);

      if (palm) {
        const crown = new THREE.Group();
        crown.position.y = trunkHeight;
        for (let frond = 0; frond < 7; frond += 1) {
          const angle = (frond / 7) * Math.PI * 2;
          const leaf = new THREE.Mesh(new THREE.ConeGeometry(0.42, rand(3.2, 4.6), 5), lightFoliage);
          leaf.position.set(Math.cos(angle) * 1.18, -0.12, Math.sin(angle) * 1.18);
          leaf.rotation.z = -Math.cos(angle) * 0.98;
          leaf.rotation.x = Math.sin(angle) * 0.98;
          leaf.castShadow = true;
          crown.add(leaf);
        }
        tree.add(crown);
      } else {
        const crownY = trunkHeight * 0.75;
        const crown = new THREE.Group();
        crown.position.y = crownY;
        for (let leaf = 0; leaf < 5; leaf += 1) {
          const orb = new THREE.Mesh(
            new THREE.IcosahedronGeometry(rand(1.25, 1.95), 1),
            leaf % 2 ? foliage : lightFoliage,
          );
          orb.position.set(rand(-1.45, 1.45), rand(-0.55, 1.05), rand(-0.9, 0.9));
          orb.scale.set(1.12, 0.86, 1);
          orb.castShadow = true;
          crown.add(orb);
        }
        tree.add(crown);
      }
      this.scene.add(tree);
      this.trees.push(tree);
    }

    for (let i = 0; i < 25; i += 1) {
      const side = i % 2 === 0 ? -1 : 1;
      const bush = new THREE.Group();
      bush.position.set(side * rand(5.3, 7.1), -0.04, -112 + i * 8.7);
      for (let leaf = 0; leaf < 4; leaf += 1) {
        const tuft = new THREE.Mesh(
          new THREE.DodecahedronGeometry(rand(0.42, 0.75), 0),
          leaf % 2 ? foliage : lightFoliage,
        );
        tuft.position.set(rand(-0.5, 0.5), rand(0.18, 0.55), rand(-0.35, 0.35));
        tuft.castShadow = true;
        bush.add(tuft);
      }
      this.scene.add(bush);
      this.trees.push(bush);
    }

    this.loader.load(
      `${BASE_PATH}assets/unity/BroadleafBillboard.png`,
      (texture) => {
        this.onFoliageTextureLoaded(texture, 'broadleaf');
      },
      undefined,
      (error) => console.warn('Billboard broadleaf Unity tidak dapat dimuat.', error),
    );
    this.loader.load(
      `${BASE_PATH}assets/unity/PalmBillboard.png`,
      (texture) => {
        this.onFoliageTextureLoaded(texture, 'palm');
      },
      undefined,
      (error) => console.warn('Billboard palm Unity tidak dapat dimuat.', error),
    );
  }

  private onFoliageTextureLoaded(texture: THREE.Texture, kind: 'broadleaf' | 'palm') {
    if (this.disposed) {
      texture.dispose();
      return;
    }
    texture.colorSpace = THREE.SRGBColorSpace;
    texture.anisotropy = 4;
    if (kind === 'broadleaf') this.treeTexture = texture;
    else this.palmTexture = texture;

    this.trees.forEach((tree) => {
      if (tree.userData.billboardAdded) return;
      if (!tree.userData.species) return;
      const treeTexture = tree.userData.species === 'palm' ? this.palmTexture : this.treeTexture;
      if (!treeTexture) return;
      tree.userData.billboardAdded = true;
      const height = tree.userData.species === 'palm' ? rand(5.7, 7) : rand(5.1, 6.4);
      const panelMaterial = new THREE.MeshBasicMaterial({
        map: treeTexture,
        transparent: true,
        alphaTest: 0.22,
        side: THREE.DoubleSide,
        depthWrite: false,
        color: '#ffffff',
      });
      for (let angle = 0; angle < 2; angle += 1) {
        const panel = new THREE.Mesh(new THREE.PlaneGeometry(4.6, height), panelMaterial);
        panel.position.y = height * 0.59;
        panel.rotation.y = (angle * Math.PI) / 2;
        tree.add(panel);
      }
    });
  }

  private createRunner() {
    this.runnerRoot.position.set(0, 0, 0.7);
    this.runnerVisual.rotation.y = Math.PI;
    this.runnerRoot.add(this.runnerVisual);
    this.scene.add(this.runnerRoot);

    const suit = new THREE.MeshStandardMaterial({ color: '#eb8f48', roughness: 0.72 });
    const darkSuit = new THREE.MeshStandardMaterial({ color: '#26352f', roughness: 0.8 });
    const skin = new THREE.MeshStandardMaterial({ color: '#d99a69', roughness: 0.82 });
    const boot = new THREE.MeshStandardMaterial({ color: '#473a2d', roughness: 0.9 });
    const fallback = new THREE.Group();

    const torso = new THREE.Mesh(new THREE.CapsuleGeometry(0.33, 0.74, 4, 8), suit);
    torso.position.y = 1.16;
    torso.castShadow = true;
    fallback.add(torso);

    const backpack = new THREE.Mesh(new THREE.BoxGeometry(0.42, 0.62, 0.2), darkSuit);
    backpack.position.set(0, 1.27, 0.28);
    backpack.castShadow = true;
    fallback.add(backpack);

    const head = new THREE.Mesh(new THREE.SphereGeometry(0.27, 16, 12), skin);
    head.position.y = 1.92;
    head.castShadow = true;
    fallback.add(head);

    const cap = new THREE.Mesh(new THREE.SphereGeometry(0.3, 12, 7, 0, Math.PI * 2, 0, Math.PI / 2), darkSuit);
    cap.position.set(0, 2.09, 0);
    fallback.add(cap);

    for (const x of [-0.19, 0.19]) {
      const legPivot = new THREE.Group();
      legPivot.position.set(x, 0.79, 0);
      const leg = new THREE.Mesh(new THREE.CapsuleGeometry(0.115, 0.53, 4, 7), darkSuit);
      leg.position.y = -0.3;
      leg.castShadow = true;
      legPivot.add(leg);
      const shoe = new THREE.Mesh(new THREE.BoxGeometry(0.22, 0.14, 0.39), boot);
      shoe.position.set(0, -0.59, -0.08);
      legPivot.add(shoe);
      fallback.add(legPivot);
      this.fallbackLegs.push(legPivot);

      const armPivot = new THREE.Group();
      armPivot.position.set(x * 1.8, 1.47, 0);
      const arm = new THREE.Mesh(new THREE.CapsuleGeometry(0.1, 0.47, 4, 7), suit);
      arm.position.y = -0.25;
      arm.castShadow = true;
      armPivot.add(arm);
      fallback.add(armPivot);
      this.fallbackArms.push(armPivot);
    }
    this.runnerVisual.add(fallback);

    this.shieldMesh = new THREE.Mesh(
      new THREE.SphereGeometry(1.28, 20, 16),
      new THREE.MeshBasicMaterial({ color: '#80c8ff', transparent: true, opacity: 0.17, side: THREE.DoubleSide }),
    );
    this.shieldMesh.position.y = 1.04;
    this.shieldMesh.visible = false;
    this.runnerRoot.add(this.shieldMesh);
  }

  private loadUnityAssets() {
    if (!this.renderer) {
      this.callbacks.onAssetWarning('Mode grafis ringan aktif karena WebGL2 tidak tersedia di browser ini.');
      return;
    }
    this.callbacks.onAssetWarning(null);
    this.fbxLoader.load(
      `${BASE_PATH}assets/unity/Ethan.fbx`,
      (model) => {
        if (this.disposed) return;
        const bounds = new THREE.Box3().setFromObject(model);
        const size = bounds.getSize(new THREE.Vector3());
        const center = bounds.getCenter(new THREE.Vector3());
        const scale = 1.87 / Math.max(size.y, 0.1);
        model.scale.setScalar(scale);
        model.position.set(-center.x * scale, -bounds.min.y * scale, -center.z * scale);
        model.traverse((child) => {
          if (!(child instanceof THREE.Mesh)) return;
          child.castShadow = true;
          child.receiveShadow = true;
          const materials = Array.isArray(child.material) ? child.material : [child.material];
          materials.forEach((material) => {
            if ('roughness' in material) material.roughness = 0.78;
            if ('metalness' in material) material.metalness = 0.02;
          });
        });
        const fallback = this.runnerVisual.children[0];
        if (fallback) this.runnerVisual.remove(fallback);
        this.runnerVisual.add(model);
        this.mixer = new THREE.AnimationMixer(model);
        this.callbacks.onAssetWarning(null);
        this.loadAnimation('HumanoidRun.fbx', 'run', model);
        this.loadAnimation('HumanoidJumpAndFall.fbx', 'jump', model);
      },
      undefined,
      (error) => {
        console.error('Mesh karakter Ethan.fbx tidak dapat dimuat.', error);
        this.callbacks.onAssetWarning('Mesh karakter Unity tidak terbaca; karakter cadangan tetap digunakan.');
      },
    );
  }

  private loadAnimation(file: string, name: 'run' | 'jump', rig: THREE.Object3D) {
    this.fbxLoader.load(
      `${BASE_PATH}assets/unity/${file}`,
      (animationModel) => {
        if (this.disposed || !this.mixer) return;
        const clip = animationModel.animations[0];
        if (!clip) return;
        const tracks = clip.tracks.flatMap((track) => {
          const match = track.name.match(/^([^.]+)(.*)$/);
          if (!match) return [];
          let nodeName = match[1];
          if (!rig.getObjectByName(nodeName) && rig.getObjectByName(`Ethan${nodeName}`)) {
            nodeName = `Ethan${nodeName}`;
          }
          if (!rig.getObjectByName(nodeName)) return [];
          if (nodeName === match[1]) return [track];
          const mappedTrack = track.clone();
          mappedTrack.name = `${nodeName}${match[2]}`;
          return [mappedTrack];
        });
        if (tracks.length === 0) {
          console.warn(`Animasi Unity ${file} tidak memiliki track yang cocok dengan rig Ethan.`);
          return;
        }
        const mappedClip = new THREE.AnimationClip(clip.name, clip.duration, tracks, clip.blendMode);
        const action = this.mixer.clipAction(mappedClip);
        if (name === 'run') {
          action.setLoop(THREE.LoopRepeat, Infinity);
          this.runAction = action;
          if (this.mode === 'playing' && this.grounded) this.playAnimation('run');
        } else {
          action.setLoop(THREE.LoopOnce, 1);
          action.clampWhenFinished = true;
          this.jumpAction = action;
        }
      },
      undefined,
      (error) => console.warn(`Animasi Unity ${file} tidak dapat dimuat.`, error),
    );
  }

  private playAnimation(name: 'run' | 'jump') {
    const next = name === 'jump' ? this.jumpAction : this.runAction;
    if (!next || next === this.activeAction) return;
    this.activeAction?.fadeOut(0.12);
    next.reset().fadeIn(0.12).play();
    this.activeAction = next;
  }

  private populatePreview() {
    this.spawnRow(-36);
    this.spawnRow(-51);
    this.spawnRow(-66);
    this.spawnRow(-81);
  }

  private resetRun() {
    this.entities.forEach((entity) => this.disposeEntity(entity));
    this.entities.length = 0;
    this.powerups.clear();
    this.distance = 0;
    this.coins = 0;
    this.score = 0;
    this.pickupPoints = 0;
    this.scoreAccumulator = 0;
    this.speed = START_SPEED;
    this.laneIndex = 1;
    this.runnerX = 0;
    this.jumpHeight = 0;
    this.verticalSpeed = 0;
    this.grounded = true;
    this.previousSingleFreeLane = 1;
    this.previousRowWasSingle = false;
    let z = -35;
    for (let row = 0; row < 4; row += 1) {
      this.spawnRow(z);
      z -= ROW_SPACING;
    }
    this.spawnHeadZ = z;
    this.setRunnerAnimation('run');
    this.emitStats(true);
  }

  private spawnRow(z: number) {
    const blocked = [false, false, false];
    if (Math.random() < 0.67) {
      const first = Math.floor(Math.random() * 3);
      blocked[first] = true;
      if (Math.random() < 0.34) {
        const second = (first + (Math.random() < 0.5 ? 1 : 2)) % 3;
        blocked[second] = true;
      }
    }

    let freeLanes = [0, 1, 2].filter((lane) => !blocked[lane]);
    if (freeLanes.length === 0) {
      blocked[Math.floor(Math.random() * 3)] = false;
      freeLanes = [0, 1, 2].filter((lane) => !blocked[lane]);
    }

    if (this.previousRowWasSingle && freeLanes.length === 1) {
      const current = freeLanes[0];
      if (Math.abs(current - this.previousSingleFreeLane) > 1) {
        const safe = clamp(this.previousSingleFreeLane + (current > this.previousSingleFreeLane ? 1 : -1), 0, 2);
        blocked[0] = true;
        blocked[1] = true;
        blocked[2] = true;
        blocked[safe] = false;
        freeLanes = [safe];
      }
    }

    const isSingle = freeLanes.length === 1;
    const freeLane = pick(freeLanes);
    blocked.forEach((isBlocked, lane) => {
      if (isBlocked) this.spawnObstacle(lane, z + rand(-0.35, 0.35));
    });

    if (Math.random() < 0.84) {
      for (let coin = 0; coin < 5; coin += 1) {
        this.spawnPickup('coin', freeLane, z - 2.8 + coin * 1.35);
      }
    }
    if (Math.random() < 0.15) this.spawnPickup('gem', freeLane, z - rand(5, 8), 1.72);

    if (Math.random() < 0.22) {
      const type = pick(POWERUP_ORDER);
      const lane = pick(freeLanes);
      this.spawnPickup('powerup', lane, z - rand(8, 10), 1.8, type);
    }

    this.previousSingleFreeLane = freeLane;
    this.previousRowWasSingle = isSingle;
  }

  private spawnObstacle(lane: number, z: number) {
    const kind = pick(['log', 'rock', 'stump'] as const);
    const group = new THREE.Group();
    const bark = new THREE.MeshStandardMaterial({ color: '#6c4328', roughness: 0.96 });
    const stone = new THREE.MeshStandardMaterial({ color: '#777665', roughness: 1 });
    const cutWood = new THREE.MeshStandardMaterial({ color: '#c58b4a', roughness: 0.9 });
    let obstacleHeight = 1.05;

    if (kind === 'log') {
      const log = new THREE.Mesh(new THREE.CylinderGeometry(0.42, 0.5, 2.35, 9), bark);
      log.rotation.z = Math.PI / 2;
      log.position.y = 0.48;
      log.castShadow = true;
      group.add(log);
      const end = new THREE.Mesh(new THREE.CircleGeometry(0.43, 9), cutWood);
      end.position.set(1.19, 0.48, 0);
      end.rotation.y = Math.PI / 2;
      group.add(end);
    } else if (kind === 'rock') {
      obstacleHeight = 1.18;
      const rock = new THREE.Mesh(new THREE.DodecahedronGeometry(0.84, 0), stone);
      rock.scale.set(1.12, 0.98, 0.92);
      rock.rotation.set(rand(-0.2, 0.2), rand(0, Math.PI), rand(-0.15, 0.15));
      rock.position.y = 0.65;
      rock.castShadow = true;
      rock.receiveShadow = true;
      group.add(rock);
    } else {
      obstacleHeight = 1.36;
      const stump = new THREE.Mesh(new THREE.CylinderGeometry(0.5, 0.68, 1.3, 9), bark);
      stump.position.y = 0.66;
      stump.castShadow = true;
      group.add(stump);
      const top = new THREE.Mesh(new THREE.CylinderGeometry(0.5, 0.5, 0.06, 9), cutWood);
      top.position.y = 1.32;
      group.add(top);
    }

    group.position.set(LANES[lane], 0, z);
    this.scene.add(group);
    this.entities.push({ group, kind: 'obstacle', lane, z, y: 0, type: kind, spent: false, phase: Math.random(), obstacleHeight });
  }

  private spawnPickup(kind: 'coin' | 'gem' | 'powerup', lane: number, z: number, y = 1.35, type?: PowerupId) {
    const group = new THREE.Group();
    if (kind === 'coin') {
      const coin = new THREE.Mesh(
        new THREE.TorusGeometry(0.31, 0.09, 9, 20),
        new THREE.MeshStandardMaterial({ color: '#ffc94f', metalness: 0.68, roughness: 0.24, emissive: '#76541d', emissiveIntensity: 0.2 }),
      );
      coin.castShadow = true;
      group.add(coin);
      const core = new THREE.Mesh(
        new THREE.TorusGeometry(0.17, 0.026, 6, 14),
        new THREE.MeshBasicMaterial({ color: '#fff2af' }),
      );
      core.position.z = 0.035;
      group.add(core);
    } else if (kind === 'gem') {
      const gem = new THREE.Mesh(
        new THREE.OctahedronGeometry(0.43, 0),
        new THREE.MeshStandardMaterial({ color: '#dc70ff', metalness: 0.24, roughness: 0.2, emissive: '#521d72', emissiveIntensity: 0.6 }),
      );
      gem.scale.set(0.77, 1.25, 0.77);
      gem.castShadow = true;
      group.add(gem);
      const halo = new THREE.Mesh(
        new THREE.TorusGeometry(0.57, 0.022, 6, 24),
        new THREE.MeshBasicMaterial({ color: '#eb9dff', transparent: true, opacity: 0.85 }),
      );
      halo.rotation.x = Math.PI / 2;
      group.add(halo);
    } else {
      const colors: Record<PowerupId, string> = {
        magnet: '#64deee',
        shield: '#85bfff',
        speed: '#ff9b4d',
        fly: '#c389ff',
        double: '#f7dc50',
      };
      const color = type ? colors[type] : '#f5e354';
      const orb = new THREE.Mesh(
        new THREE.IcosahedronGeometry(0.43, 1),
        new THREE.MeshStandardMaterial({ color, emissive: color, emissiveIntensity: 0.45, metalness: 0.25, roughness: 0.25 }),
      );
      orb.castShadow = true;
      group.add(orb);
      const halo = new THREE.Mesh(
        new THREE.TorusGeometry(0.68, 0.035, 7, 24),
        new THREE.MeshBasicMaterial({ color, transparent: true, opacity: 0.76 }),
      );
      halo.rotation.x = Math.PI / 2;
      group.add(halo);
    }

    group.position.set(LANES[lane], y, z);
    this.scene.add(group);
    this.entities.push({ group, kind, lane, z, y, type, spent: false, phase: rand(0, Math.PI * 2) });
  }

  private frame = () => {
    if (this.disposed) return;
    const now = performance.now();
    const dt = Math.min(Math.max((now - this.lastFrameAt) / 1000, 0), 0.04);
    this.lastFrameAt = now;
    if (this.mode === 'playing') this.updateGame(dt);
    if (this.renderer) this.renderer.render(this.scene, this.camera);
    else this.renderFallbackCanvas();
    this.animationFrame = window.requestAnimationFrame(this.frame);
  };

  private updateGame(dt: number) {
    this.speed = Math.min(MAX_SPEED, this.speed + 0.24 * dt);
    const speedFactor = this.powerups.has('speed') ? 1.44 : 1;
    const flyFactor = this.powerups.has('fly') ? 1 : 0;
    const currentSpeed = this.speed * speedFactor;
    this.distance += currentSpeed * dt;
    this.scoreAccumulator += currentSpeed * dt * (this.powerups.has('double') ? 2 : 1);
    this.score = Math.floor(this.scoreAccumulator) + this.pickupPoints;
    this.updateBest();

    this.spawnHeadZ += currentSpeed * dt;
    while (this.spawnHeadZ > SPAWN_HORIZON) {
      this.spawnRow(this.spawnHeadZ);
      this.spawnHeadZ -= ROW_SPACING;
    }

    this.runnerX += (LANES[this.laneIndex] - this.runnerX) * Math.min(1, dt * 13);
    this.runnerRoot.position.x = this.runnerX;

    if (this.powerups.has('fly')) {
      this.jumpHeight = 0;
      this.verticalSpeed = 0;
      this.grounded = true;
    } else if (!this.grounded) {
      this.verticalSpeed -= 24 * dt;
      this.jumpHeight = Math.max(0, this.jumpHeight + this.verticalSpeed * dt);
      if (this.jumpHeight <= 0) {
        this.grounded = true;
        this.verticalSpeed = 0;
        this.jumpHeight = 0;
        this.setRunnerAnimation('run');
      }
    }

    const flyHeight = flyFactor ? 2.35 : 0;
    this.runnerRoot.position.y = this.jumpHeight + flyHeight;
    this.animateFallback(dt);
    this.mixer?.update(dt);
    this.shieldMesh && (this.shieldMesh.visible = this.powerups.has('shield'));
    if (this.shieldMesh) this.shieldMesh.scale.setScalar(1 + Math.sin(performance.now() / 170) * 0.035);

    for (const tree of this.trees) {
      tree.position.z += currentSpeed * dt * 0.46;
      if (tree.position.z > 22) tree.position.z -= 142;
      tree.rotation.z = Math.sin(performance.now() / 1400 + tree.position.x) * 0.018;
    }
    for (const marker of this.laneMarkers) {
      marker.position.z += currentSpeed * dt;
      if (marker.position.z > 14) marker.position.z -= 126;
    }
    if (this.pathTexture) this.pathTexture.offset.y -= currentSpeed * dt * 0.011;

    this.tickPowerups(dt);
    this.updateEntities(dt, currentSpeed);
    if (performance.now() - this.lastStatsAt > 90) this.emitStats(false);
  }

  private updateEntities(dt: number, currentSpeed: number) {
    const removeIndices: number[] = [];
    for (let index = 0; index < this.entities.length; index += 1) {
      const entity = this.entities[index];
      const previousZ = entity.z;
      entity.z += currentSpeed * dt;
      entity.group.position.z = entity.z;
      entity.phase += dt;

      if (entity.kind === 'coin' || entity.kind === 'gem' || entity.kind === 'powerup') {
        entity.group.rotation.y += dt * (entity.kind === 'coin' ? 3.8 : 1.6);
        entity.group.position.y = entity.y + Math.sin(entity.phase * 2.6) * 0.13;
        if (entity.kind === 'powerup') entity.group.scale.setScalar(1 + Math.sin(entity.phase * 3) * 0.055);

        const magnet = this.powerups.has('magnet');
        const dx = this.runnerX - entity.group.position.x;
        if (magnet && entity.kind === 'coin' && Math.abs(entity.z) < 7 && Math.abs(dx) < 4.3) {
          entity.group.position.x += dx * Math.min(1, dt * 5.8);
        }
        const xDistance = Math.abs(this.runnerX - entity.group.position.x);
        const yDistance = Math.abs(this.runnerRoot.position.y + 1.05 - entity.group.position.y);
        const crossedRunner = previousZ <= 0.85 && entity.z >= -0.75;
        const closeEnough = xDistance < (magnet && entity.kind === 'coin' ? 1.1 : 0.72) && yDistance < (this.powerups.has('fly') ? 3.4 : 2.4);
        if (!entity.spent && crossedRunner && closeEnough) {
          entity.spent = true;
          this.collect(entity);
          removeIndices.push(index);
          continue;
        }
      }

      if (entity.kind === 'obstacle') {
        const crossedRunner = previousZ < 0.74 && entity.z >= -0.55;
        if (!entity.spent && crossedRunner && Math.abs(this.runnerX - LANES[entity.lane]) < 1.08) {
          entity.spent = true;
          const flying = this.powerups.has('fly');
          const jumpedOver = this.jumpHeight > (entity.obstacleHeight ?? 1) + 0.1;
          if (!flying && !jumpedOver) {
            if (this.powerups.has('shield')) {
              this.powerups.delete('shield');
              this.playTone('shield');
              this.burstAt(entity.group.position);
              removeIndices.push(index);
              continue;
            }
            this.finishRun();
            return;
          }
        }
      }

      if (entity.z > 12) removeIndices.push(index);
    }

    for (let index = removeIndices.length - 1; index >= 0; index -= 1) {
      const entity = this.entities[removeIndices[index]];
      if (entity) {
        this.disposeEntity(entity);
        this.entities.splice(removeIndices[index], 1);
      }
    }
  }

  private disposeEntity(entity: Entity) {
    this.scene.remove(entity.group);
    const geometries = new Set<THREE.BufferGeometry>();
    const materials = new Set<THREE.Material>();
    entity.group.traverse((object) => {
      if (!(object instanceof THREE.Mesh)) return;
      geometries.add(object.geometry);
      (Array.isArray(object.material) ? object.material : [object.material]).forEach((material) => {
        materials.add(material);
      });
    });
    geometries.forEach((geometry) => geometry.dispose());
    materials.forEach((material) => material.dispose());
  }

  private collect(entity: Entity) {
    this.burstAt(entity.group.position);
    if (entity.kind === 'coin') {
      this.coins += 1;
      this.pickupPoints += this.powerups.has('double') ? 20 : 10;
      this.playTone('coin');
    } else if (entity.kind === 'gem') {
      this.pickupPoints += this.powerups.has('double') ? 200 : 100;
      this.playTone('gem');
    } else if (entity.kind === 'powerup' && entity.type && entity.type in POWERUP_META) {
      const meta = POWERUP_META[entity.type as PowerupId];
      this.powerups.set(entity.type as PowerupId, { label: meta.label, color: meta.color, remaining: meta.duration });
      this.playTone('powerup');
    }
    this.score = Math.floor(this.scoreAccumulator) + this.pickupPoints;
    this.updateBest();
    this.emitStats(true);
  }

  private tickPowerups(dt: number) {
    for (const [id, state] of this.powerups.entries()) {
      state.remaining -= dt;
      if (state.remaining <= 0) this.powerups.delete(id);
    }
  }

  private finishRun() {
    this.setMode('gameover');
    this.playTone('crash');
    this.emitStats(true);
  }

  private setRunnerAnimation(name: 'run' | 'jump') {
    this.playAnimation(name);
  }

  private animateFallback(dt: number) {
    const running = this.mode === 'playing' && this.grounded;
    const t = performance.now() / 1000;
    const phase = running ? t * (6 + this.speed * 0.27) : t * 1.6;
    this.fallbackLegs.forEach((leg, index) => {
      leg.rotation.x = running ? Math.sin(phase + index * Math.PI) * 0.72 : 0;
    });
    this.fallbackArms.forEach((arm, index) => {
      arm.rotation.x = running ? Math.sin(phase + index * Math.PI + Math.PI) * 0.52 : 0.06;
    });
    if (!running && this.mode === 'menu') this.runnerVisual.position.y = Math.sin(t * 1.5) * 0.035;
    else this.runnerVisual.position.y = 0;
    void dt;
  }

  private burstAt(position: THREE.Vector3) {
    const particleMaterial = new THREE.MeshBasicMaterial({ color: '#ffe47a', transparent: true, opacity: 0.82 });
    const particles = new THREE.Group();
    for (let i = 0; i < 5; i += 1) {
      const speck = new THREE.Mesh(new THREE.SphereGeometry(0.055, 6, 5), particleMaterial);
      const angle = (i / 5) * Math.PI * 2;
      speck.position.set(Math.cos(angle) * 0.28, Math.sin(angle) * 0.24, 0);
      particles.add(speck);
    }
    particles.position.copy(position);
    this.scene.add(particles);
    const start = performance.now();
    const animate = () => {
      if (this.disposed || !particles.parent) return;
      const progress = (performance.now() - start) / 260;
      particles.scale.setScalar(1 + progress * 1.4);
      particleMaterial.opacity = Math.max(0, 0.82 * (1 - progress));
      if (progress >= 1) {
        this.scene.remove(particles);
        particles.traverse((object) => {
          if (object instanceof THREE.Mesh) object.geometry.dispose();
        });
        particleMaterial.dispose();
        return;
      }
      window.requestAnimationFrame(animate);
    };
    window.requestAnimationFrame(animate);
  }

  private playTone(kind: 'coin' | 'gem' | 'powerup' | 'jump' | 'shield' | 'crash') {
    if (!this.soundEnabled || typeof window === 'undefined' || !window.AudioContext) return;
    const context = this.lastSoundContext ?? new window.AudioContext();
    this.lastSoundContext = context;
    if (context.state === 'suspended') void context.resume();
    const now = context.currentTime;
    const notes: Record<typeof kind, { frequency: number; duration: number; wave: OscillatorType; volume: number }> = {
      coin: { frequency: 850, duration: 0.09, wave: 'sine', volume: 0.11 },
      gem: { frequency: 620, duration: 0.19, wave: 'triangle', volume: 0.13 },
      powerup: { frequency: 520, duration: 0.22, wave: 'sine', volume: 0.11 },
      jump: { frequency: 300, duration: 0.12, wave: 'triangle', volume: 0.055 },
      shield: { frequency: 420, duration: 0.18, wave: 'sawtooth', volume: 0.07 },
      crash: { frequency: 105, duration: 0.24, wave: 'triangle', volume: 0.1 },
    };
    const note = notes[kind];
    const oscillator = context.createOscillator();
    const gain = context.createGain();
    oscillator.type = note.wave;
    oscillator.frequency.setValueAtTime(note.frequency, now);
    if (kind === 'powerup' || kind === 'gem') {
      oscillator.frequency.exponentialRampToValueAtTime(note.frequency * 1.65, now + note.duration);
    } else if (kind === 'crash') {
      oscillator.frequency.exponentialRampToValueAtTime(48, now + note.duration);
    }
    gain.gain.setValueAtTime(note.volume, now);
    gain.gain.exponentialRampToValueAtTime(0.001, now + note.duration);
    oscillator.connect(gain);
    gain.connect(context.destination);
    oscillator.start(now);
    oscillator.stop(now + note.duration);
  }

  private updateBest() {
    if (this.score <= this.best) return;
    this.best = this.score;
    try {
      window.localStorage.setItem(BEST_KEY, String(this.best));
    } catch {
      // The run still works when browser storage is unavailable.
    }
  }

  private readBest() {
    try {
      return Number(window.localStorage.getItem(BEST_KEY)) || 0;
    } catch {
      return 0;
    }
  }

  private emitStats(force: boolean) {
    const now = performance.now();
    if (!force && now - this.lastStatsAt < 90) return;
    this.lastStatsAt = now;
    const activePowerups = [...this.powerups.entries()].map(([id, state]) => ({
      id,
      label: state.label,
      remaining: state.remaining,
      color: state.color,
    }));
    this.callbacks.onStats({
      score: this.score,
      coins: this.coins,
      distance: this.distance,
      best: this.best,
      activePowerups,
    });
  }

  private setMode(mode: GameMode) {
    if (this.mode === mode) return;
    this.mode = mode;
    if (mode === 'playing') this.playAnimation(this.grounded ? 'run' : 'jump');
    this.callbacks.onModeChange(mode);
    this.emitStats(true);
  }

  private resize() {
    const width = this.host.clientWidth || window.innerWidth;
    const height = this.host.clientHeight || window.innerHeight;
    this.camera.aspect = width / height;
    this.camera.updateProjectionMatrix();
    if (this.renderer) {
      this.renderer.setSize(width, height);
      this.renderer.setPixelRatio(Math.min(window.devicePixelRatio || 1, 1.65));
    } else if (this.fallbackCanvas) {
      const pixelRatio = Math.min(window.devicePixelRatio || 1, 1.65);
      this.fallbackCanvas.width = Math.round(width * pixelRatio);
      this.fallbackCanvas.height = Math.round(height * pixelRatio);
      this.fallbackCanvas.style.width = `${width}px`;
      this.fallbackCanvas.style.height = `${height}px`;
      this.fallbackContext?.setTransform(pixelRatio, 0, 0, pixelRatio, 0, 0);
    }
  }

  private renderFallbackCanvas() {
    const context = this.fallbackContext;
    const canvas = this.fallbackCanvas;
    if (!context || !canvas) return;
    const width = this.host.clientWidth || window.innerWidth;
    const height = this.host.clientHeight || window.innerHeight;
    const pixelRatio = Math.min(window.devicePixelRatio || 1, 1.65);
    if (canvas.width !== Math.round(width * pixelRatio) || canvas.height !== Math.round(height * pixelRatio)) {
      canvas.width = Math.round(width * pixelRatio);
      canvas.height = Math.round(height * pixelRatio);
      canvas.style.width = `${width}px`;
      canvas.style.height = `${height}px`;
      context.setTransform(pixelRatio, 0, 0, pixelRatio, 0, 0);
    }

    const centerX = width / 2;
    const horizon = height * 0.38;
    const focal = height * 1.08;
    const project = (x: number, y: number, z: number) => {
      const depth = Math.max(5.5, 10.2 - z);
      const scale = focal / depth;
      return {
        x: centerX + x * scale,
        y: horizon + (4.8 - y) * scale,
        scale,
      };
    };

    const sky = context.createLinearGradient(0, 0, 0, horizon + 40);
    sky.addColorStop(0, '#8dbb79');
    sky.addColorStop(0.6, '#c7d88c');
    sky.addColorStop(1, '#f1d38b');
    context.fillStyle = sky;
    context.fillRect(0, 0, width, height);

    const sun = context.createRadialGradient(width * 0.33, height * 0.18, 2, width * 0.33, height * 0.18, height * 0.13);
    sun.addColorStop(0, 'rgba(255, 245, 190, .9)');
    sun.addColorStop(1, 'rgba(255, 226, 143, 0)');
    context.fillStyle = sun;
    context.fillRect(0, 0, width, height * 0.42);

    context.fillStyle = '#557d4f';
    context.beginPath();
    context.moveTo(0, horizon + 28);
    context.quadraticCurveTo(width * 0.16, horizon - 36, width * 0.34, horizon + 14);
    context.quadraticCurveTo(width * 0.55, horizon - 44, width * 0.75, horizon + 14);
    context.quadraticCurveTo(width * 0.9, horizon - 26, width, horizon + 22);
    context.lineTo(width, height);
    context.lineTo(0, height);
    context.closePath();
    context.fill();

    const ground = context.createLinearGradient(0, horizon, 0, height);
    ground.addColorStop(0, '#6d9251');
    ground.addColorStop(1, '#243c2c');
    context.fillStyle = ground;
    context.fillRect(0, horizon + 18, width, height - horizon);

    context.fillStyle = 'rgba(48, 88, 47, .45)';
    context.beginPath();
    context.moveTo(centerX - 22, horizon + 28);
    context.lineTo(centerX + 22, horizon + 28);
    context.lineTo(width * 0.94, height + 20);
    context.lineTo(width * 0.06, height + 20);
    context.closePath();
    context.fill();

    const road = context.createLinearGradient(0, horizon, 0, height);
    road.addColorStop(0, '#b59a67');
    road.addColorStop(0.5, '#9b8052');
    road.addColorStop(1, '#715a3d');
    context.fillStyle = road;
    context.beginPath();
    context.moveTo(centerX - 18, horizon + 23);
    context.lineTo(centerX + 18, horizon + 23);
    context.lineTo(width * 0.77, height + 25);
    context.lineTo(width * 0.23, height + 25);
    context.closePath();
    context.fill();

    context.strokeStyle = 'rgba(237, 211, 153, .5)';
    context.lineWidth = 1;
    context.beginPath();
    context.moveTo(centerX - 18, horizon + 23);
    context.lineTo(width * 0.23, height + 25);
    context.moveTo(centerX + 18, horizon + 23);
    context.lineTo(width * 0.77, height + 25);
    context.stroke();

    const trees = [...this.trees].sort((a, b) => a.position.z - b.position.z);
    trees.forEach((tree) => {
      if (tree.position.z > 8) return;
      const point = project(tree.position.x, 0, tree.position.z);
      const scale = clamp(point.scale * tree.scale.x, 2, height * 0.24);
      if (point.x < -scale * 2 || point.x > width + scale * 2) return;
      const topY = point.y - scale * 5.4;
      context.fillStyle = 'rgba(27, 45, 28, .17)';
      context.beginPath();
      context.ellipse(point.x, point.y, scale * 1.7, scale * 0.3, 0, 0, Math.PI * 2);
      context.fill();
      context.fillStyle = '#654a31';
      context.fillRect(point.x - scale * 0.12, topY + scale * 1.25, scale * 0.24, scale * 4.1);
      context.fillStyle = tree.userData.species === 'palm' ? '#4b8145' : '#2e6841';
      context.beginPath();
      context.ellipse(point.x, topY + scale * 1.8, scale * 1.5, scale * 1.15, 0, 0, Math.PI * 2);
      context.ellipse(point.x - scale * 0.9, topY + scale * 2.65, scale * 1.02, scale * 0.82, 0, 0, Math.PI * 2);
      context.ellipse(point.x + scale * 0.95, topY + scale * 2.55, scale * 1.1, scale * 0.92, 0, 0, Math.PI * 2);
      context.fill();
      context.fillStyle = 'rgba(151, 190, 89, .42)';
      context.beginPath();
      context.ellipse(point.x - scale * 0.45, topY + scale * 1.8, scale * 0.55, scale * 0.27, -0.35, 0, Math.PI * 2);
      context.fill();
    });

    for (const marker of this.laneMarkers) {
      const near = project(1.36, 0, marker.position.z);
      const far = project(1.36, 0, marker.position.z - 3.1);
      context.strokeStyle = 'rgba(222, 198, 144, .54)';
      context.lineWidth = Math.max(1, near.scale * 0.08);
      for (const side of [-1, 1]) {
        const start = project(side * 1.36, 0, marker.position.z);
        const end = project(side * 1.36, 0, marker.position.z - 3.1);
        context.beginPath();
        context.moveTo(start.x, start.y);
        context.lineTo(end.x, end.y);
        context.stroke();
      }
      void far;
    }

    const visibleEntities = [...this.entities].sort((a, b) => a.z - b.z);
    visibleEntities.forEach((entity) => {
      if (entity.z > 9 || entity.z < -120) return;
      const point = project(entity.group.position.x, entity.y, entity.z);
      const scale = clamp(point.scale, 2, height * 0.26);
      if (entity.kind === 'coin') {
        context.save();
        context.translate(point.x, point.y);
        context.scale(scale, scale);
        context.fillStyle = '#e6a92e';
        context.beginPath();
        context.arc(0, 0, 0.34, 0, Math.PI * 2);
        context.fill();
        context.strokeStyle = '#fff0a0';
        context.lineWidth = 0.07;
        context.beginPath();
        context.arc(0, 0, 0.23, 0, Math.PI * 2);
        context.stroke();
        context.restore();
      } else if (entity.kind === 'gem') {
        context.save();
        context.translate(point.x, point.y);
        context.scale(scale, scale);
        context.fillStyle = '#df8bff';
        context.beginPath();
        context.moveTo(0, -0.44);
        context.lineTo(0.31, 0);
        context.lineTo(0, 0.45);
        context.lineTo(-0.31, 0);
        context.closePath();
        context.fill();
        context.restore();
      } else if (entity.kind === 'powerup') {
        context.save();
        context.translate(point.x, point.y);
        context.scale(scale, scale);
        context.fillStyle = '#83dfe8';
        context.strokeStyle = '#edffb2';
        context.lineWidth = 0.08;
        context.beginPath();
        context.arc(0, 0, 0.43, 0, Math.PI * 2);
        context.fill();
        context.stroke();
        context.restore();
      } else {
        const obstacleScale = scale * 0.95;
        context.save();
        context.translate(point.x, point.y);
        context.scale(obstacleScale, obstacleScale);
        if (entity.type === 'log') {
          context.fillStyle = '#67442c';
          context.beginPath();
          context.roundRect(-1.12, -0.4, 2.24, 0.72, 0.25);
          context.fill();
          context.fillStyle = '#bd8850';
          context.beginPath();
          context.ellipse(1.04, -0.04, 0.16, 0.34, 0, 0, Math.PI * 2);
          context.fill();
        } else if (entity.type === 'stump') {
          context.fillStyle = '#67442c';
          context.fillRect(-0.48, -1.25, 0.96, 1.25);
          context.fillStyle = '#c38d55';
          context.beginPath();
          context.ellipse(0, -1.24, 0.48, 0.18, 0, 0, Math.PI * 2);
          context.fill();
        } else {
          context.fillStyle = '#77776a';
          context.beginPath();
          context.moveTo(-0.78, -0.08);
          context.lineTo(-0.54, -0.82);
          context.lineTo(-0.06, -1.12);
          context.lineTo(0.56, -0.79);
          context.lineTo(0.8, -0.13);
          context.lineTo(0.52, 0.02);
          context.lineTo(-0.62, 0.02);
          context.closePath();
          context.fill();
        }
        context.restore();
      }
    });

    const player = project(this.runnerX, this.jumpHeight + (this.powerups.has('fly') ? 2.35 : 0), 0.7);
    const playerScale = clamp(player.scale, 20, height * 0.15);
    const runPhase = performance.now() / 1000 * (6 + this.speed * 0.27);
    context.save();
    context.translate(player.x, player.y);
    context.scale(playerScale, playerScale);
    context.fillStyle = 'rgba(24, 35, 25, .42)';
    context.beginPath();
    context.ellipse(0, 0.08 + (this.jumpHeight ? this.jumpHeight * 0.14 : 0), 0.56, 0.17, 0, 0, Math.PI * 2);
    context.fill();

    if (this.powerups.has('shield')) {
      context.strokeStyle = 'rgba(126, 205, 255, .78)';
      context.lineWidth = 0.06;
      context.beginPath();
      context.ellipse(0, -1.1, 0.84, 1.23, 0, 0, Math.PI * 2);
      context.stroke();
    }

    context.fillStyle = '#293c35';
    context.fillRect(-0.27 + Math.sin(runPhase) * 0.12, -0.6, 0.22, 0.59);
    context.fillRect(0.06 - Math.sin(runPhase) * 0.12, -0.6, 0.22, 0.59);
    context.fillStyle = '#43372b';
    context.fillRect(-0.32 + Math.sin(runPhase) * 0.12, -0.11, 0.35, 0.14);
    context.fillRect(0.03 - Math.sin(runPhase) * 0.12, -0.11, 0.35, 0.14);
    context.fillStyle = '#e98742';
    context.beginPath();
    context.roundRect(-0.36, -1.82, 0.72, 1.15, 0.23);
    context.fill();
    context.fillStyle = '#263a33';
    context.fillRect(0.22, -1.7, 0.26, 0.58);
    context.fillStyle = '#d89a70';
    context.beginPath();
    context.arc(0, -2.02, 0.28, 0, Math.PI * 2);
    context.fill();
    context.fillStyle = '#2b3a33';
    context.beginPath();
    context.arc(0, -2.12, 0.29, Math.PI, Math.PI * 2);
    context.lineTo(0.34, -2.04);
    context.lineTo(-0.32, -2.04);
    context.closePath();
    context.fill();
    context.restore();

    if (this.mode === 'menu') {
      const haze = context.createLinearGradient(0, height * 0.65, 0, height);
      haze.addColorStop(0, 'rgba(16, 39, 24, 0)');
      haze.addColorStop(1, 'rgba(10, 27, 19, .25)');
      context.fillStyle = haze;
      context.fillRect(0, height * 0.6, width, height * 0.4);
    }
  }
}

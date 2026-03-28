import React, { memo, useEffect, useRef, useCallback } from 'react';
import { type IkonUiComponentResolver, type UiComponentRendererProps, useUiNode } from '@ikonai/sdk-react-ui';
import { Canvas, useFrame, useThree } from '@react-three/fiber';
import { GLTFLoader } from 'three/examples/jsm/loaders/GLTFLoader.js';
import { VRMLoaderPlugin, VRMUtils, type VRM, VRMExpressionPresetName, VRMHumanBoneName } from '@pixiv/three-vrm';
import * as THREE from 'three';
import type { IkonClient } from '@ikonai/sdk';

// Lip sync constants
const LIP_SYNC_NOISE_GATE = 0.02;
const LIP_SYNC_SMOOTHING = 0.4;
const LIP_SYNC_DECAY_RATE = 0.08;
const LIP_SYNC_DECAY_DELAY_MS = 50;

// Idle animation constants
const BLINK_INTERVAL_MIN = 2000;
const BLINK_INTERVAL_MAX = 6000;
const BLINK_DURATION = 150;
const BREATHING_SPEED = 0.0008;
const BREATHING_AMPLITUDE = 0.005;
const HEAD_SWAY_SPEED = 0.0004;
const HEAD_SWAY_AMPLITUDE = 0.03;

// Pose system
type BonePose = {
  bones: Partial<Record<string, { x: number; y: number; z: number }>>;
  overlay: {
    swayAmplitude: number;
    swaySpeed: number;
    breathScale: number;
    headBobScale: number;
  };
};

const POSE_TRANSITION_DURATION = 1.5;

const POSES: Record<string, BonePose> = {
  idle: {
    bones: {
      hips: { x: 0.0, y: 0.02, z: 0.01 },
      spine: { x: 0.02, y: 0.0, z: 0.0 },
      chest: { x: -0.01, y: 0.0, z: 0.0 },
      upperChest: { x: 0.0, y: 0.0, z: 0.0 },
      neck: { x: 0.02, y: 0.0, z: 0.0 },
      head: { x: 0.03, y: 0.02, z: 0.01 },
      leftShoulder: { x: 0.0, y: 0.0, z: 0.05 },
      rightShoulder: { x: 0.0, y: 0.0, z: -0.05 },
      leftUpperArm: { x: 0.1, y: 0.0, z: 0.3 },
      rightUpperArm: { x: 0.1, y: 0.0, z: -0.3 },
      leftLowerArm: { x: 0.0, y: 0.0, z: 0.05 },
      rightLowerArm: { x: 0.0, y: 0.0, z: -0.05 },
    },
    overlay: { swayAmplitude: 0.008, swaySpeed: 0.0004, breathScale: 1.0, headBobScale: 1.0 },
  },
  thinking: {
    bones: {
      hips: { x: 0.0, y: 0.03, z: 0.02 },
      spine: { x: 0.04, y: -0.02, z: 0.0 },
      chest: { x: 0.02, y: 0.0, z: 0.0 },
      upperChest: { x: 0.02, y: 0.0, z: 0.0 },
      neck: { x: 0.05, y: -0.05, z: 0.03 },
      head: { x: 0.08, y: -0.08, z: 0.04 },
      leftShoulder: { x: 0.0, y: 0.0, z: 0.05 },
      rightShoulder: { x: 0.0, y: 0.0, z: -0.1 },
      leftUpperArm: { x: 0.1, y: 0.0, z: 0.3 },
      rightUpperArm: { x: -0.8, y: 0.3, z: -0.4 },
      leftLowerArm: { x: 0.0, y: 0.0, z: 0.05 },
      rightLowerArm: { x: -1.2, y: 0.0, z: 0.0 },
    },
    overlay: { swayAmplitude: 0.004, swaySpeed: 0.0003, breathScale: 0.8, headBobScale: 0.5 },
  },
  excited: {
    bones: {
      hips: { x: 0.0, y: 0.0, z: 0.0 },
      spine: { x: -0.03, y: 0.0, z: 0.0 },
      chest: { x: -0.05, y: 0.0, z: 0.0 },
      upperChest: { x: -0.04, y: 0.0, z: 0.0 },
      neck: { x: -0.03, y: 0.0, z: 0.0 },
      head: { x: -0.06, y: 0.0, z: 0.0 },
      leftShoulder: { x: 0.0, y: 0.0, z: 0.08 },
      rightShoulder: { x: 0.0, y: 0.0, z: -0.08 },
      leftUpperArm: { x: -0.2, y: 0.0, z: 0.5 },
      rightUpperArm: { x: -0.2, y: 0.0, z: -0.5 },
      leftLowerArm: { x: -0.3, y: 0.0, z: 0.0 },
      rightLowerArm: { x: -0.3, y: 0.0, z: 0.0 },
    },
    overlay: { swayAmplitude: 0.015, swaySpeed: 0.0006, breathScale: 1.4, headBobScale: 1.5 },
  },
  shy: {
    bones: {
      hips: { x: 0.0, y: 0.02, z: -0.02 },
      spine: { x: 0.08, y: 0.0, z: 0.0 },
      chest: { x: 0.05, y: 0.0, z: 0.0 },
      upperChest: { x: 0.03, y: 0.0, z: 0.0 },
      neck: { x: 0.1, y: -0.06, z: 0.05 },
      head: { x: 0.12, y: -0.1, z: 0.06 },
      leftShoulder: { x: 0.0, y: 0.0, z: 0.15 },
      rightShoulder: { x: 0.0, y: 0.0, z: -0.15 },
      leftUpperArm: { x: 0.2, y: 0.15, z: 0.4 },
      rightUpperArm: { x: 0.2, y: -0.15, z: -0.4 },
      leftLowerArm: { x: -0.3, y: 0.0, z: 0.0 },
      rightLowerArm: { x: -0.3, y: 0.0, z: 0.0 },
    },
    overlay: { swayAmplitude: 0.005, swaySpeed: 0.0003, breathScale: 0.7, headBobScale: 0.6 },
  },
  confident: {
    bones: {
      hips: { x: 0.0, y: -0.02, z: 0.0 },
      spine: { x: -0.04, y: 0.0, z: 0.0 },
      chest: { x: -0.06, y: 0.0, z: 0.0 },
      upperChest: { x: -0.04, y: 0.0, z: 0.0 },
      neck: { x: -0.03, y: 0.0, z: 0.0 },
      head: { x: -0.05, y: 0.02, z: 0.0 },
      leftShoulder: { x: 0.0, y: -0.03, z: 0.04 },
      rightShoulder: { x: 0.0, y: 0.03, z: -0.04 },
      leftUpperArm: { x: 0.05, y: 0.0, z: 0.45 },
      rightUpperArm: { x: 0.05, y: 0.0, z: -0.45 },
      leftLowerArm: { x: 0.0, y: 0.0, z: 0.08 },
      rightLowerArm: { x: 0.0, y: 0.0, z: -0.08 },
    },
    overlay: { swayAmplitude: 0.006, swaySpeed: 0.0003, breathScale: 1.1, headBobScale: 0.7 },
  },
  waving: {
    bones: {
      hips: { x: 0.0, y: 0.02, z: 0.02 },
      spine: { x: 0.02, y: 0.02, z: 0.02 },
      chest: { x: -0.02, y: 0.0, z: 0.02 },
      upperChest: { x: 0.0, y: 0.0, z: 0.0 },
      neck: { x: 0.02, y: 0.03, z: 0.02 },
      head: { x: 0.04, y: 0.05, z: 0.02 },
      leftShoulder: { x: 0.0, y: 0.0, z: 0.05 },
      rightShoulder: { x: 0.0, y: 0.0, z: -0.1 },
      leftUpperArm: { x: 0.1, y: 0.0, z: 0.3 },
      rightUpperArm: { x: -1.2, y: 0.3, z: -0.5 },
      leftLowerArm: { x: 0.0, y: 0.0, z: 0.05 },
      rightLowerArm: { x: -0.8, y: 0.0, z: 0.2 },
    },
    overlay: { swayAmplitude: 0.012, swaySpeed: 0.0005, breathScale: 1.0, headBobScale: 1.2 },
  },
  listening: {
    bones: {
      hips: { x: 0.0, y: 0.01, z: 0.0 },
      spine: { x: 0.05, y: 0.0, z: 0.0 },
      chest: { x: 0.03, y: 0.0, z: 0.0 },
      upperChest: { x: 0.02, y: 0.0, z: 0.0 },
      neck: { x: 0.04, y: -0.06, z: 0.04 },
      head: { x: 0.06, y: -0.08, z: 0.05 },
      leftShoulder: { x: 0.0, y: 0.0, z: 0.05 },
      rightShoulder: { x: 0.0, y: 0.0, z: -0.05 },
      leftUpperArm: { x: 0.1, y: 0.0, z: 0.3 },
      rightUpperArm: { x: 0.1, y: 0.0, z: -0.3 },
      leftLowerArm: { x: 0.0, y: 0.0, z: 0.05 },
      rightLowerArm: { x: 0.0, y: 0.0, z: -0.05 },
    },
    overlay: { swayAmplitude: 0.006, swaySpeed: 0.0003, breathScale: 0.9, headBobScale: 1.3 },
  },
  talking: {
    bones: {
      hips: { x: 0.0, y: 0.01, z: 0.01 },
      spine: { x: -0.02, y: 0.0, z: 0.0 },
      chest: { x: -0.03, y: 0.0, z: 0.0 },
      upperChest: { x: -0.02, y: 0.0, z: 0.0 },
      neck: { x: 0.02, y: 0.02, z: 0.0 },
      head: { x: 0.03, y: 0.03, z: 0.01 },
      leftShoulder: { x: 0.0, y: 0.0, z: 0.06 },
      rightShoulder: { x: 0.0, y: 0.0, z: -0.06 },
      leftUpperArm: { x: 0.05, y: 0.0, z: 0.35 },
      rightUpperArm: { x: -0.3, y: 0.1, z: -0.4 },
      leftLowerArm: { x: 0.0, y: 0.0, z: 0.05 },
      rightLowerArm: { x: -0.4, y: 0.0, z: 0.0 },
    },
    overlay: { swayAmplitude: 0.01, swaySpeed: 0.0005, breathScale: 1.1, headBobScale: 1.4 },
  },
  stretching: {
    bones: {
      hips: { x: 0.0, y: -0.02, z: 0.0 },
      spine: { x: -0.06, y: 0.0, z: 0.0 },
      chest: { x: -0.08, y: 0.0, z: 0.0 },
      upperChest: { x: -0.06, y: 0.0, z: 0.0 },
      neck: { x: -0.04, y: 0.0, z: 0.0 },
      head: { x: -0.06, y: 0.0, z: 0.0 },
      leftShoulder: { x: 0.0, y: -0.05, z: 0.1 },
      rightShoulder: { x: 0.0, y: 0.05, z: -0.1 },
      leftUpperArm: { x: -1.4, y: 0.1, z: 0.3 },
      rightUpperArm: { x: -1.4, y: -0.1, z: -0.3 },
      leftLowerArm: { x: -0.2, y: 0.0, z: 0.1 },
      rightLowerArm: { x: -0.2, y: 0.0, z: -0.1 },
    },
    overlay: { swayAmplitude: 0.005, swaySpeed: 0.0003, breathScale: 1.5, headBobScale: 0.4 },
  },
  looking_around: {
    bones: {
      hips: { x: 0.0, y: 0.04, z: 0.0 },
      spine: { x: 0.02, y: 0.03, z: 0.0 },
      chest: { x: 0.0, y: 0.02, z: 0.0 },
      upperChest: { x: 0.0, y: 0.02, z: 0.0 },
      neck: { x: 0.02, y: 0.1, z: 0.03 },
      head: { x: 0.04, y: 0.15, z: 0.04 },
      leftShoulder: { x: 0.0, y: 0.0, z: 0.05 },
      rightShoulder: { x: 0.0, y: 0.0, z: -0.05 },
      leftUpperArm: { x: 0.1, y: 0.0, z: 0.3 },
      rightUpperArm: { x: 0.1, y: 0.0, z: -0.3 },
      leftLowerArm: { x: 0.0, y: 0.0, z: 0.05 },
      rightLowerArm: { x: 0.0, y: 0.0, z: -0.05 },
    },
    overlay: { swayAmplitude: 0.01, swaySpeed: 0.0005, breathScale: 0.9, headBobScale: 1.0 },
  },
};

const BONE_NAME_MAP: Record<string, VRMHumanBoneName> = {
  hips: VRMHumanBoneName.Hips,
  spine: VRMHumanBoneName.Spine,
  chest: VRMHumanBoneName.Chest,
  upperChest: VRMHumanBoneName.UpperChest,
  neck: VRMHumanBoneName.Neck,
  head: VRMHumanBoneName.Head,
  leftShoulder: VRMHumanBoneName.LeftShoulder,
  rightShoulder: VRMHumanBoneName.RightShoulder,
  leftUpperArm: VRMHumanBoneName.LeftUpperArm,
  rightUpperArm: VRMHumanBoneName.RightUpperArm,
  leftLowerArm: VRMHumanBoneName.LeftLowerArm,
  rightLowerArm: VRMHumanBoneName.RightLowerArm,
};

function applyPoseImmediate(vrm: VRM, pose: BonePose) {
  for (const [boneName, rotation] of Object.entries(pose.bones)) {
    const vrmBoneName = BONE_NAME_MAP[boneName];
    if (!vrmBoneName) continue;
    const bone = vrm.humanoid?.getNormalizedBoneNode(vrmBoneName);
    if (bone) {
      bone.rotation.set(rotation.x, rotation.y, rotation.z, bone.rotation.w);
    }
  }
}

// Camera presets for view modes
const CAMERA_PRESETS = {
  fullBody: { position: [0, 0.9, 3.0] as [number, number, number], lookAt: [0, 0.9, 0] as [number, number, number] },
  portrait: { position: [0, 1.3, 1.5] as [number, number, number], lookAt: [0, 1.3, 0] as [number, number, number] },
  face: { position: [0, 1.45, 0.8] as [number, number, number], lookAt: [0, 1.45, 0] as [number, number, number] },
};

type ViewMode = 'fullBody' | 'portrait' | 'face';

// AIUEO vowel mapping from viseme RMS/FFT data
function mapVisemeToAIUEO(rms: number, fft: number): { aa: number; ih: number; ou: number; ee: number; oh: number } {
  // RMS = amplitude (0-1), FFT = frequency centroid (-1 to 1)
  // Map to VRM AIUEO blend shapes based on acoustic characteristics:
  // A (aa) - wide open, mid frequency
  // I (ih) - slight smile, higher frequency
  // U (ou) - pursed lips, lower frequency
  // E (ee) - wide spread, highest frequency
  // O (oh) - round open, low frequency

  const intensity = Math.min(rms * 2.5, 1.0);

  // Weight each vowel based on frequency distribution
  let aa = 0, ih = 0, ou = 0, ee = 0, oh = 0;

  if (fft > 0.3) {
    // High frequency - E or I
    ee = intensity * Math.max(0, (fft - 0.3) / 0.7);
    ih = intensity * Math.max(0, 1 - Math.abs(fft - 0.2) / 0.5);
  } else if (fft > -0.2) {
    // Mid frequency - A
    aa = intensity * Math.max(0, 1 - Math.abs(fft) / 0.4);
    ih = intensity * Math.max(0, 1 - Math.abs(fft - 0.2) / 0.5) * 0.3;
  } else {
    // Low frequency - O or U
    oh = intensity * Math.max(0, 1 - Math.abs(fft + 0.3) / 0.5);
    ou = intensity * Math.max(0, (-fft - 0.3) / 0.7);
  }

  // Add a base mouth open component proportional to RMS
  aa += intensity * 0.2;

  // Normalize so total doesn't exceed 1
  const total = aa + ih + ou + ee + oh;
  if (total > 1) {
    const scale = 1 / total;
    aa *= scale;
    ih *= scale;
    ou *= scale;
    ee *= scale;
    oh *= scale;
  }

  return { aa, ih, ou, ee, oh };
}

// VRM expression name mapping (supports both VRM 0.x and 1.0 naming)
function setVRMExpression(vrm: VRM, presetName: string, value: number) {
  if (!vrm.expressionManager) return;

  vrm.expressionManager.setValue(presetName, value);
}

type VRMSceneProps = {
  src: string;
  isListening?: boolean;
  expression?: string;
  motion?: string;
  viewMode?: ViewMode;
  client?: IkonClient;
};

function VRMScene({ src, expression, motion, viewMode = 'portrait', client }: VRMSceneProps) {
  const vrmRef = useRef<VRM | null>(null);
  const { camera, scene } = useThree();
  const clientRef = useRef<IkonClient | undefined>(client);

  // Lip sync smoothed values
  const smoothedRef = useRef({ aa: 0, ih: 0, ou: 0, ee: 0, oh: 0 });
  const lastVisemeTimeRef = useRef(0);

  // Idle animation state
  const blinkTimerRef = useRef(0);
  const nextBlinkRef = useRef(BLINK_INTERVAL_MIN + Math.random() * (BLINK_INTERVAL_MAX - BLINK_INTERVAL_MIN));
  const isBlinkingRef = useRef(false);
  const blinkStartRef = useRef(0);

  // Pose transition state
  const currentBoneRotations = useRef<Record<string, { x: number; y: number; z: number }>>({});
  const targetPoseRef = useRef<BonePose>(POSES.idle);
  const currentOverlayRef = useRef(POSES.idle.overlay);
  const blendProgressRef = useRef(1.0);

  // Camera animation
  const cameraTargetRef = useRef({ position: new THREE.Vector3(), lookAt: new THREE.Vector3() });
  const cameraCurrentRef = useRef({ position: new THREE.Vector3(), lookAt: new THREE.Vector3() });

  useEffect(() => {
    clientRef.current = client;
  }, [client]);

  // Update camera target when viewMode changes
  useEffect(() => {
    const preset = CAMERA_PRESETS[viewMode] || CAMERA_PRESETS.portrait;
    cameraTargetRef.current.position.set(...preset.position);
    cameraTargetRef.current.lookAt.set(...preset.lookAt);
  }, [viewMode]);

  // Initialize camera position immediately
  useEffect(() => {
    const preset = CAMERA_PRESETS[viewMode] || CAMERA_PRESETS.portrait;
    camera.position.set(...preset.position);
    cameraCurrentRef.current.position.set(...preset.position);
    cameraCurrentRef.current.lookAt.set(...preset.lookAt);
    camera.lookAt(...preset.lookAt);
  }, [camera, viewMode]);

  // Load VRM model
  useEffect(() => {
    if (!src) return;

    let disposed = false;
    const loader = new GLTFLoader();
    loader.register((parser) => new VRMLoaderPlugin(parser));

    loader.load(
      src,
      (gltf) => {
        if (disposed) return;

        const vrm = gltf.userData.vrm as VRM | undefined;
        if (!vrm) {
          console.error('[VRM] No VRM data found in GLTF');
          return;
        }

        VRMUtils.removeUnnecessaryJoints(gltf.scene);
        VRMUtils.removeUnnecessaryVertices(gltf.scene);
        VRMUtils.rotateVRM0(vrm);

        // Remove old model
        if (vrmRef.current) {
          scene.remove(vrmRef.current.scene);
        }

        vrmRef.current = vrm;
        scene.add(vrm.scene);

        // Apply idle pose immediately to prevent T-pose flash
        applyPoseImmediate(vrm, POSES.idle);
        const idleBones = POSES.idle.bones;
        for (const [boneName, rotation] of Object.entries(idleBones)) {
          currentBoneRotations.current[boneName] = { ...rotation };
        }
        targetPoseRef.current = POSES.idle;
        currentOverlayRef.current = POSES.idle.overlay;
        blendProgressRef.current = 1.0;

        console.log('[VRM] Model loaded:', src);
        if (vrm.expressionManager) {
          console.log('[VRM] Available expressions:', vrm.expressionManager.expressions.map(e => e.expressionName));
        }
      },
      undefined,
      (error) => {
        console.error('[VRM] Failed to load model:', error);
      }
    );

    return () => {
      disposed = true;
      if (vrmRef.current) {
        scene.remove(vrmRef.current.scene);
        vrmRef.current = null;
      }
    };
  }, [src, scene]);

  // Handle expression changes
  useEffect(() => {
    const vrm = vrmRef.current;
    if (!vrm?.expressionManager || !expression) return;

    // Reset all custom expressions first
    const expressionPresets = [
      VRMExpressionPresetName.Happy,
      VRMExpressionPresetName.Angry,
      VRMExpressionPresetName.Sad,
      VRMExpressionPresetName.Relaxed,
      VRMExpressionPresetName.Surprised,
    ];

    for (const preset of expressionPresets) {
      setVRMExpression(vrm, preset, 0);
    }

    // Set the requested expression
    const expressionMap: Record<string, string> = {
      happy: VRMExpressionPresetName.Happy,
      angry: VRMExpressionPresetName.Angry,
      sad: VRMExpressionPresetName.Sad,
      relaxed: VRMExpressionPresetName.Relaxed,
      surprised: VRMExpressionPresetName.Surprised,
    };

    const presetName = expressionMap[expression.toLowerCase()];
    if (presetName) {
      setVRMExpression(vrm, presetName, 1.0);
    }
  }, [expression]);

  // Handle motion (pose) changes
  useEffect(() => {
    if (!motion) return;
    const pose = POSES[motion];
    if (!pose) return;
    targetPoseRef.current = pose;
    blendProgressRef.current = 0.0;
  }, [motion]);

  // Animation loop
  useFrame((_, delta) => {
    const vrm = vrmRef.current;
    if (!vrm) return;

    const now = performance.now();
    const clampedDelta = Math.min(delta, 0.1);

    // --- Camera animation (smooth lerp to target) ---
    const camLerpSpeed = 3.0 * clampedDelta;
    cameraCurrentRef.current.position.lerp(cameraTargetRef.current.position, camLerpSpeed);
    cameraCurrentRef.current.lookAt.lerp(cameraTargetRef.current.lookAt, camLerpSpeed);
    camera.position.copy(cameraCurrentRef.current.position);
    camera.lookAt(cameraCurrentRef.current.lookAt);

    // --- Blink animation ---
    blinkTimerRef.current += clampedDelta * 1000;

    if (!isBlinkingRef.current && blinkTimerRef.current >= nextBlinkRef.current) {
      isBlinkingRef.current = true;
      blinkStartRef.current = now;
      blinkTimerRef.current = 0;
      nextBlinkRef.current = BLINK_INTERVAL_MIN + Math.random() * (BLINK_INTERVAL_MAX - BLINK_INTERVAL_MIN);
    }

    let blinkValue = 0;
    if (isBlinkingRef.current) {
      const blinkElapsed = now - blinkStartRef.current;
      if (blinkElapsed < BLINK_DURATION) {
        // Triangle wave: 0 -> 1 -> 0
        const t = blinkElapsed / BLINK_DURATION;
        blinkValue = t < 0.5 ? t * 2 : (1 - t) * 2;
      } else {
        isBlinkingRef.current = false;
      }
    }
    setVRMExpression(vrm, VRMExpressionPresetName.Blink, blinkValue);

    // --- Pose transition (lerp toward target) ---
    const targetPose = targetPoseRef.current;
    const progress = blendProgressRef.current;
    const lerpFactor = Math.min(clampedDelta / POSE_TRANSITION_DURATION, 1.0);

    if (progress < 1.0) {
      blendProgressRef.current = Math.min(progress + clampedDelta / POSE_TRANSITION_DURATION, 1.0);
      // Lerp overlay params
      const tOverlay = targetPose.overlay;
      const cOverlay = currentOverlayRef.current;
      cOverlay.swayAmplitude += (tOverlay.swayAmplitude - cOverlay.swayAmplitude) * lerpFactor;
      cOverlay.swaySpeed += (tOverlay.swaySpeed - cOverlay.swaySpeed) * lerpFactor;
      cOverlay.breathScale += (tOverlay.breathScale - cOverlay.breathScale) * lerpFactor;
      cOverlay.headBobScale += (tOverlay.headBobScale - cOverlay.headBobScale) * lerpFactor;
    }

    // Lerp each bone toward target and apply
    for (const [boneName, targetRot] of Object.entries(targetPose.bones)) {
      const vrmBoneName = BONE_NAME_MAP[boneName];
      if (!vrmBoneName) continue;
      const bone = vrm.humanoid?.getNormalizedBoneNode(vrmBoneName);
      if (!bone) continue;

      let current = currentBoneRotations.current[boneName];
      if (!current) {
        current = { x: 0, y: 0, z: 0 };
        currentBoneRotations.current[boneName] = current;
      }

      current.x += (targetRot.x - current.x) * lerpFactor;
      current.y += (targetRot.y - current.y) * lerpFactor;
      current.z += (targetRot.z - current.z) * lerpFactor;

      bone.rotation.x = current.x;
      bone.rotation.y = current.y;
      bone.rotation.z = current.z;
    }

    // --- Procedural overlay: breathing (on spine) ---
    const overlay = currentOverlayRef.current;
    const spineBone = vrm.humanoid?.getNormalizedBoneNode(VRMHumanBoneName.Spine);
    if (spineBone) {
      const breathOffset = Math.sin(now * BREATHING_SPEED) * BREATHING_AMPLITUDE * overlay.breathScale;
      spineBone.rotation.x += breathOffset;
    }

    // --- Procedural overlay: head bob ---
    const headBone = vrm.humanoid?.getNormalizedBoneNode(VRMHumanBoneName.Head);
    if (headBone) {
      const bobX = Math.sin(now * HEAD_SWAY_SPEED) * HEAD_SWAY_AMPLITUDE * overlay.headBobScale;
      const bobZ = Math.sin(now * HEAD_SWAY_SPEED * 0.7) * HEAD_SWAY_AMPLITUDE * 0.5 * overlay.headBobScale;
      headBone.rotation.y += bobX;
      headBone.rotation.z += bobZ;
    }

    // --- Procedural overlay: weight-shift sway (hips + spine lateral) ---
    const hipsBone = vrm.humanoid?.getNormalizedBoneNode(VRMHumanBoneName.Hips);
    if (hipsBone) {
      const swayZ = Math.sin(now * overlay.swaySpeed) * overlay.swayAmplitude;
      hipsBone.rotation.z += swayZ;
    }
    if (spineBone) {
      const spineSwayZ = Math.sin(now * overlay.swaySpeed * 1.1) * overlay.swayAmplitude * 0.5;
      spineBone.rotation.z += spineSwayZ;
    }

    // --- Lip sync from viseme data ---
    const visemeData = clientRef.current?.media?.audio.getCurrentVisemeValues();

    let targetAA = 0, targetIH = 0, targetOU = 0, targetEE = 0, targetOH = 0;

    if (visemeData && visemeData.mouthOpenY > LIP_SYNC_NOISE_GATE) {
      const vowels = mapVisemeToAIUEO(visemeData.mouthOpenY, visemeData.mouthForm);
      targetAA = vowels.aa;
      targetIH = vowels.ih;
      targetOU = vowels.ou;
      targetEE = vowels.ee;
      targetOH = vowels.oh;
      lastVisemeTimeRef.current = now;
    } else {
      // Decay to zero when no audio
      const timeSinceUpdate = now - lastVisemeTimeRef.current;
      if (timeSinceUpdate > LIP_SYNC_DECAY_DELAY_MS) {
        // Already at 0 targets, smoothing will handle decay
      } else {
        // Keep current values during brief gaps
        targetAA = smoothedRef.current.aa;
        targetIH = smoothedRef.current.ih;
        targetOU = smoothedRef.current.ou;
        targetEE = smoothedRef.current.ee;
        targetOH = smoothedRef.current.oh;
      }
    }

    // EMA smoothing
    const s = smoothedRef.current;
    const decayOrSmooth = (visemeData && visemeData.mouthOpenY > LIP_SYNC_NOISE_GATE) ? LIP_SYNC_SMOOTHING : LIP_SYNC_DECAY_RATE;
    s.aa += decayOrSmooth * (targetAA - s.aa);
    s.ih += decayOrSmooth * (targetIH - s.ih);
    s.ou += decayOrSmooth * (targetOU - s.ou);
    s.ee += decayOrSmooth * (targetEE - s.ee);
    s.oh += decayOrSmooth * (targetOH - s.oh);

    // Apply to VRM blend shapes (try both VRM 1.0 and 0.x names)
    setVRMExpression(vrm, VRMExpressionPresetName.Aa, s.aa);
    setVRMExpression(vrm, VRMExpressionPresetName.Ih, s.ih);
    setVRMExpression(vrm, VRMExpressionPresetName.Ou, s.ou);
    setVRMExpression(vrm, VRMExpressionPresetName.Ee, s.ee);
    setVRMExpression(vrm, VRMExpressionPresetName.Oh, s.oh);

    // Update VRM
    vrm.update(clampedDelta);
  });

  return null;
}

type VRMCanvasProps = {
  src: string;
  isListening?: boolean;
  expression?: string;
  motion?: string;
  viewMode?: ViewMode;
  className?: string;
  client?: IkonClient;
};

const vrmPropsAreEqual = (prev: VRMCanvasProps, next: VRMCanvasProps) => {
  return (
    prev.src === next.src &&
    prev.isListening === next.isListening &&
    prev.expression === next.expression &&
    prev.motion === next.motion &&
    prev.viewMode === next.viewMode &&
    prev.className === next.className &&
    prev.client === next.client
  );
};

const VRMCanvasInner = memo(function VRMCanvasInner(props: VRMCanvasProps) {
  return (
    <div className={props.className} style={{ width: '100%', height: '100%' }}>
      <Canvas
        gl={{ alpha: true, antialias: true }}
        style={{ background: 'transparent' }}
      >
        <ambientLight intensity={0.6} />
        <directionalLight position={[1, 2, 3]} intensity={0.8} />
        <directionalLight position={[-1, 1, -1]} intensity={0.3} />
        <VRMScene
          src={props.src}
          isListening={props.isListening}
          expression={props.expression}
          motion={props.motion}
          viewMode={props.viewMode}
          client={props.client}
        />
      </Canvas>
    </div>
  );
}, vrmPropsAreEqual);

function toStringValue(value: unknown): string | undefined {
  if (typeof value === 'string') {
    const trimmed = value.trim();
    return trimmed.length > 0 ? trimmed : undefined;
  }
  return undefined;
}

function toOptionalBoolean(value: unknown): boolean | undefined {
  if (typeof value === 'boolean') {
    return value;
  }
  if (typeof value === 'string') {
    if (value === 'true') return true;
    if (value === 'false') return false;
  }
  return undefined;
}

function combineClassNames(
  styleIds: readonly string[],
  className?: string,
  inlineStyle?: unknown,
): string | undefined {
  const parts: string[] = [];

  for (const value of styleIds) {
    if (typeof value === 'string') {
      const trimmed = value.trim();
      if (trimmed.length > 0) {
        parts.push(trimmed);
      }
    }
  }

  if (typeof className === 'string') {
    const trimmed = className.trim();
    if (trimmed.length > 0) {
      parts.push(trimmed);
    }
  }

  if (typeof inlineStyle === 'string') {
    const trimmed = inlineStyle.trim();
    if (trimmed.length > 0) {
      parts.push(trimmed);
    }
  }

  return parts.length > 0 ? parts.join(' ') : undefined;
}

const VRMCanvasRenderer = memo(function VRMCanvasRenderer({
  nodeId,
  context,
  className,
}: UiComponentRendererProps & { initialNode?: unknown }) {
  const node = useUiNode(context.store, nodeId);

  if (!node) return null;

  const src = toStringValue(node.props['src']);
  if (!src) return null;

  const isListening = toOptionalBoolean(node.props['isListening']);
  const expression = toStringValue(node.props['expression']);
  const motion = toStringValue(node.props['motion']);
  const viewModeValue = toStringValue(node.props['viewMode']);
  const viewMode = (viewModeValue === 'fullBody' || viewModeValue === 'portrait' || viewModeValue === 'face')
    ? viewModeValue as ViewMode
    : undefined;

  const combinedClassName = combineClassNames(
    node.styleIds,
    className,
    toStringValue(node.props['style']),
  );

  return (
    <VRMCanvasInner
      src={src}
      isListening={isListening}
      expression={expression}
      motion={motion}
      viewMode={viewMode}
      className={combinedClassName}
      client={context.client}
    />
  );
});

export function createVRMCanvasResolver(): IkonUiComponentResolver {
  return (initialNode) => {
    if (initialNode.type !== 'vrm-canvas') {
      return undefined;
    }

    return VRMCanvasRenderer;
  };
}

import { memo, useEffect, useRef } from 'react';
import { type IkonUiComponentResolver, type UiComponentRendererProps, useUiNode } from '@ikonai/sdk-react-ui';
import * as THREE from 'three';
import { OrbitControls } from 'three/addons/controls/OrbitControls.js';
import { CONTINENT_OUTLINES } from '../data/world-outlines';

type GlobeProps = {
  data?: string;
  seriesName?: string;
  seriesColor?: string;
  autoRotate?: boolean;
  rotationSpeed?: number;
  globeColor?: string;
  atmosphereColor?: string;
  className?: string;
  onSpikeClickId?: string;
  dispatchAction?: (actionId: string, payload: unknown) => void;
};

function toStringValue(value: unknown): string | undefined {
  if (typeof value === 'string') {
    const trimmed = value.trim();
    return trimmed.length > 0 ? trimmed : undefined;
  }
  return undefined;
}

function toFiniteNumber(value: unknown): number | undefined {
  if (typeof value === 'number' && Number.isFinite(value)) {
    return value;
  }
  if (typeof value === 'string') {
    const parsed = Number(value.trim());
    return Number.isFinite(parsed) ? parsed : undefined;
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

function latLongToVector3(lat: number, lon: number, radius: number): THREE.Vector3 {
  const phi = (90 - lat) * (Math.PI / 180);
  const theta = (lon + 180) * (Math.PI / 180);

  const x = -radius * Math.sin(phi) * Math.cos(theta);
  const y = radius * Math.cos(phi);
  const z = radius * Math.sin(phi) * Math.sin(theta);

  return new THREE.Vector3(x, y, z);
}

type DataPoint = { lat: number; lon: number; magnitude: number; label: string };

function parseDataPoints(dataString: string | undefined): DataPoint[] {
  if (!dataString) return [];
  try {
    const parsed = JSON.parse(dataString);
    if (Array.isArray(parsed)) {
      return parsed
        .filter(
          (item): item is [number, number, number, string?] =>
            Array.isArray(item) &&
            item.length >= 3 &&
            typeof item[0] === 'number' &&
            typeof item[1] === 'number' &&
            typeof item[2] === 'number'
        )
        .map(item => ({
          lat: item[0],
          lon: item[1],
          magnitude: item[2],
          label: typeof item[3] === 'string' ? item[3] : '',
        }));
    }
  } catch {
    console.warn('Failed to parse globe data');
  }
  return [];
}

function createLabelSprite(text: string, color: THREE.Color): THREE.Sprite {
  const canvas = document.createElement('canvas');
  const ctx = canvas.getContext('2d')!;
  canvas.width = 256;
  canvas.height = 64;

  ctx.font = 'bold 24px sans-serif';
  ctx.fillStyle = `rgb(${Math.round(color.r * 255)}, ${Math.round(color.g * 255)}, ${Math.round(color.b * 255)})`;
  ctx.textAlign = 'center';
  ctx.textBaseline = 'middle';
  ctx.fillText(text, 128, 32);

  const texture = new THREE.CanvasTexture(canvas);
  texture.minFilter = THREE.LinearFilter;
  const material = new THREE.SpriteMaterial({
    map: texture,
    transparent: true,
    depthTest: true,
    depthWrite: false,
  });
  const sprite = new THREE.Sprite(material);
  sprite.scale.set(0.3, 0.075, 1);
  return sprite;
}

function hexToRgb(hex: string): { r: number; g: number; b: number } {
  const result = /^#?([a-f\d]{2})([a-f\d]{2})([a-f\d]{2})$/i.exec(hex);
  if (result) {
    return {
      r: parseInt(result[1], 16) / 255,
      g: parseInt(result[2], 16) / 255,
      b: parseInt(result[3], 16) / 255,
    };
  }
  return { r: 0, g: 1, b: 0 };
}


const WebGLGlobeInner = memo(function WebGLGlobeInner(props: GlobeProps) {
  const containerRef = useRef<HTMLDivElement>(null);
  const rendererRef = useRef<THREE.WebGLRenderer | null>(null);
  const sceneRef = useRef<THREE.Scene | null>(null);
  const cameraRef = useRef<THREE.PerspectiveCamera | null>(null);
  const globeRef = useRef<THREE.Mesh | null>(null);
  const spikesGroupRef = useRef<THREE.Group | null>(null);
  const controlsRef = useRef<OrbitControls | null>(null);
  const animationFrameRef = useRef<number>(0);
  const lastInteractionRef = useRef<number>(0);
  const autoRotateSpeedRef = useRef<number>(0);
  const raycasterRef = useRef<THREE.Raycaster>(new THREE.Raycaster());
  const mouseRef = useRef<THREE.Vector2>(new THREE.Vector2());

  useEffect(() => {
    if (!containerRef.current) return;

    const container = containerRef.current;
    const width = container.clientWidth;
    const height = container.clientHeight;

    const scene = new THREE.Scene();
    sceneRef.current = scene;

    const camera = new THREE.PerspectiveCamera(45, width / height, 0.1, 1000);
    camera.position.z = 3;
    cameraRef.current = camera;

    const renderer = new THREE.WebGLRenderer({ antialias: true, alpha: true });
    renderer.setSize(width, height);
    renderer.setPixelRatio(Math.min(window.devicePixelRatio, 2));
    renderer.setClearColor(0x000000, 0);
    container.appendChild(renderer.domElement);
    rendererRef.current = renderer;

    const globeColor = props.globeColor || '#1e3a5f';
    const atmosphereColor = props.atmosphereColor || '#3b82f6';

    const globeGeometry = new THREE.SphereGeometry(1, 64, 64);
    const globeMaterial = new THREE.MeshPhongMaterial({
      color: new THREE.Color(globeColor),
      transparent: true,
      opacity: 0.95,
      shininess: 30,
      specular: new THREE.Color(0x333333),
    });
    const globe = new THREE.Mesh(globeGeometry, globeMaterial);
    scene.add(globe);
    globeRef.current = globe;

    // Add latitude/longitude grid lines
    const gridGeometry = new THREE.SphereGeometry(1.003, 36, 18);
    const gridMaterial = new THREE.MeshBasicMaterial({
      color: new THREE.Color(atmosphereColor),
      wireframe: true,
      transparent: true,
      opacity: 0.25,
    });
    const grid = new THREE.Mesh(gridGeometry, gridMaterial);
    globe.add(grid);

    // Add equator and meridian lines
    const linesMaterial = new THREE.LineBasicMaterial({
      color: new THREE.Color(atmosphereColor),
      transparent: true,
      opacity: 0.4
    });

    // Equator
    const equatorPoints = [];
    for (let i = 0; i <= 64; i++) {
      const angle = (i / 64) * Math.PI * 2;
      equatorPoints.push(new THREE.Vector3(Math.cos(angle) * 1.004, 0, Math.sin(angle) * 1.004));
    }
    const equatorGeometry = new THREE.BufferGeometry().setFromPoints(equatorPoints);
    const equator = new THREE.Line(equatorGeometry, linesMaterial);
    globe.add(equator);

    // Create continent texture using canvas
    const continentCanvas = document.createElement('canvas');
    continentCanvas.width = 2048;
    continentCanvas.height = 1024;
    const ctx = continentCanvas.getContext('2d')!;

    // Transparent background
    ctx.clearRect(0, 0, continentCanvas.width, continentCanvas.height);

    // Draw filled continents
    ctx.fillStyle = 'rgba(45, 212, 191, 0.35)';
    ctx.strokeStyle = 'rgba(45, 212, 191, 0.7)';
    ctx.lineWidth = 1;

    for (const outline of CONTINENT_OUTLINES) {
      ctx.beginPath();
      for (let i = 0; i < outline.length; i++) {
        const [lat, lon] = outline[i];
        // Convert lat/lon to UV coordinates (equirectangular projection)
        const x = ((lon + 180) / 360) * continentCanvas.width;
        const y = ((90 - lat) / 180) * continentCanvas.height;
        if (i === 0) {
          ctx.moveTo(x, y);
        } else {
          ctx.lineTo(x, y);
        }
      }
      ctx.closePath();
      ctx.fill();
      ctx.stroke();
    }

    const continentTexture = new THREE.CanvasTexture(continentCanvas);
    continentTexture.minFilter = THREE.LinearFilter;
    continentTexture.magFilter = THREE.LinearFilter;

    // Add continent overlay sphere slightly above the globe
    const continentGeometry = new THREE.SphereGeometry(1.004, 64, 64);
    const continentMaterial = new THREE.MeshBasicMaterial({
      map: continentTexture,
      transparent: true,
      depthTest: true,
      depthWrite: false,
    });
    const continentMesh = new THREE.Mesh(continentGeometry, continentMaterial);
    globe.add(continentMesh);

    const atmosphereGeometry = new THREE.SphereGeometry(1.12, 64, 64);
    const atmosphereMaterial = new THREE.ShaderMaterial({
      vertexShader: `
        varying vec3 vNormal;
        void main() {
          vNormal = normalize(normalMatrix * normal);
          gl_Position = projectionMatrix * modelViewMatrix * vec4(position, 1.0);
        }
      `,
      fragmentShader: `
        uniform vec3 glowColor;
        varying vec3 vNormal;
        void main() {
          float intensity = pow(0.65 - dot(vNormal, vec3(0.0, 0.0, 1.0)), 2.0);
          gl_FragColor = vec4(glowColor, 1.0) * intensity * 0.8;
        }
      `,
      uniforms: {
        glowColor: { value: new THREE.Color(atmosphereColor) },
      },
      side: THREE.BackSide,
      blending: THREE.AdditiveBlending,
      transparent: true,
      depthWrite: false,
    });
    const atmosphere = new THREE.Mesh(atmosphereGeometry, atmosphereMaterial);
    atmosphere.renderOrder = -1;
    scene.add(atmosphere);

    const spikesGroup = new THREE.Group();
    spikesGroup.renderOrder = 1;
    globe.add(spikesGroup);
    spikesGroupRef.current = spikesGroup;

    const ambientLight = new THREE.AmbientLight(0xffffff, 0.6);
    scene.add(ambientLight);

    const directionalLight = new THREE.DirectionalLight(0xffffff, 1.0);
    directionalLight.position.set(5, 3, 5);
    scene.add(directionalLight);

    const directionalLight2 = new THREE.DirectionalLight(0xffffff, 0.4);
    directionalLight2.position.set(-3, -2, 3);
    scene.add(directionalLight2);

    const pointLight = new THREE.PointLight(0x3b82f6, 0.6);
    pointLight.position.set(-5, -3, -5);
    scene.add(pointLight);

    // Add orbit controls for mouse/touch interaction
    const controls = new OrbitControls(camera, renderer.domElement);
    controls.enableDamping = true;
    controls.dampingFactor = 0.05;
    controls.enableZoom = true;
    controls.minDistance = 1.5;
    controls.maxDistance = 6;
    controls.enablePan = false;
    controls.rotateSpeed = 0.5;
    controlsRef.current = controls;

    // Track user interaction
    const onInteraction = () => {
      lastInteractionRef.current = Date.now();
      autoRotateSpeedRef.current = 0;
    };

    controls.addEventListener('start', onInteraction);

    // Click handler for spike selection
    const handleClick = (event: MouseEvent) => {
      if (!props.onSpikeClickId || !props.dispatchAction || !spikesGroupRef.current) {
        return;
      }

      const rect = renderer.domElement.getBoundingClientRect();
      mouseRef.current.x = ((event.clientX - rect.left) / rect.width) * 2 - 1;
      mouseRef.current.y = -((event.clientY - rect.top) / rect.height) * 2 + 1;

      raycasterRef.current.setFromCamera(mouseRef.current, camera);
      const intersects = raycasterRef.current.intersectObjects(spikesGroupRef.current.children, false);

      if (intersects.length > 0) {
        const hit = intersects[0].object;
        const dataPoint = hit.userData?.dataPoint as DataPoint | undefined;

        if (dataPoint) {
          props.dispatchAction(props.onSpikeClickId, {
            lat: dataPoint.lat,
            lon: dataPoint.lon,
            magnitude: dataPoint.magnitude,
            label: dataPoint.label,
          });
        }
      }
    };

    renderer.domElement.addEventListener('click', handleClick);

    const targetAutoRotateSpeed = (props.rotationSpeed ?? 0.5) * 0.002;
    const autoRotateResumeDelay = 2000; // 2 seconds after interaction
    const autoRotateRampUpTime = 1500; // 1.5 seconds to reach full speed

    const animate = () => {
      animationFrameRef.current = requestAnimationFrame(animate);

      controls.update();

      if (props.autoRotate !== false && globeRef.current) {
        const timeSinceInteraction = Date.now() - lastInteractionRef.current;

        if (timeSinceInteraction > autoRotateResumeDelay) {
          // Gradually ramp up auto-rotation speed
          const rampProgress = Math.min(1, (timeSinceInteraction - autoRotateResumeDelay) / autoRotateRampUpTime);
          autoRotateSpeedRef.current = targetAutoRotateSpeed * rampProgress;
        }

        globeRef.current.rotation.y += autoRotateSpeedRef.current;
      }

      renderer.render(scene, camera);
    };

    // Initialize auto-rotation
    autoRotateSpeedRef.current = targetAutoRotateSpeed;
    lastInteractionRef.current = 0;

    animate();

    const handleResize = () => {
      if (!containerRef.current || !rendererRef.current || !cameraRef.current) return;
      const newWidth = containerRef.current.clientWidth;
      const newHeight = containerRef.current.clientHeight;
      cameraRef.current.aspect = newWidth / newHeight;
      cameraRef.current.updateProjectionMatrix();
      rendererRef.current.setSize(newWidth, newHeight);
    };

    window.addEventListener('resize', handleResize);

    return () => {
      window.removeEventListener('resize', handleResize);
      renderer.domElement.removeEventListener('click', handleClick);
      controls.removeEventListener('start', onInteraction);
      cancelAnimationFrame(animationFrameRef.current);
      controls.dispose();
      renderer.dispose();
      globeGeometry.dispose();
      globeMaterial.dispose();
      gridGeometry.dispose();
      gridMaterial.dispose();
      continentGeometry.dispose();
      continentMaterial.dispose();
      continentTexture.dispose();
      atmosphereGeometry.dispose();
      atmosphereMaterial.dispose();
      container.removeChild(renderer.domElement);
    };
  }, [props.globeColor, props.atmosphereColor, props.onSpikeClickId, props.dispatchAction]);

  useEffect(() => {
    if (!spikesGroupRef.current) return;

    while (spikesGroupRef.current.children.length > 0) {
      const child = spikesGroupRef.current.children[0];
      spikesGroupRef.current.remove(child);
      if (child instanceof THREE.Mesh) {
        child.geometry.dispose();
        if (child.material instanceof THREE.Material) {
          child.material.dispose();
        }
      }
    }

    const dataPoints = parseDataPoints(props.data);
    if (dataPoints.length === 0) return;

    const color = hexToRgb(props.seriesColor || '#00ff00');
    const spikeColor = new THREE.Color(color.r, color.g, color.b);

    const spikeMaterial = new THREE.MeshPhongMaterial({
      color: spikeColor,
      emissive: spikeColor,
      emissiveIntensity: 0.4,
      transparent: true,
      opacity: 0.9,
      depthTest: true,
      depthWrite: true,
    });

    for (const point of dataPoints) {
      const normalizedMagnitude = Math.max(0.02, Math.min(1, point.magnitude));
      const spikeHeight = normalizedMagnitude * 0.3;

      const spikeGeometry = new THREE.CylinderGeometry(0.005, 0.015, spikeHeight, 8);
      spikeGeometry.translate(0, spikeHeight / 2, 0);

      const spike = new THREE.Mesh(spikeGeometry, spikeMaterial);
      spike.userData = { dataPoint: point };

      const position = latLongToVector3(point.lat, point.lon, 1);
      spike.position.copy(position);

      // Point the spike outward from the globe center
      const outwardDirection = position.clone().normalize().multiplyScalar(2);
      spike.lookAt(outwardDirection);
      spike.rotateX(Math.PI / 2);

      spikesGroupRef.current.add(spike);

      if (normalizedMagnitude > 0.5) {
        const glowGeometry = new THREE.SphereGeometry(0.02 + normalizedMagnitude * 0.02, 8, 8);
        const glowMaterial = new THREE.MeshBasicMaterial({
          color: spikeColor,
          transparent: true,
          opacity: 0.4 * normalizedMagnitude,
        });
        const glow = new THREE.Mesh(glowGeometry, glowMaterial);
        glow.userData = { dataPoint: point };
        glow.position.copy(position);
        spikesGroupRef.current.add(glow);
      }

      // Add label for significant data points
      if (point.label && normalizedMagnitude > 0.2) {
        const labelSprite = createLabelSprite(point.label, spikeColor);
        labelSprite.userData = { dataPoint: point };
        const labelPosition = latLongToVector3(point.lat, point.lon, 1 + spikeHeight + 0.04);
        labelSprite.position.copy(labelPosition);
        spikesGroupRef.current.add(labelSprite);
      }
    }
  }, [props.data, props.seriesColor]);

  useEffect(() => {
    if (props.autoRotate === false) {
      return;
    }
  }, [props.autoRotate, props.rotationSpeed]);

  return <div ref={containerRef} className={props.className} style={{ width: '100%', height: '100%' }} />;
}, (prevProps, nextProps) => {
  return (
    prevProps.data === nextProps.data &&
    prevProps.seriesColor === nextProps.seriesColor &&
    prevProps.autoRotate === nextProps.autoRotate &&
    prevProps.rotationSpeed === nextProps.rotationSpeed &&
    prevProps.globeColor === nextProps.globeColor &&
    prevProps.atmosphereColor === nextProps.atmosphereColor &&
    prevProps.className === nextProps.className &&
    prevProps.onSpikeClickId === nextProps.onSpikeClickId &&
    prevProps.dispatchAction === nextProps.dispatchAction
  );
});

const WebGLGlobeRenderer = memo(function WebGLGlobeRenderer({
  nodeId,
  context,
  className,
}: UiComponentRendererProps & { initialNode?: unknown }) {
  const node = useUiNode(context.store, nodeId);

  if (!node) return null;

  const data = toStringValue(node.props['data']);
  const seriesName = toStringValue(node.props['seriesName']);
  const seriesColor = toStringValue(node.props['seriesColor']);
  const autoRotate = toOptionalBoolean(node.props['autoRotate']);
  const rotationSpeed = toFiniteNumber(node.props['rotationSpeed']);
  const globeColor = toStringValue(node.props['globeColor']);
  const atmosphereColor = toStringValue(node.props['atmosphereColor']);
  const onSpikeClickId = toStringValue(node.props['onSpikeClickId']);

  const combinedClassName = combineClassNames(
    node.styleIds,
    className,
    toStringValue(node.props['style']),
  );

  return (
    <WebGLGlobeInner
      data={data}
      seriesName={seriesName}
      seriesColor={seriesColor}
      autoRotate={autoRotate}
      rotationSpeed={rotationSpeed}
      globeColor={globeColor}
      atmosphereColor={atmosphereColor}
      className={combinedClassName}
      onSpikeClickId={onSpikeClickId}
      dispatchAction={context.dispatchAction}
    />
  );
});

export function createWebGLGlobeResolver(): IkonUiComponentResolver {
  return (initialNode) => {
    if (initialNode.type !== 'webgl-globe') {
      return undefined;
    }

    return WebGLGlobeRenderer;
  };
}

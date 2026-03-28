import React, { memo, useEffect, useRef, useState, useCallback } from 'react';
import { type IkonUiComponentResolver, type UiComponentRendererProps, useUiNode } from '@ikonai/sdk-react-ui';
import { Application, Container, Ticker } from 'pixi.js';
import { Live2DModel } from '@naari3/pixi-live2d-display/cubism5';
import type { IkonClient } from '@ikonai/sdk';

// Document Picture-in-Picture API type declarations (Chrome 116+)
interface DocumentPictureInPicture {
  requestWindow(options?: { width?: number; height?: number }): Promise<Window>;
  readonly window: Window | null;
}

declare global {
  interface Window {
    documentPictureInPicture?: DocumentPictureInPicture;
  }
}

const sharedTicker = Ticker.shared;

// =============================================================================
// VISEME DEBUG UI - Enable this to show real-time parameter tuning panel
// =============================================================================
const SHOW_VISEME_DEBUG_UI = false;

// =============================================================================
// LIP SYNC TUNING CONSTANTS - Edit these to adjust the idle/speaking pose
// =============================================================================
// MOUTH_OPEN_Y = Expression/happiness: 0=sad, 0.2=neutral, 0.5=happy, 1=very happy
// MOUTH_FORM = Mouth shape: -1=pursed/closed, 0=O-shape, 1=wide/teeth

// Idle pose (when not speaking)
const IDLE_MOUTH_OPEN_Y = 0.2;    // Neutral expression
const IDLE_MOUTH_FORM = -1;       // Mouth closed/pursed

// Noise gate threshold (values below this are treated as silence)
const LIP_SYNC_NOISE_GATE = 0.02;

// EMA smoothing factor for additional visual polish (0-1, higher = more responsive)
const LIP_SYNC_SMOOTHING = 0.4;

// Decay rate when no viseme data (how fast to return to idle pose)
const LIP_SYNC_DECAY_RATE = 0.08;

// Time in ms before starting decay when no new viseme data
const LIP_SYNC_DECAY_DELAY_MS = 50;

// Idle head movement constants
const IDLE_MOVEMENT_BASE_SPEED = 0.0003;
const IDLE_MOVEMENT_AMPLITUDE = 0.3;
const SPEED_VARIATION_AMOUNT = 0.3;
const SPEED_VARIATION_FREQUENCY = 0.0001;
const PATTERN_CHANGE_SPEED = 0.00005;

// Camera pan/zoom constants
const CAMERA_PAN_SPEED = 0.0001;
const CAMERA_PAN_AMPLITUDE = 0.02;
const CAMERA_ZOOM_SPEED = 0.00005;
const CAMERA_ZOOM_AMPLITUDE = 0.02;
// =============================================================================

// =============================================================================
// VISEME PRESETS - Common phoneme shapes for tuning
// Each preset defines:
//   - input: expected RMS (amplitude) and FFT (frequency) ranges from server
//   - output: target mouthOpenY and mouthForm values for Live2D
// =============================================================================
type VisemePreset = {
  name: string;
  // Output values for Live2D model
  mouthOpenY: number;
  mouthForm: number;
  description: string;
  // Input signature: what server RMS/FFT values characterize this phoneme
  // RMS = amplitude (0-1), FFT = frequency (-1 to +1, negative = low freq, positive = high freq)
  inputRmsMin: number;
  inputRmsMax: number;
  inputFftMin: number;
  inputFftMax: number;
};

// Phoneme presets with input signatures and output values
// Input signatures based on acoustic characteristics:
// - RMS: amplitude/volume (0-1)
// - FFT: frequency centroid (-1 to +1, low freq sounds like O/U are negative, high freq like S/E are positive)
const VISEME_PRESETS: VisemePreset[] = [
  // Silence/closed mouth - very low RMS
  { name: 'Idle', mouthOpenY: 0.00, mouthForm: 0.00, description: 'Closed mouth',
    inputRmsMin: 0.00, inputRmsMax: 0.05, inputFftMin: -1.0, inputFftMax: 1.0 },

  // Open vowels - high RMS
  { name: 'AAA', mouthOpenY: 0.66, mouthForm: 0.86, description: 'Open wide (ah)',
    inputRmsMin: 0.40, inputRmsMax: 1.00, inputFftMin: -0.2, inputFftMax: 0.4 },

  // Front vowels - moderate-high RMS, higher frequency
  { name: 'EEE', mouthOpenY: 0.55, mouthForm: 0.11, description: 'Smile/wide (ee)',
    inputRmsMin: 0.30, inputRmsMax: 0.80, inputFftMin: 0.3, inputFftMax: 1.0 },

  { name: 'III', mouthOpenY: 0.84, mouthForm: -0.43, description: 'Slight smile (ih)',
    inputRmsMin: 0.20, inputRmsMax: 0.60, inputFftMin: 0.1, inputFftMax: 0.5 },

  // Back/rounded vowels - moderate RMS, lower frequency
  { name: 'OOO', mouthOpenY: 0.22, mouthForm: -0.08, description: 'Round/pursed (oh)',
    inputRmsMin: 0.25, inputRmsMax: 0.70, inputFftMin: -0.6, inputFftMax: 0.0 },

  { name: 'UUU', mouthOpenY: 0.35, mouthForm: -0.14, description: 'Tight round (oo)',
    inputRmsMin: 0.15, inputRmsMax: 0.50, inputFftMin: -1.0, inputFftMax: -0.3 },

  // Bilabial/nasal consonants - very low RMS, closed mouth
  { name: 'MMM', mouthOpenY: 0.24, mouthForm: -1.00, description: 'Closed (m/b/p)',
    inputRmsMin: 0.05, inputRmsMax: 0.20, inputFftMin: -1.0, inputFftMax: -0.2 },

  // Labiodental fricatives - low RMS, mid frequency
  { name: 'FFF', mouthOpenY: 0.18, mouthForm: -0.55, description: 'Teeth on lip (f/v)',
    inputRmsMin: 0.05, inputRmsMax: 0.25, inputFftMin: -0.3, inputFftMax: 0.3 },

  // Dental fricative - low-mid RMS, mid frequency
  { name: 'THH', mouthOpenY: 0.43, mouthForm: -0.47, description: 'Tongue out (th)',
    inputRmsMin: 0.08, inputRmsMax: 0.30, inputFftMin: 0.0, inputFftMax: 0.5 },

  // Sibilant fricatives - low RMS but high frequency
  { name: 'SSS', mouthOpenY: 0.50, mouthForm: 0.11, description: 'Hissing (s/z)',
    inputRmsMin: 0.05, inputRmsMax: 0.30, inputFftMin: 0.4, inputFftMax: 1.0 },
];

// =============================================================================
// PHONEME MAPPING SYSTEM
// Maps raw RMS/FFT server values to tuned phoneme output values
// =============================================================================
const PHONEME_MAPPING_ENABLED = true; // Toggle to enable/disable mapping system

type PhonemeMatch = {
  preset: VisemePreset;
  distance: number; // Lower = better match
};

// Find the best matching phoneme for given RMS/FFT values
// Returns the weighted blend of top matches for smooth transitions
function mapRmsAndFftToPhoneme(
  rms: number,
  fft: number,
  presetValues: { [name: string]: { mouthOpenY: number; mouthForm: number } }
): { mouthOpenY: number; mouthForm: number } {
  // Calculate distance to each phoneme's input signature
  const matches: PhonemeMatch[] = [];

  for (const preset of VISEME_PRESETS) {
    if (preset.name === 'Idle') continue; // Skip idle, handled separately by noise gate

    // Calculate how well the input matches this phoneme's signature
    // Distance is 0 if perfectly within range, increases as we move outside
    let rmsDistance = 0;
    if (rms < preset.inputRmsMin) {
      rmsDistance = preset.inputRmsMin - rms;
    } else if (rms > preset.inputRmsMax) {
      rmsDistance = rms - preset.inputRmsMax;
    }

    let fftDistance = 0;
    if (fft < preset.inputFftMin) {
      fftDistance = preset.inputFftMin - fft;
    } else if (fft > preset.inputFftMax) {
      fftDistance = fft - preset.inputFftMax;
    }

    // Combined distance (weight RMS slightly more as it's more reliable)
    const distance = rmsDistance * 1.5 + fftDistance;
    matches.push({ preset, distance });
  }

  // Sort by distance (best matches first)
  matches.sort((a, b) => a.distance - b.distance);

  // Use soft blending between top matches for smoother transitions
  const TOP_N = 2;
  const BLEND_SHARPNESS = 8; // Higher = sharper transitions between phonemes

  let totalWeight = 0;
  let blendedOpenY = 0;
  let blendedForm = 0;

  for (let i = 0; i < Math.min(TOP_N, matches.length); i++) {
    const match = matches[i];
    // Convert distance to weight using exponential falloff
    const weight = Math.exp(-match.distance * BLEND_SHARPNESS);

    // Use tuned preset values if available, otherwise use defaults
    const tuned = presetValues[match.preset.name];
    const openY = tuned?.mouthOpenY ?? match.preset.mouthOpenY;
    const form = tuned?.mouthForm ?? match.preset.mouthForm;

    blendedOpenY += openY * weight;
    blendedForm += form * weight;
    totalWeight += weight;
  }

  if (totalWeight > 0) {
    return {
      mouthOpenY: blendedOpenY / totalWeight,
      mouthForm: blendedForm / totalWeight,
    };
  }

  // Fallback to first match
  const best = matches[0];
  const tuned = presetValues[best.preset.name];
  return {
    mouthOpenY: tuned?.mouthOpenY ?? best.preset.mouthOpenY,
    mouthForm: tuned?.mouthForm ?? best.preset.mouthForm,
  };
}

// Debug UI Panel Component
type VisemeDebugPanelProps = {
  currentOpenY: number;
  currentForm: number;
  rawServerOpenY: number;
  rawServerForm: number;
  detectedPhoneme: string;
  onOverride: (openY: number | null, form: number | null) => void;
  onPresetValuesChange: (values: PresetValues) => void;
  mappingEnabled: boolean;
  onMappingToggle: (enabled: boolean) => void;
  parameterNames: string[];
  parameterValues: Float32Array | null;
};

type PresetValues = { [name: string]: { mouthOpenY: number; mouthForm: number } };

// Initialize preset values from defaults
function getInitialPresetValues(): PresetValues {
  const values: PresetValues = {};
  for (const preset of VISEME_PRESETS) {
    values[preset.name] = { mouthOpenY: preset.mouthOpenY, mouthForm: preset.mouthForm };
  }
  return values;
}

function VisemeDebugPanel({
  currentOpenY,
  currentForm,
  rawServerOpenY,
  rawServerForm,
  detectedPhoneme,
  onOverride,
  onPresetValuesChange,
  mappingEnabled,
  onMappingToggle,
  parameterNames,
  parameterValues
}: VisemeDebugPanelProps) {
  const [isExpanded, setIsExpanded] = useState(true);
  const [overrideEnabled, setOverrideEnabled] = useState(false);
  const [selectedPreset, setSelectedPreset] = useState<string | null>(null);
  const [presetValues, setPresetValues] = useState<PresetValues>(getInitialPresetValues);

  // Notify parent when preset values change
  useEffect(() => {
    onPresetValuesChange(presetValues);
  }, [presetValues, onPresetValuesChange]);

  // Current slider values (either from selected preset or manual)
  const currentPresetData = selectedPreset ? presetValues[selectedPreset] : null;
  const openY = currentPresetData?.mouthOpenY ?? 0;
  const form = currentPresetData?.mouthForm ?? 0;

  const handleOverrideToggle = useCallback(() => {
    const newEnabled = !overrideEnabled;
    setOverrideEnabled(newEnabled);
    if (newEnabled && selectedPreset) {
      const preset = presetValues[selectedPreset];
      onOverride(preset.mouthOpenY, preset.mouthForm);
    } else if (!newEnabled) {
      onOverride(null, null);
    }
  }, [overrideEnabled, selectedPreset, presetValues, onOverride]);

  const handleOpenYChange = useCallback((value: number) => {
    if (!selectedPreset) return;
    setPresetValues(prev => ({
      ...prev,
      [selectedPreset]: { ...prev[selectedPreset], mouthOpenY: value }
    }));
    if (overrideEnabled) {
      onOverride(value, presetValues[selectedPreset].mouthForm);
    }
  }, [selectedPreset, overrideEnabled, presetValues, onOverride]);

  const handleFormChange = useCallback((value: number) => {
    if (!selectedPreset) return;
    setPresetValues(prev => ({
      ...prev,
      [selectedPreset]: { ...prev[selectedPreset], mouthForm: value }
    }));
    if (overrideEnabled) {
      onOverride(presetValues[selectedPreset].mouthOpenY, value);
    }
  }, [selectedPreset, overrideEnabled, presetValues, onOverride]);

  const handlePresetClick = useCallback((presetName: string) => {
    setSelectedPreset(presetName);
    setOverrideEnabled(true);
    const preset = presetValues[presetName];
    onOverride(preset.mouthOpenY, preset.mouthForm);
  }, [presetValues, onOverride]);

  const copyToClipboard = useCallback((text: string, label: string) => {
    if (!navigator?.clipboard?.writeText) {
      console.warn('[VisemeDebug] Clipboard unavailable');
      return;
    }

    navigator.clipboard.writeText(text).then(() => {
      console.log(`[VisemeDebug] Copied ${label}:`, text);
    });
  }, []);

  const copyCurrentValues = useCallback(() => {
    const values = `mouthOpenY: ${currentOpenY.toFixed(3)}, mouthForm: ${currentForm.toFixed(3)}`;
    copyToClipboard(values, 'values');
  }, [currentOpenY, currentForm, copyToClipboard]);

  const copyAllPresets = useCallback(() => {
    const lines = VISEME_PRESETS.map(preset => {
      const values = presetValues[preset.name];
      return `  { name: '${preset.name}', mouthOpenY: ${values.mouthOpenY.toFixed(2)}, mouthForm: ${values.mouthForm.toFixed(2)}, description: '${preset.description}' },`;
    });
    const output = `const VISEME_PRESETS = [\n${lines.join('\n')}\n];`;
    copyToClipboard(output, 'all presets');
  }, [presetValues, copyToClipboard]);

  const resetToDefaults = useCallback(() => {
    setPresetValues(getInitialPresetValues());
    if (selectedPreset && overrideEnabled) {
      const defaultPreset = VISEME_PRESETS.find(p => p.name === selectedPreset);
      if (defaultPreset) {
        onOverride(defaultPreset.mouthOpenY, defaultPreset.mouthForm);
      }
    }
  }, [selectedPreset, overrideEnabled, onOverride]);

  const panelStyle: React.CSSProperties = {
    position: 'absolute',
    top: '10px',
    right: '10px',
    background: 'rgba(0, 0, 0, 0.85)',
    color: '#fff',
    padding: '10px',
    borderRadius: '8px',
    fontSize: '12px',
    fontFamily: 'monospace',
    zIndex: 1000,
    minWidth: '280px',
    maxHeight: '90vh',
    overflowY: 'auto',
  };

  const headerStyle: React.CSSProperties = {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginBottom: isExpanded ? '10px' : '0',
    cursor: 'pointer',
  };

  const sliderContainerStyle: React.CSSProperties = {
    marginBottom: '8px',
  };

  const sliderStyle: React.CSSProperties = {
    width: '100%',
    marginTop: '4px',
  };

  const presetGridStyle: React.CSSProperties = {
    display: 'grid',
    gridTemplateColumns: 'repeat(5, 1fr)',
    gap: '4px',
    marginBottom: '10px',
  };

  const presetButtonStyle = (isSelected: boolean, isModified: boolean): React.CSSProperties => ({
    padding: '6px 4px',
    fontSize: '10px',
    background: isSelected ? '#4a9eff' : isModified ? '#5a4a00' : '#333',
    border: isModified ? '1px solid #aa8800' : 'none',
    borderRadius: '4px',
    color: '#fff',
    cursor: 'pointer',
  });

  // Check if a preset has been modified from its default
  const isPresetModified = (presetName: string): boolean => {
    const current = presetValues[presetName];
    const original = VISEME_PRESETS.find(p => p.name === presetName);
    if (!current || !original) return false;
    return Math.abs(current.mouthOpenY - original.mouthOpenY) > 0.001 ||
           Math.abs(current.mouthForm - original.mouthForm) > 0.001;
  };

  const buttonStyle: React.CSSProperties = {
    padding: '6px 12px',
    fontSize: '11px',
    background: '#444',
    border: 'none',
    borderRadius: '4px',
    color: '#fff',
    cursor: 'pointer',
    marginRight: '6px',
  };

  const toggleButtonStyle: React.CSSProperties = {
    ...buttonStyle,
    background: overrideEnabled ? '#e74c3c' : '#27ae60',
  };

  const valueDisplayStyle: React.CSSProperties = {
    background: '#222',
    padding: '6px 8px',
    borderRadius: '4px',
    marginBottom: '10px',
  };

  if (!isExpanded) {
    return (
      <div style={panelStyle}>
        <div style={headerStyle} onClick={() => setIsExpanded(true)}>
          <strong>Viseme Debug</strong>
          <span>▼</span>
        </div>
      </div>
    );
  }

  return (
    <div style={panelStyle}>
      <div style={headerStyle} onClick={() => setIsExpanded(false)}>
        <strong>Viseme Debug Panel</strong>
        <span>▲</span>
      </div>

      {/* Current Values */}
      <div style={valueDisplayStyle}>
        <div style={{ marginBottom: '4px' }}>
          <strong>Server (Raw):</strong>
        </div>
        <div>RMS: {rawServerOpenY.toFixed(3)} | FFT: {rawServerForm.toFixed(3)}</div>
        <div style={{ marginTop: '6px', marginBottom: '4px' }}>
          <strong>Output (Live2D):</strong>
        </div>
        <div>OpenY: {currentOpenY.toFixed(3)} | Form: {currentForm.toFixed(3)}</div>
        {mappingEnabled && detectedPhoneme && (
          <div style={{ marginTop: '4px', color: '#4a9eff' }}>
            Detected: <strong>{detectedPhoneme}</strong>
          </div>
        )}
        <button style={{ ...buttonStyle, marginTop: '6px' }} onClick={copyCurrentValues}>
          Copy Values
        </button>
      </div>

      {/* Mapping Toggle */}
      <div style={{ marginBottom: '10px' }}>
        <button
          style={{
            ...buttonStyle,
            background: mappingEnabled ? '#27ae60' : '#666',
            marginRight: '6px',
          }}
          onClick={() => onMappingToggle(!mappingEnabled)}
        >
          {mappingEnabled ? '✓ Mapping ON' : '○ Mapping OFF'}
        </button>
        <span style={{ fontSize: '10px', color: '#888' }}>
          {mappingEnabled ? 'Using phoneme presets' : 'Raw server values'}
        </span>
      </div>

      {/* Override Controls */}
      <div style={{ marginBottom: '10px' }}>
        <button style={toggleButtonStyle} onClick={handleOverrideToggle}>
          {overrideEnabled ? 'Disable Override' : 'Enable Override'}
        </button>
      </div>

      {/* Preset Buttons */}
      <div style={{ marginBottom: '6px' }}><strong>Presets:</strong> <span style={{ fontSize: '10px', color: '#888' }}>(yellow = modified)</span></div>
      <div style={presetGridStyle}>
        {VISEME_PRESETS.map((preset) => (
          <button
            key={preset.name}
            style={presetButtonStyle(selectedPreset === preset.name, isPresetModified(preset.name))}
            onClick={() => handlePresetClick(preset.name)}
            title={`${preset.description}\nOpenY: ${presetValues[preset.name]?.mouthOpenY.toFixed(2)}, Form: ${presetValues[preset.name]?.mouthForm.toFixed(2)}`}
          >
            {preset.name}
          </button>
        ))}
      </div>

      {/* Manual Sliders */}
      {selectedPreset ? (
        <>
          <div style={{ marginBottom: '6px', color: '#4a9eff' }}>
            <strong>Editing: {selectedPreset}</strong>
          </div>
          <div style={sliderContainerStyle}>
            <label>
              MouthOpenY: {openY.toFixed(2)}
              <input
                type="range"
                min="0"
                max="1"
                step="0.01"
                value={openY}
                onChange={(e) => handleOpenYChange(parseFloat(e.target.value))}
                style={sliderStyle}
              />
            </label>
          </div>

          <div style={sliderContainerStyle}>
            <label>
              MouthForm: {form.toFixed(2)}
              <input
                type="range"
                min="-1"
                max="1"
                step="0.01"
                value={form}
                onChange={(e) => handleFormChange(parseFloat(e.target.value))}
                style={sliderStyle}
              />
            </label>
          </div>
        </>
      ) : (
        <div style={{ padding: '10px', background: '#222', borderRadius: '4px', textAlign: 'center', color: '#888' }}>
          Click a preset to edit its values
        </div>
      )}

      {/* Action Buttons */}
      <div style={{ marginTop: '10px', display: 'flex', flexWrap: 'wrap', gap: '6px' }}>
        <button style={{ ...buttonStyle, background: '#27ae60' }} onClick={copyAllPresets}>
          Copy All Presets
        </button>
        <button style={buttonStyle} onClick={resetToDefaults}>
          Reset to Defaults
        </button>
      </div>

      {/* Parameter List (collapsible) */}
      {parameterNames.length > 0 && (
        <details style={{ marginTop: '10px' }}>
          <summary style={{ cursor: 'pointer', marginBottom: '6px' }}>
            All Parameters ({parameterNames.length})
          </summary>
          <div style={{ maxHeight: '200px', overflowY: 'auto', fontSize: '10px' }}>
            {parameterNames.map((name, i) => (
              <div key={i} style={{ padding: '2px 0' }}>
                [{i}] {name}: {parameterValues ? parameterValues[i]?.toFixed(3) : 'N/A'}
              </div>
            ))}
          </div>
        </details>
      )}
    </div>
  );
}

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

// Calculate idle head movement position using sin/cos for O and 8 patterns
function calculateIdleMovement(time: number): { x: number; y: number } {
  // Speed variation: makes movement speed up and slow down naturally
  const speedMultiplier = 1 + SPEED_VARIATION_AMOUNT * Math.sin(time * SPEED_VARIATION_FREQUENCY);
  const effectiveSpeed = IDLE_MOVEMENT_BASE_SPEED * speedMultiplier;

  // Pattern blend factor: smoothly alternates between O (circle) and 8 (figure-eight)
  const patternBlend = (Math.sin(time * PATTERN_CHANGE_SPEED) + 1) / 2;

  // Circle pattern (O): x = sin(t), y = cos(t)
  const circleX = Math.sin(time * effectiveSpeed);
  const circleY = Math.cos(time * effectiveSpeed);

  // Figure-eight pattern (8): x = sin(t), y = sin(2t)
  const eightX = Math.sin(time * effectiveSpeed);
  const eightY = Math.sin(time * effectiveSpeed * 2);

  // Blend between patterns
  const x = (circleX * (1 - patternBlend) + eightX * patternBlend) * IDLE_MOVEMENT_AMPLITUDE;
  const y = (circleY * (1 - patternBlend) + eightY * patternBlend) * IDLE_MOVEMENT_AMPLITUDE;

  return { x, y };
}

type ViewMode = 'fullBody' | 'portrait' | 'face';

type Live2DCanvasProps = {
  src: string;
  isListening?: boolean;
  expression?: string;
  motion?: string;
  idleMotionGroup?: string;
  viewMode?: ViewMode;
  scale?: number;
  offsetY?: number;
  className?: string;
  client?: IkonClient;
};

// Custom memo comparison - viseme data comes from audio frame shape sets, not props
const live2DPropsAreEqual = (prev: Live2DCanvasProps, next: Live2DCanvasProps) => {
  return (
    prev.src === next.src &&
    prev.isListening === next.isListening &&
    prev.expression === next.expression &&
    prev.motion === next.motion &&
    prev.idleMotionGroup === next.idleMotionGroup &&
    prev.viewMode === next.viewMode &&
    prev.scale === next.scale &&
    prev.offsetY === next.offsetY &&
    prev.className === next.className &&
    prev.client === next.client
  );
};

// Helper to update model transform based on viewport size and server-provided values
function updateModelTransform(
  model: Live2DModel,
  dimensions: { width: number; height: number },
  scale: number,
  offsetY: number,
  activeWindow: Window = window,
) {
  const viewportWidth = activeWindow.innerWidth;
  const viewportHeight = activeWindow.innerHeight;

  // Scale based on height only - allows horizontal clipping for closer zoom
  const baseScale = viewportHeight / dimensions.height;
  const finalScale = baseScale * scale;

  model.scale.set(finalScale);
  model.anchor.set(0.5, 0.5);

  // Position at viewport center, with vertical offset
  // offsetY adjusts which part of the model is shown (negative = show more head)
  model.x = viewportWidth / 2;
  model.y = viewportHeight / 2 - offsetY * viewportHeight;
}

const Live2DCanvasInner = memo(function Live2DCanvasInner(props: Live2DCanvasProps) {
  const containerRef = useRef<HTMLDivElement>(null);
  const appRef = useRef<Application | null>(null);
  const modelRef = useRef<Live2DModel | null>(null);
  const prevMotionRef = useRef<string | undefined>(undefined);
  const prevExpressionRef = useRef<string | undefined>(undefined);
  const mouthValueRef = useRef<number>(0);
  const mouthFormValueRef = useRef<number>(0);
  const lastMouthUpdateRef = useRef<number>(0);
  const modelDimensionsRef = useRef<{ width: number; height: number } | null>(null);
  const transformRef = useRef<{ scale: number; offsetY: number }>({ scale: 1, offsetY: 0 });
  const scaleRef = useRef<number>(1);
  const offsetYRef = useRef<number>(0);
  const baseTransformRef = useRef<{ x: number; y: number; scale: number } | null>(null);
  const idleTickerCallbackRef = useRef<(() => void) | null>(null);
  const lipSyncCallbackRef = useRef<(() => void) | null>(null);
  const clientRef = useRef<IkonClient | undefined>(props.client);

  // Picture-in-Picture state
  const [isPip, setIsPip] = useState(false);
  const pipWindowRef = useRef<Window | null>(null);
  const activeWindowRef = useRef<Window>(window);
  const [pipSupported] = useState(() => 'documentPictureInPicture' in window);

  // Debug UI state
  const [debugOpenY, setDebugOpenY] = useState(0);
  const [debugForm, setDebugForm] = useState(0);
  const [debugRawOpenY, setDebugRawOpenY] = useState(0);
  const [debugRawForm, setDebugRawForm] = useState(0);
  const [debugPhoneme, setDebugPhoneme] = useState('');
  const [mappingEnabled, setMappingEnabled] = useState(PHONEME_MAPPING_ENABLED);
  const [parameterNames, setParameterNames] = useState<string[]>([]);
  const [parameterValuesRef, setParameterValuesRef] = useState<Float32Array | null>(null);
  const overrideValuesRef = useRef<{ openY: number | null; form: number | null }>({ openY: null, form: null });
  const presetValuesRef = useRef<PresetValues>(getInitialPresetValues());
  const mappingEnabledRef = useRef(PHONEME_MAPPING_ENABLED);

  const handleOverride = useCallback((openY: number | null, form: number | null) => {
    overrideValuesRef.current = { openY, form };
  }, []);

  const handlePresetValuesChange = useCallback((values: PresetValues) => {
    presetValuesRef.current = values;
  }, []);

  const handleMappingToggle = useCallback((enabled: boolean) => {
    setMappingEnabled(enabled);
    mappingEnabledRef.current = enabled;
  }, []);

  // Keep client ref updated
  useEffect(() => {
    clientRef.current = props.client;
  }, [props.client]);

  // Viseme data is now embedded in audio frames via shape sets - polling happens in lipSyncUpdate

  // Update transform refs and apply if values changed
  const newScale = props.scale ?? 1;
  const newOffsetY = props.offsetY ?? 0;
  const transformChanged = scaleRef.current !== newScale || offsetYRef.current !== newOffsetY;

  if (transformChanged) {
    scaleRef.current = newScale;
    offsetYRef.current = newOffsetY;

    const model = modelRef.current;
    const dimensions = modelDimensionsRef.current;

    if (model && dimensions) {
      updateModelTransform(model, dimensions, scaleRef.current, offsetYRef.current, activeWindowRef.current);
      baseTransformRef.current = {
        x: model.x,
        y: model.y,
        scale: model.scale.x,
      };
    }
  }

  // Handle container resize
  useEffect(() => {
    const container = containerRef.current;
    if (!container) return;

    const resizeObserver = new ResizeObserver(() => {
      const app = appRef.current;
      const model = modelRef.current;
      const dimensions = modelDimensionsRef.current;
      const win = activeWindowRef.current;

      if (app) {
        const canvas = app.canvas;
        canvas.width = win.innerWidth;
        canvas.height = win.innerHeight;
        app.renderer.resize(win.innerWidth, win.innerHeight);
      }

      if (model && dimensions) {
        updateModelTransform(model, dimensions, scaleRef.current, offsetYRef.current, win);
        baseTransformRef.current = {
          x: model.x,
          y: model.y,
          scale: model.scale.x,
        };
      }
    });

    resizeObserver.observe(container);

    return () => {
      resizeObserver.disconnect();
    };
  }, []);

  // Initialize PIXI Application and load Live2D model (only when src changes)
  useEffect(() => {
    if (!containerRef.current) return;

    let destroyed = false;
    const container = containerRef.current;

    const initApp = async () => {
      if (destroyed) return;

      const app = new Application();
      await app.init({
        backgroundAlpha: 0,
        // Don't set width/height here - we'll set canvas size directly
        resolution: 1,
        autoDensity: false,
      });

      if (destroyed) {
        app.destroy(true);
        return;
      }

      const win = activeWindowRef.current;
      const canvas = app.canvas;
      canvas.width = win.innerWidth;
      canvas.height = win.innerHeight;
      canvas.style.width = '100%';
      canvas.style.height = '100%';
      canvas.style.display = 'block';

      app.renderer.resize(win.innerWidth, win.innerHeight);

      container.appendChild(canvas);
      appRef.current = app;

      if (!sharedTicker.started) {
        sharedTicker.start();
      }

      try {
        const model = await Live2DModel.from(props.src, {
          ticker: sharedTicker,
          autoUpdate: true,
          autoFocus: false,
        });

        if (destroyed) {
          model.destroy();
          return;
        }

        modelRef.current = model;
        modelDimensionsRef.current = { width: model.width, height: model.height };
        app.stage.addChild(model as unknown as Container);

        model.setRenderer(app.renderer);

        updateModelTransform(model, modelDimensionsRef.current, scaleRef.current, offsetYRef.current, activeWindowRef.current);

        // Disable default mouse tracking by setting autoInteract to false
        model.autoInteract = false;

        // Store base transform for camera movement
        baseTransformRef.current = {
          x: model.x,
          y: model.y,
          scale: model.scale.x,
        };

        // Set up idle head and camera movement animation
        // Use accumulated animation time with capped deltas to prevent jumps after sleep/background
        let lastFrameTime = performance.now();
        let animationTime = 0;
        const MAX_FRAME_DELTA = 100; // Cap at 100ms to prevent jumps
        const idleTickerCallback = () => {
          const currentModel = modelRef.current;
          const baseTransform = baseTransformRef.current;
          if (!currentModel?.internalModel?.focusController || !baseTransform) return;

          const now = performance.now();
          const delta = Math.min(now - lastFrameTime, MAX_FRAME_DELTA);
          lastFrameTime = now;
          animationTime += delta;

          // Head/eye movement
          const { x, y } = calculateIdleMovement(animationTime);
          currentModel.internalModel.focusController.focus(x, y, true);

          // Camera pan - use different frequencies for x and y for organic movement
          const panX = Math.sin(animationTime * CAMERA_PAN_SPEED) * CAMERA_PAN_AMPLITUDE;
          const panY = Math.sin(animationTime * CAMERA_PAN_SPEED * 0.7) * CAMERA_PAN_AMPLITUDE * 0.6;

          // Camera zoom - very slow breathing effect
          const zoomOffset = Math.sin(animationTime * CAMERA_ZOOM_SPEED) * CAMERA_ZOOM_AMPLITUDE;

          // Apply camera movement
          currentModel.x = baseTransform.x + panX;
          currentModel.y = baseTransform.y + panY;
          const newScale = baseTransform.scale * (1 + zoomOffset);
          currentModel.scale.set(newScale);
        };
        idleTickerCallbackRef.current = idleTickerCallback;
        sharedTicker.add(idleTickerCallback);

        // Inspect model parameters to find mouth/lip sync parameter
        const internalModel = model.internalModel as Record<string, unknown>;
        const coreModel = internalModel?.coreModel as Record<string, unknown> | undefined;
        const innerModel = coreModel?._model as Record<string, unknown> | undefined;

        // Log full structure for debugging
        console.log('[Live2D] internalModel.coreModel keys:', coreModel ? Object.keys(coreModel) : 'none');
        console.log('[Live2D] coreModel._model keys:', innerModel ? Object.keys(innerModel) : 'none');

        // Check parameters object in _model
        const parameters = innerModel?.parameters as Record<string, unknown> | undefined;
        if (parameters) {
          console.log('[Live2D] parameters keys:', Object.keys(parameters));
          console.log('[Live2D] parameters.ids:', parameters.ids);
          console.log('[Live2D] parameters.count:', parameters.count);
          console.log('[Live2D] parameters.values type:', parameters.values?.constructor?.name);
        }

        // Check _parameterValues array in coreModel
        const paramValues = coreModel?._parameterValues;
        console.log('[Live2D] _parameterValues type:', paramValues?.constructor?.name, 'length:', (paramValues as Float32Array)?.length);

        // Enable lip sync via internalModel
        if ('lipSyncEnabled' in internalModel) {
          (internalModel as { lipSyncEnabled: boolean }).lipSyncEnabled = true;
          console.log('[Live2D] Enabled lipSyncEnabled');
        }

        let loggedOnce = false;
        let mouthParamIndex = -1;

        // Get parameter IDs - try different locations
        const paramIds = parameters?.ids as string[] | undefined;
        let mouthFormIndex = -1;

        if (paramIds && Array.isArray(paramIds)) {
          // Log all parameters with their indices
          console.log('[Live2D] All parameters:');
          paramIds.forEach((id, i) => console.log(`  [${i}] ${id}`));

          // Find mouth parameters for lip sync (support various naming conventions)
          for (let i = 0; i < paramIds.length; i++) {
            const id = paramIds[i];
            const idLower = id?.toLowerCase() || '';
            // Match: ParamMouthOpenY, PARAM_MOUTH_OPEN_Y, mouth_open_y, MouthOpenY, etc.
            if (mouthParamIndex < 0 && (
              idLower.includes('mouthopeny') ||
              idLower.includes('mouth_open_y') ||
              idLower.includes('mouthopen') ||
              idLower === 'parammouthopeny'
            )) {
              mouthParamIndex = i;
              console.log('[Live2D] MouthOpenY param at index', i, ':', id);
            }
            // Match: ParamMouthForm, PARAM_MOUTH_FORM, mouth_form, MouthForm, etc.
            if (mouthFormIndex < 0 && (
              idLower.includes('mouthform') ||
              idLower.includes('mouth_form') ||
              idLower === 'parammouthform'
            )) {
              mouthFormIndex = i;
              console.log('[Live2D] MouthForm param at index', i, ':', id);
            }
          }

          // If no mouth form found, that's okay - we'll just use mouthOpenY
          if (mouthParamIndex < 0) {
            console.warn('[Live2D] WARNING: No mouth open parameter found! Lip sync will not work.');
          }
        } else {
          console.warn('[Live2D] WARNING: No parameter IDs found in model!');
        }

        // Get the values array for direct manipulation
        const parameterValues = (parameters?.values || coreModel?._parameterValues) as Float32Array | undefined;
        console.log('[Live2D] Using parameterValues:', parameterValues?.constructor?.name, 'mouthOpen:', mouthParamIndex, 'mouthForm:', mouthFormIndex);

        // Store parameter info for debug UI
        if (paramIds) {
          setParameterNames([...paramIds]);
        }
        if (parameterValues) {
          setParameterValuesRef(parameterValues);
        }

        // Throttle debug UI updates to avoid excessive re-renders
        let lastDebugUpdate = 0;
        const DEBUG_UPDATE_INTERVAL = 50; // ms

        const lipSyncUpdate = () => {
          try {
            if (!parameterValues) return;

            const override = overrideValuesRef.current;
            const now = Date.now();

            // Check if override is active
            if (override.openY !== null || override.form !== null) {
              // Use override values directly (no smoothing for immediate feedback)
              const openY = override.openY ?? mouthValueRef.current;
              const form = override.form ?? mouthFormValueRef.current;

              mouthValueRef.current = openY;
              mouthFormValueRef.current = form;

              if (mouthParamIndex >= 0) {
                parameterValues[mouthParamIndex] = openY;
              }
              if (mouthFormIndex >= 0) {
                parameterValues[mouthFormIndex] = form;
              }

              // Update debug UI (throttled)
              if (now - lastDebugUpdate > DEBUG_UPDATE_INTERVAL) {
                setDebugOpenY(openY);
                setDebugForm(form);
                lastDebugUpdate = now;
              }
              return;
            }

            // Get viseme data from SDK (already timed and interpolated)
            const visemeData = clientRef.current?.media?.audio.getCurrentVisemeValues();

            // Target values to smooth toward
            let targetOpenY: number;
            let targetForm: number;
            let detectedPhoneme = '';

            if (visemeData && visemeData.mouthOpenY > LIP_SYNC_NOISE_GATE) {
              // Raw server values (RMS amplitude and FFT frequency)
              const rawRms = visemeData.mouthOpenY;
              const rawFft = visemeData.mouthForm;

              if (mappingEnabledRef.current) {
                // Map raw RMS/FFT to phoneme-based output values
                const mapped = mapRmsAndFftToPhoneme(rawRms, rawFft, presetValuesRef.current);
                targetOpenY = mapped.mouthOpenY;
                targetForm = mapped.mouthForm;

                // Find detected phoneme for debug display (best match)
                let bestMatch = { name: 'Idle', distance: Infinity };
                for (const preset of VISEME_PRESETS) {
                  if (preset.name === 'Idle') continue;
                  let rmsD = 0;
                  if (rawRms < preset.inputRmsMin) rmsD = preset.inputRmsMin - rawRms;
                  else if (rawRms > preset.inputRmsMax) rmsD = rawRms - preset.inputRmsMax;
                  let fftD = 0;
                  if (rawFft < preset.inputFftMin) fftD = preset.inputFftMin - rawFft;
                  else if (rawFft > preset.inputFftMax) fftD = rawFft - preset.inputFftMax;
                  const d = rmsD * 1.5 + fftD;
                  if (d < bestMatch.distance) bestMatch = { name: preset.name, distance: d };
                }
                detectedPhoneme = bestMatch.name;
              } else {
                // Use raw server values directly
                targetOpenY = rawRms;
                targetForm = rawFft;
              }

              lastMouthUpdateRef.current = now;

              // Update raw values for debug display (throttled)
              if (now - lastDebugUpdate > DEBUG_UPDATE_INTERVAL) {
                setDebugRawOpenY(rawRms);
                setDebugRawForm(rawFft);
                if (mappingEnabledRef.current) {
                  setDebugPhoneme(detectedPhoneme);
                }
              }
            } else {
              // No viseme data - check if we should decay to idle
              const timeSinceUpdate = now - lastMouthUpdateRef.current;
              if (timeSinceUpdate > LIP_SYNC_DECAY_DELAY_MS) {
                // Decay toward idle pose
                targetOpenY = IDLE_MOUTH_OPEN_Y;
                targetForm = IDLE_MOUTH_FORM;
              } else {
                // Keep current values during brief gaps
                targetOpenY = mouthValueRef.current;
                targetForm = mouthFormValueRef.current;
              }
            }

            // Apply EMA smoothing for additional visual polish
            mouthValueRef.current = mouthValueRef.current + LIP_SYNC_SMOOTHING * (targetOpenY - mouthValueRef.current);
            mouthFormValueRef.current = mouthFormValueRef.current + LIP_SYNC_SMOOTHING * (targetForm - mouthFormValueRef.current);

            // Apply to Live2D parameters
            if (mouthParamIndex >= 0) {
              parameterValues[mouthParamIndex] = mouthValueRef.current;
            }
            if (mouthFormIndex >= 0) {
              parameterValues[mouthFormIndex] = mouthFormValueRef.current;
            }

            // Update debug UI (throttled)
            if (now - lastDebugUpdate > DEBUG_UPDATE_INTERVAL) {
              setDebugOpenY(mouthValueRef.current);
              setDebugForm(mouthFormValueRef.current);
              lastDebugUpdate = now;
            }

            if (!loggedOnce && visemeData && visemeData.mouthOpenY > LIP_SYNC_NOISE_GATE) {
              console.log('[Live2D] Lip sync active:', { openY: mouthValueRef.current.toFixed(3), form: mouthFormValueRef.current.toFixed(3), mouthParamIndex, mouthFormIndex });
              loggedOnce = true;
            }
          } catch (e) {
            console.error('[Live2D] Lip sync error:', e);
          }
        };

        // Set initial idle state
        mouthValueRef.current = IDLE_MOUTH_OPEN_Y;
        mouthFormValueRef.current = IDLE_MOUTH_FORM;
        lastMouthUpdateRef.current = 0;
        if (parameterValues) {
          if (mouthParamIndex >= 0) {
            console.log('[Live2D] Model default mouthOpenY was:', parameterValues[mouthParamIndex]);
            parameterValues[mouthParamIndex] = IDLE_MOUTH_OPEN_Y;
          }
          if (mouthFormIndex >= 0) {
            console.log('[Live2D] Model default mouthForm was:', parameterValues[mouthFormIndex]);
            parameterValues[mouthFormIndex] = IDLE_MOUTH_FORM;
          }
          console.log('[Live2D] Set initial idle state: mouthOpenY=' + IDLE_MOUTH_OPEN_Y + ', mouthForm=' + IDLE_MOUTH_FORM);
        }

        console.log('[Live2D] Lip sync registered');

        lipSyncCallbackRef.current = lipSyncUpdate;
        sharedTicker.add(lipSyncUpdate);

        // Note: We don't start idle motion as it may override mouth parameters
        // If you want idle motion for body/eyes, the motion file should not animate mouth params
      } catch (error) {
        console.error('Failed to load Live2D model:', error);
      }
    };

    void initApp();

    return () => {
      destroyed = true;
      if (pipWindowRef.current) {
        pipWindowRef.current.close();
        pipWindowRef.current = null;
      }
      activeWindowRef.current = window;
      if (idleTickerCallbackRef.current) {
        sharedTicker.remove(idleTickerCallbackRef.current);
        idleTickerCallbackRef.current = null;
      }
      if (lipSyncCallbackRef.current) {
        sharedTicker.remove(lipSyncCallbackRef.current);
        lipSyncCallbackRef.current = null;
      }
      if (modelRef.current) {
        modelRef.current.destroy();
        modelRef.current = null;
      }
      if (appRef.current) {
        appRef.current.destroy(true);
        appRef.current = null;
      }
      modelDimensionsRef.current = null;
    };
  }, [props.src, props.idleMotionGroup]);

  // Handle listening state - play listening/attention animation
  useEffect(() => {
    const model = modelRef.current;
    if (!model) return;

    if (props.isListening) {
      // Try to play a listening/attention motion
      const listeningMotions = ['listening', 'attention', 'think', 'idle'];
      for (const motionName of listeningMotions) {
        model.motion(motionName)?.catch(() => {
          // Motion not available, try next
        });
        break;
      }
    }
  }, [props.isListening]);

  // Handle expression changes
  useEffect(() => {
    const model = modelRef.current;
    if (!model || !props.expression) return;
    if (props.expression === prevExpressionRef.current) return;

    prevExpressionRef.current = props.expression;
    model.expression(props.expression)?.catch(() => {
      // Expression not available
    });
  }, [props.expression]);

  // Handle motion changes
  useEffect(() => {
    const model = modelRef.current;
    if (!model || !props.motion) return;
    if (props.motion === prevMotionRef.current) return;

    prevMotionRef.current = props.motion;
    model.motion(props.motion)?.catch(() => {
      // Motion not available
    });
  }, [props.motion]);

  const handlePipToggle = useCallback(async () => {
    if (isPip) {
      pipWindowRef.current?.close();
      return;
    }

    const container = containerRef.current;
    if (!container || !window.documentPictureInPicture) return;

    try {
      const pipWindow = await window.documentPictureInPicture.requestWindow({
        width: 300,
        height: 400,
      });

      // Copy stylesheets so Crosswind classes still work
      for (const sheet of document.styleSheets) {
        try {
          if (sheet.href) {
            const link = pipWindow.document.createElement('link');
            link.rel = 'stylesheet';
            link.href = sheet.href;
            pipWindow.document.head.appendChild(link);
          } else if (sheet.cssRules) {
            const style = pipWindow.document.createElement('style');
            for (const rule of sheet.cssRules) {
              style.appendChild(pipWindow.document.createTextNode(rule.cssText));
            }
            pipWindow.document.head.appendChild(style);
          }
        } catch {
          // Cross-origin stylesheets may throw
        }
      }

      // Style PiP body
      pipWindow.document.body.style.margin = '0';
      pipWindow.document.body.style.overflow = 'hidden';
      pipWindow.document.body.style.background = 'transparent';

      // Move the canvas container into PiP window
      pipWindow.document.body.appendChild(container);

      pipWindowRef.current = pipWindow;
      activeWindowRef.current = pipWindow;
      setIsPip(true);

      // Resize renderer to PiP window
      const app = appRef.current;
      if (app) {
        const canvas = app.canvas;
        canvas.width = pipWindow.innerWidth;
        canvas.height = pipWindow.innerHeight;
        app.renderer.resize(pipWindow.innerWidth, pipWindow.innerHeight);
      }

      // Recalculate model transform
      const model = modelRef.current;
      const dimensions = modelDimensionsRef.current;
      if (model && dimensions) {
        updateModelTransform(model, dimensions, scaleRef.current, offsetYRef.current, pipWindow);
        baseTransformRef.current = {
          x: model.x,
          y: model.y,
          scale: model.scale.x,
        };
      }

      // Handle PiP window resize
      const onPipResize = () => {
        const app = appRef.current;
        const model = modelRef.current;
        const dims = modelDimensionsRef.current;
        if (app) {
          const canvas = app.canvas;
          canvas.width = pipWindow.innerWidth;
          canvas.height = pipWindow.innerHeight;
          app.renderer.resize(pipWindow.innerWidth, pipWindow.innerHeight);
        }
        if (model && dims) {
          updateModelTransform(model, dims, scaleRef.current, offsetYRef.current, pipWindow);
          baseTransformRef.current = {
            x: model.x,
            y: model.y,
            scale: model.scale.x,
          };
        }
      };
      pipWindow.addEventListener('resize', onPipResize);

      // Handle PiP window close — move canvas back to main page
      pipWindow.addEventListener('pagehide', () => {
        pipWindow.removeEventListener('resize', onPipResize);

        const wrapper = document.querySelector('[data-live2d-wrapper]');
        if (wrapper && container.parentNode !== wrapper) {
          wrapper.appendChild(container);
        }

        pipWindowRef.current = null;
        activeWindowRef.current = window;
        setIsPip(false);

        // Restore main window sizing
        const app = appRef.current;
        if (app) {
          const canvas = app.canvas;
          canvas.width = window.innerWidth;
          canvas.height = window.innerHeight;
          app.renderer.resize(window.innerWidth, window.innerHeight);
        }

        const model = modelRef.current;
        const dims = modelDimensionsRef.current;
        if (model && dims) {
          updateModelTransform(model, dims, scaleRef.current, offsetYRef.current, window);
          baseTransformRef.current = {
            x: model.x,
            y: model.y,
            scale: model.scale.x,
          };
        }
      });
    } catch (e) {
      console.error('[Live2D] PiP failed:', e);
    }
  }, [isPip]);

  return (
    <div data-live2d-wrapper style={{ position: 'relative', width: '100%', height: '100%' }}>
      <div ref={containerRef} className={props.className} style={{ width: '100%', height: '100%' }} />
      {pipSupported && !isPip && (
        <button
          onClick={handlePipToggle}
          style={{
            position: 'absolute',
            top: 8,
            left: 8,
            width: 32,
            height: 32,
            border: 'none',
            borderRadius: 6,
            background: 'rgba(0, 0, 0, 0.4)',
            color: '#fff',
            cursor: 'pointer',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            opacity: 0,
            transition: 'opacity 0.2s',
            zIndex: 10,
          }}
          onMouseEnter={(e) => { e.currentTarget.style.opacity = '1'; }}
          onMouseLeave={(e) => { e.currentTarget.style.opacity = '0'; }}
          title="Pop out to Picture-in-Picture"
        >
          <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
            <rect x="2" y="3" width="20" height="14" rx="2" />
            <rect x="12" y="9" width="8" height="6" rx="1" />
          </svg>
        </button>
      )}
      {SHOW_VISEME_DEBUG_UI && (
        <VisemeDebugPanel
          currentOpenY={debugOpenY}
          currentForm={debugForm}
          rawServerOpenY={debugRawOpenY}
          rawServerForm={debugRawForm}
          detectedPhoneme={debugPhoneme}
          onOverride={handleOverride}
          onPresetValuesChange={handlePresetValuesChange}
          mappingEnabled={mappingEnabled}
          onMappingToggle={handleMappingToggle}
          parameterNames={parameterNames}
          parameterValues={parameterValuesRef}
        />
      )}
    </div>
  );
}, live2DPropsAreEqual);

// Single cached renderer component - defined once to prevent React recreation
const Live2DCanvasRenderer = memo(function Live2DCanvasRenderer({
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
  const idleMotionGroup = toStringValue(node.props['idleMotionGroup']);
  const viewModeValue = toStringValue(node.props['viewMode']);
  const viewMode = (viewModeValue === 'fullBody' || viewModeValue === 'portrait' || viewModeValue === 'face')
    ? viewModeValue as ViewMode
    : undefined;
  const scale = toFiniteNumber(node.props['scale']);
  const offsetY = toFiniteNumber(node.props['offsetY']);

  const combinedClassName = combineClassNames(
    node.styleIds,
    className,
    toStringValue(node.props['style']),
  );

  return (
    <Live2DCanvasInner
      src={src}
      isListening={isListening}
      expression={expression}
      motion={motion}
      idleMotionGroup={idleMotionGroup}
      viewMode={viewMode}
      scale={scale}
      offsetY={offsetY}
      className={combinedClassName}
      client={context.client}
    />
  );
});

export function createLive2DCanvasResolver(): IkonUiComponentResolver {
  return (initialNode) => {
    if (initialNode.type !== 'live2d-canvas') {
      return undefined;
    }

    // Always return the same component reference
    return Live2DCanvasRenderer;
  };
}

import { memo, useEffect, useRef, useState, useCallback } from 'react';
import { type IkonUiComponentResolver, type UiComponentRendererProps, useUiNode } from '@ikonai/sdk-react-ui';

type AmbientVideoProps = {
  url?: string;
  playbackRate?: number;
  loop?: boolean;
  muted?: boolean;
  autoplay?: boolean;
  className?: string;
  crossfadeDuration?: number;
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

const AmbientVideoPlayer = memo(function AmbientVideoPlayer({
  url,
  playbackRate = 0.25,
  loop = true,
  muted = true,
  autoplay = true,
  className,
  crossfadeDuration = 2,
}: AmbientVideoProps) {
  const videoARef = useRef<HTMLVideoElement>(null);
  const videoBRef = useRef<HTMLVideoElement>(null);
  const containerRef = useRef<HTMLDivElement>(null);
  const animationFrameRef = useRef<number>(0);

  // Track which video is currently "active" (foreground)
  const activeRef = useRef<'A' | 'B'>('A');
  const crossfadingRef = useRef(false);

  const [isFullscreen, setIsFullscreen] = useState(false);
  const [currentRate, setCurrentRate] = useState(playbackRate);
  const [opacityA, setOpacityA] = useState(1);
  const [opacityB, setOpacityB] = useState(0);

  useEffect(() => {
    if (videoARef.current) {
      videoARef.current.playbackRate = currentRate;
    }
    if (videoBRef.current) {
      videoBRef.current.playbackRate = currentRate;
    }
  }, [currentRate, url]);

  // Crossfade loop logic
  useEffect(() => {
    if (!loop || !url) return;

    let isActive = true;

    const tick = () => {
      if (!isActive) return;

      const activeVideo = activeRef.current === 'A' ? videoARef.current : videoBRef.current;
      const standbyVideo = activeRef.current === 'A' ? videoBRef.current : videoARef.current;

      if (activeVideo && standbyVideo && activeVideo.duration > 0 && !activeVideo.paused) {
        const timeRemaining = activeVideo.duration - activeVideo.currentTime;

        if (timeRemaining <= crossfadeDuration && !crossfadingRef.current) {
          // Start crossfade: cue up standby video from the beginning
          crossfadingRef.current = true;
          standbyVideo.currentTime = 0;
          standbyVideo.playbackRate = currentRate;
          standbyVideo.play().catch(() => {});
        }

        if (crossfadingRef.current) {
          const timeRemaining2 = activeVideo.duration - activeVideo.currentTime;
          const progress = Math.max(0, Math.min(1, 1 - timeRemaining2 / crossfadeDuration));

          if (activeRef.current === 'A') {
            setOpacityA(1 - progress);
            setOpacityB(progress);
          } else {
            setOpacityB(1 - progress);
            setOpacityA(progress);
          }

          // Crossfade complete
          if (progress >= 1) {
            activeVideo.pause();
            activeRef.current = activeRef.current === 'A' ? 'B' : 'A';
            crossfadingRef.current = false;
          }
        }
      }

      animationFrameRef.current = requestAnimationFrame(tick);
    };

    animationFrameRef.current = requestAnimationFrame(tick);

    return () => {
      isActive = false;
      if (animationFrameRef.current) {
        cancelAnimationFrame(animationFrameRef.current);
      }
    };
  }, [loop, url, crossfadeDuration, currentRate]);

  useEffect(() => {
    const handleFullscreenChange = () => {
      setIsFullscreen(!!document.fullscreenElement);
    };
    document.addEventListener('fullscreenchange', handleFullscreenChange);
    return () => document.removeEventListener('fullscreenchange', handleFullscreenChange);
  }, []);

  const toggleFullscreen = useCallback(() => {
    if (!containerRef.current) return;
    if (!document.fullscreenElement) {
      containerRef.current.requestFullscreen().catch(console.error);
    } else {
      document.exitFullscreen().catch(console.error);
    }
  }, []);

  const handleVideoLoad = useCallback((ref: React.RefObject<HTMLVideoElement | null>) => {
    return () => {
      if (ref.current) {
        ref.current.playbackRate = currentRate;
      }
    };
  }, [currentRate]);

  const speedOptions = [
    { label: '⅛', value: 0.125 },
    { label: '¼', value: 0.25 },
    { label: '½', value: 0.5 },
    { label: '1×', value: 1 },
  ];

  if (!url) {
    return null;
  }

  const videoStyle: React.CSSProperties = {
    position: 'absolute',
    width: '100%',
    height: '100%',
    objectFit: 'cover',
    transition: 'opacity 0.1s linear',
  };

  return (
    <div ref={containerRef} className={className} style={{ overflow: 'hidden' }}>
      <video
        ref={videoARef}
        src={url}
        crossOrigin="anonymous"
        muted={muted}
        autoPlay={autoplay}
        playsInline
        onLoadedData={handleVideoLoad(videoARef)}
        style={{ ...videoStyle, opacity: opacityA }}
      />

      <video
        ref={videoBRef}
        src={url}
        crossOrigin="anonymous"
        muted={muted}
        playsInline
        onLoadedData={handleVideoLoad(videoBRef)}
        style={{ ...videoStyle, opacity: opacityB }}
      />

      {/* Controls */}
      <div
        style={{
          position: 'absolute',
          bottom: '16px',
          right: '16px',
          display: 'flex',
          gap: '8px',
          alignItems: 'center',
          zIndex: 50,
        }}
        onClick={(e) => e.stopPropagation()}
      >
        <div
          style={{
            display: 'flex',
            gap: '4px',
            backgroundColor: 'rgba(0, 0, 0, 0.6)',
            borderRadius: '8px',
            padding: '4px',
            backdropFilter: 'blur(8px)',
          }}
        >
          {speedOptions.map((option) => (
            <button
              key={option.value}
              onClick={() => setCurrentRate(option.value)}
              style={{
                padding: '6px 10px',
                borderRadius: '6px',
                border: 'none',
                backgroundColor: currentRate === option.value ? 'rgba(255, 255, 255, 0.3)' : 'transparent',
                color: currentRate === option.value ? '#fff' : 'rgba(255, 255, 255, 0.7)',
                cursor: 'pointer',
                fontSize: '12px',
                fontWeight: currentRate === option.value ? 600 : 400,
                transition: 'all 0.2s ease',
              }}
            >
              {option.label}
            </button>
          ))}
        </div>

        <button
          onClick={toggleFullscreen}
          style={{
            padding: '8px',
            borderRadius: '8px',
            border: 'none',
            backgroundColor: 'rgba(0, 0, 0, 0.6)',
            color: 'rgba(255, 255, 255, 0.9)',
            cursor: 'pointer',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            backdropFilter: 'blur(8px)',
            transition: 'all 0.2s ease',
          }}
          title={isFullscreen ? 'Exit fullscreen' : 'Enter fullscreen'}
        >
          {isFullscreen ? (
            <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
              <path d="M8 3v3a2 2 0 0 1-2 2H3" />
              <path d="M21 8h-3a2 2 0 0 1-2-2V3" />
              <path d="M3 16h3a2 2 0 0 1 2 2v3" />
              <path d="M16 21v-3a2 2 0 0 1 2-2h3" />
            </svg>
          ) : (
            <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
              <path d="M8 3H5a2 2 0 0 0-2 2v3" />
              <path d="M21 8V5a2 2 0 0 0-2-2h-3" />
              <path d="M3 16v3a2 2 0 0 0 2 2h3" />
              <path d="M16 21h3a2 2 0 0 0 2-2v-3" />
            </svg>
          )}
        </button>
      </div>
    </div>
  );
});

const AmbientVideoRenderer = memo(function AmbientVideoRenderer({
  nodeId,
  context,
  className,
}: UiComponentRendererProps & { initialNode?: unknown }) {
  const node = useUiNode(context.store, nodeId);

  if (!node) {
    return null;
  }

  const url = toStringValue(node.props['url']);
  const playbackRate = toFiniteNumber(node.props['playbackRate']) ?? 0.25;
  const loop = toOptionalBoolean(node.props['loop']) ?? true;
  const muted = toOptionalBoolean(node.props['muted']) ?? true;
  const autoplay = toOptionalBoolean(node.props['autoplay']) ?? true;
  const crossfadeDuration = toFiniteNumber(node.props['crossfadeDuration']) ?? 2;
  const inlineStyle = toStringValue(node.props['style']);

  const combinedClassName = combineClassNames(node.styleIds, className, inlineStyle);

  return (
    <AmbientVideoPlayer
      url={url}
      playbackRate={playbackRate}
      loop={loop}
      muted={muted}
      autoplay={autoplay}
      crossfadeDuration={crossfadeDuration}
      className={combinedClassName}
    />
  );
});

export function createAmbientVideoResolver(): IkonUiComponentResolver {
  return (initialNode) => {
    if (initialNode.type !== 'ambient-video-player') {
      return undefined;
    }

    return AmbientVideoRenderer;
  };
}

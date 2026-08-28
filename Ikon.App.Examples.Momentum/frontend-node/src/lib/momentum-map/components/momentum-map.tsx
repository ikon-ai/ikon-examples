import { memo, useEffect, useRef } from 'react';
import { type IkonUiComponentResolver, type UiComponentRendererProps, useUiNode } from '@ikonai/sdk-react-ui';
import L from 'leaflet';
import 'leaflet/dist/leaflet.css';

// A dark Leaflet map carrying the recorded line, optionally with one stretch of it lit up. Everything
// it draws arrives as JSON props streamed from the C# app, and the map owns its own pan and zoom — a
// re-render never yanks the view away from the rider unless `follow` asks it to stay with them.

const PAGE = '#0b0b0d';
const SURFACE = '#161618';
const TEXT = '#f7f7f7';

interface MapPoint {
  lat: number;
  lon: number;
}

interface MapMarker {
  id: string;
  lat: number;
  lon: number;
  type: string;
  label?: string;
}

type MomentumMapProps = {
  track?: string;
  emphasis?: string;
  markers?: string;
  center?: string;
  zoom?: number;
  follow?: boolean;
  lineColor?: string;
  emphasisColor?: string;
  className?: string;
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

function toBooleanValue(value: unknown): boolean {
  return value === true || value === 'true';
}

function combineClassNames(styleIds: readonly string[], className?: string): string | undefined {
  const parts = styleIds.filter((value) => typeof value === 'string' && value.trim().length > 0).map((value) => value.trim());
  if (typeof className === 'string' && className.trim().length > 0) {
    parts.push(className.trim());
  }
  return parts.length > 0 ? parts.join(' ') : undefined;
}

function parseJsonArray<T>(json: string | undefined, isItem: (item: unknown) => item is T): T[] {
  if (!json) {
    return [];
  }
  try {
    const parsed: unknown = JSON.parse(json);
    return Array.isArray(parsed) ? parsed.filter(isItem) : [];
  } catch {
    return [];
  }
}

function isPoint(item: unknown): item is MapPoint {
  return typeof item === 'object' && item !== null && typeof (item as MapPoint).lat === 'number' && typeof (item as MapPoint).lon === 'number';
}

function isMarker(item: unknown): item is MapMarker {
  return isPoint(item) && typeof (item as MapMarker).id === 'string' && typeof (item as MapMarker).type === 'string';
}

function parseCenter(json: string | undefined): [number, number] | undefined {
  if (!json) {
    return undefined;
  }
  try {
    const parsed: unknown = JSON.parse(json);
    if (Array.isArray(parsed) && parsed.length >= 2 && typeof parsed[0] === 'number' && typeof parsed[1] === 'number') {
      return [parsed[0], parsed[1]];
    }
  } catch {
    return undefined;
  }
  return undefined;
}

function hereIcon(color: string): L.DivIcon {
  const html = `
    <div style="position:relative;width:44px;height:44px;">
      <div style="position:absolute;inset:0;border-radius:50%;border:1px solid ${color};opacity:.5;animation:momentumPulse 2.2s ease-out infinite;"></div>
      <div style="position:absolute;top:50%;left:50%;transform:translate(-50%,-50%);width:13px;height:13px;border-radius:50%;background:${color};border:2px solid ${PAGE};box-shadow:0 0 0 2px ${color}55;"></div>
    </div>`;
  return L.divIcon({ html, className: 'momentum-here', iconSize: [44, 44], iconAnchor: [22, 22] });
}

function dotIcon(marker: MapMarker, lineColor: string, emphasisColor: string): L.DivIcon {
  const fill = marker.type === 'highlight' ? emphasisColor : marker.type === 'end' ? lineColor : PAGE;
  const size = marker.type === 'highlight' ? 12 : 10;
  const html = `<div style="width:${size}px;height:${size}px;border-radius:50%;background:${fill};border:2px solid ${TEXT};"></div>`;
  return L.divIcon({ html, className: 'momentum-dot', iconSize: [size, size], iconAnchor: [size / 2, size / 2] });
}

const MomentumMapInner = memo(
  function MomentumMapInner(props: MomentumMapProps) {
    const containerRef = useRef<HTMLDivElement>(null);
    const mapRef = useRef<L.Map | null>(null);
    const trackLayerRef = useRef<L.LayerGroup | null>(null);
    const markersLayerRef = useRef<L.LayerGroup | null>(null);
    const fittedRef = useRef(false);
    // The first center/zoom seed the map; later values never move it, so they are read once.
    const initialViewRef = useRef({ center: parseCenter(props.center) ?? ([60.1719, 24.9414] as [number, number]), zoom: props.zoom ?? 13 });

    const lineColor = props.lineColor ?? '#db176e';
    const emphasisColor = props.emphasisColor ?? '#f2da00';

    useEffect(() => {
      if (!containerRef.current || mapRef.current) {
        return;
      }

      const map = L.map(containerRef.current, {
        center: initialViewRef.current.center,
        zoom: initialViewRef.current.zoom,
        zoomControl: false,
        attributionControl: false,
      });

      L.tileLayer('https://{s}.basemaps.cartocdn.com/dark_nolabels/{z}/{x}/{y}{r}.png', {
        subdomains: 'abcd',
        maxZoom: 19,
      }).addTo(map);

      trackLayerRef.current = L.layerGroup().addTo(map);
      markersLayerRef.current = L.layerGroup().addTo(map);
      mapRef.current = map;

      const resizeObserver = new ResizeObserver(() => map.invalidateSize());
      resizeObserver.observe(containerRef.current);
      const sizeTimer = window.setTimeout(() => map.invalidateSize(), 150);

      return () => {
        resizeObserver.disconnect();
        window.clearTimeout(sizeTimer);
        map.remove();
        mapRef.current = null;
        trackLayerRef.current = null;
        markersLayerRef.current = null;
      };
    }, []);

    useEffect(() => {
      const layer = trackLayerRef.current;
      const map = mapRef.current;
      if (!layer || !map) {
        return;
      }

      layer.clearLayers();
      const points = parseJsonArray(props.track, isPoint);

      if (points.length < 2) {
        fittedRef.current = false;
        return;
      }

      const latLngs = points.map((p) => [p.lat, p.lon] as [number, number]);
      // A dark casing under the line keeps it legible where the track crosses itself or runs over a
      // pale patch of the basemap.
      L.polyline(latLngs, { color: PAGE, weight: 7, opacity: 0.6, lineCap: 'round', lineJoin: 'round' }).addTo(layer);
      L.polyline(latLngs, { color: lineColor, weight: 3, opacity: 0.95, lineCap: 'round', lineJoin: 'round' }).addTo(layer);

      const emphasis = parseJsonArray(props.emphasis, isPoint);

      if (emphasis.length > 1) {
        L.polyline(
          emphasis.map((p) => [p.lat, p.lon] as [number, number]),
          { color: emphasisColor, weight: 5, opacity: 1, lineCap: 'round', lineJoin: 'round' },
        ).addTo(layer);
      }

      // A finished outing is fitted once; a live one keeps the rider's own framing. The fit waits for
      // Leaflet's first size pass — fitting a container that still measures 0x0 yields an invalid zoom.
      if (!props.follow && !fittedRef.current) {
        fittedRef.current = true;
        const bounds = L.latLngBounds(latLngs);
        const fitTimer = window.setTimeout(() => {
          if (mapRef.current) {
            mapRef.current.invalidateSize();
            mapRef.current.fitBounds(bounds, { padding: [30, 30] });
          }
        }, 200);
        return () => window.clearTimeout(fitTimer);
      }

      return undefined;
    }, [props.track, props.emphasis, props.follow, lineColor, emphasisColor]);

    useEffect(() => {
      const layer = markersLayerRef.current;
      const map = mapRef.current;
      if (!layer || !map) {
        return;
      }

      layer.clearLayers();

      for (const marker of parseJsonArray(props.markers, isMarker)) {
        const icon = marker.type === 'here' ? hereIcon(lineColor) : dotIcon(marker, lineColor, emphasisColor);
        L.marker([marker.lat, marker.lon], { icon, zIndexOffset: marker.type === 'here' ? 500 : 100, interactive: false }).addTo(layer);

        if (marker.type === 'here' && props.follow) {
          map.panTo([marker.lat, marker.lon], { animate: true, duration: 0.9 });
        }
      }
    }, [props.markers, props.follow, lineColor, emphasisColor]);

    return <div ref={containerRef} className={props.className} style={{ background: SURFACE }} />;
  },
  (prev, next) =>
    prev.track === next.track &&
    prev.emphasis === next.emphasis &&
    prev.markers === next.markers &&
    prev.follow === next.follow &&
    prev.className === next.className,
);

const MomentumMapRenderer = memo(function MomentumMapRenderer({ nodeId, context, className }: UiComponentRendererProps & { initialNode?: unknown }) {
  const node = useUiNode(context.store, nodeId);

  if (!node) {
    return null;
  }

  return (
    <MomentumMapInner
      track={toStringValue(node.props['track'])}
      emphasis={toStringValue(node.props['emphasis'])}
      markers={toStringValue(node.props['markers'])}
      center={toStringValue(node.props['center'])}
      zoom={toFiniteNumber(node.props['zoom'])}
      follow={toBooleanValue(node.props['follow'])}
      lineColor={toStringValue(node.props['lineColor'])}
      emphasisColor={toStringValue(node.props['emphasisColor'])}
      className={combineClassNames(node.styleIds, className)}
    />
  );
});

export function createMomentumMapResolver(): IkonUiComponentResolver {
  return (initialNode) => (initialNode.type === 'momentum-map' ? MomentumMapRenderer : undefined);
}

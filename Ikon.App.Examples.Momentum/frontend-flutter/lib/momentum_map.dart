import 'dart:convert';

import 'package:flutter/material.dart';
import 'package:flutter_map/flutter_map.dart';
import 'package:ikon_sdk/ikon_sdk.dart';
import 'package:latlong2/latlong.dart';

/// The Flutter half of the `momentum-map` node. The React frontend draws the same node with Leaflet;
/// both are fed by one `view.MomentumMap(...)` call in the C# app, so the map is the same map on a
/// phone as it is in a browser.
///
/// Everything arrives as JSON strings in the node's props, which is what the Parallax wire carries.
/// Bad or half-written JSON is treated as an empty layer rather than an error: a map with no line on
/// it is a far better outcome than a red screen over a recording that is still running.
const String momentumMapNodeType = 'momentum-map';

const Color _page = Color(0xFF0B0B0D);
const Color _surface = Color(0xFF161618);
const Color _text = Color(0xFFF7F7F7);
const Color _defaultLine = Color(0xFFDB176E);
const Color _defaultEmphasis = Color(0xFFF2DA00);

/// The default registry with this app's map node added to it.
IkonComponentRegistry buildMomentumRegistry() {
  final registry = IkonComponentRegistry();
  registry.register(momentumMapNodeType, (node, style, children, context) {
    return MomentumMap(
      track: _parsePoints(node.props['track']),
      emphasis: _parsePoints(node.props['emphasis']),
      markers: _parseMarkers(node.props['markers']),
      center: _parseCenter(node.props['center']),
      zoom: _parseDouble(node.props['zoom']) ?? 13,
      follow: node.props['follow'] == true || node.props['follow'] == 'true',
      lineColor: _parseColor(node.props['lineColor']) ?? _defaultLine,
      emphasisColor: _parseColor(node.props['emphasisColor']) ?? _defaultEmphasis,
    );
  });

  return registry;
}

class MomentumMapMarker {
  const MomentumMapMarker({required this.id, required this.point, required this.type, this.label});

  final String id;
  final LatLng point;
  final String type;
  final String? label;
}

class MomentumMap extends StatefulWidget {
  const MomentumMap({
    super.key,
    required this.track,
    required this.emphasis,
    required this.markers,
    required this.center,
    required this.zoom,
    required this.follow,
    required this.lineColor,
    required this.emphasisColor,
  });

  final List<LatLng> track;
  final List<LatLng> emphasis;
  final List<MomentumMapMarker> markers;
  final LatLng? center;
  final double zoom;
  final bool follow;
  final Color lineColor;
  final Color emphasisColor;

  @override
  State<MomentumMap> createState() => _MomentumMapState();
}

class _MomentumMapState extends State<MomentumMap> {
  final MapController _controller = MapController();
  bool _fitted = false;
  bool _ready = false;

  @override
  void didUpdateWidget(covariant MomentumMap oldWidget) {
    super.didUpdateWidget(oldWidget);

    if (!_ready) {
      return;
    }

    if (widget.follow) {
      final here = _here();

      if (here != null) {
        _controller.move(here, _controller.camera.zoom);
      }

      return;
    }

    // A finished outing is framed once. Re-fitting on every prop change would fight the rider every
    // time they panned or pinched.
    if (!_fitted && widget.track.length > 1) {
      _fitted = true;
      _controller.fitCamera(CameraFit.bounds(bounds: LatLngBounds.fromPoints(widget.track), padding: const EdgeInsets.all(28)));
    }
  }

  LatLng? _here() {
    for (final marker in widget.markers) {
      if (marker.type == 'here') {
        return marker.point;
      }
    }

    return widget.track.isNotEmpty ? widget.track.last : null;
  }

  @override
  Widget build(BuildContext context) {
    final initial = widget.center ?? (widget.track.isNotEmpty ? widget.track.first : const LatLng(60.1719, 24.9414));

    return ColoredBox(
      color: _surface,
      child: FlutterMap(
        mapController: _controller,
        options: MapOptions(
          initialCenter: initial,
          initialZoom: widget.zoom,
          backgroundColor: _surface,
          interactionOptions: const InteractionOptions(flags: InteractiveFlag.pinchZoom | InteractiveFlag.drag | InteractiveFlag.doubleTapZoom),
          onMapReady: () {
            _ready = true;

            if (!widget.follow && widget.track.length > 1) {
              _fitted = true;
              _controller.fitCamera(CameraFit.bounds(bounds: LatLngBounds.fromPoints(widget.track), padding: const EdgeInsets.all(28)));
            }
          },
        ),
        children: [
          TileLayer(
            urlTemplate: 'https://basemaps.cartocdn.com/dark_nolabels/{z}/{x}/{y}{r}.png',
            userAgentPackageName: 'com.ikonai.momentum',
            retinaMode: RetinaMode.isHighDensity(context),
          ),
          if (widget.track.length > 1)
            PolylineLayer(
              polylines: [
                // A dark casing under the line keeps it readable where the track crosses itself.
                Polyline(points: widget.track, strokeWidth: 7, color: _page.withValues(alpha: 0.6)),
                Polyline(points: widget.track, strokeWidth: 3, color: widget.lineColor),
                if (widget.emphasis.length > 1) Polyline(points: widget.emphasis, strokeWidth: 5, color: widget.emphasisColor),
              ],
            ),
          if (widget.markers.isNotEmpty)
            MarkerLayer(
              markers: [
                for (final marker in widget.markers)
                  Marker(
                    point: marker.point,
                    width: marker.type == 'here' ? 40 : 14,
                    height: marker.type == 'here' ? 40 : 14,
                    child: _MarkerDot(type: marker.type, lineColor: widget.lineColor, emphasisColor: widget.emphasisColor),
                  ),
              ],
            ),
        ],
      ),
    );
  }
}

class _MarkerDot extends StatelessWidget {
  const _MarkerDot({required this.type, required this.lineColor, required this.emphasisColor});

  final String type;
  final Color lineColor;
  final Color emphasisColor;

  @override
  Widget build(BuildContext context) {
    if (type == 'here') {
      return Center(
        child: Container(
          width: 15,
          height: 15,
          decoration: BoxDecoration(
            color: lineColor,
            shape: BoxShape.circle,
            border: Border.all(color: _page, width: 2),
            boxShadow: [BoxShadow(color: lineColor.withValues(alpha: 0.4), blurRadius: 0, spreadRadius: 3)],
          ),
        ),
      );
    }

    final fill = type == 'highlight' ? emphasisColor : type == 'end' ? lineColor : _page;

    return Container(
      decoration: BoxDecoration(color: fill, shape: BoxShape.circle, border: Border.all(color: _text, width: 2)),
    );
  }
}

List<LatLng> _parsePoints(Object? value) {
  final decoded = _decodeList(value);
  final points = <LatLng>[];

  for (final item in decoded) {
    if (item is Map && item['lat'] is num && item['lon'] is num) {
      points.add(LatLng((item['lat'] as num).toDouble(), (item['lon'] as num).toDouble()));
    }
  }

  return points;
}

List<MomentumMapMarker> _parseMarkers(Object? value) {
  final decoded = _decodeList(value);
  final markers = <MomentumMapMarker>[];

  for (final item in decoded) {
    if (item is Map && item['lat'] is num && item['lon'] is num && item['id'] is String && item['type'] is String) {
      markers.add(MomentumMapMarker(
        id: item['id'] as String,
        point: LatLng((item['lat'] as num).toDouble(), (item['lon'] as num).toDouble()),
        type: item['type'] as String,
        label: item['label'] as String?,
      ));
    }
  }

  return markers;
}

List<Object?> _decodeList(Object? value) {
  if (value is! String || value.isEmpty) {
    return const [];
  }

  try {
    final decoded = jsonDecode(value);
    return decoded is List ? decoded : const [];
  } catch (_) {
    // A prop that arrived truncated is one frame of a map with no line on it, not a crash.
    return const [];
  }
}

LatLng? _parseCenter(Object? value) {
  final decoded = _decodeList(value);
  return decoded.length >= 2 && decoded[0] is num && decoded[1] is num
      ? LatLng((decoded[0]! as num).toDouble(), (decoded[1]! as num).toDouble())
      : null;
}

double? _parseDouble(Object? value) {
  if (value is num) {
    return value.toDouble();
  }

  return value is String ? double.tryParse(value) : null;
}

Color? _parseColor(Object? value) {
  if (value is! String || !value.startsWith('#') || value.length != 7) {
    return null;
  }

  final parsed = int.tryParse(value.substring(1), radix: 16);
  return parsed == null ? null : Color(0xFF000000 | parsed);
}

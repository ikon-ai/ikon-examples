// The depth-parallax fragment shader. iChannel0 is the generated image, iChannel1 the depth map.
// The mouse offset from center drives a per-pixel displacement scaled by depth, producing the
// "diorama" look. uAlgorithm selects between four techniques of increasing quality/cost.
public static class DepthParallaxShader
{
    public const string Source = """
        // iChannel0 = original image, iChannel1 = depth map (lighter = nearer).
        uniform float uParallaxStrength;   // overall displacement scale
        uniform int   uAlgorithm;          // 0 direct, 1 iterative, 2 steep, 3 occlusion
        uniform int   uSteps;              // ray-march steps for algorithms 2/3
        uniform int   uShowDepth;          // 1 = display the raw depth map for inspection

        float sampleDepth(vec2 uv) { return texture(iChannel1, uv).r; }

        // Aspect-correct "cover" fit so a 16:9 canvas never stretches the image, regardless of the
        // model's actual output ratio. Maps canvas uv -> image uv using iChannelResolution[0].
        vec2 coverUv(vec2 uv) {
            vec2 img = iChannelResolution[0].xy;
            if (img.x < 1.0 || img.y < 1.0) { return uv; }
            float canvasA = iResolution.x / iResolution.y;
            float imgA = img.x / img.y;
            vec2 scale = canvasA > imgA ? vec2(1.0, imgA / canvasA) : vec2(canvasA / imgA, 1.0);
            return (uv - 0.5) * scale + 0.5;
        }

        // 0 - single-sample UV shift. Cheap; smears at depth discontinuities.
        vec2 direct(vec2 uv, vec2 v) {
            float d = sampleDepth(uv);
            return uv - v * (d - 0.5);
        }

        // 1 - fixed-point refinement: re-evaluate depth at the displaced UV a few times.
        vec2 iterative(vec2 uv, vec2 v) {
            vec2 p = uv;
            for (int i = 0; i < 8; i++) {
                float d = sampleDepth(p);
                p = uv - v * (d - 0.5);
            }
            return p;
        }

        // 2/3 - steep ray-march along v; optional POM interpolation of the last two layers.
        vec2 march(vec2 uv, vec2 v, bool occlusion) {
            int steps = max(uSteps, 2);
            float layerStep = 1.0 / float(steps);
            float curLayer = 0.0;
            vec2 delta = v / float(steps);
            vec2 p = uv;
            float d = sampleDepth(p);

            for (int i = 0; i < 256; i++) {
                if (i >= steps || curLayer >= d) { break; }
                p -= delta;
                curLayer += layerStep;
                d = sampleDepth(p);
            }

            if (!occlusion) { return p; }

            vec2 prev = p + delta;
            float after = d - curLayer;
            float before = sampleDepth(prev) - (curLayer - layerStep);
            float w = after / (after - before);
            return mix(p, prev, clamp(w, 0.0, 1.0));
        }

        void mainImage(out vec4 fragColor, in vec2 fragCoord) {
            vec2 uv = coverUv(fragCoord / iResolution.xy);
            // Slight zoom-in: leaves a margin so the parallax offset never samples past the texture
            // edge (which would otherwise wrap around and show the opposite side).
            uv = (uv - 0.5) * 0.92 + 0.5;
            vec2 m = (iMouse.xy / iResolution.xy) * 2.0 - 1.0;
            vec2 v = m * uParallaxStrength;

            if (uShowDepth == 1) {
                fragColor = vec4(vec3(sampleDepth(uv)), 1.0);
                return;
            }

            vec2 p;
            if (uAlgorithm == 0) { p = direct(uv, v); }
            else if (uAlgorithm == 1) { p = iterative(uv, v); }
            else if (uAlgorithm == 2) { p = march(uv, v, false); }
            else { p = march(uv, v, true); }

            fragColor = texture(iChannel0, p);
        }
        """;
}

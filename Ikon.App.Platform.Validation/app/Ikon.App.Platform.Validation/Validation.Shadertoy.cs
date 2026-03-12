public partial class Validation
{
    private readonly Reactive<string> _shaderSource = new(DefaultShaderSource);
    private readonly Reactive<double> _shaderFps = new(60);
    private readonly Reactive<double> _shaderTimeSpeed = new(0.1);
    private readonly Reactive<double> _shaderBokehDensity = new(450.0);
    private readonly Reactive<double> _shaderBokehMinSize = new(30.0);
    private readonly Reactive<double> _shaderBokehMaxSize = new(70.0);
    private readonly Reactive<double> _shaderGlowIntensity = new(0.3);
    private readonly Reactive<double> _shaderColorIntensity = new(3.0);

    private const string DefaultShaderSource = @"const float MATH_PI = float( 3.14159265359 );

uniform float uTimeSpeed;
uniform float uBokehDensity;
uniform float uBokehMinSize;
uniform float uBokehMaxSize;
uniform float uGlowIntensity;
uniform float uColorIntensity;

void Rotate( inout vec2 p, float a )
{
    p = cos( a ) * p + sin( a ) * vec2( p.y, -p.x );
}

float Circle( vec2 p, float r )
{
    return ( length( p / r ) - 1.0 ) * r;
}

float Rand( vec2 c )
{
    return fract( sin( dot( c.xy, vec2( 12.9898, 78.233 ) ) ) * 43758.5453 );
}

float saturate( float x )
{
    return clamp( x, 0.0, 1.0 );
}

void BokehLayer( inout vec3 color, vec2 p, vec3 c )
{
    float wrap = uBokehDensity;
    if ( mod( floor( p.y / wrap + 0.5 ), 2.0 ) == 0.0 )
    {
        p.x += wrap * 0.5;
    }

    vec2 p2 = mod( p + 0.5 * wrap, wrap ) - 0.5 * wrap;
    vec2 cell = floor( p / wrap + 0.5 );
    float cellR = Rand( cell );

    c *= fract( cellR * 3.33 + 3.33 );
    float radius = mix( uBokehMinSize, uBokehMaxSize, fract( cellR * 7.77 + 7.77 ) );
    p2.x *= mix( 0.9, 1.1, fract( cellR * 11.13 + 11.13 ) );
    p2.y *= mix( 0.9, 1.1, fract( cellR * 17.17 + 17.17 ) );

    float sdf = Circle( p2, radius );
    float circle = 1.0 - smoothstep( 0.0, 1.0, sdf * 0.04 );
    float glow = exp( -sdf * 0.025 ) * uGlowIntensity * ( 1.0 - circle );
    color += c * ( circle + glow );
}

void mainImage( out vec4 fragColor, in vec2 fragCoord )
{
    vec2 uv = fragCoord.xy / iResolution.xy;
    vec2 p = ( 2.0 * fragCoord - iResolution.xy ) / iResolution.x * 1000.0;

    vec3 color = mix( vec3( 0.3, 0.1, 0.3 ), vec3( 0.1, 0.4, 0.5 ), dot( uv, vec2( 0.2, 0.7 ) ) );

    float time = iTime * uTimeSpeed - 15.0;

    Rotate( p, 0.2 + time * 0.03 );
    BokehLayer( color, p + vec2( -50.0 * time +  0.0, 0.0  ), uColorIntensity * vec3( 0.4, 0.1, 0.2 ) );
    Rotate( p, 0.3 - time * 0.05 );
    BokehLayer( color, p + vec2( -70.0 * time + 33.0, -33.0 ), uColorIntensity * 1.17 * vec3( 0.6, 0.4, 0.2 ) );
    Rotate( p, 0.5 + time * 0.07 );
    BokehLayer( color, p + vec2( -60.0 * time + 55.0, 55.0 ), uColorIntensity * vec3( 0.4, 0.3, 0.2 ) );
    Rotate( p, 0.9 - time * 0.03 );
    BokehLayer( color, p + vec2( -25.0 * time + 77.0, 77.0 ), uColorIntensity * vec3( 0.4, 0.2, 0.1 ) );
    Rotate( p, 0.0 + time * 0.05 );
    BokehLayer( color, p + vec2( -15.0 * time + 99.0, 99.0 ), uColorIntensity * vec3( 0.2, 0.0, 0.4 ) );

    fragColor = vec4( color, 1.0 );
}";

    private void RenderShadertoySection(UIView view)
    {
        view.Column([Layout.Column.Lg], content: view =>
        {
            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H2, "mb-4"], "Shadertoy Preview");
                view.Text([Text.Caption, "mb-4"], "Shadertoy-compatible fragment shader canvas with built-in uniforms (iTime, iResolution, iMouse, etc.)");

                view.Box(["w-full h-80 rounded-lg overflow-hidden bg-black"], content: view =>
                {
                    view.ShadertoyCanvas(
                        ["w-full h-full"],
                        shaderSource: _shaderSource.Value,
                        fps: (int)_shaderFps.Value,
                        uniforms: new Dictionary<string, ShaderUniform>
                        {
                            ["uTimeSpeed"] = ShaderUniform.Float((float)_shaderTimeSpeed.Value),
                            ["uBokehDensity"] = ShaderUniform.Float((float)_shaderBokehDensity.Value),
                            ["uBokehMinSize"] = ShaderUniform.Float((float)_shaderBokehMinSize.Value),
                            ["uBokehMaxSize"] = ShaderUniform.Float((float)_shaderBokehMaxSize.Value),
                            ["uGlowIntensity"] = ShaderUniform.Float((float)_shaderGlowIntensity.Value),
                            ["uColorIntensity"] = ShaderUniform.Float((float)_shaderColorIntensity.Value)
                        },
                        enableMouse: true);
                });
            });

            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H2, "mb-4"], "Render Settings");

                view.Column([Layout.Column.Md], content: view =>
                {
                    view.Column(content: view =>
                    {
                        view.Text([Text.Label, "mb-2"], $"FPS: {(int)_shaderFps.Value}");
                        view.Slider(
                            [Slider.Default],
                            value: [_shaderFps.Value],
                            min: 1,
                            max: 120,
                            step: 1,
                            onValueChange: values =>
                            {
                                if (values.Count > 0)
                                {
                                    _shaderFps.Value = values[0];
                                }

                                return Task.CompletedTask;
                            },
                            content: view =>
                            {
                                view.SliderTrack([Slider.Track], content: view =>
                                {
                                    view.SliderRange([Slider.Range]);
                                });
                                view.SliderThumb([Slider.Thumb]);
                            });
                    });
                });
            });

            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H2, "mb-4"], "Uniforms");
                view.Text([Text.Caption, "mb-4"], "Adjust shader parameters in real-time");

                view.Column([Layout.Column.Md], content: view =>
                {
                    view.Column(content: view =>
                    {
                        view.Text([Text.Label, "mb-2"], $"Time Speed: {_shaderTimeSpeed.Value:F2}");
                        view.Slider(
                            [Slider.Default],
                            value: [_shaderTimeSpeed.Value],
                            min: 0.01,
                            max: 0.5,
                            step: 0.01,
                            onValueChange: values =>
                            {
                                if (values.Count > 0)
                                {
                                    _shaderTimeSpeed.Value = values[0];
                                }

                                return Task.CompletedTask;
                            },
                            content: view =>
                            {
                                view.SliderTrack([Slider.Track], content: view =>
                                {
                                    view.SliderRange([Slider.Range]);
                                });
                                view.SliderThumb([Slider.Thumb]);
                            });
                    });

                    view.Column(content: view =>
                    {
                        view.Text([Text.Label, "mb-2"], $"Bokeh Density: {_shaderBokehDensity.Value:F0}");
                        view.Slider(
                            [Slider.Default],
                            value: [_shaderBokehDensity.Value],
                            min: 100,
                            max: 800,
                            step: 10,
                            onValueChange: values =>
                            {
                                if (values.Count > 0)
                                {
                                    _shaderBokehDensity.Value = values[0];
                                }

                                return Task.CompletedTask;
                            },
                            content: view =>
                            {
                                view.SliderTrack([Slider.Track], content: view =>
                                {
                                    view.SliderRange([Slider.Range]);
                                });
                                view.SliderThumb([Slider.Thumb]);
                            });
                    });

                    view.Column(content: view =>
                    {
                        view.Text([Text.Label, "mb-2"], $"Bokeh Min Size: {_shaderBokehMinSize.Value:F0}");
                        view.Slider(
                            [Slider.Default],
                            value: [_shaderBokehMinSize.Value],
                            min: 5,
                            max: 100,
                            step: 5,
                            onValueChange: values =>
                            {
                                if (values.Count > 0)
                                {
                                    _shaderBokehMinSize.Value = values[0];
                                }

                                return Task.CompletedTask;
                            },
                            content: view =>
                            {
                                view.SliderTrack([Slider.Track], content: view =>
                                {
                                    view.SliderRange([Slider.Range]);
                                });
                                view.SliderThumb([Slider.Thumb]);
                            });
                    });

                    view.Column(content: view =>
                    {
                        view.Text([Text.Label, "mb-2"], $"Bokeh Max Size: {_shaderBokehMaxSize.Value:F0}");
                        view.Slider(
                            [Slider.Default],
                            value: [_shaderBokehMaxSize.Value],
                            min: 30,
                            max: 200,
                            step: 5,
                            onValueChange: values =>
                            {
                                if (values.Count > 0)
                                {
                                    _shaderBokehMaxSize.Value = values[0];
                                }

                                return Task.CompletedTask;
                            },
                            content: view =>
                            {
                                view.SliderTrack([Slider.Track], content: view =>
                                {
                                    view.SliderRange([Slider.Range]);
                                });
                                view.SliderThumb([Slider.Thumb]);
                            });
                    });

                    view.Column(content: view =>
                    {
                        view.Text([Text.Label, "mb-2"], $"Glow Intensity: {_shaderGlowIntensity.Value:F2}");
                        view.Slider(
                            [Slider.Default],
                            value: [_shaderGlowIntensity.Value],
                            min: 0.0,
                            max: 1.0,
                            step: 0.05,
                            onValueChange: values =>
                            {
                                if (values.Count > 0)
                                {
                                    _shaderGlowIntensity.Value = values[0];
                                }

                                return Task.CompletedTask;
                            },
                            content: view =>
                            {
                                view.SliderTrack([Slider.Track], content: view =>
                                {
                                    view.SliderRange([Slider.Range]);
                                });
                                view.SliderThumb([Slider.Thumb]);
                            });
                    });

                    view.Column(content: view =>
                    {
                        view.Text([Text.Label, "mb-2"], $"Color Intensity: {_shaderColorIntensity.Value:F1}");
                        view.Slider(
                            [Slider.Default],
                            value: [_shaderColorIntensity.Value],
                            min: 0.5,
                            max: 6.0,
                            step: 0.1,
                            onValueChange: values =>
                            {
                                if (values.Count > 0)
                                {
                                    _shaderColorIntensity.Value = values[0];
                                }

                                return Task.CompletedTask;
                            },
                            content: view =>
                            {
                                view.SliderTrack([Slider.Track], content: view =>
                                {
                                    view.SliderRange([Slider.Range]);
                                });
                                view.SliderThumb([Slider.Thumb]);
                            });
                    });
                });
            });

            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H2, "mb-4"], "Shadertoy Source");
                view.Text([Text.Caption, "mb-4"], "Edit the GLSL shader code (must define mainImage function)");

                view.TextArea(
                    [Input.Default, "font-mono text-sm h-96"],
                    value: _shaderSource.Value,
                    onValueChange: value =>
                    {
                        _shaderSource.Value = value;
                        return Task.CompletedTask;
                    });
            });
        });
    }
}

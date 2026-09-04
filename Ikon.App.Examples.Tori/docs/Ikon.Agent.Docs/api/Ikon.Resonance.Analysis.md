namespace Ikon.Resonance.Analysis
  readonly struct AudioAnalysisResult
    ctor(uint setId, IReadOnlyList<float> values)
    uint SetId { get; }
    // The analysis values for this shape set. Analyzers may reuse the backing storage between frames — copy the values if you need them beyond the current frame.
    IReadOnlyList<float> Values { get; }
  readonly struct AudioShapeSetDeclaration
    ctor(uint setId, string name, IReadOnlyList<string> shapeNames)
    string Name { get; }
    uint SetId { get; }
    IReadOnlyList<string> ShapeNames { get; }
  interface IAudioAnalyzer
    AudioShapeSetDeclaration ShapeSetDeclaration { get; }
    // sampleRate: Mixer output sample rate.
    // channelCount: Mixer output channel count.
    IAudioAnalyzerInstance Create(int sampleRate, int channelCount)
  interface IAudioAnalyzerInstance
    // buffer: The audio buffer to analyze (interleaved samples).
    AudioAnalysisResult Analyze(ReadOnlySpan<float> buffer)
    void Reset()
  // Produces MouthOpenY (0-1) from RMS and MouthForm (-1 to +1) from spectral analysis.
  sealed class VisemeAnalyzer : IAudioAnalyzer
    ctor()
    AudioShapeSetDeclaration ShapeSetDeclaration { get; }
    IAudioAnalyzerInstance Create(int sampleRate, int channelCount)

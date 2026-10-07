namespace Ikon.App.Patterns;

// The gallery runs every example that calls an AI service on that service's mock, so rendering
// every demo and pressing every button costs nothing and needs no credential. There is no switch
// back to the real providers: an example's worth here is that it runs and draws, and a model's
// answer is checked by Ikon.AI.Test's per-model tests, not by a gallery that must never bill.
internal static class GalleryMocks
{
    // On the session's own selector, so every handler of the session — a button pressed long after
    // the render — constructs its services as mocks too
    public static void Apply()
    {
        ImplementationSelector.Instance.UseMocks = true;
    }

    public static void Require()
    {
        if (!ImplementationSelector.Instance.UseMocks)
        {
            throw new InvalidOperationException("The Patterns gallery reached an AI service with mocks off: PatternsApp.Main calls GalleryMocks.Apply, and a test renders under ImplementationSelector.UseMocksInScope");
        }
    }
}

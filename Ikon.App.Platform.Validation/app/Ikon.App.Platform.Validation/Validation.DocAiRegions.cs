// Generated holder for ikon-ai-library-overview.md — one class per guide SECTION, because a section is one
// reader's file: two of them may each declare a `UI` or a `Main` without either being wrong.
// Each class carries only the placeholder names its own fences use and do not declare.

file sealed class DocAiRObjectGeneration(IApp<SessionIdentity, ClientParams> app)
{

    public async Task AirObjectGeneration(UIView view)
    {
        #region docsnippet:air-object-generation
        var result = await Emerge.Run<PersonDetails>(LLMModel.Gpt5Mini, pass =>
        {
            pass.Command = "Return invented personal details about a person.";
            pass.Regions = [ModelRegion.Eu, ModelRegion.Global];
        });
        #endregion
    }
}

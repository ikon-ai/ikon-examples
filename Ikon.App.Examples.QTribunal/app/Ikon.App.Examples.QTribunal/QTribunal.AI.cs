public partial class QTribunal
{
    private const string QPersonality =
        "You are Q, the omnipotent being from Star Trek. You have put humanity on trial. "
        + "Your personality: theatrical, sardonic, intellectually playful, grandiose. "
        + "You speak with dramatic flair, mixing cosmic condescension with genuine curiosity about human nature. "
        + "You teleport the accused to impossible locations that physically embody abstract crimes against universal law. "
        + "Every element in the scene is a metaphor for the hidden principle the player must discover.";

    private async Task<Accusation> GenerateAccusationAsync(int roundNumber, List<RoundResult> previousRounds, CancellationToken ct)
    {
        var previousContext = "";

        if (previousRounds.Count > 0)
        {
            previousContext = "\n\nPrevious rounds (avoid repeating themes):\n"
                + string.Join("\n", previousRounds.Select(r => $"- Crime: {r.Crime}, Law: {r.HiddenLaw}"));
        }

        var systemPrompt = QPersonality
            + "\n\nGenerate a trial accusation for round " + roundNumber + " of 3."
            + "\n\nRules:"
            + "\n- The Crime should be an abstract, metaphysical offense (e.g. 'The Hoarding of Silence', 'The Weaponization of Nostalgia')"
            + "\n- The HiddenLaw is the universal principle violated - something philosophical and surprising"
            + "\n- The Scene must be a surreal location where every element is a physical metaphor for the hidden law"
            + "\n- Include exactly 3 witnesses who are part of the scene's metaphor"
            + "\n- The ImagePrompt should describe a cinematic, surreal scene suitable for AI image generation (no text, no people's faces)"
            + "\n- ExaminableElements should list 4-5 key objects/features the player can examine"
            + "\n- HiddenConnection explains how the scene elements connect to the hidden law (this is never shown to the player)"
            + "\n- QSpeech is Q's dramatic opening monologue accusing humanity of this crime (2-3 sentences, theatrical)"
            + previousContext;

        var (result, _) = await Emerge.Run<Accusation>(
            LLMModel.Claude45Sonnet,
            new KernelContext(),
            pass =>
            {
                pass.SystemPrompt = systemPrompt;
                pass.Command = "Generate accusation for round " + roundNumber;
                pass.Temperature = 0.9f;
                pass.MaxOutputTokens = 2000;
            },
            ct
        ).FinalAsync(ct);

        return result;
    }

    private async Task<InvestigationResponse> ProcessInvestigationAsync(
        Accusation currentAccusation,
        ParsedCommand command,
        List<TranscriptEntry> recentTranscript,
        float currentProximity,
        CancellationToken ct)
    {
        var transcriptText = string.Join("\n", recentTranscript.TakeLast(10)
            .Select(e => $"[{e.Role}] {e.Speaker}: {e.Text}"));

        var witnessInfo = string.Join(", ", currentAccusation.Scene.Witnesses.Select(w => w.Name + " (" + w.Description + ")"));
        var elements = string.Join(", ", currentAccusation.Scene.ExaminableElements);

        var systemPrompt = QPersonality
            + "\n\nYou are running an investigation phase of the trial. The player is exploring a scene."
            + "\n\nScene: " + currentAccusation.Scene.LocationName + " - " + currentAccusation.Scene.Description
            + "\nCrime: " + currentAccusation.Crime
            + "\nHidden Law (NEVER reveal directly): " + currentAccusation.HiddenLaw
            + "\nHidden Connection: " + currentAccusation.Scene.HiddenConnection
            + "\nWitnesses present: " + witnessInfo
            + "\nExaminable elements: " + elements
            + "\nCurrent proximity to truth: " + currentProximity
            + "\n\nRecent transcript:\n" + transcriptText
            + "\n\nRules:"
            + "\n- For 'examine': describe what the player observes, weaving in subtle clues about the hidden law"
            + "\n- For 'ask': a witness responds in character, hinting at the truth obliquely"
            + "\n- For 'reflect': the narrator provides philosophical musing that connects scene elements"
            + "\n- For 'look': describe the broader scene, noting new details that weren't obvious before"
            + "\n- Narration should be atmospheric and evocative (2-3 sentences)"
            + "\n- QComment is Q's sardonic reaction (1 sentence, can hint at proximity to truth)"
            + "\n- ProximityToTruth: score 0.0-1.0 based on how close the player's line of inquiry is to the hidden law"
            + "\n- ImageEvolutionHint: brief visual descriptor for how the scene should evolve (e.g. 'growing brighter', 'shadows receding')";

        var (result, _) = await Emerge.Run<InvestigationResponse>(
            LLMModel.Claude45Sonnet,
            new KernelContext(),
            pass =>
            {
                pass.SystemPrompt = systemPrompt;
                pass.Command = "[" + command.Type + "] " + command.Argument;
                pass.Temperature = 0.7f;
                pass.MaxOutputTokens = 1000;
            },
            ct
        ).FinalAsync(ct);

        return result;
    }

    private async Task<JudgmentResponse> JudgeProposalAsync(
        Accusation currentAccusation,
        string playerTheory,
        CancellationToken ct)
    {
        var systemPrompt = QPersonality
            + "\n\nJudge the player's theory about the hidden universal law."
            + "\n\nCrime: " + currentAccusation.Crime
            + "\nHidden Law: " + currentAccusation.HiddenLaw
            + "\nHidden Connection: " + currentAccusation.Scene.HiddenConnection
            + "\n\nRules:"
            + "\n- Compare the player's theory to the actual hidden law"
            + "\n- Accuracy: 0.0-1.0 score. Give partial credit for related insights"
            + "\n- Correct: true if accuracy >= 0.7"
            + "\n- QSpeech: Q's theatrical verdict (2-3 sentences). If correct, grudging admiration. If wrong, dramatic disappointment"
            + "\n- Explanation: reveal the hidden law and explain how the scene embodied it (2-3 sentences)";

        var (result, _) = await Emerge.Run<JudgmentResponse>(
            LLMModel.Claude45Sonnet,
            new KernelContext(),
            pass =>
            {
                pass.SystemPrompt = systemPrompt;
                pass.Command = "Player's theory: " + playerTheory;
                pass.Temperature = 0.7f;
                pass.MaxOutputTokens = 800;
            },
            ct
        ).FinalAsync(ct);

        return result;
    }

    private async Task<(byte[] Data, string MimeType)?> GenerateSceneImageAsync(
        string basePrompt,
        float proximity,
        CancellationToken ct)
    {
        try
        {
            var atmosphereSuffix = proximity switch
            {
                < 0.3f => ", dark and obscured atmosphere, thick fog, deep shadows, mysterious and foreboding, barely visible details",
                < 0.6f => ", partially illuminated, some fog lifting, amber light breaking through, details becoming clearer",
                _ => ", radiant and illuminated, crystal clear details, golden light, truth revealed in every element"
            };

            using var imageGenerator = new ImageGenerator(ImageGeneratorModel.Flux1KontextPro);
            var results = await imageGenerator.GenerateImageAsync(new ImageGeneratorConfig
            {
                Prompt = basePrompt + atmosphereSuffix + ", cinematic wide angle, surreal digital art, no text, no faces",
                Width = 1024,
                Height = 576
            }, ct);

            if (results.Count > 0)
            {
                return (results[0].Data, results[0].MimeType);
            }
        }
        catch (Exception ex)
        {
            Log.Instance.Warning($"Image generation failed: {ex.Message}");
        }

        return null;
    }
}

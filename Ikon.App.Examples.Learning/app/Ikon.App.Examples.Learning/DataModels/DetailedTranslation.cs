using Ikon.Common;

namespace Ikon.App.Examples.Learning.DataModels;

public class WordEntry
{
    [Description("The word translation information.")]
    public WordTranslation WordTranslation { get; set; } = new();

    [Description("Linguistic information about the word.")]
    public WordInformation WordInformation { get; set; } = new();

    [Description("Example sentences using this word.")]
    public List<Example> Examples { get; set; } = [];

    [Description("Additional explanation about this word in context.")]
    public string Explanation { get; set; } = string.Empty;
}

public class WordTranslation
{
    [Description("The original word in the target language.")]
    public string OriginalWord { get; set; } = string.Empty;

    [Description("Transliteration of the word (for non-Latin alphabets).")]
    public string Transliteration { get; set; } = string.Empty;

    [Description("Translation of the word into the user's preferred language.")]
    public string Translation { get; set; } = string.Empty;
}

public class WordInformation
{
    [Description("The basic/dictionary form of the word.")]
    public string BasicForm { get; set; } = string.Empty;

    [Description("Part of speech (noun, verb, adjective, etc.).")]
    public string PartOfSpeech { get; set; } = string.Empty;

    [Description("The meaning of the word.")]
    public string Meaning { get; set; } = string.Empty;

    [Description("How the word is used in the sentence (subject, object, modifier, etc.).")]
    public string Use { get; set; } = string.Empty;

    [Description("Pronunciation guide or notes.")]
    public string Pronunciation { get; set; } = string.Empty;
}

public class Example
{
    [Description("Example sentence in the target language.")]
    public string Sentence { get; set; } = string.Empty;

    [Description("Translation of the example sentence.")]
    public string Translation { get; set; } = string.Empty;
}

public class Tokenized
{
    [Description("Array of words/tokens from the sentence.")]
    public string[] Words { get; set; } = [];
}

public class DetailedTranslationResult
{
    [Description("The word entry with detailed linguistic information.")]
    public WordEntry WordEntry { get; set; } = new();
}

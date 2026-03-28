using Ikon.Common;

namespace Ikon.App.Examples.Learning.DataModels;

public class News
{
    [Description("Current UTC formatted time string.")]
    public string LastModified { get; set; } = string.Empty;

    [Description("A list of the extracted articles.")]
    public List<Article> Articles { get; set; } = [];
}

public class Article
{
    [Description("A Version 4 UUID for the article.")]
    public string Id { get; set; } = string.Empty;

    [Description("The title of the article.")]
    public string Title { get; set; } = string.Empty;

    [Description("A single-sentence summary of the article.")]
    public string Summary { get; set; } = string.Empty;

    [Description("Details about the article's poster/cover image.")]
    public Poster Poster { get; set; } = new();

    [Description("The text content of the article.")]
    public string Content { get; set; } = string.Empty;
}

public class Poster
{
    [Description("The poster image's url")]
    public string Url { get; set; } = string.Empty;

    [Description("The poster image's photographer (credits).")]
    public string Credits { get; set; } = string.Empty;

    [Description("Caption text for the poster image.")]
    public string Caption { get; set; } = string.Empty;
}

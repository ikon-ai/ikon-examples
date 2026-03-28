namespace Ikon.App.Examples.Learning.States;

public class NewsState(LearningApp outer) : ILearningState
{
    private readonly Reactive<News?> _news = new(null);
    private readonly Reactive<bool> _isLoading = new(true);
    private readonly Reactive<bool> _isGeneratingExercise = new(false);
    private readonly Dictionary<string, string> _articleImages = new();
    private readonly Reactive<int> _imagesVersion = new(0);
    private bool _imagesLoading = false;

    public async Task EnterAsync()
    {
        _isLoading.Value = true;
        _news.Value = null;

        _ = Task.Run(async () =>
        {
            try
            {
                // Try to load cached news first
                var cachedNews = await outer.GetCachedNewsAsync();

                if (cachedNews != null && cachedNews.Articles.Count > 0)
                {
                    _news.Value = cachedNews;
                    _isLoading.Value = false;
                    LoadArticleImages();
                    return;
                }

                // Fetch fresh news
                var freshNews = await FetchNews();
                _news.Value = freshNews;

                // Cache the news for today
                await outer.SaveNewsToCacheAsync(freshNews);

                LoadArticleImages();
            }
            catch (Exception ex)
            {
                Log.Instance.Error($"Error fetching news: {ex.Message}");
                _news.Value = new News { Articles = [] };
            }
            finally
            {
                _isLoading.Value = false;
            }
        });
    }

    private void LoadArticleImages()
    {
        if (_imagesLoading || _news.Value == null)
        {
            return;
        }

        _imagesLoading = true;

        _ = Task.Run(async () =>
        {
            foreach (var article in _news.Value.Articles)
            {
                if (_articleImages.ContainsKey(article.Title))
                {
                    continue;
                }

                try
                {
                    var articleId = HashId(article.Title);
                    var description = $"News illustration for article: {article.Title}. {article.Summary ?? article.Content}";

                    var imageUrl = await outer.GetOrCreateImageAsync(
                        "news-articles",
                        articleId,
                        description,
                        width: 400,
                        height: 200
                    );

                    if (!string.IsNullOrEmpty(imageUrl))
                    {
                        _articleImages[article.Title] = imageUrl;
                        _imagesVersion.Value++;
                    }
                }
                catch (Exception ex)
                {
                    Log.Instance.Warning($"Failed to load image for article {article.Title}: {ex.Message}");
                }
            }
        });
    }

    private static string HashId(string input)
    {
        using var sha = System.Security.Cryptography.SHA256.Create();
        var hash = sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(hash)[..16].ToLowerInvariant();
    }

    private async Task<News> FetchNews()
    {
        var scraper = new WebScraper(WebScraperModel.Jina);
        var scrapingConfig = new SinglePageScrapeConfig
        {
            Url = "https://yle.fi/selkouutiset"
        };
        var output = await scraper.ScrapeSinglePageAsync(scrapingConfig);
        var articlesMarkdown = output.Content;

        var news = await ExtractArticlesShader.GenerateAsync(
            LLMModel.Gpt41Mini.ToString(),
            nameof(ReasoningEffort.None),
            articlesMarkdown
        );

        return news;
    }

    private async Task<Exercise> GenerateNewsExercise(Article article)
    {
        var articleId = HashId(article.Title);

        // Try to load cached exercise first
        var cachedExercise = await outer.GetNewsExerciseAsync(articleId);
        if (cachedExercise != null)
        {
            Log.Instance.Info($"[NewsState] Using cached exercise for article: {article.Title}");
            return cachedExercise;
        }

        Log.Instance.Info($"[NewsState] Generating new exercise for article: {article.Title}");

        var exercise = await GenerateExerciseShader.GenerateAsync(
            LLMModel.Gpt41.ToString(),
            nameof(ReasoningEffort.None),
            outer.UserState!,
            null,
            null,
            null,
            null,
            null,
            $"News Article: {article.Title}\n\n{article.Content}",
            ExerciseType.Conversational,
            ExerciseSource.News,
            ExerciseCategory.Assignment
        );

        // Cache the exercise
        await outer.SaveNewsExerciseAsync(articleId, exercise);

        return exercise;
    }

    public Task ExitAsync()
    {
        return Task.CompletedTask;
    }

    public Task HandleUserMessageAsync(string userId, string text)
    {
        return Task.CompletedTask;
    }

    public Task HandleAIMessageAsync(string message)
    {
        return Task.CompletedTask;
    }

    public void Render(UIView contentView)
    {
        var translations = outer.Translations;
        var news = _news.Value;
        var _ = _imagesVersion.Value; // Read to trigger re-render when images load

        contentView.Column([Container.Lg, "gap-6"], content: view =>
        {
            // Header section
            view.Box([Card.Default, "p-6 bg-white/90 backdrop-blur-xl"], content: headerView =>
            {
                headerView.Column(["gap-2"], content: col =>
                {
                    col.Text([Text.H2], translations.TodaysNews);
                    col.Text([Text.Body, "text-muted-foreground"], translations.HeresTodaysNews);
                });
            });

            if (_isLoading.Value)
            {
                view.Box([Card.Default, "p-8 bg-white/90 backdrop-blur-xl text-center"], content: loadingView =>
                {
                    loadingView.Column(["items-center gap-4"], content: col =>
                    {
                        col.Icon([Icon.Default, "w-12 h-12 text-primary animate-spin"], name: "loader");
                        col.Text([Text.Body, "text-muted-foreground"], translations.Fetching);
                    });
                });
            }
            else if (_isGeneratingExercise.Value)
            {
                view.Box([Card.Default, "p-8 bg-white/90 backdrop-blur-xl text-center"], content: loadingView =>
                {
                    loadingView.Column(["items-center gap-4"], content: col =>
                    {
                        col.Icon([Icon.Default, "w-12 h-12 text-primary animate-spin"], name: "loader");
                        col.Text([Text.Body, "text-muted-foreground"], translations.OrganizingContent);
                    });
                });
            }
            else if (news == null || news.Articles.Count == 0)
            {
                view.Box([Card.Default, "p-8 bg-white/90 backdrop-blur-xl text-center"], content: emptyView =>
                {
                    emptyView.Column(["items-center gap-4"], content: col =>
                    {
                        col.Icon([Icon.Default, "w-16 h-16 text-gray-300"], name: "newspaper");
                        col.Text([Text.Body, "text-muted-foreground"], translations.NoArticlesFound);
                    });
                });
            }
            else
            {
                view.Column(["gap-4"], content: articlesView =>
                {
                    foreach (var article in news.Articles)
                    {
                        var currentArticle = article;
                        var hasImage = _articleImages.TryGetValue(article.Title, out var imageUrl);

                        articlesView.Button([Card.Interactive, "overflow-hidden text-left w-full bg-white/90 backdrop-blur-xl rounded-xl shadow-lg border border-gray-200 hover:shadow-xl transition-shadow"],
                            onClick: async () =>
                            {
                                _isGeneratingExercise.Value = true;

                                try
                                {
                                    outer.CurrentArticle = currentArticle;
                                    var exercise = await GenerateNewsExercise(currentArticle);
                                    outer.CurrentExercise = exercise;
                                    await outer.States.StateMachine.FireAsync(Trigger.StartExercise);
                                }
                                catch (Exception ex)
                                {
                                    Log.Instance.Error($"Error generating exercise: {ex.Message}");
                                }
                                finally
                                {
                                    _isGeneratingExercise.Value = false;
                                }
                            },
                            content: cardView =>
                            {
                                cardView.Column([], content: col =>
                                {
                                    // Image section
                                    if (hasImage && !string.IsNullOrEmpty(imageUrl))
                                    {
                                        col.Image(["w-full h-32 object-cover"], src: imageUrl, alt: article.Title);
                                    }
                                    else
                                    {
                                        // Placeholder with gradient
                                        col.Box(["w-full h-32 bg-gradient-to-br from-blue-100/50 to-blue-50/30 flex items-center justify-center"], content: placeholder =>
                                        {
                                            placeholder.Icon([Icon.Default, "w-12 h-12 text-blue-300"], name: "newspaper");
                                        });
                                    }

                                    // Content section
                                    col.Row(["p-4 gap-4 items-start"], content: row =>
                                    {
                                        // Content
                                        row.Column(["gap-2 flex-1 min-w-0"], content: textCol =>
                                        {
                                            textCol.Text([LearningApp.Styles.H4], article.Title);
                                            textCol.Text([Text.Caption, "text-muted-foreground line-clamp-2"],
                                                !string.IsNullOrEmpty(article.Summary) ? article.Summary : article.Content);
                                        });

                                        // Chevron
                                        row.Icon([Icon.Default, "text-gray-400 shrink-0"], name: "chevron-right");
                                    });
                                });
                            });
                    }
                });
            }
        });
    }
}

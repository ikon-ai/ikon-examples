public partial class Validation
{
    private void RenderCardsSection(UIView view)
    {
        view.Column([Layout.Column.Lg], content: view =>
        {
            // Card variants
            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H2, "mb-4"], "Card Variants");
                view.Column([Layout.Column.Md], content: view =>
                {
                    view.Box([Card.Default, "p-4"], content: view =>
                    {
                        view.Text([Text.BodyStrong], "Card.Default");
                        view.Text([Text.Caption], "Standard card with default shadow");
                    });

                    view.Box([Card.Sm, "p-4"], content: view =>
                    {
                        view.Text([Text.BodyStrong], "Card.Sm");
                        view.Text([Text.Caption], "Small shadow card");
                    });

                    view.Box([Card.Md, "p-4"], content: view =>
                    {
                        view.Text([Text.BodyStrong], "Card.Md");
                        view.Text([Text.Caption], "Medium shadow card");
                    });

                    view.Box([Card.Lg, "p-4"], content: view =>
                    {
                        view.Text([Text.BodyStrong], "Card.Lg");
                        view.Text([Text.Caption], "Large shadow card");
                    });

                    view.Box([Card.Elevated, "p-4"], content: view =>
                    {
                        view.Text([Text.BodyStrong], "Card.Elevated");
                        view.Text([Text.Caption], "Elevated card with more shadow");
                    });

                    view.Box([Card.Interactive, "p-4"], content: view =>
                    {
                        view.Text([Text.BodyStrong], "Card.Interactive");
                        view.Text([Text.Caption], "Hover to see effect");
                    });
                });
            });

            // Card style variants
            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H2, "mb-4"], "Card Style Variants");
                view.Text([Text.Caption, "mb-4"], "Different visual styles defined in Card.Variant");
                view.Column([Layout.Column.Md], content: view =>
                {
                    view.Box([Card.Base, Card.Variant.Default, "p-4"], content: view =>
                    {
                        view.Text([Text.BodyStrong], "Card.Variant.Default");
                        view.Text([Text.Caption], "Standard shadow-sm style");
                    });

                    view.Box([Card.Base, Card.Variant.Elevated, "p-4"], content: view =>
                    {
                        view.Text([Text.BodyStrong], "Card.Variant.Elevated");
                        view.Text([Text.Caption], "Elevated with shadow-md");
                    });

                    view.Box([Card.Base, Card.Variant.Strong, "p-4"], content: view =>
                    {
                        view.Text([Text.BodyStrong], "Card.Variant.Strong");
                        view.Text([Text.Caption], "Strong emphasis with shadow-lg");
                    });

                    view.Box([Card.Base, Card.Variant.Outline, "p-4"], content: view =>
                    {
                        view.Text([Text.BodyStrong], "Card.Variant.Outline");
                        view.Text([Text.Caption], "No shadow, thicker border");
                    });

                    view.Box([Card.Base, Card.Variant.Ghost, "p-4 border border-dashed border-quaternary"], content: view =>
                    {
                        view.Text([Text.BodyStrong], "Card.Variant.Ghost");
                        view.Text([Text.Caption], "Transparent background (dashed border added for visibility)");
                    });

                    view.Box([Card.Base, Card.Variant.Interactive, "p-4"], content: view =>
                    {
                        view.Text([Text.BodyStrong], "Card.Variant.Interactive");
                        view.Text([Text.Caption], "Hover for shadow and border effect");
                    });

                    view.Box([Card.Glass, "p-4"], content: view =>
                    {
                        view.Text([Text.BodyStrong], "Card.Glass");
                        view.Text([Text.Caption], "Glassmorphism with backdrop blur");
                    });

                    view.Box([Card.GlassSubtle, "p-4"], content: view =>
                    {
                        view.Text([Text.BodyStrong], "Card.GlassSubtle");
                        view.Text([Text.Caption], "Subtle glass effect with less blur");
                    });
                });
            });

            // Glass cards with interactions
            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H2, "mb-4"], "Glass Cards with Interactions");
                view.Text([Text.Caption, "mb-4"], "Combining glass effects with hover interactions");
                // Complex background to showcase glass blur effect
                view.Box(["rounded-lg p-6 bg-gradient-to-br from-violet-600 via-blue-600 to-pink-600 relative overflow-hidden"], content: view =>
                {
                    // Background shapes for blur visibility
                    view.Box(["absolute -top-10 -left-10 w-40 h-40 bg-yellow-400 rounded-full opacity-60"]);
                    view.Box(["absolute top-20 right-10 w-32 h-32 bg-cyan-400 rounded-full opacity-60"]);
                    view.Box(["absolute -bottom-5 left-1/3 w-28 h-28 bg-rose-400 rounded-full opacity-60"]);
                    view.Box(["absolute top-5 left-1/2 w-20 h-20 bg-green-400 rounded-full opacity-50"]);
                    view.Box(["absolute bottom-10 right-1/4 w-24 h-24 bg-orange-400 rounded-full opacity-50"]);

                    view.Row([Layout.Row.Md, "flex-wrap relative z-10"], content: view =>
                    {
                        view.Box([Card.Glass, Interaction.HoverCard, "p-4 w-48"], content: view =>
                        {
                            view.Text([Text.BodyStrong, "text-white"], "Hover Lift + Glow");
                            view.Text([Text.Caption, "text-white/70"], "Glass with full hover effect");
                        });

                        view.Box([Card.GlassSubtle, Interaction.HoverLift, "p-4 w-48"], content: view =>
                        {
                            view.Text([Text.BodyStrong, "text-white"], "Hover Lift Only");
                            view.Text([Text.Caption, "text-white/70"], "Subtle lift on hover");
                        });

                        view.Box([Card.Glass, Interaction.HoverGlow, "p-4 w-48"], content: view =>
                        {
                            view.Text([Text.BodyStrong, "text-white"], "Hover Glow Only");
                            view.Text([Text.Caption, "text-white/70"], "Shadow glow on hover");
                        });
                    });
                });
            });

            // Card with slots
            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H2, "mb-4"], "Card with Slots");
                view.Box([Card.Default], content: view =>
                {
                    view.Box([Card.Header], content: view =>
                    {
                        view.Text([Text.H3], "Card Title");
                        view.Text([Text.Caption], "Card description goes here");
                    });
                    view.Box([Card.Content], content: view =>
                    {
                        view.Text([Text.Body], "This is the card content area. It can contain any content you need.");
                    });
                    view.Box([Card.Footer], content: view =>
                    {
                        view.Button([Button.OutlineMd], label: "Cancel");
                        view.Button([Button.PrimaryMd], label: "Submit");
                    });
                });
            });

            // Surfaces
            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H2, "mb-4"], "Surfaces");
                view.Column([Layout.Column.Md], content: view =>
                {
                    view.Box([Surface.Card, "p-4"], content: view =>
                    {
                        view.Text([Text.BodyStrong], "Surface.Card");
                    });

                    view.Box([Surface.CardElevated, "p-4"], content: view =>
                    {
                        view.Text([Text.BodyStrong], "Surface.CardElevated");
                    });

                    view.Box([Surface.Popover, "p-4"], content: view =>
                    {
                        view.Text([Text.BodyStrong], "Surface.Popover");
                    });

                    view.Box([Surface.Floating, "p-4"], content: view =>
                    {
                        view.Text([Text.BodyStrong], "Surface.Floating");
                    });

                    view.Box([Surface.Glass, "p-4"], content: view =>
                    {
                        view.Text([Text.BodyStrong], "Surface.Glass");
                    });

                    view.Box([Surface.GlassCard, "p-4"], content: view =>
                    {
                        view.Text([Text.BodyStrong], "Surface.GlassCard");
                    });
                });
            });

            // Badge variants
            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H2, "mb-4"], "Badge Variants");
                view.Column([Layout.Column.Md], content: view =>
                {
                    view.Text([Text.Caption], "Default sizes");
                    view.Row([Layout.Row.Sm, "flex-wrap"], content: view =>
                    {
                        view.Box([Badge.DefaultSm], content: v => v.Text(text: "Sm"));
                        view.Box([Badge.DefaultMd], content: v => v.Text(text: "Md"));
                        view.Box([Badge.DefaultLg], content: v => v.Text(text: "Lg"));
                    });

                    view.Text([Text.Caption], "Variants");
                    view.Row([Layout.Row.Sm, "flex-wrap"], content: view =>
                    {
                        view.Box([Badge.Default], content: v => v.Text(text: "Default"));
                        view.Box([Badge.Secondary], content: v => v.Text(text: "Secondary"));
                        view.Box([Badge.Danger], content: v => v.Text(text: "Danger"));
                        view.Box([Badge.Subtle], content: v => v.Text(text: "Subtle"));
                        view.Box([Badge.Outline], content: v => v.Text(text: "Outline"));
                    });
                });
            });

            // Alert variants
            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H2, "mb-4"], "Alert Variants");
                view.Column([Layout.Column.Md], content: view =>
                {
                    view.Box([Alert.Default], content: view =>
                    {
                        view.Text([Alert.Title], "Default Alert");
                        view.Text([Alert.Description], "This is a default alert message.");
                    });

                    view.Box([Alert.Danger], content: view =>
                    {
                        view.Text([Alert.Title], "Danger Alert");
                        view.Text([Alert.Description], "This is a danger/error alert message.");
                    });

                    view.Box([Alert.Success], content: view =>
                    {
                        view.Text([Alert.Title], "Success Alert");
                        view.Text([Alert.Description], "This is a success alert message.");
                    });

                    view.Box([Alert.Warning], content: view =>
                    {
                        view.Text([Alert.Title], "Warning Alert");
                        view.Text([Alert.Description], "This is a warning alert message.");
                    });

                    view.Box([Alert.Info], content: view =>
                    {
                        view.Text([Alert.Title], "Info Alert");
                        view.Text([Alert.Description], "This is an info alert message.");
                    });
                });
            });

            // Image onClick test
            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H2, "mb-4"], "Image onClick");
                view.Text([Text.Body, "mb-4"], "Click the image below");
                view.Column([Layout.Column.Md], content: view =>
                {
                    view.Image(["w-32 h-32 rounded-lg border border-gray-300 object-cover"],
                        src: "https://picsum.photos/128/128",
                        alt: "Clickable sample image",
                        onClick: async () =>
                        {
                            _imageClickCount.Value++;
                            _imageClickStatus.Value = $"Clicked {_imageClickCount.Value} time(s) at {DateTime.Now:HH:mm:ss}";
                        });
                    view.Text([Text.Caption, "mt-2"], $"Status: {_imageClickStatus.Value}");
                });
            });
        });
    }
}

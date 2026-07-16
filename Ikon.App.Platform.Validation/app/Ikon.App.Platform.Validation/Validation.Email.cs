public partial class Validation
{
    private static readonly string[] AllowedRecipientDomains = ["ikon.live", "ikonai.com"];

    // Send state
    private readonly Reactive<string> _emailTo = new("");
    private readonly Reactive<string> _emailSubject = new("Validation test email");
    private readonly Reactive<string> _emailBody = new("<p>Hello from the Ikon validation app.</p>");
    private readonly Reactive<bool> _emailAttachSample = new(false);
    private readonly Reactive<bool> _emailSending = new(false);
    private readonly Reactive<string?> _emailSendResult = new(null);
    private readonly Reactive<string?> _emailSendError = new(null);

    // Inbox state
    private readonly Reactive<bool> _inboxLoading = new(false);
    private readonly Reactive<string?> _inboxError = new(null);
    private readonly ReactiveList<InboundEmailSummary> _inboxEmails = new();
    private readonly Reactive<string?> _inboxNextCursor = new(null);
    private readonly Reactive<string> _inboxFilterRecipient = new("");
    private readonly Reactive<string> _inboxFilterFrom = new("");

    // Detail state
    private readonly Reactive<string?> _selectedEmailId = new(null);
    private readonly Reactive<InboundEmailDetail?> _emailDetail = new(null);
    private readonly Reactive<bool> _emailDetailLoading = new(false);
    private readonly Reactive<string?> _emailDetailError = new(null);
    private readonly Reactive<string?> _emailHtmlBodyUrl = new(null);

    // Attachment download state — attachment id -> public URL once prepared
    private readonly ReactiveDictionary<string, string> _emailAttachmentUrls = new();
    private readonly Reactive<string?> _emailAttachmentBusyId = new(null);

    private void RenderEmailSection(UIView view)
    {
        if (RenderSectionLocked(view, "Email"))
        {
            return;
        }

        view.Column([Layout.Column.Lg], content: view =>
        {
            view.Box([Card.Default, "p-6 mb-6"], content: view =>
            {
                view.Text([Text.H2, "mb-2"], "Email");
                view.Text([Text.Caption], "Send a test email and browse received messages through the platform mailer. Sending is restricted to @ikon.live and @ikonai.com recipients. Requires the Email feature to be enabled on this space.");
            });

            RenderExternalLinkCard(view);
            RenderEmailSendCard(view);
            RenderEmailInboxCard(view);

            if (_selectedEmailId.Value != null)
            {
                RenderEmailDetailCard(view);
            }
        });
    }

    private void RenderExternalLinkCard(UIView view)
    {
        view.Box([Card.Default, "p-6 mb-6"], content: view =>
        {
            view.Text([Text.H3, "mb-2"], "External handler links");
            view.Text([Text.Caption, "mb-4"], "These anchors hand off to an OS app (mail / phone). Tapping one fires the browser's page-unload events; the app must stay responsive after returning.");

            view.Row([Layout.Row.Md, "items-center flex-wrap"], content: view =>
            {
                view.Link(
                    [Button.OutlineMd],
                    href: "mailto:test@ikon.live?subject=Validation%20test",
                    content: v =>
                    {
                        v.Icon([Icon.Default, "mr-2"], name: "mail");
                        v.Text(text: "Email test@ikon.live");
                    });

                view.Link(
                    [Button.OutlineMd],
                    href: "tel:+358401234567",
                    content: v =>
                    {
                        v.Icon([Icon.Default, "mr-2"], name: "phone");
                        v.Text(text: "Call +358 40 123 4567");
                    });
            });
        });
    }

    private void RenderEmailSendCard(UIView view)
    {
        view.Box([Card.Default, "p-6 mb-6"], content: view =>
        {
            view.Text([Text.H3, "mb-2"], "Send Email");
            view.Text([Text.Caption, "mb-4"], "The platform sets the visible From address; you only choose the recipient.");

            view.Column([Layout.Column.Md], content: view =>
            {
                view.Box([FormField.Root], content: view =>
                {
                    view.Text([FormField.Label], "To");
                    view.TextField(
                        [Input.Default],
                        value: _emailTo.Value,
                        placeholder: "name@ikon.live",
                        onValueChange: async v =>
                        {
                            _emailTo.Value = v ?? "";
                            _emailSendError.Value = null;
                            _emailSendResult.Value = null;
                        });

                    if (!string.IsNullOrWhiteSpace(_emailTo.Value) && !IsAllowedRecipient(_emailTo.Value))
                    {
                        view.Text([Text.Caption, "text-error-primary mt-1"], "Recipient must be @ikon.live or @ikonai.com");
                    }
                });

                view.Box([FormField.Root], content: view =>
                {
                    view.Text([FormField.Label], "Subject");
                    view.TextField(
                        [Input.Default],
                        value: _emailSubject.Value,
                        onValueChange: async v => _emailSubject.Value = v ?? "");
                });

                view.Box([FormField.Root], content: view =>
                {
                    view.Text([FormField.Label], "HTML Body");
                    view.TextArea(
                        [Textarea.Default, "min-h-[120px]"],
                        value: _emailBody.Value,
                        onValueChange: async v => _emailBody.Value = v ?? "");
                });

                view.Row([Layout.Row.InlineCenter], content: view =>
                {
                    view.Checkbox(
                        [Checkbox.Default],
                        value: _emailAttachSample.Value,
                        onValueChange: async v => _emailAttachSample.Value = v);
                    view.Text([Text.Body], "Attach sample image (santa.jpg)");
                });

                view.Row([Layout.Row.Md, "items-center"], content: view =>
                {
                    view.Button(
                        [Button.PrimaryMd],
                        text: "Send",
                        disabled: _emailSending.Value
                            || string.IsNullOrWhiteSpace(_emailSubject.Value)
                            || !IsAllowedRecipient(_emailTo.Value),
                        onClick: SendTestEmailAsync);

                    if (_emailSending.Value)
                    {
                        view.Box([Icon.Spinner]);
                    }
                });

                if (!string.IsNullOrEmpty(_emailSendError.Value))
                {
                    view.Box([Alert.Error, "mt-4"], content: view =>
                    {
                        view.Text([Alert.Description], _emailSendError.Value);
                    });
                }

                if (!string.IsNullOrEmpty(_emailSendResult.Value))
                {
                    view.Box([Alert.Success, "mt-4"], content: view =>
                    {
                        view.Text([Alert.Title], "Accepted");
                        view.Text([Alert.Description], _emailSendResult.Value);
                    });
                }
            });
        });
    }

    private void RenderEmailInboxCard(UIView view)
    {
        view.Box([Card.Default, "p-6 mb-6"], content: view =>
        {
            view.Text([Text.H3, "mb-2"], "Inbox");
            view.Text([Text.Caption, "mb-1"], "Received emails delivered to this space.");
            view.Row([Layout.Row.InlineCenter, "gap-1 mb-4 flex-wrap"], content: view =>
            {
                view.Text([Text.Caption], "To test receiving, send an email to");
                view.Text([Text.Caption, "font-mono text-primary"], "test@validation.dev.ikonai.app");
                view.Text([Text.Caption], "(any local part works).");
            });

            view.Row([Layout.Row.Md, "items-end mb-4 flex-wrap"], content: view =>
            {
                view.Box([FormField.Root, "flex-1 min-w-[200px]"], content: view =>
                {
                    view.Text([FormField.Label], "Filter by recipient");
                    view.TextField(
                        [Input.Default],
                        value: _inboxFilterRecipient.Value,
                        onValueChange: async v => _inboxFilterRecipient.Value = v ?? "");
                });

                view.Box([FormField.Root, "flex-1 min-w-[200px]"], content: view =>
                {
                    view.Text([FormField.Label], "Filter by sender");
                    view.TextField(
                        [Input.Default],
                        value: _inboxFilterFrom.Value,
                        onValueChange: async v => _inboxFilterFrom.Value = v ?? "");
                });

                view.Button(
                    [Button.PrimaryMd],
                    text: "Refresh",
                    disabled: _inboxLoading.Value,
                    onClick: async () => await RefreshInboxAsync(reset: true));

                if (_inboxLoading.Value)
                {
                    view.Box([Icon.Spinner]);
                }
            });

            if (!string.IsNullOrEmpty(_inboxError.Value))
            {
                view.Box([Alert.Error, "mb-4"], content: view =>
                {
                    view.Text([Alert.Description], _inboxError.Value);
                });
            }

            if (_inboxEmails.Value.Count == 0)
            {
                view.Text([Text.Caption], _inboxLoading.Value ? "Loading..." : "No emails. Press Refresh to load.");
            }
            else
            {
                view.Column([Layout.Column.Sm], content: view =>
                {
                    foreach (var email in _inboxEmails.Value)
                    {
                        RenderInboxRow(view, email);
                    }
                });

                if (!string.IsNullOrEmpty(_inboxNextCursor.Value))
                {
                    view.Button(
                        [Button.OutlineMd, "mt-4"],
                        text: "Load more",
                        disabled: _inboxLoading.Value,
                        onClick: async () => await RefreshInboxAsync(reset: false));
                }
            }
        });
    }

    private void RenderInboxRow(UIView view, InboundEmailSummary email)
    {
        var id = email.Id;
        var isSelected = _selectedEmailId.Value == id;

        view.Row([Layout.Row.Md, "items-center"], content: view =>
        {
            view.Box([isSelected ? Card.Selected : Card.Interactive, "p-3 flex-1 min-w-0"],
                onClick: async () => await LoadEmailDetailAsync(id),
                content: view =>
                {
                    view.Column([Layout.Column.Xs, "min-w-0"], content: view =>
                    {
                        view.Text([Text.BodyStrong, "truncate"], string.IsNullOrEmpty(email.Subject) ? "(no subject)" : email.Subject);
                        view.Text([Text.Caption, "truncate"], $"From: {email.From}");
                        view.Row([Layout.Row.InlineCenter, "gap-2 flex-wrap"], content: view =>
                        {
                            view.Text([Text.Caption], email.ReceivedAt.ToString("yyyy-MM-dd HH:mm"));

                            if (email.AttachmentCount > 0)
                            {
                                view.Box([Badge.Default], content: v => v.Text(text: $"{email.AttachmentCount} attachment(s)"));
                            }

                            if (email.SpamScore is { } score && score > 0)
                            {
                                view.Box([Badge.Default], content: v => v.Text(text: $"Spam {score:F0}"));
                            }
                        });
                    });
                });

            view.Button(
                [Button.GhostMd, Button.Icon],
                onClick: async () => await DeleteEmailAsync(id),
                content: v => v.Icon([Icon.Default], name: "trash-2"));
        });
    }

    private void RenderEmailDetailCard(UIView view)
    {
        view.Box([Card.Default, "p-6 mb-6"], content: view =>
        {
            view.Row(["items-center justify-between mb-4"], content: view =>
            {
                view.Text([Text.H3], "Message");
                view.Button(
                    [Button.GhostMd, Button.Icon],
                    onClick: async () =>
                    {
                        _selectedEmailId.Value = null;
                        _emailDetail.Value = null;
                    },
                    content: v => v.Icon([Icon.Default], name: "x"));
            });

            if (_emailDetailLoading.Value)
            {
                view.Box([Icon.Spinner]);
                return;
            }

            if (!string.IsNullOrEmpty(_emailDetailError.Value))
            {
                view.Box([Alert.Error], content: view =>
                {
                    view.Text([Alert.Description], _emailDetailError.Value);
                });
                return;
            }

            var detail = _emailDetail.Value;

            if (detail == null)
            {
                return;
            }

            view.Column([Layout.Column.Sm], content: view =>
            {
                RenderDetailField(view, "Subject", string.IsNullOrEmpty(detail.Subject) ? "(no subject)" : detail.Subject);
                RenderDetailField(view, "From", detail.From);
                RenderDetailField(view, "To", string.Join(", ", detail.To.Select(a => a.Email)));

                if (detail.Cc.Count > 0)
                {
                    RenderDetailField(view, "Cc", string.Join(", ", detail.Cc.Select(a => a.Email)));
                }

                if (!string.IsNullOrEmpty(detail.ReplyTo))
                {
                    RenderDetailField(view, "Reply-To", detail.ReplyTo);
                }

                RenderDetailField(view, "Received", detail.ReceivedAt.ToString("yyyy-MM-dd HH:mm"));

                if (detail.SpamScore is { } score)
                {
                    RenderDetailField(view, "Spam score", score.ToString("F0"));
                }
            });

            if (!string.IsNullOrEmpty(detail.BodyText))
            {
                view.Box([Card.Elevated, "mt-4 p-4 max-h-96 overflow-auto"], content: view =>
                {
                    view.Text([Text.BodyStrong, "mb-2"], "Text body");
                    view.Text([Text.Body, "whitespace-pre-wrap font-mono text-sm"], detail.BodyText);
                });
            }

            if (!string.IsNullOrEmpty(detail.BodyHtml))
            {
                view.Box([Card.Elevated, "mt-4 p-4"], content: view =>
                {
                    view.Row(["items-center justify-between mb-2"], content: view =>
                    {
                        view.Text([Text.BodyStrong], "HTML body");

                        if (!string.IsNullOrEmpty(_emailHtmlBodyUrl.Value))
                        {
                            view.Button([Button.OutlineMd],
                                href: _emailHtmlBodyUrl.Value,
                                target: "_blank",
                                rel: "noopener noreferrer",
                                content: v =>
                                {
                                    v.Icon([Icon.Default, "mr-2"], name: "external-link");
                                    v.Text(text: "Open rendered HTML");
                                });
                        }
                        else
                        {
                            view.Button([Button.OutlineMd],
                                text: "Render HTML",
                                onClick: OpenHtmlBodyAsync);
                        }
                    });

                    view.Box(["max-h-96 overflow-auto"], content: view =>
                    {
                        view.Text([Text.Body, "whitespace-pre-wrap font-mono text-sm"], detail.BodyHtml);
                    });
                });
            }

            if (detail.Attachments.Count > 0)
            {
                view.Box(["mt-4"], content: view =>
                {
                    view.Text([Text.BodyStrong, "mb-2"], "Attachments");

                    view.Column([Layout.Column.Sm], content: view =>
                    {
                        foreach (var attachment in detail.Attachments)
                        {
                            RenderAttachmentRow(view, detail.Id, attachment);
                        }
                    });
                });
            }
        });
    }

    private void RenderAttachmentRow(UIView view, string emailId, InboundAttachmentInfo attachment)
    {
        var attachmentId = attachment.Id;
        _emailAttachmentUrls.Value.TryGetValue(attachmentId, out var url);

        view.Box([Card.Elevated, "p-3"], content: view =>
        {
            view.Row(["items-center justify-between gap-3"], content: view =>
            {
                view.Column([Layout.Column.Xs, "min-w-0"], content: view =>
                {
                    view.Text([Text.Body, "truncate"], attachment.Filename);
                    view.Text([Text.Caption], $"{attachment.MimeType} · {attachment.Size / 1024} KB");
                });

                if (!string.IsNullOrEmpty(url))
                {
                    view.Button([Button.OutlineMd],
                        href: url,
                        target: "_blank",
                        rel: "noopener noreferrer",
                        content: v =>
                        {
                            v.Icon([Icon.Default, "mr-2"], name: "external-link");
                            v.Text(text: "Open file");
                        });
                }
                else
                {
                    view.Button([Button.OutlineMd],
                        text: "Open",
                        disabled: _emailAttachmentBusyId.Value == attachmentId,
                        onClick: async () => await OpenAttachmentAsync(emailId, attachment));

                    if (_emailAttachmentBusyId.Value == attachmentId)
                    {
                        view.Box([Icon.Spinner]);
                    }
                }
            });

            if (!string.IsNullOrEmpty(url) && attachment.MimeType.StartsWith("image/"))
            {
                view.Image(["max-w-xs h-auto rounded-lg mt-2"], src: url);
            }
        });
    }

    private static void RenderDetailField(UIView view, string label, string value)
    {
        view.Row(["items-start gap-2"], content: view =>
        {
            view.Text([Text.Caption, "min-w-[110px] shrink-0"], label);
            view.Text([Text.Body, "break-all"], value);
        });
    }

    private async Task SendTestEmailAsync()
    {
        _emailSending.Value = true;
        _emailSendError.Value = null;
        _emailSendResult.Value = null;

        try
        {
            var to = _emailTo.Value.Trim();

            if (!IsAllowedRecipient(to))
            {
                _emailSendError.Value = "Recipient must be a @ikon.live or @ikonai.com address";
                return;
            }

            IReadOnlyList<EmailAttachment>? attachments = null;

            if (_emailAttachSample.Value)
            {
                var bytes = await File.ReadAllBytesAsync(Path.Combine(app.DataDirectory, "santa.jpg"));
                attachments = [new EmailAttachment("santa.jpg", MimeTypes.ImageJpeg, bytes)];
            }

            var request = new EmailSendRequest(
                To: to,
                Subject: _emailSubject.Value,
                HtmlBody: _emailBody.Value,
                Attachments: attachments);

            await app.Email.SendAsync(request);

            _emailSendResult.Value = $"Email accepted for delivery to {to}";
        }
        catch (Exception ex)
        {
            _emailSendError.Value = ex.Message;
        }
        finally
        {
            _emailSending.Value = false;
        }
    }

    private async Task RefreshInboxAsync(bool reset)
    {
        if (_inboxLoading.Value)
        {
            return;
        }

        _inboxLoading.Value = true;
        _inboxError.Value = null;

        try
        {
            var query = new InboxQuery
            {
                Limit = 25,
                Cursor = reset ? null : _inboxNextCursor.Value,
                Recipient = NullIfEmpty(_inboxFilterRecipient.Value),
                From = NullIfEmpty(_inboxFilterFrom.Value)
            };

            var page = await app.Email.GetInboxPageAsync(query);

            if (reset)
            {
                _inboxEmails.ReplaceAll(page.Items);
            }
            else
            {
                _inboxEmails.AddRange(page.Items);
            }

            _inboxNextCursor.Value = page.NextCursor;
        }
        catch (Exception ex)
        {
            _inboxError.Value = ex.Message;
        }
        finally
        {
            _inboxLoading.Value = false;
        }
    }

    private async Task LoadEmailDetailAsync(string id)
    {
        _selectedEmailId.Value = id;
        _emailDetail.Value = null;
        _emailDetailError.Value = null;
        _emailHtmlBodyUrl.Value = null;
        _emailDetailLoading.Value = true;

        try
        {
            _emailDetail.Value = await app.Email.GetMessageAsync(id);
        }
        catch (Exception ex)
        {
            _emailDetailError.Value = ex.Message;
        }
        finally
        {
            _emailDetailLoading.Value = false;
        }
    }

    private async Task OpenAttachmentAsync(string emailId, InboundAttachmentInfo attachment)
    {
        _emailAttachmentBusyId.Value = attachment.Id;

        try
        {
            await using var download = await app.Email.DownloadAttachmentAsync(emailId, attachment.Id);

            using var buffer = new MemoryStream();
            await download.Content.CopyToAsync(buffer);
            var bytes = buffer.ToArray();

            var url = await UploadForDownloadAsync(attachment.Filename, bytes, attachment.MimeType);

            if (url != null)
            {
                _emailAttachmentUrls[attachment.Id] = url;
            }
            else
            {
                _emailDetailError.Value = "Failed to prepare attachment for download";
            }
        }
        catch (Exception ex)
        {
            _emailDetailError.Value = ex.Message;
        }
        finally
        {
            _emailAttachmentBusyId.Value = null;
        }
    }

    private async Task OpenHtmlBodyAsync()
    {
        var html = _emailDetail.Value?.BodyHtml;

        if (string.IsNullOrEmpty(html))
        {
            return;
        }

        try
        {
            var bytes = System.Text.Encoding.UTF8.GetBytes(html);
            _emailHtmlBodyUrl.Value = await UploadForDownloadAsync("email-body.html", bytes, MimeTypes.TextHtml);
        }
        catch (Exception ex)
        {
            _emailDetailError.Value = ex.Message;
        }
    }

    private async Task DeleteEmailAsync(string id)
    {
        try
        {
            await app.Email.DeleteAsync(id);

            _inboxEmails.RemoveAll(e => e.Id == id);

            if (_selectedEmailId.Value == id)
            {
                _selectedEmailId.Value = null;
                _emailDetail.Value = null;
            }
        }
        catch (Exception ex)
        {
            _inboxError.Value = ex.Message;
        }
    }

    private static bool IsAllowedRecipient(string to)
    {
        if (string.IsNullOrWhiteSpace(to))
        {
            return false;
        }

        var trimmed = to.Trim();
        var atIndex = trimmed.LastIndexOf('@');

        if (atIndex < 0 || atIndex == trimmed.Length - 1)
        {
            return false;
        }

        var domain = trimmed[(atIndex + 1)..];
        return AllowedRecipientDomains.Contains(domain, StringComparer.OrdinalIgnoreCase);
    }

    private static string? NullIfEmpty(string value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

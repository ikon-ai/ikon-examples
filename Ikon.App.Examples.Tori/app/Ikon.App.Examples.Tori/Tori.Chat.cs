public partial class Tori
{
    private void RenderChatMessage(UIView view, ChatMessage message)
    {
        var timeDisplay = FormatTimeInClientTimezone(message.Timestamp);

        view.Column(["gap-1"], key: message.Id, content: col =>
        {
            col.Row(["gap-2 items-baseline"], content: row =>
            {
                row.Text([Text.BodyStrong, "text-primary"], message.SenderName);
                row.Text([Text.Caption], timeDisplay);
            });

            col.Text([Text.Body], message.Content);
        });
    }

    private async Task SendChatMessage()
    {
        await SendChatMessage(_chatInputText.Value);
    }

    private async Task SendChatMessage(string? submitted)
    {
        var text = (submitted ?? "").Trim();

        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        var participant = GetCurrentClientParticipant();
        var senderName = participant?.Name ?? "Unknown";

        var message = new ChatMessage(
            Guid.NewGuid().ToString(),
            senderName,
            DateTime.UtcNow,
            text);

        _chatMessages.Update(messages => messages.TakeLast(MaxChatEntries - 1).Append(message));

        _chatInputText.Value = "";
    }
}

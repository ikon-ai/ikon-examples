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
        var text = _chatInputText.Value.Trim();

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

        var messages = _chatMessages.Value.TakeLast(MaxChatEntries - 1).ToList();
        messages.Add(message);
        _chatMessages.Value = messages;
        _chatMessagesVersion.Value++;

        _chatInputText.Value = "";
    }
}

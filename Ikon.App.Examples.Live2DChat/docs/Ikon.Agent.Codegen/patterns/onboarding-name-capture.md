<!-- mined-from: NoBrainer -->
# Onboarding Name Capture — One-Field Greeter Screen

A full-viewport vertical center column shown on first connect: avatar, two lines of warm copy, a single text field with center-aligned underline-style input, and a "Let's go" pill button. On submit, the user gets a profile, a personal Space is created, and the onboarding flag flips off — the user lands directly into the main app.

## When to use

Friendly first-run for solo or B2C apps where you only need a display name (or any single piece of info) before the user can use the product. Use over a multi-step wizard when there's no real configuration to do yet — get out of the user's way.

## Snippet

```csharp
private void RenderOnboarding(UIView view)
{
    view.Column(["h-screen items-center justify-center bg-[#faf9f7]"], content: view =>
    {
        view.Column(["items-center gap-6 max-w-sm w-full px-8"], content: view =>
        {
            if (_avatarImage.Value != null && _avatarMime.Value != null)
            {
                view.Image(["w-20 h-20 rounded-full object-cover"],
                    data: _avatarImage.Value, mimeType: _avatarMime.Value, alt: "No-Brainer");
            }
            else
            {
                view.Box(["w-20 h-20 rounded-full bg-black/[0.04]",
                    "motion-[0:opacity-40%,50:opacity-70%,100:opacity-40%] motion-duration-4000ms motion-loop motion-ease-[ease-in-out]"]);
            }

            view.Text(["text-xl font-light text-black/40"], "Hi. I'm No-Brainer.");
            view.Text(["text-sm text-black/25 text-center font-light leading-relaxed"],
                "I handle the things you shouldn't have to think about. Tasks, calls, reminders, research — just say the word.");

            view.TextField([
                "w-full bg-transparent border-0 border-b border-black/10 text-center",
                "text-black/60 text-lg placeholder:text-black/15 py-3",
                "focus:border-black/20 focus:ring-0"],
                placeholder: "What should I call you?",
                value: _onboardingName.Value,
                onValueChange: async v => _onboardingName.Value = v,
                onSubmit: async submitted => await CompleteOnboardingAsync(submitted));

            view.Button([
                "bg-black/[0.04] hover:bg-black/[0.08] border-0 rounded-full px-8 py-3",
                "text-sm text-black/40 hover:text-black/60 transition-colors duration-200"],
                disabled: string.IsNullOrWhiteSpace(_onboardingName.Value),
                onClick: async () => await CompleteOnboardingAsync(_onboardingName.Value),
                content: v => v.Text(text: "Let's go"));
        });
    });
}

// Triggered on ClientJoinedAsync:
// if (!_userProfiles.Value.ContainsKey(userId)) _showOnboarding.Value = true;

private async Task CompleteOnboardingAsync(string? submitted)
{
    var userId = app.SessionIdentity.UserId;
    var name = submitted?.Trim();
    if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(name)) return;

    var personalSpaceId = EnsurePersonalSpace(userId, name);

    var profiles = new Dictionary<string, UserProfile>(_userProfiles.Value);
    profiles[userId] = new UserProfile
    {
        UserId = userId, DisplayName = name, OnboardingComplete = true,
        ActiveSkills = ["reminders", "research"],
        PersonalSpaceId = personalSpaceId
    };
    _userProfiles.Value = profiles;
    SaveUserProfiles();

    _activeSpaceId.Value = personalSpaceId;
    _showOnboarding.Value = false;
}
```

## Notes

- `ClientReactive<bool> _showOnboarding` flipped from `app.ClientJoinedAsync` based on whether the user has a profile — different first-run state per session, not per app.
- Both `onSubmit` (Enter key) and the button's `onClick` route to the same `CompleteOnboardingAsync(string?)` overload — Enter-to-continue feels right here.
- `disabled: string.IsNullOrWhiteSpace(_onboardingName.Value)` keeps the button visibly off until the field has content; cheaper than form validation for a one-field gate.
- Sane defaults baked into the new profile (e.g. `ActiveSkills = ["reminders", "research"]`) get the user to *something useful* without making them tour a settings panel first.
- Use `bg-transparent border-0 border-b text-center` for that "elegant single-input" look — bordered text-fields scream form, this reads like a question.

## See also

- `multi-step-wizard` — when there's actually configuration to do
- `persistent-user-preferences` — saving the resulting profile

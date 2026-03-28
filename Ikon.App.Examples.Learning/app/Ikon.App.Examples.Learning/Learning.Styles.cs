public partial class LearningApp
{
    public enum LearningTheme
    {
        LakeBlue,
        PineGreen,
        NordicTeal
    }

    internal static class Styles
    {
        // Animation tokens
        public const string FadeIn = "motion-[0:opacity-0,100:opacity-100] motion-duration-300ms motion-fill-both";
        public const string SlideUp = "motion-[0:opacity-0_translate-y-[12px],100:opacity-100_translate-y-0] motion-duration-300ms motion-fill-both";
        public const string SlideIn = "motion-[0:opacity-0_translate-x-[-12px],100:opacity-100_translate-x-0] motion-duration-300ms motion-fill-both";

        // Base palette (Nordic-inspired)
        public const string BackgroundBase = "bg-[#faf9f7]";
        public const string SurfaceCard = "bg-white/80";
        public const string TextPrimary = "text-[#1a1a1a]";
        public const string TextSecondary = "text-[#6b7280]";

        // Goals/highlight styles - modern gradient glass effect
        public static string GetGoalsCard(LearningTheme theme) => theme switch
        {
            LearningTheme.LakeBlue => "bg-gradient-to-br from-blue-50/90 to-indigo-50/80 backdrop-blur-sm",
            LearningTheme.PineGreen => "bg-gradient-to-br from-emerald-50/90 to-teal-50/80 backdrop-blur-sm",
            LearningTheme.NordicTeal => "bg-gradient-to-br from-teal-50/90 to-cyan-50/80 backdrop-blur-sm",
            _ => "bg-gradient-to-br from-blue-50/90 to-indigo-50/80 backdrop-blur-sm"
        };

        public static string GetGoalsBorder(LearningTheme theme) => theme switch
        {
            LearningTheme.LakeBlue => "border-l-4 border-blue-400/70",
            LearningTheme.PineGreen => "border-l-4 border-emerald-400/70",
            LearningTheme.NordicTeal => "border-l-4 border-teal-400/70",
            _ => "border-l-4 border-blue-400/70"
        };

        public static string GetGoalsTitle(LearningTheme theme) => theme switch
        {
            LearningTheme.LakeBlue => "text-blue-700",
            LearningTheme.PineGreen => "text-emerald-700",
            LearningTheme.NordicTeal => "text-teal-700",
            _ => "text-blue-700"
        };

        public static string GetGoalsText(LearningTheme theme) => theme switch
        {
            LearningTheme.LakeBlue => "text-blue-900/80",
            LearningTheme.PineGreen => "text-emerald-900/80",
            LearningTheme.NordicTeal => "text-teal-900/80",
            _ => "text-blue-900/80"
        };

        // Card styles
        public const string StatCard = "p-4 flex-1 min-w-[120px]";
        public const string MenuCard = "p-4 cursor-pointer hover:bg-accent/50 transition-colors";

        // Glass morphism base styles
        public const string GlassCard = "bg-white/70 backdrop-blur-md border border-white/40 shadow-sm";
        public const string GlassCardStrong = "bg-white/80 backdrop-blur-lg border border-white/50 shadow-lg";

        // Custom button variants not in Default theme
        public const string GhostXs = $"{Button.Base} {Button.Variant.Ghost} px-2 py-1 text-xs {State.Pressable}";
        public const string GhostSm = $"{Button.Base} {Button.Variant.Ghost} {Button.Size.Sm} {State.Pressable}";
        public const string SecondaryLg = $"{Button.Base} {Button.Variant.Secondary} {Button.Size.Lg} {State.Pressable}";
        public const string OutlineLg = $"{Button.Base} {Button.Variant.Outline} {Button.Size.Lg} {State.Pressable}";

        // Text styles
        public const string H4 = "block text-base font-medium";

        // Theme color definitions
        public static class Theme
        {
            // Lake Blue (default) - crisp, bright, clear-headed
            public const string LakeBlueGradient = "bg-gradient-to-r from-[#3b82f6] to-[#1d4ed8]";
            public const string LakeBlueSolid = "bg-[#3b82f6]";
            public const string LakeBlueText = "text-[#3b82f6]";
            public const string LakeBlueBorder = "border-[#3b82f6]";
            public const string LakeBlueRing = "ring-[#3b82f6]";

            // Pine Green - calm, focused, study mode
            public const string PineGreenGradient = "bg-gradient-to-r from-[#22c55e] to-[#15803d]";
            public const string PineGreenSolid = "bg-[#22c55e]";
            public const string PineGreenText = "text-[#22c55e]";
            public const string PineGreenBorder = "border-[#22c55e]";
            public const string PineGreenRing = "ring-[#22c55e]";

            // Nordic Teal - fresh, playful, character-led
            public const string NordicTealGradient = "bg-gradient-to-r from-[#14b8a6] to-[#0d9488]";
            public const string NordicTealSolid = "bg-[#14b8a6]";
            public const string NordicTealText = "text-[#14b8a6]";
            public const string NordicTealBorder = "border-[#14b8a6]";
            public const string NordicTealRing = "ring-[#14b8a6]";
        }

        public static string GetAccentGradient(LearningTheme theme) => theme switch
        {
            LearningTheme.LakeBlue => Theme.LakeBlueGradient,
            LearningTheme.PineGreen => Theme.PineGreenGradient,
            LearningTheme.NordicTeal => Theme.NordicTealGradient,
            _ => Theme.LakeBlueGradient
        };

        public static string GetAccentSolid(LearningTheme theme) => theme switch
        {
            LearningTheme.LakeBlue => Theme.LakeBlueSolid,
            LearningTheme.PineGreen => Theme.PineGreenSolid,
            LearningTheme.NordicTeal => Theme.NordicTealSolid,
            _ => Theme.LakeBlueSolid
        };

        public static string GetAccentText(LearningTheme theme) => theme switch
        {
            LearningTheme.LakeBlue => Theme.LakeBlueText,
            LearningTheme.PineGreen => Theme.PineGreenText,
            LearningTheme.NordicTeal => Theme.NordicTealText,
            _ => Theme.LakeBlueText
        };

        public static string GetAccentBorder(LearningTheme theme) => theme switch
        {
            LearningTheme.LakeBlue => Theme.LakeBlueBorder,
            LearningTheme.PineGreen => Theme.PineGreenBorder,
            LearningTheme.NordicTeal => Theme.NordicTealBorder,
            _ => Theme.LakeBlueBorder
        };

        // Message bubble styles
        public static string GetUserBubble(LearningTheme theme) =>
            $"self-end max-w-[75%] rounded-3xl rounded-br-lg {GetAccentGradient(theme)} px-5 py-4 text-white shadow-lg";

        public const string AssistantBubble =
            "self-start max-w-[85%] rounded-3xl rounded-bl-lg " +
            "bg-white/70 backdrop-blur-md border border-white/40 " +
            "px-5 py-4 text-[#1a1a1a] shadow-sm";

        public const string StructuredBriefCard =
            "rounded-3xl bg-white/80 backdrop-blur-lg border border-white/50 p-6 shadow-lg";

        // Input area styles
        public const string InputCard =
            "rounded-2xl bg-white/90 backdrop-blur-md border border-[#e5e7eb] shadow-sm px-4 py-3";

        public static string GetSendButton(LearningTheme theme, bool disabled) => disabled
            ? "bg-gray-300 text-gray-500 px-5 py-2 rounded-xl font-medium cursor-not-allowed"
            : $"{GetAccentGradient(theme)} hover:opacity-90 text-white px-5 py-2 rounded-xl font-medium transition-opacity";
    }
}

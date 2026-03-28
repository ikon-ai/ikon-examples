public static class Styles
{
    public const string Root = "font-sans min-h-screen bg-slate-950 text-gray-100 flex flex-col";

    public static class Header
    {
        public const string Bar = "flex items-center justify-between px-6 py-3 border-b border-amber-900/30 bg-slate-950/80 backdrop-blur-sm shrink-0";
        public const string Title = "text-xl font-bold tracking-wider text-amber-400";
        public const string TitleMotion = "letter:motion-[0:opacity-0_blur-[4px],100:opacity-100_blur-0] letter:motion-duration-300ms letter:motion-stagger-80ms letter:motion-per-letter letter:motion-fill-both";
        public const string Stats = "text-sm text-gray-400 font-mono";
        public const string Score = "text-amber-400 font-bold";
    }

    public static class Scene
    {
        public const string Container = "relative w-full shrink-0";
        public const string ImageWrapper = "w-full aspect-[16/9] bg-slate-900 overflow-hidden relative";
        public const string Image = "w-full h-full object-cover";
        public const string ImageMotion = "motion-[0:opacity-0_scale-[1.05],100:opacity-100_scale-100] motion-duration-700ms motion-fill-both motion-ease-[cubic-bezier(0.25,1,0.35,1)]";
        public const string Placeholder = "w-full h-full flex items-center justify-center bg-slate-900";
        public const string PlaceholderText = "text-amber-400/60 text-lg font-mono tracking-widest animate-pulse";
        public const string ProximityBar = "h-1.5 w-full bg-slate-800";
    }

    public static class Transcript
    {
        public const string Container = "flex-1 min-h-0 overflow-hidden";
        public const string EntryBase = "px-6 py-2.5 border-l-2";
        public const string EntryMotion = "motion-[0:opacity-0_translate-x-[-8px],100:opacity-100_translate-x-0] motion-duration-300ms motion-fill-both";

        public const string QEntry = "border-l-amber-400 bg-amber-950/20";
        public const string QSpeaker = "text-xs font-bold text-amber-400 uppercase tracking-wider mb-0.5";
        public const string QText = "text-amber-200/90";
        public const string QTextMotion = "letter:motion-[0:opacity-0_blur-[3px],100:opacity-100_blur-0] letter:motion-duration-150ms letter:motion-stagger-30ms letter:motion-per-letter letter:motion-fill-both";

        public const string PlayerEntry = "border-l-cyan-400 bg-cyan-950/20";
        public const string PlayerSpeaker = "text-xs font-bold text-cyan-400 uppercase tracking-wider mb-0.5";
        public const string PlayerText = "text-cyan-200/90";

        public const string NarratorEntry = "border-l-purple-400 bg-purple-950/20";
        public const string NarratorSpeaker = "text-xs font-bold text-purple-400 uppercase tracking-wider mb-0.5";
        public const string NarratorText = "text-purple-200/90 italic";

        public const string WitnessEntry = "border-l-emerald-400 bg-emerald-950/20";
        public const string WitnessSpeaker = "text-xs font-bold text-emerald-400 uppercase tracking-wider mb-0.5";
        public const string WitnessText = "text-emerald-200/90";

        public const string SystemEntry = "border-l-gray-600 bg-slate-900/40";
        public const string SystemSpeaker = "text-xs font-bold text-gray-500 uppercase tracking-wider mb-0.5";
        public const string SystemText = "text-gray-400 text-sm";
    }

    public static class Input
    {
        public const string Container = "px-4 py-3 border-t border-amber-900/30 bg-slate-950/90 backdrop-blur-sm shrink-0";
        public const string Row = "flex gap-2 items-center";
        public const string Field = "flex-1 bg-slate-900 border border-amber-900/40 rounded-lg px-4 py-2.5 text-gray-100 placeholder-gray-600 focus:outline-none focus:border-amber-500/60 focus:ring-1 focus:ring-amber-500/30";
        public const string SendButton = "px-4 py-2.5 bg-amber-600 hover:bg-amber-500 text-slate-950 font-bold rounded-lg transition-colors";
        public const string SendButtonDisabled = "px-4 py-2.5 bg-slate-700 text-slate-500 font-bold rounded-lg cursor-not-allowed";
        public const string HintRow = "flex gap-1.5 mt-2";
        public const string HintButton = "px-2.5 py-1 text-xs font-mono rounded bg-slate-800 text-amber-400/70 hover:bg-slate-700 hover:text-amber-400 border border-amber-900/30 transition-colors";
    }

    public static class FinalVerdict
    {
        public const string Container = "flex-1 flex items-center justify-center p-8";
        public const string Card = "max-w-lg w-full bg-slate-900 border border-amber-900/40 rounded-2xl p-8";
        public const string Title = "text-2xl font-bold text-amber-400 text-center mb-6";
        public const string RoundRow = "flex justify-between items-center py-2 border-b border-slate-800";
        public const string RoundLabel = "text-gray-400 text-sm";
        public const string RoundScore = "font-mono font-bold";
        public const string TotalRow = "flex justify-between items-center pt-4 mt-2";
        public const string TotalLabel = "text-lg font-bold text-gray-200";
        public const string TotalScore = "text-2xl font-bold text-amber-400";
        public const string PlayAgain = "mt-6 w-full py-3 bg-amber-600 hover:bg-amber-500 text-slate-950 font-bold rounded-lg text-center transition-colors";
    }
}

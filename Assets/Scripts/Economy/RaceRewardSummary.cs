namespace SuperRacing.Economy
{
    public readonly struct RaceRewardSummary
    {
        public RaceRewardSummary(int completion, int record, int drift)
        {
            CompletionReward = completion;
            NewRecordBonus = record;
            CleanDriftBonus = drift;
        }

        public int CompletionReward { get; }
        public int NewRecordBonus { get; }
        public int CleanDriftBonus { get; }
        public int Total => CompletionReward + NewRecordBonus + CleanDriftBonus;
    }
}

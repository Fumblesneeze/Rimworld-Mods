namespace ImmersiveChefs;

public sealed class AssistantContributionAccumulator
{
    private float normalizedSkillTicks;
    private int leadCookingTicks;

    public float CurrentSpeedBonus { get; private set; }

    public float TotalSpeedWork { get; private set; }

    public float QualityScore => leadCookingTicks <= 0
        ? 0f
        : Clamp(100f * normalizedSkillTicks / (4f * leadCookingTicks), 0f, 100f);

    public void RecordLeadTick(IEnumerable<int> simultaneousAssistantSkills, float effectScale)
    {
        var scale = Clamp(effectScale, 0f, 3f);
        var normalizedSum = simultaneousAssistantSkills.Sum(skill => Clamp(skill / 20f, 0f, 1f));
        CurrentSpeedBonus = Clamp(0.15f * normalizedSum * scale, 0f, 1.8f);
        TotalSpeedWork += CurrentSpeedBonus;
        normalizedSkillTicks += normalizedSum * scale;
        leadCookingTicks++;
    }

    private static float Clamp(float value, float minimum, float maximum) =>
        Math.Max(minimum, Math.Min(maximum, value));
}

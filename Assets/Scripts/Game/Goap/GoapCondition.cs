public class GoapCondition
{
    public string Tag { get; }
    public bool ExpectedValue { get; }

    public GoapCondition(string tag, bool expectedValue)
    {
        Tag = tag;
        ExpectedValue = expectedValue;
    }

    public override bool Equals(object obj)
    {
        if (obj is GoapCondition other)
        {
            return Tag == other.Tag && ExpectedValue == other.ExpectedValue;
        }
        return false;
    }

    public override int GetHashCode()
    {
        return (Tag, ExpectedValue).GetHashCode();
    }
} 
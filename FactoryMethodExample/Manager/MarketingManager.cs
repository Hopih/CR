namespace FactoryMethodExample;

public class MarketingManager : HiringManager
{
    public override IInterviewer MakeInterviewer()
    {
        return new CommunityExecutive();
    }
}
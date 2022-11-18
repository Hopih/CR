namespace FactoryMethodExample;

public class DevelopmentManager : HiringManager
{
    public override IInterviewer MakeInterviewer()
    {
        return new Developer();
    }
}
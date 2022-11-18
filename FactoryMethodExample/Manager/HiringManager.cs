namespace FactoryMethodExample;

public abstract class HiringManager
{
    private IInterviewer Interviewer;
    public abstract IInterviewer MakeInterviewer();

    public void TakeInterview()
    {
        Interviewer = MakeInterviewer();
        Console.WriteLine(Interviewer.AskQuestion());
    }
}
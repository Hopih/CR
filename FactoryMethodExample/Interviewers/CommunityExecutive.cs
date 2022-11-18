namespace FactoryMethodExample;

public class CommunityExecutive : IInterviewer
{
    public string AskQuestion()
    {
        return "Спрашивает о работе с сообществом";
    }
}
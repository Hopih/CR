namespace FactoryMethodExample;

public class Developer : IInterviewer
{
    public string AskQuestion()
    {
        return "Спрашивает про шаблоны проектирования!";
    }
}
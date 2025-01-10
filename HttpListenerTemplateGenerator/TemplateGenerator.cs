using System.Text.RegularExpressions;

namespace HttpListenerTemplateGenerator;

public static class TemplateGenerator
{
    public static string Render(string template, Dictionary<string, string> data)
    {
        template = ProcessCondition(template, data);
        
        return data.Keys.Aggregate(template, (current, key) => 
            current.Replace($"{{{{{key}}}}}", data[key]));
    }

    private static string ProcessCondition(string template, Dictionary<string, string> data)
    {
        var conditionPattern = @"{{#if\s+(\w+)}}[\r\n]*\s*(.*?)[\r\n]*\s*{{\/if}}";
        var matches = Regex.Matches(template, conditionPattern);
        
        if (matches.Count == 0)
        {
            return template;
        }

        foreach (Match match in matches)
        {
            var condition = match.Groups[1].Value;
            var value = match.Groups[2].Value;

            template = data.ContainsKey(condition) && data[condition] == "true"
                ? template.Replace(match.Value, value)
                : template.Replace(match.Value, "");
        }
        
        return template;
    }
}
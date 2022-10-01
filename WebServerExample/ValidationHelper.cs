using System.ComponentModel.DataAnnotations;

namespace WebServerExample;

public static class ValidationHelper
{
    public static List<ValidationResult> ValidateModel<T>(T model)
    {
        var results = new List<ValidationResult>();
        var context = new ValidationContext(model);
        
        return !Validator.TryValidateObject(model, context, results, true) 
            ? results 
            : null;
    }
}
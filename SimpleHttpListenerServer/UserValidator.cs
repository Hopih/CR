using System.ComponentModel.DataAnnotations;

namespace SimpleHttpListenerServer;

public class UserValidator: IValidatableObject
{
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var results = new List<ValidationResult>(); 
        Validator.TryValidateObject(validationContext.ObjectInstance, validationContext, results, true);
        return results;
    }
}
using FluentValidation;

namespace OneSwiss.Server.Components;

public class ModelValidator<T> : AbstractValidator<T>
{
    public Func<object, string, Task<IEnumerable<string>>> ValidateValue => async (model, propertyName) =>
    {
        var name = propertyName.StartsWith("Model.") ? propertyName.Replace("Model.", "") : propertyName;

        var result = await ValidateAsync(ValidationContext<T>
            .CreateWithOptions((T)model, x => x.IncludeProperties(name)));

        return result.IsValid ? [] : result.Errors.Select(e => e.ErrorMessage);
    };
}
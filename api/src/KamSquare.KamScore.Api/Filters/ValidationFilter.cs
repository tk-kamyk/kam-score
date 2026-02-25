using FluentValidation;

namespace KamSquare.KamScore.Api.Filters;

public class ValidationFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        foreach (var argument in context.Arguments)
        {
            if (argument is null) continue;

            var argumentType = argument.GetType();
            var validatorType = typeof(IValidator<>).MakeGenericType(argumentType);
            if (context.HttpContext.RequestServices.GetService(validatorType) is IValidator validator)
            {
                var validationContext = new ValidationContext<object>(argument);
                var result = await validator.ValidateAsync(validationContext);
                if (!result.IsValid)
                    throw new ValidationException(result.Errors);
            }
        }
        return await next(context);
    }
}

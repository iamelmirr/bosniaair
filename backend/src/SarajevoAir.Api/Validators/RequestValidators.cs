/*
=== FLUENTVALIDATION VALIDATORS ===
Professional input validation for Request DTOs

PREDNOSTI FluentValidation:
1. Centralizirane validation rules
2. Chainable syntax - lako čitanje
3. Custom validation logic
4. Async validation support
5. Lokalizacija error poruka
6. Integration sa ASP.NET Core model binding

INTERVJU PLUS:
- Shows enterprise-grade input validation
- Separation of concerns - validation logic odvojena od Controller-a
- Testabilnost - validators se mogu unit testirati
- Maintainability - validation rules na jednom mjestu
*/

using FluentValidation;
using SarajevoAir.Api.Dtos.Requests;

namespace SarajevoAir.Api.Validators;

/// <summary>
/// Validator za LiveDataRequest
/// Demonstrira professional input validation patterns
/// </summary>
public class LiveDataRequestValidator : AbstractValidator<LiveDataRequest>
{
    public LiveDataRequestValidator()
    {
        // Timeout validation - mora biti između 5 i 300 sekundi
        RuleFor(x => x.TimeoutSeconds)
            .InclusiveBetween(5, 300)
            .WithMessage("Timeout must be between 5 and 300 seconds for reliable API calls");

        // ForceFresh je bool - automatski valjan, ali možemo dodati business logic
        When(x => x.ForceFresh, () => {
            RuleFor(x => x.TimeoutSeconds)
                .GreaterThanOrEqualTo(10)
                .WithMessage("Fresh API calls require minimum 10 seconds timeout");
        });
    }
}

/// <summary>
/// Validator za ForecastRequest
/// Shows complex validation scenarios
/// </summary>
public class ForecastRequestValidator : AbstractValidator<ForecastRequest>
{
    public ForecastRequestValidator()
    {
        // Days validation
        RuleFor(x => x.Days)
            .InclusiveBetween(1, 7)
            .WithMessage("Forecast days must be between 1 and 7");

        // Business logic validation
        When(x => x.ForceFresh, () => {
            RuleFor(x => x.Days)
                .LessThanOrEqualTo(5)
                .WithMessage("Fresh forecast requests are limited to 5 days to prevent API abuse");
        });
    }
}

/// <summary>
/// Validator za CompleteDataRequest
/// Demonstrates nested object validation
/// </summary>
public class CompleteDataRequestValidator : AbstractValidator<CompleteDataRequest>
{
    public CompleteDataRequestValidator()
    {
        // Nested validator - automatski poziva LiveDataRequestValidator
        RuleFor(x => x.LiveData)
            .SetValidator(new LiveDataRequestValidator());

        // Nested validator - automatski poziva ForecastRequestValidator  
        RuleFor(x => x.Forecast)
            .SetValidator(new ForecastRequestValidator());

        // Business rule - ne možemo imati i city comparison i fresh data odjednom
        When(x => x.IncludeCityComparison && x.LiveData.ForceFresh, () => {
            RuleFor(x => x.IncludeCityComparison)
                .Equal(false)
                .WithMessage("City comparison cannot be included with fresh data requests due to performance constraints");
        });
    }
}
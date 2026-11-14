using FluentValidation;
using OrderHub.Catalog.Application.UseCases.Commands;

namespace OrderHub.Catalog.Application.UseCases.Validators;

internal sealed class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductCommandValidator()
    {
        RuleFor(x => x.Sku)
            .NotEmpty()
                .WithMessage("SKU cannot be empty.")
            .Length(3, 32)
                .WithMessage("SKU must be between 3 and 32 characters.")
            .Matches(@"^[a-zA-Z0-9\-]+$")
                .WithMessage("SKU must contain only alphanumeric characters and hyphens.");

        RuleFor(x => x.Name)
            .NotEmpty()
                .WithMessage("Name cannot be empty.");

        RuleFor(x => x.Description)
            .NotEmpty()
                .WithMessage("Description cannot be empty.");

        RuleFor(x => x.Price)
            .GreaterThan(0)
                .WithMessage("Price must be greater than zero.");
    }
}

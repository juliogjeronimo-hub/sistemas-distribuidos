using FluentValidation;
using OrderHub.Catalog.Application.UseCases.Commands;

namespace OrderHub.Catalog.Application.UseCases.Validators;

internal sealed class UpdateProductCommandValidator : AbstractValidator<UpdateProductCommand>
{
    public UpdateProductCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
                .WithMessage("Product ID cannot be empty.");

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

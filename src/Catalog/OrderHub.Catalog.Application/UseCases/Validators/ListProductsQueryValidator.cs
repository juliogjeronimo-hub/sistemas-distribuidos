using FluentValidation;
using OrderHub.Catalog.Application.UseCases.Queries;

namespace OrderHub.Catalog.Application.UseCases.Validators;

internal sealed class ListProductsQueryValidator : AbstractValidator<ListProductsQuery>
{
    public ListProductsQueryValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(1)
                .WithMessage("Page must be greater than or equal to 1.");

        RuleFor(x => x.PageSize)
            .GreaterThanOrEqualTo(1)
                .WithMessage("PageSize must be greater than or equal to 1.")
            .LessThanOrEqualTo(50)
                .WithMessage("PageSize must be less than or equal to 50.");
    }
}

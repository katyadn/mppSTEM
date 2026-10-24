using FluentValidation;

using Microclimate.Contracts.Rooms;
using Microclimate.Domain.Rooms;

namespace Microclimate.Api.Validators;

public sealed class CreateRoomRequestValidator : AbstractValidator<CreateRoomRequest>
{
    public CreateRoomRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Назва кімнати обов'язкова.")
            .MaximumLength(Room.NameMaxLength).WithMessage($"Назва кімнати не може перевищувати {Room.NameMaxLength} символів.");

        RuleFor(x => x.Floor)
            .InclusiveBetween(Room.MinFloor, Room.MaxFloor)
            .WithMessage($"Поверх має бути в межах від {Room.MinFloor} до {Room.MaxFloor}.");

        RuleFor(x => x.AreaSqM)
            .GreaterThan(0).When(x => x.AreaSqM.HasValue)
            .WithMessage("Площа кімнати має бути додатною.");
    }
}

public sealed class UpdateRoomRequestValidator : AbstractValidator<UpdateRoomRequest>
{
    public UpdateRoomRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Назва кімнати обов'язкова.")
            .MaximumLength(Room.NameMaxLength).WithMessage($"Назва кімнати не може перевищувати {Room.NameMaxLength} символів.");

        RuleFor(x => x.Floor)
            .InclusiveBetween(Room.MinFloor, Room.MaxFloor)
            .WithMessage($"Поверх має бути в межах від {Room.MinFloor} до {Room.MaxFloor}.");

        RuleFor(x => x.AreaSqM)
            .GreaterThan(0).When(x => x.AreaSqM.HasValue)
            .WithMessage("Площа кімнати має бути додатною.");
    }
}

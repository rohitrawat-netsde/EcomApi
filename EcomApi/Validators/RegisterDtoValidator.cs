using EcomApi.DTOs;
using FluentValidation;

namespace EcomApi.Validators
{
    public class RegisterDtoValidator : AbstractValidator<RegisterDto>
    {
        public RegisterDtoValidator()
        {
            RuleFor(x => x.Id).NotEmpty().MaximumLength(100);
            RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
            RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
            RuleFor(x => x.Photo).NotEmpty().MaximumLength(1024);
            RuleFor(x => x.Gender).NotEmpty().Must(g => g is "male" or "female")
                .WithMessage("Gender must be 'male' or 'female'");
            RuleFor(x => x.Dob).NotEmpty().LessThan(DateTime.UtcNow.AddYears(-5)); // min age 5+
            RuleFor(x => x.Password)
                .NotEmpty()
                .MinimumLength(8)
                .Matches("[A-Z]").WithMessage("Password must contain uppercase")
                .Matches("[a-z]").WithMessage("Password must contain lowercase")
                .Matches("[0-9]").WithMessage("Password must contain a digit")
                .Matches("[^a-zA-Z0-9]").WithMessage("Password must contain a symbol");
        }
    }
}

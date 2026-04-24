using FluentValidation;
using Microsoft.AspNetCore.Identity;
using VendingIot.Models;
using VendingIot.Models.DTO;

namespace VendingIot.Validators;

public class LoginValidator : AbstractValidator<LoginDTO>
{
    public LoginValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email wajib diisi.")
            .EmailAddress().WithMessage("Format email tidak valid.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password wajib diisi.");
    }
}


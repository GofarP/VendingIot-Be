using FluentValidation;

public class UpdateProfileValidator : AbstractValidator<UpdateProfileDTO>
{
    public UpdateProfileValidator()
    {
        RuleFor(x => x.FullName)
        .NotEmpty().WithMessage("Nama Lengkap wajib diisi")
                    .MaximumLength(100).WithMessage("Nama Lengkap maksimal 100 karakter.");

        RuleFor(x => x.Email)
        .NotEmpty().WithMessage("Email wajib diisi.")
        .EmailAddress().WithMessage("Format email tidak valid.");

    }
}
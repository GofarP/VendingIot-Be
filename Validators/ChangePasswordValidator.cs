using FluentValidation;

public class ChangePasswordValidator : AbstractValidator<ChangePasswordDTO>
{
    public ChangePasswordValidator()
    {
        RuleFor(x => x.OldPassword)
        .NotEmpty().WithMessage("Password Lama Wajib Diisi");

        RuleFor(x => x.NewPassword)
        .NotEmpty().WithMessage("Password baru wajib diisi.")
        .MinimumLength(8).WithMessage("Password baru minimal harus 8 karakter.")
        .Matches("[A-Z]").WithMessage("Password baru harus mengandung setidaknya satu huruf besar.")
        .Matches("[a-z]").WithMessage("Password baru harus mengandung setidaknya satu huruf kecil.")
        .Matches("[0-9]").WithMessage("Password baru harus mengandung setidaknya satu angka.")
        .NotEqual(x => x.OldPassword).WithMessage("Password baru tidak boleh sama dengan password lama.");

        RuleFor(x => x.ConfirmPassword)
        .NotEmpty().WithMessage("Konfirmasi password wajib diisi.")
        .Equal(x => x.NewPassword).WithMessage("Konfirmasi password tidak cocok dengan password baru.");

    }
}
using CsvReader.Models.CsvFile;
using FluentValidation;


namespace CsvReader.Validators
{
    public class UploadFileRequestValidator : AbstractValidator<UploadFileRequest>
    {
        private readonly string[] _permittedExtensions = {  ".csv" };
        private const long _maxFileBytes = 10 * 1024 * 1024;

        public UploadFileRequestValidator()
        {
            RuleFor(x => x.File)
                .NotNull()
                .WithMessage("Please upload a file.")
                .Must(f => f!.Length > 0)
                  .WithMessage("The file is empty.")
                .Must(f => f!.Length <= _maxFileBytes)
                  .WithMessage($"File too large. Maximum allowed is {_maxFileBytes / 1024 / 1024} MB.")
                .Must(f => _permittedExtensions
                    .Contains(Path.GetExtension(f!.FileName).ToLowerInvariant()))
                  .WithMessage("Unsupported file type. Only .xls and .xlsx are allowed.");

        }
    }
}

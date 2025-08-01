namespace OneSwiss.V8;

public record ProcessResult(
    string Output,
    string Error,
    int ExitCode);
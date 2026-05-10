using System;
using System.IO;

namespace Adliance.QmDoc;

public class ProcessorError(string filePath, string errorMessage, bool isWarningOnly = false)
{
    public string FileName { get; } = Path.GetFileName(filePath);
    public string FilePath { get; } = filePath;
    public string ErrorMessage { get; } = errorMessage;
    public bool IsWarningOnly { get; } = isWarningOnly;

    public override bool Equals(object? obj)
    {
        if (obj != null && obj is ProcessorError other)
        {
            return Equals(other);
        }

        return false;
    }

    private bool Equals(ProcessorError other)
    {
        return FileName == other.FileName && FilePath == other.FilePath && ErrorMessage == other.ErrorMessage && IsWarningOnly == other.IsWarningOnly;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(FileName, FilePath, ErrorMessage, IsWarningOnly);
    }
}

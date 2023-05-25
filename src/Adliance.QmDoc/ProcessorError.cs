using System;
using System.IO;

namespace Adliance.QmDoc;

public class ProcessorError
{
    public ProcessorError(string filePath, string errorMessage, bool isWarningOnly = false)
    {
        FilePath = filePath;
        FileName = Path.GetFileName(filePath);
        ErrorMessage = errorMessage;
        IsWarningOnly = isWarningOnly;
    }

    public string FileName { get; }
    public string FilePath { get; }
    public string ErrorMessage { get; }
    public bool IsWarningOnly { get; }

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
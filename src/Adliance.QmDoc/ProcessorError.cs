using System.IO;

namespace Adliance.QmDoc
{
    public class ProcessorError
    {
        public ProcessorError(string filePath, string errorMessage, bool isWarningOnly = false)
        {
            FilePath = filePath;
            FileName = Path.GetFileName(filePath);
            ErrorMessage = errorMessage;
            IsWarningOnly = isWarningOnly;
        }

        public string FileName { get; set; }
        public string FilePath { get; set; }
        public string ErrorMessage { get; set; }
        public bool IsWarningOnly { get; set; }
    }
}

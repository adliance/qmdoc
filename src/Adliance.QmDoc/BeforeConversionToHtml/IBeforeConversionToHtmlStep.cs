using System;
using System.Collections.Generic;

namespace Adliance.QmDoc.BeforeConversionToHtml
{
    internal interface IBeforeConversionToHtmlStep
    {
        Result Apply(string markdown, Context context);
    }

    public class Result
    {
        public Result(string resultingMarkdown, Context context)
        {
            Context = context;
            ResultingMarkdown = resultingMarkdown;
        }

        public string ResultingMarkdown { get; set; }
        public IList<ProcessorError> Errors { get; set; } = new List<ProcessorError>();
        public Context Context { get; set; }
    }

    public class Context
    {
        public IList<LinkedDocument> LinkedDocuments { get; set; } = new List<LinkedDocument>();
    }

    public class LinkedDocument
    {
        public LinkedDocument(string fileName, string niceName)
        {
            FileName = fileName;
            NiceName = niceName;
        }

        public string FileName { get; set; }
        public string NiceName { get; set; }

        public override bool Equals(object? obj)
        {
            if (obj is LinkedDocument o)
            {
                return (FileName + NiceName).Equals(o.FileName + o.NiceName, StringComparison.OrdinalIgnoreCase);
            }

            return false;
        }

        public override int GetHashCode()
        {
            return (FileName + NiceName).ToLower().GetHashCode();
        }
    }
}

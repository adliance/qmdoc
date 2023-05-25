﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Adliance.QmDoc.AfterConversionToHtml;

public class ConfigurablePlaceholders : IAfterConversionToHtmlStep
{
    private readonly IDictionary<string, string> _placeholders = new Dictionary<string, string>();

    public ConfigurablePlaceholders(string sourceFileName, string pathToJsonFile)
    {
        if (!Path.IsPathRooted(pathToJsonFile)) pathToJsonFile = Path.Combine(Path.GetDirectoryName(sourceFileName)!, pathToJsonFile);
            
        var file = new FileInfo(pathToJsonFile);
         
        if (file.Exists)
        {
            try
            {
                _placeholders = JsonSerializer.Deserialize<IDictionary<string, string>>(File.ReadAllText(file.FullName)) ?? throw new Exception("Unable to deserialize placeholders file.");
            }
            catch (Exception ex)
            {
                throw new Exception($"Unable to read {file.FullName}: {ex.Message}");
            }
        }
    }

    public Result Apply(string html)
    {
        var result = html;
        foreach (var (placeholder, value) in _placeholders)
        {
            result = Regex.Replace(result, @"\{?\{\W*" + placeholder + @"\W*\}\}?", value, RegexOptions.IgnoreCase);
        }

        return new Result(result);
    }
}
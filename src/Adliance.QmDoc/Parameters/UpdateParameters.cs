using CommandLine;

// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global
// ReSharper disable ClassNeverInstantiated.Global

namespace Adliance.QmDoc.Parameters;

[Verb("update", HelpText = "Updates to the latest version, by using the dotnet tools update command.")]
public class UpdateParameters
{
}
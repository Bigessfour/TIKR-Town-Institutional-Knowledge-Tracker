namespace TIKR.Infrastructure.Services;

/// <summary>
/// Local folder guess used before the chat model. A confident hit skips the generate call.
/// </summary>
public interface IFolderClassifier
{
    FolderPrediction? Classify(string fileName, string? content, string? libraryRelativePath);
}

public readonly record struct FolderPrediction(string Folder, double Confidence, bool IsConfident);

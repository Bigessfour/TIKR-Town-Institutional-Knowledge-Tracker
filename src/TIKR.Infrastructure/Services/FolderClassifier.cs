using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace TIKR.Infrastructure.Services;

/// <summary>
/// Scores the JSON linear model produced by <c>scripts/train_folder_classifier.py</c>.
/// No Python process and no GPU. A missing file leaves tagging on the chat model.
/// </summary>
public sealed partial class FolderClassifier : IFolderClassifier
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly Model _model;
    private readonly Dictionary<string, int> _vocabulary;

    private FolderClassifier(Model model)
    {
        _model = model;
        _vocabulary = new Dictionary<string, int>(model.Vocabulary.Length, StringComparer.Ordinal);
        for (var i = 0; i < model.Vocabulary.Length; i++)
            _vocabulary[model.Vocabulary[i]] = i;
    }

    public static IFolderClassifier Disabled { get; } = new DisabledClassifier();

    public static IFolderClassifier Load(string? path, ILogger? logger = null)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            return Disabled;

        try
        {
            var json = File.ReadAllText(path);
            var model = JsonSerializer.Deserialize<Model>(json, JsonOptions)
                ?? throw new InvalidDataException("Folder classifier JSON was empty.");
            if (model.Version != 1
                || model.Labels.Length == 0
                || model.Vocabulary.Length != model.Idf.Length
                || model.Coefficients.Length != model.Labels.Length
                || model.Intercept.Length != model.Labels.Length)
            {
                throw new InvalidDataException("Folder classifier JSON is missing weights.");
            }

            return new FolderClassifier(model);
        }
        catch (Exception ex) when (ex is JsonException or InvalidDataException or IOException)
        {
            logger?.LogWarning(ex, "Folder classifier at {Path} failed to load. Tagging stays on the chat model.", path);
            return Disabled;
        }
    }

    public FolderPrediction? Classify(string fileName, string? content, string? libraryRelativePath)
    {
        var text = string.Join(
            '\n',
            new[] { libraryRelativePath, fileName, content }.Where(part => !string.IsNullOrWhiteSpace(part)));
        if (string.IsNullOrWhiteSpace(text))
            return null;

        var features = new double[_model.Vocabulary.Length];
        var seen = false;
        foreach (Match match in TokenPattern().Matches(text.ToLowerInvariant()))
        {
            if (!_vocabulary.TryGetValue(match.Value, out var index))
                continue;
            features[index] += _model.Idf[index];
            seen = true;
        }

        if (!seen)
            return null;

        double sumSquares = 0;
        for (var i = 0; i < features.Length; i++)
            sumSquares += features[i] * features[i];
        var norm = Math.Sqrt(sumSquares);
        if (norm == 0)
            return null;
        for (var i = 0; i < features.Length; i++)
            features[i] /= norm;

        var scores = new double[_model.Labels.Length];
        var max = double.NegativeInfinity;
        for (var label = 0; label < scores.Length; label++)
        {
            var score = _model.Intercept[label];
            var coefficients = _model.Coefficients[label];
            for (var i = 0; i < features.Length; i++)
            {
                if (features[i] != 0)
                    score += coefficients[i] * features[i];
            }

            scores[label] = score;
            if (score > max)
                max = score;
        }

        double sum = 0;
        for (var label = 0; label < scores.Length; label++)
        {
            scores[label] = Math.Exp(scores[label] - max);
            sum += scores[label];
        }

        for (var label = 0; label < scores.Length; label++)
            scores[label] /= sum;

        var best = 0;
        for (var label = 1; label < scores.Length; label++)
        {
            if (scores[label] > scores[best])
                best = label;
        }

        var runnerUp = 0.0;
        for (var label = 0; label < scores.Length; label++)
        {
            if (label != best && scores[label] > runnerUp)
                runnerUp = scores[label];
        }

        var confidence = scores[best];
        var margin = confidence - runnerUp;
        return new FolderPrediction(
            _model.Labels[best],
            confidence,
            confidence >= _model.Threshold && margin >= _model.MinMargin);
    }

    [GeneratedRegex(@"\b[\p{L}\p{N}_]{2,}\b", RegexOptions.CultureInvariant)]
    private static partial Regex TokenPattern();

    private sealed class DisabledClassifier : IFolderClassifier
    {
        public FolderPrediction? Classify(string fileName, string? content, string? libraryRelativePath) => null;
    }

    private sealed class Model
    {
        public int Version { get; set; }
        public string[] Labels { get; set; } = [];
        public string[] Vocabulary { get; set; } = [];
        public double[] Idf { get; set; } = [];
        [JsonPropertyName("coefficients")]
        public double[][] Coefficients { get; set; } = [];
        public double[] Intercept { get; set; } = [];
        public double Threshold { get; set; } = 0.40;
        public double MinMargin { get; set; } = 0.20;
    }
}

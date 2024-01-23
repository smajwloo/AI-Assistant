using System.IO.Compression;
using System.Text.RegularExpressions;

namespace aia_api.Application.Helpers;

public class CommentChecker
{
    private readonly ILogger<CommentChecker> _logger;
    private readonly List<string> _logs;
    private ZipArchiveEntry _file;
    private MatchCollection _allComments;
    private MatchCollection _eslintComments;

    public CommentChecker(ILogger<CommentChecker> logger)
    {
        _logger = logger;
        _logs = new List<string>();
    }

    public void LogLogsAndClear()
    {
        var logs = string.Join("\n", _logs);
        _logger.LogInformation(logs);
        _logs.Clear();
    }

    public bool HasComments(ZipArchiveEntry zipArchiveEntry, string fileExtension)
    {
        _file = zipArchiveEntry;
        switch (fileExtension)
        {
            case ".ts":
                var detectCommentsPattern = @"((?<=\s|^)\/\/[^\n]*|\/\*[\s\S]*?\*\/|\/\*\*[\s\S]*?\*\/)";
                var detectEslintCommentsPattern = 
                    @"(\/\/.*eslint-.*|\/\*[\s\S]*?eslint-[\s\S]*?\*\/|\/\*\*[\s\S]*?eslint-[\s\S]*?\*\/)";
                return FileHasComments(detectCommentsPattern, detectEslintCommentsPattern);
            default:
                _logger.LogDebug("File extension {extension} not supported.", fileExtension);
                return false;
        }
    }

    private bool FileHasComments(string allCommentPattern, string eslintCommentPattern)
    {
        var fileContent = ReadFileContent(_file);
        _allComments = FindMatches(fileContent, allCommentPattern);
        _eslintComments = FindMatches(fileContent, eslintCommentPattern);
        
        return AnalyzeComments();
    }

    private string ReadFileContent(ZipArchiveEntry file)
    {
        using var reader = new StreamReader(file.Open());
        return reader.ReadToEnd();
    }

    private MatchCollection FindMatches(string text, string pattern)
    {
        return Regex.Matches(text, pattern, RegexOptions.Multiline);
    }

    private bool AnalyzeComments()
    {
        if (_allComments.Count == 0)
        {
            Log($"{_file.Name} does not contain comments.");
            return false;
        }

        if (IsOnlyTypeOfComment(_eslintComments, "eslint"))
            return false;

        LogComments(_allComments);
        return true;
    }

    private bool IsOnlyTypeOfComment(MatchCollection specificComments, string commentType)
    {
        if (_allComments.Count <= specificComments.Count)
        {
            Log($"Found only {commentType} comments in {_file.FullName}. Skipping.");
            return true;
        }

        return false;
    }

    private void LogComments(MatchCollection comments)
    {
        Log($"Found comments in {_file.FullName}.");
        for (var index = 0; index < comments.Count; index++)
        {
            var match = comments[index];
            Log($"Comment {index}: {match.Value}");
        }
    }

    private void Log(string message)
    {
        _logs.Add(message);
    }
}

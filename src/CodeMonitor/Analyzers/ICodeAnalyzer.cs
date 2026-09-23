using System.Collections.Generic;
using CodeMonitor.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CodeMonitor.Analyzers
{
    public interface ICodeAnalyzer
    {
        string RuleId { get; }
        string RuleName { get; }
        IEnumerable<Violation> Analyze(SyntaxTree tree, string filePath, QualityConfig config, ISet<int>? changedLines = null);
    }
}

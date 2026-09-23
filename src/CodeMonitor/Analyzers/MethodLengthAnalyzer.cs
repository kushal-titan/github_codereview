using System.Collections.Generic;
using System.Linq;
using CodeMonitor.Knowledge;
using CodeMonitor.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CodeMonitor.Analyzers
{
    public class MethodLengthAnalyzer : ICodeAnalyzer
    {
        public string RuleId => "CQ001";
        public string RuleName => "Method Length Exceeded";

        public IEnumerable<Violation> Analyze(SyntaxTree tree, string filePath, QualityConfig config, ISet<int>? changedLines = null)
        {
            var root = tree.GetRoot();
            var methodNodes = root.DescendantNodes().OfType<BaseMethodDeclarationSyntax>();

            foreach (var method in methodNodes)
            {
                var lineSpan = method.GetLocation().GetLineSpan();
                int startLine = lineSpan.StartLinePosition.Line + 1;
                int endLine = lineSpan.EndLinePosition.Line + 1;
                int lineCount = (endLine - startLine) + 1;

                // Check if this method is in the scope of modified lines (if git diff provided)
                if (changedLines != null && changedLines.Count > 0)
                {
                    bool isModified = Enumerable.Range(startLine, lineCount).Any(line => changedLines.Contains(line));
                    if (!isModified)
                    {
                        continue;
                    }
                }

                if (lineCount > config.MaxMethodLines)
                {
                    string methodName = method is MethodDeclarationSyntax m
                        ? m.Identifier.Text
                        : (method is ConstructorDeclarationSyntax c ? c.Identifier.Text : "AnonymousMethod");

                    var (rationale, recommendation) = RecommendationEngine.GetMethodLengthAdvice(methodName, lineCount, config.MaxMethodLines);

                    yield return new Violation
                    {
                        RuleId = RuleId,
                        RuleName = RuleName,
                        TargetFile = filePath,
                        MemberName = methodName,
                        LineNumber = startLine,
                        EndLineNumber = endLine,
                        ActualValue = lineCount,
                        ThresholdValue = config.MaxMethodLines,
                        Severity = lineCount > (config.MaxMethodLines * 1.5) ? ViolationSeverity.Error : ViolationSeverity.Warning,
                        Description = $"Method '{methodName}' is {lineCount} lines long (maximum allowed: {config.MaxMethodLines}).",
                        Rationale = rationale,
                        RecommendedFix = recommendation
                    };
                }
            }
        }
    }
}

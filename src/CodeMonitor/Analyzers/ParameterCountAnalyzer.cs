using System.Collections.Generic;
using System.Linq;
using CodeMonitor.Knowledge;
using CodeMonitor.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CodeMonitor.Analyzers
{
    public class ParameterCountAnalyzer : ICodeAnalyzer
    {
        public string RuleId => "CQ003";
        public string RuleName => "Excessive Parameter Count";

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

                if (changedLines != null && changedLines.Count > 0)
                {
                    bool isModified = Enumerable.Range(startLine, lineCount).Any(line => changedLines.Contains(line));
                    if (!isModified)
                    {
                        continue;
                    }
                }

                int paramCount = method.ParameterList?.Parameters.Count ?? 0;

                if (paramCount > config.MaxParameterCount)
                {
                    string methodName = method is MethodDeclarationSyntax m
                        ? m.Identifier.Text
                        : (method is ConstructorDeclarationSyntax c ? c.Identifier.Text : "AnonymousMethod");

                    var (rationale, recommendation) = RecommendationEngine.GetParameterCountAdvice(methodName, paramCount, config.MaxParameterCount);

                    yield return new Violation
                    {
                        RuleId = RuleId,
                        RuleName = RuleName,
                        TargetFile = filePath,
                        MemberName = methodName,
                        LineNumber = startLine,
                        EndLineNumber = endLine,
                        ActualValue = paramCount,
                        ThresholdValue = config.MaxParameterCount,
                        Severity = paramCount > (config.MaxParameterCount + 2) ? ViolationSeverity.Error : ViolationSeverity.Warning,
                        Description = $"Method '{methodName}' accepts {paramCount} parameters (maximum allowed: {config.MaxParameterCount}).",
                        Rationale = rationale,
                        RecommendedFix = recommendation
                    };
                }
            }
        }
    }
}

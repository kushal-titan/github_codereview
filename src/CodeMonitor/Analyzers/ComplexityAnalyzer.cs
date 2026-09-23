using System.Collections.Generic;
using System.Linq;
using CodeMonitor.Knowledge;
using CodeMonitor.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CodeMonitor.Analyzers
{
    public class ComplexityAnalyzer : ICodeAnalyzer
    {
        public string RuleId => "CQ002";
        public string RuleName => "High Cyclomatic Complexity";

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

                int complexity = CalculateComplexity(method);

                if (complexity > config.MaxCyclomaticComplexity)
                {
                    string methodName = method is MethodDeclarationSyntax m
                        ? m.Identifier.Text
                        : (method is ConstructorDeclarationSyntax c ? c.Identifier.Text : "AnonymousMethod");

                    var (rationale, recommendation, steps, example) = RecommendationEngine.GetComplexityAdvice(methodName, complexity, config.MaxCyclomaticComplexity);

                    yield return new Violation
                    {
                        RuleId = RuleId,
                        RuleName = RuleName,
                        TargetFile = filePath,
                        MemberName = methodName,
                        LineNumber = startLine,
                        EndLineNumber = endLine,
                        ActualValue = complexity,
                        ThresholdValue = config.MaxCyclomaticComplexity,
                        Severity = complexity > (config.MaxCyclomaticComplexity * 1.5) ? ViolationSeverity.Error : ViolationSeverity.Warning,
                        Description = $"Method '{methodName}' has a cyclomatic complexity of {complexity} (maximum allowed: {config.MaxCyclomaticComplexity}).",
                        Rationale = rationale,
                        RecommendedFix = recommendation,
                        ActionSteps = steps,
                        CodeExample = example
                    };
                }
            }
        }

        private static int CalculateComplexity(SyntaxNode methodNode)
        {
            int complexity = 1; // Base complexity

            foreach (var node in methodNode.DescendantNodes())
            {
                switch (node.Kind())
                {
                    case SyntaxKind.IfStatement:
                    case SyntaxKind.WhileStatement:
                    case SyntaxKind.ForStatement:
                    case SyntaxKind.ForEachStatement:
                    case SyntaxKind.DoStatement:
                    case SyntaxKind.CaseSwitchLabel:
                    case SyntaxKind.CasePatternSwitchLabel:
                    case SyntaxKind.CatchClause:
                    case SyntaxKind.ConditionalExpression:
                    case SyntaxKind.CoalesceExpression:
                    case SyntaxKind.CoalesceAssignmentExpression:
                        complexity++;
                        break;

                    case SyntaxKind.LogicalAndExpression:
                    case SyntaxKind.LogicalOrExpression:
                        complexity++;
                        break;
                }
            }

            return complexity;
        }
    }
}

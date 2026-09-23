using System.Collections.Generic;
using System.Linq;
using CodeMonitor.Knowledge;
using CodeMonitor.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CodeMonitor.Analyzers
{
    public class NestingDepthAnalyzer : ICodeAnalyzer
    {
        public string RuleId => "CQ004";
        public string RuleName => "Deep Control Flow Nesting";

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

                int maxDepth = GetMaxNestingDepth(method, 0);

                if (maxDepth > config.MaxNestingDepth)
                {
                    string methodName = method is MethodDeclarationSyntax m
                        ? m.Identifier.Text
                        : (method is ConstructorDeclarationSyntax c ? c.Identifier.Text : "AnonymousMethod");

                    var (rationale, recommendation, steps, example) = RecommendationEngine.GetNestingDepthAdvice(methodName, maxDepth, config.MaxNestingDepth);

                    yield return new Violation
                    {
                        RuleId = RuleId,
                        RuleName = RuleName,
                        TargetFile = filePath,
                        MemberName = methodName,
                        LineNumber = startLine,
                        EndLineNumber = endLine,
                        ActualValue = maxDepth,
                        ThresholdValue = config.MaxNestingDepth,
                        Severity = maxDepth > (config.MaxNestingDepth + 1) ? ViolationSeverity.Error : ViolationSeverity.Warning,
                        Description = $"Method '{methodName}' has a maximum nesting depth of {maxDepth} (maximum allowed: {config.MaxNestingDepth}).",
                        Rationale = rationale,
                        RecommendedFix = recommendation,
                        ActionSteps = steps,
                        CodeExample = example
                    };
                }
            }
        }

        private static int GetMaxNestingDepth(SyntaxNode node, int currentDepth)
        {
            int max = currentDepth;

            foreach (var child in node.ChildNodes())
            {
                int nextDepth = currentDepth;
                if (IsNestingBlock(child))
                {
                    nextDepth++;
                }

                int childMax = GetMaxNestingDepth(child, nextDepth);
                if (childMax > max)
                {
                    max = childMax;
                }
            }

            return max;
        }

        private static bool IsNestingBlock(SyntaxNode node)
        {
            return node is IfStatementSyntax
                || node is WhileStatementSyntax
                || node is ForStatementSyntax
                || node is ForEachStatementSyntax
                || node is DoStatementSyntax
                || node is SwitchStatementSyntax
                || node is TryStatementSyntax;
        }
    }
}

using System.Collections.Generic;
using System.Linq;
using CodeMonitor.Knowledge;
using CodeMonitor.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CodeMonitor.Analyzers
{
    /// <summary>
    /// Category 1: Structural Complexity
    /// // [SonarQube: S134] [Clean Code: Arrow Anti-Pattern] Excessive Block Nesting Depth
    /// </summary>
    public class NestingDepthAnalyzer : ICodeAnalyzer
    {
        public string RuleId => "CQ004";
        public string RuleName => "Deep Nesting Depth";

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

                // // [SonarQube: S134] Check maximum statement nesting depth
                int maxDepth = CalculateMaxNesting(method);

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
                        Description = $"Method '{methodName}' has a statement nesting depth of {maxDepth} (maximum allowed: {config.MaxNestingDepth}).",
                        Rationale = rationale,
                        RecommendedFix = recommendation,
                        ActionSteps = steps,
                        CodeExample = example
                    };
                }
            }
        }

        private static int CalculateMaxNesting(SyntaxNode methodNode)
        {
            int maxDepth = 0;
            var blocks = methodNode.DescendantNodes().OfType<BlockSyntax>();

            foreach (var block in blocks)
            {
                int currentDepth = 0;
                var current = block.Parent;

                while (current != null && current != methodNode)
                {
                    if (current is BlockSyntax)
                    {
                        currentDepth++;
                    }
                    current = current.Parent;
                }

                if (currentDepth > maxDepth)
                {
                    maxDepth = currentDepth;
                }
            }

            return maxDepth;
        }
    }
}

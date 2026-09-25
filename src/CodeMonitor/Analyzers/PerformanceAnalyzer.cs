using System.Collections.Generic;
using System.Linq;
using CodeMonitor.Knowledge;
using CodeMonitor.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CodeMonitor.Analyzers
{
    public class PerformanceAnalyzer : ICodeAnalyzer
    {
        public string RuleId => "PERF000";
        public string RuleName => "Performance & Memory Optimization Analyzer";

        public IEnumerable<Violation> Analyze(SyntaxTree tree, string filePath, QualityConfig config, ISet<int>? changedLines = null)
        {
            var root = tree.GetRoot();

            // 1. PERF001: String Concatenation (+ or +=) inside Loops
            var loopNodes = root.DescendantNodes().Where(n =>
                n is ForStatementSyntax ||
                n is ForEachStatementSyntax ||
                n is WhileStatementSyntax ||
                n is DoStatementSyntax);

            foreach (var loop in loopNodes)
            {
                foreach (var assign in loop.DescendantNodes().OfType<AssignmentExpressionSyntax>())
                {
                    if (assign.IsKind(SyntaxKind.AddAssignmentExpression))
                    {
                        int line = assign.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
                        if (changedLines == null || changedLines.Contains(line))
                        {
                            var (rationale, recommendation, steps, example) = RecommendationEngine.GetStringInLoopAdvice(assign.ToString());
                            yield return new Violation
                            {
                                RuleId = "PERF001",
                                RuleName = "String Concatenation in Loop",
                                TargetFile = filePath,
                                MemberName = assign.ToString(),
                                LineNumber = line,
                                Severity = ViolationSeverity.Warning,
                                Description = "Repeated string concatenation (+=) inside a loop causes excessive GC memory allocations.",
                                Rationale = rationale,
                                RecommendedFix = recommendation,
                                ActionSteps = steps,
                                CodeExample = example
                            };
                        }
                    }
                }
            }

            // 2. PERF002: Boxing Allocations (Legacy non-generic collections: ArrayList, Hashtable)
            string[] legacyCollections = { "ArrayList", "Hashtable" };
            foreach (var creation in root.DescendantNodes().OfType<ObjectCreationExpressionSyntax>())
            {
                string typeName = creation.Type.ToString();
                if (legacyCollections.Any(c => typeName == c || typeName.EndsWith("." + c)))
                {
                    int line = creation.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
                    if (changedLines == null || changedLines.Contains(line))
                    {
                        var (rationale, recommendation, steps, example) = RecommendationEngine.GetBoxingAdvice(typeName);
                        yield return new Violation
                        {
                            RuleId = "PERF002",
                            RuleName = "Boxing Allocation (Non-Generic Collection)",
                            TargetFile = filePath,
                            MemberName = typeName,
                            LineNumber = line,
                            Severity = ViolationSeverity.Warning,
                            Description = $"Non-generic collection '{typeName}' boxes value types into objects, causing heap allocations and runtime overhead.",
                            Rationale = rationale,
                            RecommendedFix = recommendation,
                            ActionSteps = steps,
                            CodeExample = example
                        };
                    }
                }
            }

            // 3. PERF003: LINQ .Count() > 0 / .Count() == 0 Smell
            foreach (var binary in root.DescendantNodes().OfType<BinaryExpressionSyntax>())
            {
                string leftStr = binary.Left.ToString();
                string rightStr = binary.Right.ToString();

                bool isCountCall = leftStr.EndsWith(".Count()") || rightStr.EndsWith(".Count()");
                if (isCountCall)
                {
                    bool isComparisonToZero = leftStr == "0" || rightStr == "0" || leftStr == "1" || rightStr == "1";
                    if (isComparisonToZero)
                    {
                        int line = binary.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
                        if (changedLines == null || changedLines.Contains(line))
                        {
                            var (rationale, recommendation, steps, example) = RecommendationEngine.GetLinqCountAdvice(binary.ToString());
                            yield return new Violation
                            {
                                RuleId = "PERF003",
                                RuleName = "LINQ Count() Efficiency Anti-Pattern",
                                TargetFile = filePath,
                                MemberName = binary.ToString(),
                                LineNumber = line,
                                Severity = ViolationSeverity.Warning,
                                Description = $"Using '{binary}' enumerates the entire collection just to check emptiness.",
                                Rationale = rationale,
                                RecommendedFix = recommendation,
                                ActionSteps = steps,
                                CodeExample = example
                            };
                        }
                    }
                }
            }
        }
    }
}

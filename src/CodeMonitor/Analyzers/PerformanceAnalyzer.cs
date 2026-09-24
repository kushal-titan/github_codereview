using System.Collections.Generic;
using System.Linq;
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
            foreach (var loop in root.DescendantNodes().Where(n => n is ForStatementSyntax || n is ForEachStatementSyntax || n is WhileStatementSyntax || n is DoStatementSyntax))
            {
                foreach (var assign in loop.DescendantNodes().OfType<AssignmentExpressionSyntax>())
                {
                    if (assign.IsKind(SyntaxKind.AddAssignmentExpression))
                    {
                        int line = assign.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
                        if (changedLines == null || changedLines.Contains(line))
                        {
                            yield return new Violation
                            {
                                RuleId = "PERF001",
                                RuleName = "String Concatenation in Loop",
                                TargetFile = filePath,
                                MemberName = assign.ToString(),
                                LineNumber = line,
                                Severity = ViolationSeverity.Warning,
                                Description = "Repeated string concatenation (+=) inside a loop causes excessive GC memory allocations.",
                                RecommendedFix = "Use 'StringBuilder' to assemble strings efficiently in iterations."
                            };
                        }
                    }
                }
            }
        }
    }
}

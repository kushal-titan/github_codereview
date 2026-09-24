using System;
using System.Collections.Generic;
using System.Linq;
using CodeMonitor.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CodeMonitor.Analyzers
{
    public class RuntimeSafetyAnalyzer : ICodeAnalyzer
    {
        public string RuleId => "SAF000";
        public string RuleName => "Runtime Safety & Defect Analyzer";

        public IEnumerable<Violation> Analyze(SyntaxTree tree, string filePath, QualityConfig config, ISet<int>? changedLines = null)
        {
            var root = tree.GetRoot();

            // 1. SAF001: Potential Null Reference Dereference (Deep chained property access like a.b.c.d)
            foreach (var member in root.DescendantNodes().OfType<MemberAccessExpressionSyntax>())
            {
                // Only inspect the top-level outer chain
                if (member.Parent is MemberAccessExpressionSyntax)
                    continue;

                string expr = member.ToString();
                int dotCount = expr.Count(c => c == '.');

                // Flag deeply nested un-guarded property navigation (>= 3 dots e.g. order.Customer.Address.City)
                if (dotCount >= 3 && !expr.Contains("?.") && !expr.StartsWith("System.") && !expr.StartsWith("Microsoft."))
                {
                    int line = member.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
                    if (changedLines == null || changedLines.Contains(line))
                    {
                        yield return new Violation
                        {
                            RuleId = "SAF001",
                            RuleName = "Deep Member Dereferencing",
                            TargetFile = filePath,
                            MemberName = expr,
                            LineNumber = line,
                            Severity = ViolationSeverity.Warning,
                            Description = $"Deeply chained member access '{expr}' may throw NullReferenceException if an intermediate object is null.",
                            RecommendedFix = "Use safe navigation (e.g., obj?.Property?.SubProperty) or validate preceding objects before dereferencing."
                        };
                    }
                }
            }

            // 2. SAF002: Division by Zero Risk
            foreach (var binary in root.DescendantNodes().OfType<BinaryExpressionSyntax>())
            {
                if (binary.IsKind(SyntaxKind.DivideExpression) || binary.IsKind(SyntaxKind.ModuloExpression))
                {
                    if (binary.Right is LiteralExpressionSyntax lit && (lit.Token.ValueText == "0" || lit.Token.ValueText == "0.0"))
                    {
                        int line = binary.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
                        if (changedLines == null || changedLines.Contains(line))
                        {
                            yield return new Violation
                            {
                                RuleId = "SAF002",
                                RuleName = "Division by Zero Defect",
                                TargetFile = filePath,
                                MemberName = binary.ToString(),
                                LineNumber = line,
                                Severity = ViolationSeverity.Error,
                                Description = "Direct division or modulo by literal zero detected.",
                                RecommendedFix = "Add a guard clause to ensure the divisor is non-zero before division."
                            };
                        }
                    }
                }
            }

            // 3. SAF003: Unmanaged IDisposable Resource Leak (Missing 'using')
            foreach (var creation in root.DescendantNodes().OfType<ObjectCreationExpressionSyntax>())
            {
                string typeName = creation.Type.ToString();
                string[] disposableTypes = { "SqlConnection", "HttpClient", "FileStream", "MemoryStream", "StreamReader", "StreamWriter", "DbContext" };

                if (disposableTypes.Any(t => typeName.Contains(t)))
                {
                    bool isInsideUsing = creation.Ancestors().Any(a => a is UsingStatementSyntax || a is LocalDeclarationStatementSyntax l && l.UsingKeyword.IsKind(SyntaxKind.UsingKeyword));
                    if (!isInsideUsing)
                    {
                        int line = creation.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
                        if (changedLines == null || changedLines.Contains(line))
                        {
                            yield return new Violation
                            {
                                RuleId = "SAF003",
                                RuleName = "Resource Leak (Missing 'using' Statement)",
                                TargetFile = filePath,
                                MemberName = typeName,
                                LineNumber = line,
                                Severity = ViolationSeverity.Error,
                                Description = $"Disposable object '{typeName}' is created without a 'using' statement or disposal lifecycle.",
                                RecommendedFix = $"Wrap '{typeName}' in a 'using var' statement or 'using (...) {{ }}' block to prevent handle/connection leaks."
                            };
                        }
                    }
                }
            }
        }
    }
}

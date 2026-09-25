using System;
using System.Collections.Generic;
using System.Linq;
using CodeMonitor.Knowledge;
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

            // 1. SAF001: Potential Deep Null Reference Dereference (Deep chained property access like a.b.c.d)
            foreach (var member in root.DescendantNodes().OfType<MemberAccessExpressionSyntax>())
            {
                if (member.Parent is MemberAccessExpressionSyntax)
                    continue;

                string expr = member.ToString();
                int dotCount = expr.Count(c => c == '.');

                if (dotCount >= 3 && !expr.Contains("?.") && !expr.StartsWith("System.") && !expr.StartsWith("Microsoft."))
                {
                    int line = member.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
                    if (changedLines == null || changedLines.Contains(line))
                    {
                        var (rationale, recommendation, steps, example) = RecommendationEngine.GetNullDereferenceAdvice(expr);
                        yield return new Violation
                        {
                            RuleId = "SAF001",
                            RuleName = "Deep Member Dereferencing",
                            TargetFile = filePath,
                            MemberName = expr,
                            LineNumber = line,
                            Severity = ViolationSeverity.Warning,
                            Description = $"Deeply chained member access '{expr}' may throw NullReferenceException if an intermediate object is null.",
                            Rationale = rationale,
                            RecommendedFix = recommendation,
                            ActionSteps = steps,
                            CodeExample = example
                        };
                    }
                }
            }

            // 2. SAF002: Division / Modulo by Zero Defect
            foreach (var binary in root.DescendantNodes().OfType<BinaryExpressionSyntax>())
            {
                if (binary.IsKind(SyntaxKind.DivideExpression) || binary.IsKind(SyntaxKind.ModuloExpression))
                {
                    if (binary.Right is LiteralExpressionSyntax lit && (lit.Token.ValueText == "0" || lit.Token.ValueText == "0.0" || lit.Token.ValueText == "0f" || lit.Token.ValueText == "0m"))
                    {
                        int line = binary.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
                        if (changedLines == null || changedLines.Contains(line))
                        {
                            var (rationale, recommendation, steps, example) = RecommendationEngine.GetDivisionByZeroAdvice(binary.ToString());
                            yield return new Violation
                            {
                                RuleId = "SAF002",
                                RuleName = "Division by Zero Defect",
                                TargetFile = filePath,
                                MemberName = binary.ToString(),
                                LineNumber = line,
                                Severity = ViolationSeverity.Error,
                                Description = $"Direct division or modulo by literal zero detected in '{binary}'.",
                                Rationale = rationale,
                                RecommendedFix = recommendation,
                                ActionSteps = steps,
                                CodeExample = example
                            };
                        }
                    }
                }
            }

            // 3. SAF003: Unmanaged IDisposable Resource Leak (Missing 'using')
            string[] disposableTypes = { "SqlConnection", "HttpClient", "FileStream", "MemoryStream", "StreamReader", "StreamWriter", "DbContext", "Socket", "TcpClient", "SqlCommand" };
            foreach (var creation in root.DescendantNodes().OfType<ObjectCreationExpressionSyntax>())
            {
                string typeName = creation.Type.ToString();
                if (disposableTypes.Any(t => typeName.Contains(t)))
                {
                    bool isInsideUsing = creation.Ancestors().Any(a =>
                        a is UsingStatementSyntax ||
                        (a is LocalDeclarationStatementSyntax l && l.UsingKeyword.IsKind(SyntaxKind.UsingKeyword)));

                    if (!isInsideUsing)
                    {
                        int line = creation.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
                        if (changedLines == null || changedLines.Contains(line))
                        {
                            var (rationale, recommendation, steps, example) = RecommendationEngine.GetResourceLeakAdvice(typeName);
                            yield return new Violation
                            {
                                RuleId = "SAF003",
                                RuleName = "Resource Leak (Missing 'using' Statement)",
                                TargetFile = filePath,
                                MemberName = typeName,
                                LineNumber = line,
                                Severity = ViolationSeverity.Error,
                                Description = $"Disposable object '{typeName}' is created without a 'using' statement or disposal lifecycle.",
                                Rationale = rationale,
                                RecommendedFix = recommendation,
                                ActionSteps = steps,
                                CodeExample = example
                            };
                        }
                    }
                }
            }

            // 4. SAF004: Array Bounds Violation / Off-by-One Loops
            // 4a. Negative index literal
            foreach (var elem in root.DescendantNodes().OfType<ElementAccessExpressionSyntax>())
            {
                var arg = elem.ArgumentList.Arguments.FirstOrDefault();
                if (arg?.Expression is PrefixUnaryExpressionSyntax prefix && prefix.IsKind(SyntaxKind.UnaryMinusExpression))
                {
                    int line = elem.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
                    if (changedLines == null || changedLines.Contains(line))
                    {
                        var (rationale, recommendation, steps, example) = RecommendationEngine.GetArrayBoundsAdvice(elem.ToString(), "negative index access");
                        yield return new Violation
                        {
                            RuleId = "SAF004",
                            RuleName = "Array Bounds Violation (Negative Index)",
                            TargetFile = filePath,
                            MemberName = elem.ToString(),
                            LineNumber = line,
                            Severity = ViolationSeverity.Error,
                            Description = $"Negative index detected in element access '{elem}'.",
                            Rationale = rationale,
                            RecommendedFix = recommendation,
                            ActionSteps = steps,
                            CodeExample = example
                        };
                    }
                }
            }

            // 4b. Off-by-one loop condition: for (int i = 0; i <= arr.Length; i++)
            foreach (var forStmt in root.DescendantNodes().OfType<ForStatementSyntax>())
            {
                if (forStmt.Condition is BinaryExpressionSyntax cond && cond.IsKind(SyntaxKind.LessThanOrEqualExpression))
                {
                    string rightExpr = cond.Right.ToString();
                    if (rightExpr.EndsWith(".Length") || rightExpr.EndsWith(".Count"))
                    {
                        int line = forStmt.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
                        if (changedLines == null || changedLines.Contains(line))
                        {
                            var (rationale, recommendation, steps, example) = RecommendationEngine.GetArrayBoundsAdvice(forStmt.Condition.ToString(), "off-by-one '<=' with Length/Count");
                            yield return new Violation
                            {
                                RuleId = "SAF004",
                                RuleName = "Off-by-One Loop Boundary Violation",
                                TargetFile = filePath,
                                MemberName = forStmt.Condition.ToString(),
                                LineNumber = line,
                                Severity = ViolationSeverity.Error,
                                Description = $"Loop condition '{cond}' uses '<=' with Length/Count, which will cause IndexOutOfRangeException on the final iteration.",
                                Rationale = rationale,
                                RecommendedFix = recommendation,
                                ActionSteps = steps,
                                CodeExample = example
                            };
                        }
                    }
                }
            }

            // 5. SAF005: Empty Catch Block (Swallowed Exceptions)
            foreach (var catchClause in root.DescendantNodes().OfType<CatchClauseSyntax>())
            {
                var statements = catchClause.Block.Statements;
                bool isEmpty = statements.Count == 0 || statements.All(s => s is EmptyStatementSyntax);

                if (isEmpty)
                {
                    int line = catchClause.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
                    if (changedLines == null || changedLines.Contains(line))
                    {
                        string excType = catchClause.Declaration?.Type.ToString() ?? "Exception";
                        var (rationale, recommendation, steps, example) = RecommendationEngine.GetEmptyCatchAdvice(excType);
                        yield return new Violation
                        {
                            RuleId = "SAF005",
                            RuleName = "Empty Catch Block (Swallowed Exception)",
                            TargetFile = filePath,
                            MemberName = $"catch ({excType})",
                            LineNumber = line,
                            Severity = ViolationSeverity.Warning,
                            Description = $"Empty catch block catches '{excType}' without handling, logging, or rethrowing.",
                            Rationale = rationale,
                            RecommendedFix = recommendation,
                            ActionSteps = steps,
                            CodeExample = example
                        };
                    }
                }
            }

            // 6. SAF006: Unreachable / Dead Code
            foreach (var block in root.DescendantNodes().OfType<BlockSyntax>())
            {
                var stmts = block.Statements;
                for (int i = 0; i < stmts.Count - 1; i++)
                {
                    var current = stmts[i];
                    bool isUnconditionalExit = current is ReturnStatementSyntax ||
                                              current is ThrowStatementSyntax ||
                                              current is BreakStatementSyntax ||
                                              current is ContinueStatementSyntax;

                    if (isUnconditionalExit)
                    {
                        var nextStmt = stmts[i + 1];
                        int line = nextStmt.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
                        if (changedLines == null || changedLines.Contains(line))
                        {
                            var (rationale, recommendation, steps, example) = RecommendationEngine.GetUnreachableCodeAdvice(nextStmt.ToString());
                            yield return new Violation
                            {
                                RuleId = "SAF006",
                                RuleName = "Unreachable Dead Code",
                                TargetFile = filePath,
                                MemberName = nextStmt.ToString(),
                                LineNumber = line,
                                Severity = ViolationSeverity.Warning,
                                Description = $"Statement '{nextStmt.ToString().TrimEnd(';')}' will never be executed because it appears after an unconditional {current.Kind().ToString().Replace("Statement", "")}.",
                                Rationale = rationale,
                                RecommendedFix = recommendation,
                                ActionSteps = steps,
                                CodeExample = example
                            };
                        }
                        break;
                    }
                }
            }

            // 7. SAF007: Generic Exception Throw
            foreach (var throwStmt in root.DescendantNodes().OfType<ThrowStatementSyntax>())
            {
                if (throwStmt.Expression is ObjectCreationExpressionSyntax creation && creation.Type.ToString() == "Exception")
                {
                    int line = throwStmt.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
                    if (changedLines == null || changedLines.Contains(line))
                    {
                        var (rationale, recommendation, steps, example) = RecommendationEngine.GetGenericExceptionAdvice(throwStmt.ToString());
                        yield return new Violation
                        {
                            RuleId = "SAF007",
                            RuleName = "Generic Exception Throw",
                            TargetFile = filePath,
                            MemberName = throwStmt.ToString(),
                            LineNumber = line,
                            Severity = ViolationSeverity.Warning,
                            Description = "Throwing generic 'System.Exception' is discouraged. Use specific semantic exception types.",
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

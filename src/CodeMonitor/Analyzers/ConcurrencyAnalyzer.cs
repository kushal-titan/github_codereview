using System.Collections.Generic;
using System.Linq;
using CodeMonitor.Knowledge;
using CodeMonitor.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CodeMonitor.Analyzers
{
    public class ConcurrencyAnalyzer : ICodeAnalyzer
    {
        public string RuleId => "CON000";
        public string RuleName => "Concurrency & Async Safety Analyzer";

        public IEnumerable<Violation> Analyze(SyntaxTree tree, string filePath, QualityConfig config, ISet<int>? changedLines = null)
        {
            var root = tree.GetRoot();

            // 1. CON001: Deadlock - Lock Order Inversion Check
            var lockPairs = new List<(string Outer, string Inner, int Line, string Method)>();
            foreach (var outerLock in root.DescendantNodes().OfType<LockStatementSyntax>())
            {
                var innerLock = outerLock.Statement.DescendantNodes().OfType<LockStatementSyntax>().FirstOrDefault();
                if (innerLock != null)
                {
                    var method = outerLock.Ancestors().OfType<BaseMethodDeclarationSyntax>().FirstOrDefault();
                    string mName = method is MethodDeclarationSyntax m ? m.Identifier.Text : "Method";
                    int line = outerLock.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
                    lockPairs.Add((outerLock.Expression.ToString(), innerLock.Expression.ToString(), line, mName));
                }
            }

            for (int i = 0; i < lockPairs.Count; i++)
            {
                for (int j = i + 1; j < lockPairs.Count; j++)
                {
                    if (lockPairs[i].Outer == lockPairs[j].Inner && lockPairs[i].Inner == lockPairs[j].Outer)
                    {
                        var (rationale, recommendation, steps, example) = RecommendationEngine.GetDeadlockAdvice(
                            lockPairs[j].Method, lockPairs[i].Method,
                            $"{lockPairs[j].Outer}->{lockPairs[j].Inner}", $"{lockPairs[i].Outer}->{lockPairs[i].Inner}");

                        yield return new Violation
                        {
                            RuleId = "CON001",
                            RuleName = "Lock Order Inversion (Deadlock Risk)",
                            TargetFile = filePath,
                            MemberName = lockPairs[j].Method,
                            LineNumber = lockPairs[j].Line,
                            Severity = ViolationSeverity.Error,
                            Description = $"Deadlock Risk: Method '{lockPairs[j].Method}' acquires locks ({lockPairs[j].Outer} -> {lockPairs[j].Inner}), inverting lock order in '{lockPairs[i].Method}'.",
                            Rationale = rationale,
                            RecommendedFix = recommendation,
                            ActionSteps = steps,
                            CodeExample = example
                        };
                    }
                }
            }

            // 2. CON002: async void Method Detection (Fire-and-Forget trap)
            foreach (var method in root.DescendantNodes().OfType<MethodDeclarationSyntax>())
            {
                if (method.Modifiers.Any(SyntaxKind.AsyncKeyword) && method.ReturnType.ToString() == "void")
                {
                    int line = method.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
                    if (changedLines == null || changedLines.Contains(line))
                    {
                        var (rationale, recommendation, steps, example) = RecommendationEngine.GetAsyncVoidAdvice(method.Identifier.Text);
                        yield return new Violation
                        {
                            RuleId = "CON002",
                            RuleName = "async void Method Anti-Pattern",
                            TargetFile = filePath,
                            MemberName = method.Identifier.Text,
                            LineNumber = line,
                            Severity = ViolationSeverity.Error,
                            Description = $"Method '{method.Identifier.Text}' is declared 'async void'. Unhandled exceptions will crash the process.",
                            Rationale = rationale,
                            RecommendedFix = recommendation,
                            ActionSteps = steps,
                            CodeExample = example
                        };
                    }
                }
            }

            // 3. CON003: Sync-over-Async Deadlock (.Result / .Wait() / .GetAwaiter().GetResult())
            foreach (var member in root.DescendantNodes().OfType<MemberAccessExpressionSyntax>())
            {
                string memberName = member.Name.Identifier.Text;
                if (memberName == "Result" || memberName == "Wait" || memberName == "GetResult")
                {
                    int line = member.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
                    if (changedLines == null || changedLines.Contains(line))
                    {
                        var (rationale, recommendation, steps, example) = RecommendationEngine.GetSyncOverAsyncAdvice(member.ToString());
                        yield return new Violation
                        {
                            RuleId = "CON003",
                            RuleName = "Sync-Over-Async Blocking Call",
                            TargetFile = filePath,
                            MemberName = member.ToString(),
                            LineNumber = line,
                            Severity = ViolationSeverity.Error,
                            Description = $"Synchronous blocking call '{memberName}' on Task can cause thread pool starvation and deadlocks.",
                            Rationale = rationale,
                            RecommendedFix = recommendation,
                            ActionSteps = steps,
                            CodeExample = example
                        };
                    }
                }
            }

            // 4. CON004: Unsafe Lock Target (lock(this), lock("string"), lock(typeof(...)))
            foreach (var lk in root.DescendantNodes().OfType<LockStatementSyntax>())
            {
                string expr = lk.Expression.ToString();
                if (expr == "this" || expr.StartsWith("\"") || expr.StartsWith("typeof"))
                {
                    int line = lk.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
                    if (changedLines == null || changedLines.Contains(line))
                    {
                        var (rationale, recommendation, steps, example) = RecommendationEngine.GetUnsafeLockAdvice(expr);
                        yield return new Violation
                        {
                            RuleId = "CON004",
                            RuleName = "Unsafe Lock Target",
                            TargetFile = filePath,
                            MemberName = expr,
                            LineNumber = line,
                            Severity = ViolationSeverity.Warning,
                            Description = $"Locking on '{expr}' exposes synchronization object to external code.",
                            Rationale = rationale,
                            RecommendedFix = recommendation,
                            ActionSteps = steps,
                            CodeExample = example
                        };
                    }
                }
            }

            // 5. CON005: Unawaited Async Task Call (Fire-and-forget unawaited task call in expression statement)
            foreach (var exprStmt in root.DescendantNodes().OfType<ExpressionStatementSyntax>())
            {
                if (exprStmt.Expression is InvocationExpressionSyntax invocation)
                {
                    string invocationStr = invocation.Expression.ToString();
                    bool isAsyncMethodCall = invocationStr.EndsWith("Async") || invocationStr.Contains("Async(");

                    if (isAsyncMethodCall)
                    {
                        // Check if it's not awaited
                        bool isAwaited = exprStmt.Expression is AwaitExpressionSyntax;
                        if (!isAwaited)
                        {
                            int line = exprStmt.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
                            if (changedLines == null || changedLines.Contains(line))
                            {
                                var (rationale, recommendation, steps, example) = RecommendationEngine.GetUnawaitedTaskAdvice(invocation.ToString());
                                yield return new Violation
                                {
                                    RuleId = "CON005",
                                    RuleName = "Unawaited Async Task Call",
                                    TargetFile = filePath,
                                    MemberName = invocation.ToString(),
                                    LineNumber = line,
                                    Severity = ViolationSeverity.Warning,
                                    Description = $"Async invocation '{invocation}' is called without 'await'. Exceptions thrown in this task will be unhandled.",
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
}

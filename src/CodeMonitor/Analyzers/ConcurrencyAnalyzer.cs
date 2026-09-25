using System.Collections.Generic;
using System.Linq;
using CodeMonitor.Knowledge;
using CodeMonitor.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CodeMonitor.Analyzers
{
    /// <summary>
    /// Category 3: Concurrency & Async Safety Suite
    /// Implements standards from SonarQube (S2445, S3168, S4457, S2222, S2696, S3236, S3217, S3928, S2931)
    /// and Microsoft CA (CA2002, CA2008, CA2012, CA2211, CA2007, CA2016).
    /// </summary>
    public class ConcurrencyAnalyzer : ICodeAnalyzer
    {
        public string RuleId => "CON000";
        public string RuleName => "Concurrency & Async Safety Suite";

        public IEnumerable<Violation> Analyze(SyntaxTree tree, string filePath, QualityConfig config, ISet<int>? changedLines = null)
        {
            var root = tree.GetRoot();

            // =========================================================================
            // 1. [SonarQube: S2445] [Microsoft: CA2002] CON001: Deadlock - Lock Order Inversion Check
            // =========================================================================
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

            // =========================================================================
            // 2. [SonarQube: S3168] [Microsoft: CA2008] CON002: async void Method Detection
            // =========================================================================
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

            // =========================================================================
            // 3. [SonarQube: S4457] [Microsoft: CA2008] CON003: Sync-over-Async Deadlock (.Result / .Wait() / .GetAwaiter().GetResult())
            // =========================================================================
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

            // =========================================================================
            // 4. [SonarQube: S2445] [Microsoft: CA2002] CON004: Unsafe Lock Target (lock(this), lock("string"), lock(typeof(...)))
            // =========================================================================
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

            // =========================================================================
            // 5. [SonarQube: S3168] [Microsoft: CA2012] CON005: Unawaited Async Task Call
            // =========================================================================
            foreach (var exprStmt in root.DescendantNodes().OfType<ExpressionStatementSyntax>())
            {
                if (exprStmt.Expression is InvocationExpressionSyntax invocation)
                {
                    string invocationStr = invocation.Expression.ToString();
                    if (invocationStr.EndsWith("Async") && !invocationStr.Contains("Task.Run") && !invocationStr.Contains("Task.Factory") && !invocationStr.Contains("Task.Delay"))
                    {
                        int line = exprStmt.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
                        if (changedLines == null || changedLines.Contains(line))
                        {
                            var (rationale, recommendation, steps, example) = RecommendationEngine.GetUnawaitedTaskAdvice(invocationStr);
                            yield return new Violation
                            {
                                RuleId = "CON005",
                                RuleName = "Unawaited Async Task Call",
                                TargetFile = filePath,
                                MemberName = invocationStr,
                                LineNumber = line,
                                Severity = ViolationSeverity.Warning,
                                Description = $"Async method '{invocationStr}' is invoked as a standalone statement without 'await'.",
                                Rationale = rationale,
                                RecommendedFix = recommendation,
                                ActionSteps = steps,
                                CodeExample = example
                            };
                        }
                    }
                }
            }

            // =========================================================================
            // 6. [SonarQube: S2222] CON006: Thread.Sleep in Async Method
            // =========================================================================
            foreach (var method in root.DescendantNodes().OfType<MethodDeclarationSyntax>())
            {
                if (method.Modifiers.Any(SyntaxKind.AsyncKeyword) && method.Body != null)
                {
                    var sleepCalls = method.Body.DescendantNodes().OfType<InvocationExpressionSyntax>()
                        .Where(i => i.Expression.ToString() == "Thread.Sleep");

                    foreach (var sleep in sleepCalls)
                    {
                        int line = sleep.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
                        if (changedLines == null || changedLines.Contains(line))
                        {
                            var (rationale, recommendation, steps, example) = RecommendationEngine.GetThreadSleepInAsyncAdvice(method.Identifier.Text);
                            yield return new Violation
                            {
                                RuleId = "CON006",
                                RuleName = "Thread.Sleep in Async Method",
                                TargetFile = filePath,
                                MemberName = $"{method.Identifier.Text}->Thread.Sleep",
                                LineNumber = line,
                                Severity = ViolationSeverity.Error,
                                Description = $"Calling 'Thread.Sleep' inside async method '{method.Identifier.Text}' blocks thread pool worker thread. Use 'await Task.Delay(...)'.",
                                Rationale = rationale,
                                RecommendedFix = recommendation,
                                ActionSteps = steps,
                                CodeExample = example
                            };
                        }
                    }
                }
            }

            // =========================================================================
            // 7. [SonarQube: S2696] [Microsoft: CA2211] CON007: Shared Static State Mutation from Instance Method
            // =========================================================================
            var classDeclarations = root.DescendantNodes().OfType<ClassDeclarationSyntax>();
            foreach (var cls in classDeclarations)
            {
                var staticFieldNames = cls.Members.OfType<FieldDeclarationSyntax>()
                    .Where(f => f.Modifiers.Any(SyntaxKind.StaticKeyword) && !f.Modifiers.Any(SyntaxKind.ReadOnlyKeyword) && !f.Modifiers.Any(SyntaxKind.ConstKeyword))
                    .SelectMany(f => f.Declaration.Variables.Select(v => v.Identifier.Text))
                    .ToHashSet();

                if (staticFieldNames.Count > 0)
                {
                    var instanceMethods = cls.Members.OfType<MethodDeclarationSyntax>()
                        .Where(m => !m.Modifiers.Any(SyntaxKind.StaticKeyword) && m.Body != null);

                    foreach (var method in instanceMethods)
                    {
                        var assignments = method.Body!.DescendantNodes().OfType<AssignmentExpressionSyntax>()
                            .Where(a => staticFieldNames.Contains(a.Left.ToString()));

                        foreach (var assign in assignments)
                        {
                            bool isInsideLock = assign.Ancestors().Any(a => a is LockStatementSyntax);
                            if (!isInsideLock)
                            {
                                int line = assign.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
                                if (changedLines == null || changedLines.Contains(line))
                                {
                                    var (rationale, recommendation, steps, example) = RecommendationEngine.GetSharedStaticMutationAdvice(assign.Left.ToString(), method.Identifier.Text);
                                    yield return new Violation
                                    {
                                        RuleId = "CON007",
                                        RuleName = "Shared Static State Mutation",
                                        TargetFile = filePath,
                                        MemberName = assign.Left.ToString(),
                                        LineNumber = line,
                                        Severity = ViolationSeverity.Warning,
                                        Description = $"Static field '{assign.Left}' is mutated from instance method '{method.Identifier.Text}' without synchronization locks.",
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

            // =========================================================================
            // 8. [SonarQube: S3236] [Microsoft: CA2007] CON008: Missing ConfigureAwait(false) in Non-UI Code
            // =========================================================================
            foreach (var awaitExpr in root.DescendantNodes().OfType<AwaitExpressionSyntax>())
            {
                string exprText = awaitExpr.Expression.ToString();
                if (!exprText.Contains("ConfigureAwait") && (exprText.EndsWith("Async()") || exprText.Contains("Task.")))
                {
                    int line = awaitExpr.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
                    if (changedLines == null || changedLines.Contains(line))
                    {
                        var (rationale, recommendation, steps, example) = RecommendationEngine.GetMissingConfigureAwaitAdvice(exprText);
                        yield return new Violation
                        {
                            RuleId = "CON008",
                            RuleName = "Missing ConfigureAwait(false)",
                            TargetFile = filePath,
                            MemberName = exprText,
                            LineNumber = line,
                            Severity = ViolationSeverity.Warning,
                            Description = $"Awaited task '{exprText}' does not specify 'ConfigureAwait(false)', risking synchronization context overhead and deadlocks.",
                            Rationale = rationale,
                            RecommendedFix = recommendation,
                            ActionSteps = steps,
                            CodeExample = example
                        };
                    }
                }
            }

            // =========================================================================
            // 9. [SonarQube: S3217] CON009: Loop Variable Closure in Async / Tasks
            // =========================================================================
            foreach (var loop in root.DescendantNodes().Where(n => n is ForStatementSyntax || n is ForEachStatementSyntax))
            {
                string loopVar = "";
                if (loop is ForStatementSyntax forS && forS.Declaration != null && forS.Declaration.Variables.Count > 0)
                    loopVar = forS.Declaration.Variables[0].Identifier.Text;
                else if (loop is ForEachStatementSyntax feS)
                    loopVar = feS.Identifier.Text;

                if (!string.IsNullOrEmpty(loopVar))
                {
                    var taskRuns = loop.DescendantNodes().OfType<InvocationExpressionSyntax>()
                        .Where(inv => inv.Expression.ToString() == "Task.Run" || inv.Expression.ToString() == "ThreadPool.QueueUserWorkItem");

                    foreach (var tr in taskRuns)
                    {
                        bool capturesLoopVar = tr.ArgumentList.Arguments.Any(a => a.ToString().Contains(loopVar));
                        if (capturesLoopVar)
                        {
                            int line = tr.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
                            if (changedLines == null || changedLines.Contains(line))
                            {
                                var (rationale, recommendation, steps, example) = RecommendationEngine.GetLoopClosureAdvice(loopVar);
                                yield return new Violation
                                {
                                    RuleId = "CON009",
                                    RuleName = "Loop Variable Closure in Async Task",
                                    TargetFile = filePath,
                                    MemberName = loopVar,
                                    LineNumber = line,
                                    Severity = ViolationSeverity.Error,
                                    Description = $"Loop variable '{loopVar}' is captured in a background task closure, leading to race conditions across iterations.",
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

            // =========================================================================
            // 10. [SonarQube: S3928] [Microsoft: CA2016] CON010: Discarded CancellationToken
            // =========================================================================
            foreach (var method in root.DescendantNodes().OfType<MethodDeclarationSyntax>())
            {
                if (method.ParameterList != null && method.Body != null)
                {
                    var ctParam = method.ParameterList.Parameters.FirstOrDefault(p => p.Type?.ToString() == "CancellationToken");
                    if (ctParam != null)
                    {
                        string ctName = ctParam.Identifier.Text;
                        var asyncCalls = method.Body.DescendantNodes().OfType<InvocationExpressionSyntax>()
                            .Where(inv => inv.Expression.ToString().EndsWith("Async"));

                        foreach (var ac in asyncCalls)
                        {
                            bool passesToken = ac.ArgumentList.Arguments.Any(arg => arg.ToString() == ctName);
                            if (!passesToken && !ac.ToString().Contains("Task.Delay") && !ac.ToString().Contains("Task.Yield"))
                            {
                                int line = ac.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
                                if (changedLines == null || changedLines.Contains(line))
                                {
                                    var (rationale, recommendation, steps, example) = RecommendationEngine.GetDiscardedCancellationTokenAdvice(ac.ToString(), ctName);
                                    yield return new Violation
                                    {
                                        RuleId = "CON010",
                                        RuleName = "Discarded CancellationToken",
                                        TargetFile = filePath,
                                        MemberName = ac.ToString(),
                                        LineNumber = line,
                                        Severity = ViolationSeverity.Warning,
                                        Description = $"Method accepts '{ctName}' but omits passing it to async invocation '{ac.Expression}'.",
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

            // =========================================================================
            // 11. [SonarQube: S2931] CON011: Thread-Unsafe Collection Mutation in Parallel Loop
            // =========================================================================
            foreach (var parallelCall in root.DescendantNodes().OfType<InvocationExpressionSyntax>())
            {
                string pExpr = parallelCall.Expression.ToString();
                if (pExpr.StartsWith("Parallel.ForEach") || pExpr.StartsWith("Parallel.For"))
                {
                    var unsafeMutations = parallelCall.ArgumentList.DescendantNodes().OfType<InvocationExpressionSyntax>()
                        .Where(inv =>
                        {
                            string s = inv.Expression.ToString();
                            return s.EndsWith(".Add") || s.EndsWith(".Remove") || s.EndsWith(".Clear");
                        });

                    foreach (var badMut in unsafeMutations)
                    {
                        int line = badMut.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
                        if (changedLines == null || changedLines.Contains(line))
                        {
                            var (rationale, recommendation, steps, example) = RecommendationEngine.GetParallelCollectionMutationAdvice(badMut.ToString());
                            yield return new Violation
                            {
                                RuleId = "CON011",
                                RuleName = "Thread-Unsafe Collection Mutation in Parallel",
                                TargetFile = filePath,
                                MemberName = badMut.ToString(),
                                LineNumber = line,
                                Severity = ViolationSeverity.Error,
                                Description = $"Mutating non-thread-safe collection '{badMut}' inside Parallel loop causes data corruption and memory faults.",
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

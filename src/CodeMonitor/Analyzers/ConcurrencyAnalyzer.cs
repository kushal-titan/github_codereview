using System.Collections.Generic;
using System.Linq;
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
                        yield return new Violation
                        {
                            RuleId = "CON001",
                            RuleName = "Lock Order Inversion (Deadlock Risk)",
                            TargetFile = filePath,
                            MemberName = lockPairs[j].Method,
                            LineNumber = lockPairs[j].Line,
                            Severity = ViolationSeverity.Error,
                            Description = $"Deadlock Risk: Method '{lockPairs[j].Method}' acquires locks ({lockPairs[j].Outer} -> {lockPairs[j].Inner}), inverting lock order in '{lockPairs[i].Method}'.",
                            RecommendedFix = "Establish a strict, global lock acquisition hierarchy across all threads."
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
                        yield return new Violation
                        {
                            RuleId = "CON002",
                            RuleName = "async void Method Anti-Pattern",
                            TargetFile = filePath,
                            MemberName = method.Identifier.Text,
                            LineNumber = line,
                            Severity = ViolationSeverity.Error,
                            Description = $"Method '{method.Identifier.Text}' is declared 'async void'. Unhandled exceptions will terminate the process.",
                            RecommendedFix = "Change return type from 'void' to 'Task' or 'ValueTask'."
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
                        yield return new Violation
                        {
                            RuleId = "CON003",
                            RuleName = "Sync-Over-Async Blocking Call",
                            TargetFile = filePath,
                            MemberName = member.ToString(),
                            LineNumber = line,
                            Severity = ViolationSeverity.Error,
                            Description = $"Synchronous blocking call '{memberName}' on Task can cause thread starvation and deadlocks.",
                            RecommendedFix = "Use 'await' asynchronously instead of blocking with .Result or .Wait()."
                        };
                    }
                }
            }

            // 4. CON004: Unsafe Lock Target (lock(this) or lock("string"))
            foreach (var lk in root.DescendantNodes().OfType<LockStatementSyntax>())
            {
                string expr = lk.Expression.ToString();
                if (expr == "this" || expr.StartsWith("\"") || expr.StartsWith("typeof"))
                {
                    int line = lk.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
                    if (changedLines == null || changedLines.Contains(line))
                    {
                        yield return new Violation
                        {
                            RuleId = "CON004",
                            RuleName = "Unsafe Lock Target",
                            TargetFile = filePath,
                            MemberName = expr,
                            LineNumber = line,
                            Severity = ViolationSeverity.Warning,
                            Description = $"Locking on '{expr}' exposes synchronization object to external code.",
                            RecommendedFix = "Lock on a private, static readonly object instance: 'private static readonly object _lock = new object();'."
                        };
                    }
                }
            }
        }
    }
}

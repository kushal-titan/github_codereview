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
    /// Category 5: Performance & Memory Optimization Suite
    /// Implements standards from SonarQube (S1643, S1155, S4158, S1157, S3242, S3902, S4004, S1118, S3247)
    /// and Microsoft CA (CA1806, CA1827, CA1851, CA1862, CA1846, CA1820, CA1821, CA1815).
    /// </summary>
    public class PerformanceAnalyzer : ICodeAnalyzer
    {
        public string RuleId => "PERF000";
        public string RuleName => "Performance & Memory Optimization Suite";

        public IEnumerable<Violation> Analyze(SyntaxTree tree, string filePath, QualityConfig config, ISet<int>? changedLines = null)
        {
            var root = tree.GetRoot();

            // =========================================================================
            // 1. [SonarQube: S1643] PERF001: String Concatenation (+ or +=) inside Loops
            // =========================================================================
            var loopNodes = root.DescendantNodes().Where(n =>
                n is ForStatementSyntax ||
                n is ForEachStatementSyntax ||
                n is WhileStatementSyntax ||
                n is DoStatementSyntax);

            foreach (var loop in loopNodes)
            {
                foreach (var assign in loop.DescendantNodes().OfType<AssignmentExpressionSyntax>())
                {
                    if (assign.IsKind(SyntaxKind.AddAssignmentExpression) && IsStringConcatenation(assign, root))
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
                                Description = "Repeated string concatenation (+=) inside a loop causes excessive GC memory allocations. Use StringBuilder.",
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
            // 2. [SonarQube: S1155] [Microsoft: CA1806] PERF002: Boxing Allocations (Legacy collections)
            // =========================================================================
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

            // =========================================================================
            // 3. [SonarQube: S1155] [Microsoft: CA1827] PERF003: LINQ .Count() > 0 / == 0 Smell
            // =========================================================================
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
                                Description = $"Using '{binary}' enumerates the entire collection just to check emptiness. Use '.Any()'.",
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
            // 4. [SonarQube: S4158] [Microsoft: CA1851] PERF004: Multiple Enumeration of IEnumerable<T>
            // =========================================================================
            foreach (var method in root.DescendantNodes().OfType<MethodDeclarationSyntax>())
            {
                if (method.ParameterList != null && method.Body != null)
                {
                    var ienumParams = method.ParameterList.Parameters
                        .Where(p => p.Type?.ToString().StartsWith("IEnumerable") == true)
                        .Select(p => p.Identifier.Text);

                    foreach (var pName in ienumParams)
                    {
                        int enumUsageCount = method.Body.DescendantNodes().OfType<InvocationExpressionSyntax>()
                            .Count(i => i.ToString().StartsWith($"{pName}.") && (i.ToString().Contains(".Where") || i.ToString().Contains(".Select") || i.ToString().Contains(".Count") || i.ToString().Contains(".Any") || i.ToString().Contains(".ToList") || i.ToString().Contains(".ToArray")));

                        if (enumUsageCount >= 2)
                        {
                            int line = method.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
                            if (changedLines == null || changedLines.Contains(line))
                            {
                                var (rationale, recommendation, steps, example) = RecommendationEngine.GetMultipleEnumerationAdvice(pName);
                                yield return new Violation
                                {
                                    RuleId = "PERF004",
                                    RuleName = "Multiple Enumeration of IEnumerable",
                                    TargetFile = filePath,
                                    MemberName = $"{method.Identifier.Text}({pName})",
                                    LineNumber = line,
                                    Severity = ViolationSeverity.Warning,
                                    Description = $"Parameter '{pName}' of type IEnumerable is enumerated {enumUsageCount} times without caching to a materialized collection.",
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
            // 5. [SonarQube: S1157] [Microsoft: CA1862] PERF005: Inefficient String Comparison
            // =========================================================================
            foreach (var binary in root.DescendantNodes().OfType<BinaryExpressionSyntax>())
            {
                if (binary.IsKind(SyntaxKind.EqualsExpression) || binary.IsKind(SyntaxKind.NotEqualsExpression))
                {
                    string left = binary.Left.ToString();
                    string right = binary.Right.ToString();

                    if (left.EndsWith(".ToLower()") || left.EndsWith(".ToUpper()") ||
                        right.EndsWith(".ToLower()") || right.EndsWith(".ToUpper()"))
                    {
                        int line = binary.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
                        if (changedLines == null || changedLines.Contains(line))
                        {
                            var (rationale, recommendation, steps, example) = RecommendationEngine.GetInefficientStringComparisonAdvice(binary.ToString());
                            yield return new Violation
                            {
                                RuleId = "PERF005",
                                RuleName = "Inefficient Case-Insensitive String Comparison",
                                TargetFile = filePath,
                                MemberName = binary.ToString(),
                                LineNumber = line,
                                Severity = ViolationSeverity.Warning,
                                Description = $"Using '{binary}' allocates temporary lowercased strings. Use 'string.Equals(a, b, StringComparison.OrdinalIgnoreCase)'.",
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
            // 6. [SonarQube: S3242] [Microsoft: CA1846] PERF006: Span Memory Optimization
            // =========================================================================
            foreach (var loop in loopNodes)
            {
                var substringCalls = loop.DescendantNodes().OfType<InvocationExpressionSyntax>()
                    .Where(i => i.Expression.ToString().EndsWith(".Substring"));

                foreach (var sub in substringCalls)
                {
                    int line = sub.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
                    if (changedLines == null || changedLines.Contains(line))
                    {
                        var (rationale, recommendation, steps, example) = RecommendationEngine.GetSpanOptimizationAdvice(sub.ToString());
                        yield return new Violation
                        {
                            RuleId = "PERF006",
                            RuleName = "Span / Memory Allocation Optimization",
                            TargetFile = filePath,
                            MemberName = sub.ToString(),
                            LineNumber = line,
                            Severity = ViolationSeverity.Warning,
                            Description = $"Calling '{sub}' in a loop allocates repeated heap strings. Use 'ReadOnlySpan<char>' or 'AsSpan()' to avoid allocations.",
                            Rationale = rationale,
                            RecommendedFix = recommendation,
                            ActionSteps = steps,
                            CodeExample = example
                        };
                    }
                }
            }

            // =========================================================================
            // 7. [SonarQube: S3902] PERF007: Closure Allocation in Loop
            // =========================================================================
            foreach (var loop in loopNodes)
            {
                var lambdas = loop.DescendantNodes().OfType<LambdaExpressionSyntax>();
                foreach (var lambda in lambdas)
                {
                    int line = lambda.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
                    if (changedLines == null || changedLines.Contains(line))
                    {
                        var (rationale, recommendation, steps, example) = RecommendationEngine.GetClosureInLoopAdvice();
                        yield return new Violation
                        {
                            RuleId = "PERF007",
                            RuleName = "Closure Allocation in Loop",
                            TargetFile = filePath,
                            MemberName = "lambda in loop",
                            LineNumber = line,
                            Severity = ViolationSeverity.Warning,
                            Description = "Lambda expression inside loop allocates delegate closure objects on every iteration.",
                            Rationale = rationale,
                            RecommendedFix = recommendation,
                            ActionSteps = steps,
                            CodeExample = example
                        };
                    }
                }
            }

            // =========================================================================
            // 8. [SonarQube: S4004] [Microsoft: CA1820] PERF008: Uncompiled / Repeated Regex Creation in Method
            // =========================================================================
            foreach (var method in root.DescendantNodes().OfType<MethodDeclarationSyntax>())
            {
                if (method.Body != null)
                {
                    var regexCreations = method.Body.DescendantNodes().OfType<ObjectCreationExpressionSyntax>()
                        .Where(c => c.Type.ToString() == "Regex" || c.Type.ToString() == "System.Text.RegularExpressions.Regex");

                    foreach (var rc in regexCreations)
                    {
                        int line = rc.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
                        if (changedLines == null || changedLines.Contains(line))
                        {
                            var (rationale, recommendation, steps, example) = RecommendationEngine.GetRegexCompilationAdvice();
                            yield return new Violation
                            {
                                RuleId = "PERF008",
                                RuleName = "Uncached Regex Instantiation in Method",
                                TargetFile = filePath,
                                MemberName = "new Regex()",
                                LineNumber = line,
                                Severity = ViolationSeverity.Warning,
                                Description = "Instantiating 'new Regex(...)' inside method recompiles regular expression DFA on every call. Use static readonly Regex or [GeneratedRegex].",
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
            // 9. [SonarQube: S1118] [Microsoft: CA1821] PERF009: Unnecessary Finalizer / Destructor
            // =========================================================================
            foreach (var destructor in root.DescendantNodes().OfType<DestructorDeclarationSyntax>())
            {
                int line = destructor.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
                if (changedLines == null || changedLines.Contains(line))
                {
                    var (rationale, recommendation, steps, example) = RecommendationEngine.GetFinalizerAdvice(destructor.Identifier.Text);
                    yield return new Violation
                    {
                        RuleId = "PERF009",
                        RuleName = "Unnecessary Finalizer / Destructor",
                        TargetFile = filePath,
                        MemberName = $"~{destructor.Identifier.Text}()",
                        LineNumber = line,
                        Severity = ViolationSeverity.Warning,
                        Description = $"Class defines finalizer '~{destructor.Identifier.Text}()' which forces object onto finalizer queue and delays GC collection.",
                        Rationale = rationale,
                        RecommendedFix = recommendation,
                        ActionSteps = steps,
                        CodeExample = example
                    };
                }
            }

            // =========================================================================
            // 10. [SonarQube: S3247] [Microsoft: CA1815] PERF010: Large Struct Pass-by-Value
            // =========================================================================
            foreach (var method in root.DescendantNodes().OfType<MethodDeclarationSyntax>())
            {
                if (method.ParameterList != null)
                {
                    foreach (var param in method.ParameterList.Parameters)
                    {
                        string pType = param.Type?.ToString() ?? "";
                        if ((pType.Contains("Matrix") || pType.Contains("Vector") || pType.Contains("Buffer") || pType.Contains("Payload")) &&
                            !param.Modifiers.Any(m => m.IsKind(SyntaxKind.InKeyword) || m.IsKind(SyntaxKind.RefKeyword)))
                        {
                            int line = param.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
                            if (changedLines == null || changedLines.Contains(line))
                            {
                                var (rationale, recommendation, steps, example) = RecommendationEngine.GetLargeStructAdvice(param.Identifier.Text, pType);
                                yield return new Violation
                                {
                                    RuleId = "PERF010",
                                    RuleName = "Large Struct Passed By Value",
                                    TargetFile = filePath,
                                    MemberName = $"{pType} {param.Identifier.Text}",
                                    LineNumber = line,
                                    Severity = ViolationSeverity.Warning,
                                    Description = $"Potentially large struct '{pType}' is passed by value, copying memory on stack. Consider using 'in {pType}'.",
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

        private static bool IsStringConcatenation(AssignmentExpressionSyntax assign, SyntaxNode root)
        {
            string right = assign.Right.ToString();
            if (assign.Right is LiteralExpressionSyntax lit && lit.IsKind(SyntaxKind.StringLiteralExpression))
                return true;
            if (assign.Right is InterpolatedStringExpressionSyntax)
                return true;
            if (right.EndsWith(".ToString()"))
                return true;

            string leftVarName = assign.Left.ToString();
            var enclosingMethod = assign.Ancestors().OfType<BaseMethodDeclarationSyntax>().FirstOrDefault();
            if (enclosingMethod != null)
            {
                var localDecl = enclosingMethod.DescendantNodes().OfType<VariableDeclarationSyntax>()
                    .FirstOrDefault(v => v.Variables.Any(varDecl => varDecl.Identifier.Text == leftVarName));

                if (localDecl != null)
                {
                    string typeName = localDecl.Type.ToString();
                    if (typeName == "string" || typeName == "String")
                        return true;
                    if (IsNumericType(typeName))
                        return false;
                }

                var param = enclosingMethod.ParameterList?.Parameters.FirstOrDefault(p => p.Identifier.Text == leftVarName);
                if (param != null)
                {
                    string typeName = param.Type?.ToString() ?? "";
                    if (typeName == "string" || typeName == "String")
                        return true;
                    if (IsNumericType(typeName))
                        return false;
                }
            }

            var enclosingClass = assign.Ancestors().OfType<ClassDeclarationSyntax>().FirstOrDefault();
            if (enclosingClass != null)
            {
                var fieldDecl = enclosingClass.DescendantNodes().OfType<FieldDeclarationSyntax>()
                    .FirstOrDefault(f => f.Declaration.Variables.Any(v => v.Identifier.Text == leftVarName));

                if (fieldDecl != null)
                {
                    string typeName = fieldDecl.Declaration.Type.ToString();
                    if (typeName == "string" || typeName == "String")
                        return true;
                    if (IsNumericType(typeName))
                        return false;
                }
            }

            string lowerLeft = leftVarName.ToLowerInvariant();
            if (lowerLeft == "total" || lowerLeft == "sum" || lowerLeft == "count" || lowerLeft == "i" ||
                lowerLeft == "j" || lowerLeft == "k" || lowerLeft == "idx" || lowerLeft == "index" ||
                lowerLeft == "amount" || lowerLeft == "balance" || lowerLeft == "score" || lowerLeft == "result" ||
                lowerLeft == "val" || lowerLeft == "value" || lowerLeft == "num" || lowerLeft == "number")
            {
                return false;
            }

            if (lowerLeft == "str" || lowerLeft == "text" || lowerLeft == "msg" || lowerLeft == "message" ||
                lowerLeft == "output" || lowerLeft == "content" || lowerLeft == "body" || lowerLeft == "html" ||
                lowerLeft == "xml" || lowerLeft == "json" || lowerLeft == "csv" || lowerLeft == "sql" || lowerLeft == "query")
            {
                return true;
            }

            return false;
        }

        private static bool IsNumericType(string typeName)
        {
            return typeName == "int" || typeName == "long" || typeName == "double" || typeName == "float" ||
                   typeName == "decimal" || typeName == "byte" || typeName == "short" || typeName == "uint" ||
                   typeName == "ulong" || typeName == "ushort" || typeName == "sbyte" || typeName == "nint" ||
                   typeName == "nuint" || typeName == "Int32" || typeName == "Int64" || typeName == "Double" ||
                   typeName == "Single" || typeName == "Decimal";
        }
    }
}

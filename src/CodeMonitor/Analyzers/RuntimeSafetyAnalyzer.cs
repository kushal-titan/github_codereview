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
        public string RuleName => "Runtime Safety & Bug Suite";

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

            // 2. SAF002: Division / Modulo by Zero Defect (Literals, Constants & Local/Field Variable Tracking)
            foreach (var node in root.DescendantNodes())
            {
                ExpressionSyntax? divisor = null;
                string opExpr = "";
                int line = 0;

                if (node is BinaryExpressionSyntax binary &&
                    (binary.IsKind(SyntaxKind.DivideExpression) || binary.IsKind(SyntaxKind.ModuloExpression)))
                {
                    divisor = binary.Right;
                    opExpr = binary.ToString();
                    line = binary.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
                }
                else if (node is AssignmentExpressionSyntax assign &&
                    (assign.IsKind(SyntaxKind.DivideAssignmentExpression) || assign.IsKind(SyntaxKind.ModuloAssignmentExpression)))
                {
                    divisor = assign.Right;
                    opExpr = assign.ToString();
                    line = assign.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
                }

                if (divisor != null)
                {
                    var enclosingScope = node.Ancestors().FirstOrDefault(a =>
                        a is BaseMethodDeclarationSyntax ||
                        a is LocalFunctionStatementSyntax ||
                        a is AnonymousFunctionExpressionSyntax ||
                        a is AccessorDeclarationSyntax);

                    if (IsZeroValue(divisor, enclosingScope, node.SpanStart, node))
                    {
                        if (changedLines == null || changedLines.Contains(line))
                        {
                            var (rationale, recommendation, steps, example) = RecommendationEngine.GetDivisionByZeroAdvice(opExpr);
                            yield return new Violation
                            {
                                RuleId = "SAF002",
                                RuleName = "Division by Zero Defect",
                                TargetFile = filePath,
                                MemberName = opExpr,
                                LineNumber = line,
                                Severity = ViolationSeverity.Error,
                                Description = $"Division or modulo by zero detected in '{opExpr}' (divisor '{divisor}' resolves to 0).",
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
                                Description = $"Statement '{nextStmt.ToString().TrimEnd(';')}' will never be executed because it appears after an unconditional exit.",
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

            // 8. SAF008: Missing Null Argument Guard in Public Methods
            foreach (var method in root.DescendantNodes().OfType<MethodDeclarationSyntax>())
            {
                if (method.Modifiers.Any(m => m.IsKind(SyntaxKind.PublicKeyword)) && method.Body != null && method.ParameterList != null)
                {
                    foreach (var param in method.ParameterList.Parameters)
                    {
                        string paramType = param.Type?.ToString() ?? "";
                        string paramName = param.Identifier.Text;
                        if (!IsValueType(paramType) && !string.IsNullOrEmpty(paramName) && paramType != "string" && !paramType.EndsWith("?"))
                        {
                            // Check if parameter is dereferenced inside method body
                            bool isDereferenced = method.Body.DescendantNodes()
                                .OfType<MemberAccessExpressionSyntax>()
                                .Any(m => m.Expression.ToString() == paramName && !m.ToString().StartsWith($"{paramName}?."));

                            if (isDereferenced)
                            {
                                string bodyText = method.Body.ToString();
                                bool hasGuard = bodyText.Contains($"ArgumentNullException.ThrowIfNull({paramName})") ||
                                               bodyText.Contains($"{paramName} == null") ||
                                               bodyText.Contains($"{paramName} is null");

                                if (!hasGuard)
                                {
                                    int line = param.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
                                    if (changedLines == null || changedLines.Contains(line))
                                    {
                                        var (rationale, recommendation, steps, example) = RecommendationEngine.GetMissingNullGuardAdvice(paramName, method.Identifier.Text);
                                        yield return new Violation
                                        {
                                            RuleId = "SAF008",
                                            RuleName = "Missing Null Argument Guard",
                                            TargetFile = filePath,
                                            MemberName = $"{method.Identifier.Text}({paramName})",
                                            LineNumber = line,
                                            Severity = ViolationSeverity.Warning,
                                            Description = $"Public method '{method.Identifier.Text}' dereferences parameter '{paramName}' without null validation.",
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

            // 9. SAF009: Infinite Loop Defect (while(true) / for(;;) with no reachable break/return/throw)
            foreach (var whileStmt in root.DescendantNodes().OfType<WhileStatementSyntax>())
            {
                if (whileStmt.Condition.ToString() == "true")
                {
                    bool hasExit = whileStmt.Statement.DescendantNodes().Any(n =>
                        n is BreakStatementSyntax || n is ReturnStatementSyntax || n is ThrowStatementSyntax);

                    if (!hasExit)
                    {
                        int line = whileStmt.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
                        if (changedLines == null || changedLines.Contains(line))
                        {
                            var (rationale, recommendation, steps, example) = RecommendationEngine.GetInfiniteLoopAdvice();
                            yield return new Violation
                            {
                                RuleId = "SAF009",
                                RuleName = "Infinite Loop Defect",
                                TargetFile = filePath,
                                MemberName = "while(true)",
                                LineNumber = line,
                                Severity = ViolationSeverity.Error,
                                Description = "Infinite loop detected: 'while(true)' contains no reachable break, return, or throw statement.",
                                Rationale = rationale,
                                RecommendedFix = recommendation,
                                ActionSteps = steps,
                                CodeExample = example
                            };
                        }
                    }
                }
            }

            // 10. SAF010: Inexact Float/Double Equality
            foreach (var binary in root.DescendantNodes().OfType<BinaryExpressionSyntax>())
            {
                if (binary.IsKind(SyntaxKind.EqualsExpression) || binary.IsKind(SyntaxKind.NotEqualsExpression))
                {
                    string left = binary.Left.ToString();
                    string right = binary.Right.ToString();
                    if (left.EndsWith("f") || left.EndsWith("d") || left.EndsWith("F") || left.EndsWith("D") ||
                        right.EndsWith("f") || right.EndsWith("d") || right.EndsWith("F") || right.EndsWith("D"))
                    {
                        int line = binary.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
                        if (changedLines == null || changedLines.Contains(line))
                        {
                            var (rationale, recommendation, steps, example) = RecommendationEngine.GetFloatEqualityAdvice(binary.ToString());
                            yield return new Violation
                            {
                                RuleId = "SAF010",
                                RuleName = "Inexact Floating Point Equality",
                                TargetFile = filePath,
                                MemberName = binary.ToString(),
                                LineNumber = line,
                                Severity = ViolationSeverity.Warning,
                                Description = $"Comparing floating-point values directly using '{binary.OperatorToken.Text}' is unsafe due to precision rounding errors. Use Math.Abs(a - b) < epsilon.",
                                Rationale = rationale,
                                RecommendedFix = recommendation,
                                ActionSteps = steps,
                                CodeExample = example
                            };
                        }
                    }
                }
            }

            // 11. SAF011: Redundant Null Coalescing Trap (x ?? x)
            foreach (var coalesce in root.DescendantNodes().OfType<BinaryExpressionSyntax>())
            {
                if (coalesce.IsKind(SyntaxKind.CoalesceExpression) && coalesce.Left.ToString() == coalesce.Right.ToString())
                {
                    int line = coalesce.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
                    if (changedLines == null || changedLines.Contains(line))
                    {
                        var (rationale, recommendation, steps, example) = RecommendationEngine.GetRedundantNullCoalescingAdvice(coalesce.ToString());
                        yield return new Violation
                        {
                            RuleId = "SAF011",
                            RuleName = "Redundant Null Coalescing Expression",
                            TargetFile = filePath,
                            MemberName = coalesce.ToString(),
                            LineNumber = line,
                            Severity = ViolationSeverity.Warning,
                            Description = $"Redundant null coalescing expression '{coalesce}': Left and Right operands are identical.",
                            Rationale = rationale,
                            RecommendedFix = recommendation,
                            ActionSteps = steps,
                            CodeExample = example
                        };
                    }
                }
            }

            // 12. SAF012: Collection Mutation Inside Foreach Loop
            foreach (var foreachStmt in root.DescendantNodes().OfType<ForEachStatementSyntax>())
            {
                string collectionName = foreachStmt.Expression.ToString();
                var modifyingCalls = foreachStmt.Statement.DescendantNodes().OfType<InvocationExpressionSyntax>()
                    .Where(inv =>
                    {
                        string expr = inv.Expression.ToString();
                        return expr == $"{collectionName}.Add" || expr == $"{collectionName}.Remove" ||
                               expr == $"{collectionName}.Clear" || expr == $"{collectionName}.Insert";
                    });

                foreach (var mod in modifyingCalls)
                {
                    int line = mod.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
                    if (changedLines == null || changedLines.Contains(line))
                    {
                        var (rationale, recommendation, steps, example) = RecommendationEngine.GetCollectionMutationAdvice(collectionName, mod.ToString());
                        yield return new Violation
                        {
                            RuleId = "SAF012",
                            RuleName = "Collection Mutation During Iteration",
                            TargetFile = filePath,
                            MemberName = mod.ToString(),
                            LineNumber = line,
                            Severity = ViolationSeverity.Error,
                            Description = $"Modifying collection '{collectionName}' inside its own foreach loop will throw InvalidOperationException.",
                            Rationale = rationale,
                            RecommendedFix = recommendation,
                            ActionSteps = steps,
                            CodeExample = example
                        };
                    }
                }
            }

            // 13. SAF013: Stack Trace Truncation (catch(Exception ex) { throw ex; })
            foreach (var catchClause in root.DescendantNodes().OfType<CatchClauseSyntax>())
            {
                if (catchClause.Declaration != null && !string.IsNullOrEmpty(catchClause.Declaration.Identifier.Text))
                {
                    string exIdentifier = catchClause.Declaration.Identifier.Text;
                    var badThrows = catchClause.Block.DescendantNodes().OfType<ThrowStatementSyntax>()
                        .Where(t => t.Expression is IdentifierNameSyntax id && id.Identifier.Text == exIdentifier);

                    foreach (var badThrow in badThrows)
                    {
                        int line = badThrow.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
                        if (changedLines == null || changedLines.Contains(line))
                        {
                            var (rationale, recommendation, steps, example) = RecommendationEngine.GetStackTraceTruncationAdvice(exIdentifier);
                            yield return new Violation
                            {
                                RuleId = "SAF013",
                                RuleName = "Stack Trace Truncation (throw ex)",
                                TargetFile = filePath,
                                MemberName = badThrow.ToString(),
                                LineNumber = line,
                                Severity = ViolationSeverity.Warning,
                                Description = $"Using '{badThrow}' resets the original stack trace. Use 'throw;' to preserve the original exception call stack.",
                                Rationale = rationale,
                                RecommendedFix = recommendation,
                                ActionSteps = steps,
                                CodeExample = example
                            };
                        }
                    }
                }
            }

            // 14. SAF014: Equals & GetHashCode Inconsistency
            var classDecls = root.DescendantNodes().OfType<ClassDeclarationSyntax>();
            foreach (var cls in classDecls)
            {
                bool hasEquals = cls.Members.OfType<MethodDeclarationSyntax>().Any(m => m.Identifier.Text == "Equals" && m.Modifiers.Any(mod => mod.IsKind(SyntaxKind.OverrideKeyword)));
                bool hasHashCode = cls.Members.OfType<MethodDeclarationSyntax>().Any(m => m.Identifier.Text == "GetHashCode" && m.Modifiers.Any(mod => mod.IsKind(SyntaxKind.OverrideKeyword)));

                if (hasEquals ^ hasHashCode)
                {
                    int line = cls.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
                    if (changedLines == null || changedLines.Contains(line))
                    {
                        string missingMethod = hasEquals ? "GetHashCode()" : "Equals(object)";
                        var (rationale, recommendation, steps, example) = RecommendationEngine.GetEqualsHashCodeAdvice(cls.Identifier.Text, missingMethod);
                        yield return new Violation
                        {
                            RuleId = "SAF014",
                            RuleName = "Equals & GetHashCode Inconsistency",
                            TargetFile = filePath,
                            MemberName = cls.Identifier.Text,
                            LineNumber = line,
                            Severity = ViolationSeverity.Warning,
                            Description = $"Class '{cls.Identifier.Text}' overrides one of Equals/GetHashCode but not the other, breaking hash-based collections.",
                            Rationale = rationale,
                            RecommendedFix = recommendation,
                            ActionSteps = steps,
                            CodeExample = example
                        };
                    }
                }
            }

            // 15. SAF015: Dangerous Explicit Cast
            foreach (var cast in root.DescendantNodes().OfType<CastExpressionSyntax>())
            {
                if (cast.Expression is IdentifierNameSyntax && !cast.Ancestors().Any(a => a is IfStatementSyntax || a is IsPatternExpressionSyntax))
                {
                    string castType = cast.Type.ToString();
                    if (!IsPrimitiveType(castType) && !castType.EndsWith("?"))
                    {
                        int line = cast.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
                        if (changedLines == null || changedLines.Contains(line))
                        {
                            var (rationale, recommendation, steps, example) = RecommendationEngine.GetDangerousCastAdvice(cast.ToString(), castType);
                            yield return new Violation
                            {
                                RuleId = "SAF015",
                                RuleName = "Dangerous Explicit Cast",
                                TargetFile = filePath,
                                MemberName = cast.ToString(),
                                LineNumber = line,
                                Severity = ViolationSeverity.Warning,
                                Description = $"Direct explicit cast '{cast}' will throw InvalidCastException if type mismatch occurs. Use 'as' operator or pattern matching 'is'.",
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

        private static bool IsZeroValue(ExpressionSyntax expr, SyntaxNode? scope, int usageSpanStart, SyntaxNode rootNode)
        {
            if (expr is LiteralExpressionSyntax lit)
            {
                string text = lit.Token.ValueText;
                return text == "0" || text == "0.0" || text == "0f" || text == "0m" || text == "0d" || text == "0L" || text == "0D";
            }

            if (expr is ParenthesizedExpressionSyntax paren)
            {
                return IsZeroValue(paren.Expression, scope, usageSpanStart, rootNode);
            }

            if (expr is PrefixUnaryExpressionSyntax prefix)
            {
                return IsZeroValue(prefix.Operand, scope, usageSpanStart, rootNode);
            }

            if (expr is IdentifierNameSyntax id)
            {
                string varName = id.Identifier.Text;

                if (scope != null)
                {
                    var latestAssignment = scope.DescendantNodes()
                        .OfType<AssignmentExpressionSyntax>()
                        .Where(a => a.Left.ToString() == varName && a.SpanStart < usageSpanStart)
                        .OrderByDescending(a => a.SpanStart)
                        .FirstOrDefault();

                    if (latestAssignment != null)
                    {
                        return IsZeroValue(latestAssignment.Right, scope, latestAssignment.SpanStart, rootNode);
                    }

                    var declarator = scope.DescendantNodes()
                        .OfType<VariableDeclaratorSyntax>()
                        .Where(v => v.Identifier.Text == varName && v.SpanStart < usageSpanStart)
                        .OrderByDescending(v => v.SpanStart)
                        .FirstOrDefault();

                    if (declarator?.Initializer != null)
                    {
                        return IsZeroValue(declarator.Initializer.Value, scope, declarator.SpanStart, rootNode);
                    }
                }

                var classScope = (scope ?? rootNode).Ancestors().OfType<TypeDeclarationSyntax>().FirstOrDefault();
                if (classScope != null)
                {
                    var fieldDecl = classScope.DescendantNodes()
                        .OfType<VariableDeclaratorSyntax>()
                        .FirstOrDefault(v => v.Identifier.Text == varName && v.Parent?.Parent is FieldDeclarationSyntax);

                    if (fieldDecl?.Initializer != null)
                    {
                        return IsZeroValue(fieldDecl.Initializer.Value, classScope, fieldDecl.SpanStart, rootNode);
                    }
                }
            }

            return false;
        }

        private static bool IsValueType(string typeName)
        {
            return typeName == "int" || typeName == "long" || typeName == "short" || typeName == "byte" ||
                   typeName == "float" || typeName == "double" || typeName == "decimal" || typeName == "bool" ||
                   typeName == "char" || typeName == "Guid" || typeName == "DateTime" || typeName == "TimeSpan";
        }

        private static bool IsPrimitiveType(string typeName)
        {
            return IsValueType(typeName) || typeName == "object" || typeName == "string";
        }
    }
}

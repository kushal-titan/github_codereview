using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using CodeMonitor.Knowledge;
using CodeMonitor.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CodeMonitor.Analyzers
{
    public class SecurityAnalyzer : ICodeAnalyzer
    {
        public string RuleId => "SEC000";
        public string RuleName => "Application Security Analyzer";

        public IEnumerable<Violation> Analyze(SyntaxTree tree, string filePath, QualityConfig config, ISet<int>? changedLines = null)
        {
            var root = tree.GetRoot();

            // 1. SEC001: SQL Injection Detection (String Interpolation / Concatenation in SQL Commands)
            string[] sqlMethods = { "ExecuteNonQuery", "ExecuteReader", "ExecuteScalar", "FromSqlRaw", "SqlCommand", "Query", "QueryAsync", "Execute", "ExecuteAsync", "SqlDataAdapter" };
            foreach (var invocation in root.DescendantNodes().OfType<InvocationExpressionSyntax>())
            {
                string call = invocation.ToString();
                if (sqlMethods.Any(m => call.Contains(m)))
                {
                    if (invocation.ArgumentList.Arguments.Any(a =>
                        a.Expression is InterpolatedStringExpressionSyntax ||
                        (a.Expression is BinaryExpressionSyntax b && b.IsKind(SyntaxKind.AddExpression))))
                    {
                        int line = invocation.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
                        if (changedLines == null || changedLines.Contains(line))
                        {
                            var (rationale, recommendation, steps, example) = RecommendationEngine.GetSqlInjectionAdvice(invocation.ToString());
                            yield return new Violation
                            {
                                RuleId = "SEC001",
                                RuleName = "SQL Injection Vulnerability",
                                TargetFile = filePath,
                                MemberName = invocation.ToString(),
                                LineNumber = line,
                                Severity = ViolationSeverity.Error,
                                Description = "Dynamic SQL query constructed using raw string interpolation or concatenation.",
                                Rationale = rationale,
                                RecommendedFix = recommendation,
                                ActionSteps = steps,
                                CodeExample = example
                            };
                        }
                    }
                }
            }

            // 2. SEC002: Hardcoded Secrets, Passwords, and API Keys
            var secretRegex = new Regex(@"(password|passwd|api_key|apikey|secret|token|private_key|connstr|connectionstring)\s*=\s*""[^""]{6,}""", RegexOptions.IgnoreCase);
            foreach (var literal in root.DescendantNodes().OfType<LiteralExpressionSyntax>())
            {
                if (literal.IsKind(SyntaxKind.StringLiteralExpression))
                {
                    string parentStatement = literal.Parent?.ToString() ?? "";
                    if (secretRegex.IsMatch(parentStatement))
                    {
                        int line = literal.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
                        if (changedLines == null || changedLines.Contains(line))
                        {
                            var (rationale, recommendation, steps, example) = RecommendationEngine.GetHardcodedSecretAdvice(literal.ToString());
                            yield return new Violation
                            {
                                RuleId = "SEC002",
                                RuleName = "Hardcoded Secret / Credential",
                                TargetFile = filePath,
                                MemberName = literal.ToString(),
                                LineNumber = line,
                                Severity = ViolationSeverity.Error,
                                Description = "Potential hardcoded secret, password, or API key credential embedded in source code.",
                                Rationale = rationale,
                                RecommendedFix = recommendation,
                                ActionSteps = steps,
                                CodeExample = example
                            };
                        }
                    }
                }
            }

            // 3. SEC003: Insecure Cryptographic Algorithm (MD5, SHA1, DES, RC2, TripleDES)
            string[] weakCrypto = { "MD5", "SHA1", "DES", "RC2", "TripleDES", "TripleDESCryptoServiceProvider" };
            foreach (var node in root.DescendantNodes())
            {
                string? matchedName = null;
                if (node is ObjectCreationExpressionSyntax creation)
                {
                    string typeName = creation.Type.ToString();
                    if (weakCrypto.Any(w => typeName.Contains(w))) matchedName = typeName;
                }
                else if (node is InvocationExpressionSyntax invocation)
                {
                    string call = invocation.ToString();
                    if (weakCrypto.Any(w => call.Contains(w))) matchedName = call;
                }

                if (matchedName != null)
                {
                    int line = node.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
                    if (changedLines == null || changedLines.Contains(line))
                    {
                        var (rationale, recommendation, steps, example) = RecommendationEngine.GetWeakCryptoAdvice(matchedName);
                        yield return new Violation
                        {
                            RuleId = "SEC003",
                            RuleName = "Weak Cryptographic Algorithm",
                            TargetFile = filePath,
                            MemberName = matchedName,
                            LineNumber = line,
                            Severity = ViolationSeverity.Error,
                            Description = $"Insecure cryptography provider '{matchedName}' detected.",
                            Rationale = rationale,
                            RecommendedFix = recommendation,
                            ActionSteps = steps,
                            CodeExample = example
                        };
                    }
                }
            }

            // 4. SEC004: Cross-Site Scripting (XSS) / Raw Unencoded Output
            string[] xssSinks = { "Response.Write", "Response.WriteAsync", "HtmlString", "Html.Raw" };
            foreach (var invocation in root.DescendantNodes().OfType<InvocationExpressionSyntax>())
            {
                string call = invocation.Expression.ToString();
                if (xssSinks.Any(sink => call.EndsWith(sink) || call.Contains(sink)))
                {
                    var firstArg = invocation.ArgumentList.Arguments.FirstOrDefault();
                    if (firstArg != null)
                    {
                        bool isDynamic = firstArg.Expression is InterpolatedStringExpressionSyntax ||
                                         firstArg.Expression is BinaryExpressionSyntax b && b.IsKind(SyntaxKind.AddExpression) ||
                                         firstArg.Expression.ToString().Contains("Request.");

                        if (isDynamic)
                        {
                            int line = invocation.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
                            if (changedLines == null || changedLines.Contains(line))
                            {
                                var (rationale, recommendation, steps, example) = RecommendationEngine.GetXssAdvice(invocation.ToString());
                                yield return new Violation
                                {
                                    RuleId = "SEC004",
                                    RuleName = "Cross-Site Scripting (XSS) Risk",
                                    TargetFile = filePath,
                                    MemberName = invocation.ToString(),
                                    LineNumber = line,
                                    Severity = ViolationSeverity.Error,
                                    Description = "Writing unencoded dynamic data directly to HTML response can allow XSS injection.",
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

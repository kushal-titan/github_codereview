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
        public string RuleName => "Application Security & Vulnerability Suite";

        public IEnumerable<Violation> Analyze(SyntaxTree tree, string filePath, QualityConfig config, ISet<int>? changedLines = null)
        {
            var root = tree.GetRoot();

            // 1. SEC001: SQL Injection Detection
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
                                    Description = $"Unencoded dynamic string written directly to '{call}', introducing Cross-Site Scripting risk.",
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

            // 5. SEC005: Insecure Random for Security / Tokens
            foreach (var creation in root.DescendantNodes().OfType<ObjectCreationExpressionSyntax>())
            {
                if (creation.Type.ToString() == "Random" || creation.Type.ToString() == "System.Random")
                {
                    var enclosingMethod = creation.Ancestors().OfType<MethodDeclarationSyntax>().FirstOrDefault();
                    string mName = enclosingMethod?.Identifier.Text.ToLowerInvariant() ?? "";
                    if (mName.Contains("token") || mName.Contains("password") || mName.Contains("key") || mName.Contains("auth") || mName.Contains("salt") || mName.Contains("otp"))
                    {
                        int line = creation.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
                        if (changedLines == null || changedLines.Contains(line))
                        {
                            var (rationale, recommendation, steps, example) = RecommendationEngine.GetInsecureRandomAdvice(mName);
                            yield return new Violation
                            {
                                RuleId = "SEC005",
                                RuleName = "Insecure Random Number Generator",
                                TargetFile = filePath,
                                MemberName = "new Random()",
                                LineNumber = line,
                                Severity = ViolationSeverity.Error,
                                Description = $"'System.Random' used in security-sensitive method '{enclosingMethod?.Identifier.Text}'. Use 'RandomNumberGenerator.Create()'.",
                                Rationale = rationale,
                                RecommendedFix = recommendation,
                                ActionSteps = steps,
                                CodeExample = example
                            };
                        }
                    }
                }
            }

            // 6. SEC006: Path Traversal Vulnerability
            string[] fileIoMethods = { "File.Open", "File.ReadAllText", "File.WriteAllText", "File.ReadAllBytes", "File.Delete", "Directory.Delete" };
            foreach (var invocation in root.DescendantNodes().OfType<InvocationExpressionSyntax>())
            {
                string call = invocation.Expression.ToString();
                if (fileIoMethods.Any(m => call == m))
                {
                    var firstArg = invocation.ArgumentList.Arguments.FirstOrDefault();
                    if (firstArg != null && (firstArg.Expression is IdentifierNameSyntax || firstArg.Expression is InterpolatedStringExpressionSyntax))
                    {
                        bool isChecked = invocation.Ancestors().OfType<BaseMethodDeclarationSyntax>().Any(m =>
                            m.ToString().Contains("Path.GetFileName") || m.ToString().Contains("Path.GetFullPath"));

                        if (!isChecked)
                        {
                            int line = invocation.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
                            if (changedLines == null || changedLines.Contains(line))
                            {
                                var (rationale, recommendation, steps, example) = RecommendationEngine.GetPathTraversalAdvice(call);
                                yield return new Violation
                                {
                                    RuleId = "SEC006",
                                    RuleName = "Path Traversal Vulnerability",
                                    TargetFile = filePath,
                                    MemberName = call,
                                    LineNumber = line,
                                    Severity = ViolationSeverity.Error,
                                    Description = $"File I/O operation '{call}' accesses files using dynamic path without canonical path sanitization.",
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

            // 7. SEC007: Insecure Deserialization
            string[] dangerousFormatters = { "BinaryFormatter", "NetDataContractSerializer", "LosFormatter", "SoapFormatter" };
            foreach (var creation in root.DescendantNodes().OfType<ObjectCreationExpressionSyntax>())
            {
                string typeName = creation.Type.ToString();
                if (dangerousFormatters.Any(d => typeName.Contains(d)))
                {
                    int line = creation.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
                    if (changedLines == null || changedLines.Contains(line))
                    {
                        var (rationale, recommendation, steps, example) = RecommendationEngine.GetInsecureDeserializationAdvice(typeName);
                        yield return new Violation
                        {
                            RuleId = "SEC007",
                            RuleName = "Insecure Deserialization Provider",
                            TargetFile = filePath,
                            MemberName = typeName,
                            LineNumber = line,
                            Severity = ViolationSeverity.Error,
                            Description = $"Insecure deserializer '{typeName}' allows arbitrary remote code execution. Use System.Text.Json or Protobuf.",
                            Rationale = rationale,
                            RecommendedFix = recommendation,
                            ActionSteps = steps,
                            CodeExample = example
                        };
                    }
                }
            }

            // 8. SEC008: Command Injection
            foreach (var invocation in root.DescendantNodes().OfType<InvocationExpressionSyntax>())
            {
                string call = invocation.Expression.ToString();
                if (call == "Process.Start")
                {
                    bool hasDynamicArgs = invocation.ArgumentList.Arguments.Count > 1 &&
                        (invocation.ArgumentList.Arguments[1].Expression is InterpolatedStringExpressionSyntax ||
                         invocation.ArgumentList.Arguments[1].Expression is BinaryExpressionSyntax);

                    if (hasDynamicArgs)
                    {
                        int line = invocation.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
                        if (changedLines == null || changedLines.Contains(line))
                        {
                            var (rationale, recommendation, steps, example) = RecommendationEngine.GetCommandInjectionAdvice(invocation.ToString());
                            yield return new Violation
                            {
                                RuleId = "SEC008",
                                RuleName = "Command Injection Vulnerability",
                                TargetFile = filePath,
                                MemberName = "Process.Start",
                                LineNumber = line,
                                Severity = ViolationSeverity.Error,
                                Description = "Process.Start invoked with dynamically concatenated command arguments, risking OS command injection.",
                                Rationale = rationale,
                                RecommendedFix = recommendation,
                                ActionSteps = steps,
                                CodeExample = example
                            };
                        }
                    }
                }
            }

            // 9. SEC009: XML External Entity (XXE) Injection
            foreach (var creation in root.DescendantNodes().OfType<ObjectCreationExpressionSyntax>())
            {
                string typeName = creation.Type.ToString();
                if (typeName == "XmlDocument" || typeName == "XmlReaderSettings")
                {
                    var enclosingMethod = creation.Ancestors().OfType<BaseMethodDeclarationSyntax>().FirstOrDefault();
                    string mText = enclosingMethod?.ToString() ?? "";
                    if (!mText.Contains("DtdProcessing.Prohibit") && !mText.Contains("XmlResolver = null"))
                    {
                        int line = creation.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
                        if (changedLines == null || changedLines.Contains(line))
                        {
                            var (rationale, recommendation, steps, example) = RecommendationEngine.GetXxeAdvice(typeName);
                            yield return new Violation
                            {
                                RuleId = "SEC009",
                                RuleName = "XML External Entity (XXE) Vulnerability",
                                TargetFile = filePath,
                                MemberName = typeName,
                                LineNumber = line,
                                Severity = ViolationSeverity.Error,
                                Description = $"XML parser '{typeName}' configured without prohibiting external DTD processing, risking XXE injection.",
                                Rationale = rationale,
                                RecommendedFix = recommendation,
                                ActionSteps = steps,
                                CodeExample = example
                            };
                        }
                    }
                }
            }

            // 10. SEC010: Open Redirect Vulnerability
            foreach (var invocation in root.DescendantNodes().OfType<InvocationExpressionSyntax>())
            {
                string call = invocation.Expression.ToString();
                if (call == "Response.Redirect" || call == "Redirect")
                {
                    var firstArg = invocation.ArgumentList.Arguments.FirstOrDefault();
                    if (firstArg != null && firstArg.Expression is IdentifierNameSyntax)
                    {
                        var enclosingMethod = invocation.Ancestors().OfType<BaseMethodDeclarationSyntax>().FirstOrDefault();
                        string mText = enclosingMethod?.ToString() ?? "";
                        if (!mText.Contains("Url.IsLocalUrl") && !mText.Contains("IsLocalUrl"))
                        {
                            int line = invocation.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
                            if (changedLines == null || changedLines.Contains(line))
                            {
                                var (rationale, recommendation, steps, example) = RecommendationEngine.GetOpenRedirectAdvice(firstArg.ToString());
                                yield return new Violation
                                {
                                    RuleId = "SEC010",
                                    RuleName = "Open Redirect Vulnerability",
                                    TargetFile = filePath,
                                    MemberName = call,
                                    LineNumber = line,
                                    Severity = ViolationSeverity.Error,
                                    Description = $"Redirect target '{firstArg}' is dynamic without 'Url.IsLocalUrl()' verification, exposing phishing redirect risks.",
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

            // 11. SEC011: Insecure Cookie Flags (Missing HttpOnly / Secure)
            foreach (var creation in root.DescendantNodes().OfType<ObjectCreationExpressionSyntax>())
            {
                if (creation.Type.ToString().Contains("HttpCookie") || creation.Type.ToString().Contains("CookieOptions"))
                {
                    var enclosingScope = creation.Ancestors().OfType<BaseMethodDeclarationSyntax>().FirstOrDefault();
                    string scopeText = enclosingScope?.ToString() ?? "";
                    if (!scopeText.Contains("HttpOnly = true") && !scopeText.Contains(".HttpOnly = true"))
                    {
                        int line = creation.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
                        if (changedLines == null || changedLines.Contains(line))
                        {
                            var (rationale, recommendation, steps, example) = RecommendationEngine.GetInsecureCookieAdvice();
                            yield return new Violation
                            {
                                RuleId = "SEC011",
                                RuleName = "Insecure Cookie Configuration",
                                TargetFile = filePath,
                                MemberName = creation.Type.ToString(),
                                LineNumber = line,
                                Severity = ViolationSeverity.Warning,
                                Description = "Cookie created without 'HttpOnly = true' and 'Secure = true' flags, exposing session cookies to XSS theft.",
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

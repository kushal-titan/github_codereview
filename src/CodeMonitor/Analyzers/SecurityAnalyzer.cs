using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
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

            // 1. SEC001: SQL Injection Detection (String Interpolation in SQL Commands)
            foreach (var invocation in root.DescendantNodes().OfType<InvocationExpressionSyntax>())
            {
                string call = invocation.ToString();
                if (call.Contains("ExecuteNonQuery") || call.Contains("ExecuteReader") || call.Contains("FromSqlRaw") || call.Contains("SqlCommand"))
                {
                    if (invocation.ArgumentList.Arguments.Any(a => a.Expression is InterpolatedStringExpressionSyntax || a.Expression is BinaryExpressionSyntax b && b.IsKind(SyntaxKind.AddExpression)))
                    {
                        int line = invocation.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
                        if (changedLines == null || changedLines.Contains(line))
                        {
                            yield return new Violation
                            {
                                RuleId = "SEC001",
                                RuleName = "SQL Injection Vulnerability",
                                TargetFile = filePath,
                                MemberName = invocation.ToString(),
                                LineNumber = line,
                                Severity = ViolationSeverity.Error,
                                Description = "Dynamic SQL query constructed using raw string interpolation or concatenation.",
                                RecommendedFix = "Use parameterized queries (e.g. SqlParameter, Dapper parameters, or EF Core FromSqlInterpolated)."
                            };
                        }
                    }
                }
            }

            // 2. SEC002: Hardcoded Secrets, Passwords, and API Keys
            var secretRegex = new Regex(@"(password|passwd|api_key|apikey|secret|token|private_key)\s*=\s*""[^""]{6,}""", RegexOptions.IgnoreCase);
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
                            yield return new Violation
                            {
                                RuleId = "SEC002",
                                RuleName = "Hardcoded Secret / Credential",
                                TargetFile = filePath,
                                MemberName = literal.ToString(),
                                LineNumber = line,
                                Severity = ViolationSeverity.Error,
                                Description = "Potential hardcoded secret or API key credential embedded in source code.",
                                RecommendedFix = "Move secrets to Azure Key Vault, AWS Secrets Manager, or Environment Variables."
                            };
                        }
                    }
                }
            }

            // 3. SEC003: Insecure Cryptographic Algorithm
            string[] weakCrypto = { "MD5", "SHA1", "DES", "RC2", "TripleDESCryptoServiceProvider" };
            foreach (var creation in root.DescendantNodes().OfType<ObjectCreationExpressionSyntax>())
            {
                string typeName = creation.Type.ToString();
                if (weakCrypto.Any(w => typeName.Contains(w)))
                {
                    int line = creation.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
                    if (changedLines == null || changedLines.Contains(line))
                    {
                        yield return new Violation
                        {
                            RuleId = "SEC003",
                            RuleName = "Weak Cryptographic Algorithm",
                            TargetFile = filePath,
                            MemberName = typeName,
                            LineNumber = line,
                            Severity = ViolationSeverity.Error,
                            Description = $"Insecure cryptography provider '{typeName}' detected.",
                            RecommendedFix = "Upgrade to strong algorithms: SHA-256 / SHA-512 for hashing or AES-256-GCM for encryption."
                        };
                    }
                }
            }
        }
    }
}

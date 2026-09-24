using System.Collections.Generic;
using System.Linq;
using CodeMonitor.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CodeMonitor.Analyzers
{
    public class ArchitectureAnalyzer : ICodeAnalyzer
    {
        public string RuleId => "ARCH000";
        public string RuleName => "Architecture & Clean Standards Analyzer";

        public IEnumerable<Violation> Analyze(SyntaxTree tree, string filePath, QualityConfig config, ISet<int>? changedLines = null)
        {
            var root = tree.GetRoot();

            // 1. ARCH001: Interface Naming Rule (Must begin with 'I')
            foreach (var iface in root.DescendantNodes().OfType<InterfaceDeclarationSyntax>())
            {
                string name = iface.Identifier.Text;
                if (!name.StartsWith("I") || (name.Length > 1 && char.IsLower(name[1])))
                {
                    int line = iface.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
                    if (changedLines == null || changedLines.Contains(line))
                    {
                        yield return new Violation
                        {
                            RuleId = "ARCH001",
                            RuleName = "Interface Naming Violation",
                            TargetFile = filePath,
                            MemberName = name,
                            LineNumber = line,
                            Severity = ViolationSeverity.Warning,
                            Description = $"Interface '{name}' must start with an uppercase 'I' prefix followed by PascalCase.",
                            RecommendedFix = $"Rename interface to 'I{name}'."
                        };
                    }
                }
            }

            // 2. ARCH002: Async Method Naming Rule (Must end with 'Async')
            foreach (var method in root.DescendantNodes().OfType<MethodDeclarationSyntax>())
            {
                if (method.Modifiers.Any(SyntaxKind.AsyncKeyword))
                {
                    string name = method.Identifier.Text;
                    if (!name.EndsWith("Async") && name != "Main")
                    {
                        int line = method.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
                        if (changedLines == null || changedLines.Contains(line))
                        {
                            yield return new Violation
                            {
                                RuleId = "ARCH002",
                                RuleName = "Async Naming Suffix Missing",
                                TargetFile = filePath,
                                MemberName = name,
                                LineNumber = line,
                                Severity = ViolationSeverity.Warning,
                                Description = $"Async method '{name}' should end with the 'Async' suffix (e.g. '{name}Async').",
                                RecommendedFix = $"Rename method to '{name}Async'."
                            };
                        }
                    }
                }
            }
        }
    }
}

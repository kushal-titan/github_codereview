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
    public class ArchitectureAnalyzer : ICodeAnalyzer
    {
        public string RuleId => "ARCH000";
        public string RuleName => "Architecture & Clean Standards Analyzer";

        public IEnumerable<Violation> Analyze(SyntaxTree tree, string filePath, QualityConfig config, ISet<int>? changedLines = null)
        {
            var root = tree.GetRoot();

            // 1. ARCH001: Interface Naming Rule (Must begin with 'I' + PascalCase)
            foreach (var iface in root.DescendantNodes().OfType<InterfaceDeclarationSyntax>())
            {
                string name = iface.Identifier.Text;
                if (!name.StartsWith("I") || (name.Length > 1 && char.IsLower(name[1])))
                {
                    int line = iface.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
                    if (changedLines == null || changedLines.Contains(line))
                    {
                        string expected = name.StartsWith("I") ? name : "I" + ToPascalCase(name);
                        var (rationale, recommendation, steps, example) = RecommendationEngine.GetInterfaceNamingAdvice(name, expected);
                        yield return new Violation
                        {
                            RuleId = "ARCH001",
                            RuleName = "Interface Naming Violation",
                            TargetFile = filePath,
                            MemberName = name,
                            LineNumber = line,
                            Severity = ViolationSeverity.Warning,
                            Description = $"Interface '{name}' must start with an uppercase 'I' prefix followed by PascalCase.",
                            Rationale = rationale,
                            RecommendedFix = recommendation,
                            ActionSteps = steps,
                            CodeExample = example
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
                            string expected = name + "Async";
                            var (rationale, recommendation, steps, example) = RecommendationEngine.GetAsyncNamingAdvice(name, expected);
                            yield return new Violation
                            {
                                RuleId = "ARCH002",
                                RuleName = "Async Naming Suffix Missing",
                                TargetFile = filePath,
                                MemberName = name,
                                LineNumber = line,
                                Severity = ViolationSeverity.Warning,
                                Description = $"Async method '{name}' should end with the 'Async' suffix (e.g. '{expected}').",
                                Rationale = rationale,
                                RecommendedFix = recommendation,
                                ActionSteps = steps,
                                CodeExample = example
                            };
                        }
                    }
                }
            }

            // 3. ARCH003: Class / Struct / Record Naming (Must be PascalCase, no leading lowercase or underscores)
            foreach (var classNode in root.DescendantNodes().OfType<TypeDeclarationSyntax>())
            {
                if (classNode is InterfaceDeclarationSyntax) continue;

                string name = classNode.Identifier.Text;
                if (string.IsNullOrEmpty(name)) continue;

                if (char.IsLower(name[0]) || name.Contains('_'))
                {
                    int line = classNode.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
                    if (changedLines == null || changedLines.Contains(line))
                    {
                        string expected = ToPascalCase(name);
                        var (rationale, recommendation, steps, example) = RecommendationEngine.GetTypeNamingAdvice(name, expected);
                        yield return new Violation
                        {
                            RuleId = "ARCH003",
                            RuleName = "Type Naming Violation (Must be PascalCase)",
                            TargetFile = filePath,
                            MemberName = name,
                            LineNumber = line,
                            Severity = ViolationSeverity.Warning,
                            Description = $"Type '{name}' violates C# naming standards. Types must be PascalCase without underscores.",
                            Rationale = rationale,
                            RecommendedFix = recommendation,
                            ActionSteps = steps,
                            CodeExample = example
                        };
                    }
                }
            }

            // 4. ARCH004: Property Naming (Must be PascalCase, no leading lowercase or underscores)
            foreach (var prop in root.DescendantNodes().OfType<PropertyDeclarationSyntax>())
            {
                string name = prop.Identifier.Text;
                if (string.IsNullOrEmpty(name)) continue;

                if (char.IsLower(name[0]) || name.Contains('_'))
                {
                    int line = prop.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
                    if (changedLines == null || changedLines.Contains(line))
                    {
                        string expected = ToPascalCase(name);
                        var (rationale, recommendation, steps, example) = RecommendationEngine.GetPropertyNamingAdvice(name, expected);
                        yield return new Violation
                        {
                            RuleId = "ARCH004",
                            RuleName = "Property Naming Violation (Must be PascalCase)",
                            TargetFile = filePath,
                            MemberName = name,
                            LineNumber = line,
                            Severity = ViolationSeverity.Warning,
                            Description = $"Property '{name}' must be PascalCase and start with an uppercase letter.",
                            Rationale = rationale,
                            RecommendedFix = recommendation,
                            ActionSteps = steps,
                            CodeExample = example
                        };
                    }
                }
            }

            // 5. ARCH005: Variable & Parameter Naming (Local variables & parameters must be camelCase; no snake_case)
            foreach (var varDecl in root.DescendantNodes().OfType<VariableDeclaratorSyntax>())
            {
                string name = varDecl.Identifier.Text;
                if (string.IsNullOrEmpty(name) || name.Length < 2) continue;

                bool isSnakeCase = name.Contains('_');
                bool isLocal = varDecl.Ancestors().Any(a => a is LocalDeclarationStatementSyntax);
                bool isPascalLocal = isLocal && char.IsUpper(name[0]);

                if (isSnakeCase || isPascalLocal)
                {
                    int line = varDecl.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
                    if (changedLines == null || changedLines.Contains(line))
                    {
                        string expected = ToCamelCase(name);
                        string problem = isSnakeCase ? "contains underscores (snake_case)" : "is PascalCase instead of camelCase";
                        var (rationale, recommendation, steps, example) = RecommendationEngine.GetVariableNamingAdvice(name, expected, problem);
                        yield return new Violation
                        {
                            RuleId = "ARCH005",
                            RuleName = "Variable Naming Violation (Must be camelCase)",
                            TargetFile = filePath,
                            MemberName = name,
                            LineNumber = line,
                            Severity = ViolationSeverity.Warning,
                            Description = $"Variable '{name}' {problem}. In C#, local variables and fields must follow camelCase conventions.",
                            Rationale = rationale,
                            RecommendedFix = recommendation,
                            ActionSteps = steps,
                            CodeExample = example
                        };
                    }
                }
            }

            // 6. ARCH006: Obsolete API Usage (Flagging [Obsolete] attributes or deprecated framework API calls)
            string[] obsoleteApis = { "BinaryFormatter", "Thread.Abort", "WebRequest.Create", "AppDomain.Unload" };
            foreach (var node in root.DescendantNodes())
            {
                string? matchedObsolete = null;

                if (node is ObjectCreationExpressionSyntax creation)
                {
                    string typeName = creation.Type.ToString();
                    if (obsoleteApis.Any(a => typeName.Contains(a))) matchedObsolete = typeName;
                }
                else if (node is InvocationExpressionSyntax invocation)
                {
                    string call = invocation.ToString();
                    if (obsoleteApis.Any(a => call.Contains(a))) matchedObsolete = call;
                }

                if (matchedObsolete != null)
                {
                    int line = node.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
                    if (changedLines == null || changedLines.Contains(line))
                    {
                        var (rationale, recommendation, steps, example) = RecommendationEngine.GetObsoleteApiAdvice(matchedObsolete);
                        yield return new Violation
                        {
                            RuleId = "ARCH006",
                            RuleName = "Obsolete API Usage",
                            TargetFile = filePath,
                            MemberName = matchedObsolete,
                            LineNumber = line,
                            Severity = ViolationSeverity.Warning,
                            Description = $"Reference to deprecated API '{matchedObsolete}' detected.",
                            Rationale = rationale,
                            RecommendedFix = recommendation,
                            ActionSteps = steps,
                            CodeExample = example
                        };
                    }
                }
            }

            // 7. ARCH007: Method PascalCase Naming (All method declarations must start with uppercase letter)
            foreach (var method in root.DescendantNodes().OfType<MethodDeclarationSyntax>())
            {
                string name = method.Identifier.Text;
                if (string.IsNullOrEmpty(name)) continue;

                // Ignore explicit interface implementations or special methods
                if (method.ExplicitInterfaceSpecifier != null) continue;

                if (char.IsLower(name[0]) || name.Contains('_'))
                {
                    int line = method.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
                    if (changedLines == null || changedLines.Contains(line))
                    {
                        string expected = ToPascalCase(name);
                        var (rationale, recommendation, steps, example) = RecommendationEngine.GetMethodNamingAdvice(name, expected);
                        yield return new Violation
                        {
                            RuleId = "ARCH007",
                            RuleName = "Method Naming Violation (Must be PascalCase)",
                            TargetFile = filePath,
                            MemberName = name,
                            LineNumber = line,
                            Severity = ViolationSeverity.Warning,
                            Description = $"Method '{name}' must start with an uppercase letter and use PascalCase conventions.",
                            Rationale = rationale,
                            RecommendedFix = recommendation,
                            ActionSteps = steps,
                            CodeExample = example
                        };
                    }
                }
            }

            // 8. ARCH008: Private / Internal Field Casing Standard (Must be _camelCase or camelCase)
            foreach (var field in root.DescendantNodes().OfType<FieldDeclarationSyntax>())
            {
                bool isPublic = field.Modifiers.Any(SyntaxKind.PublicKeyword);
                bool isConst = field.Modifiers.Any(SyntaxKind.ConstKeyword);
                bool isStaticReadonly = field.Modifiers.Any(SyntaxKind.StaticKeyword) && field.Modifiers.Any(SyntaxKind.ReadOnlyKeyword);

                if (!isPublic && !isConst && !isStaticReadonly)
                {
                    foreach (var variable in field.Declaration.Variables)
                    {
                        string name = variable.Identifier.Text;
                        if (string.IsNullOrEmpty(name) || name.Length < 2) continue;

                        // Flag PascalCase fields without leading underscore (e.g. EmployeeList)
                        if (char.IsUpper(name[0]))
                        {
                            int line = variable.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
                            if (changedLines == null || changedLines.Contains(line))
                            {
                                string expected = "_" + ToCamelCase(name);
                                var (rationale, recommendation, steps, example) = RecommendationEngine.GetFieldNamingAdvice(name, expected);
                                yield return new Violation
                                {
                                    RuleId = "ARCH008",
                                    RuleName = "Field Casing Violation (PascalCase in Private Field)",
                                    TargetFile = filePath,
                                    MemberName = name,
                                    LineNumber = line,
                                    Severity = ViolationSeverity.Warning,
                                    Description = $"Private field '{name}' is declared in PascalCase. Private/internal fields should use camelCase or an underscore prefix (e.g., '{expected}').",
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

        private static string ToPascalCase(string name)
        {
            if (string.IsNullOrEmpty(name)) return name;
            var parts = name.Split(new[] { '_' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) return name;
            return string.Concat(parts.Select(p => char.ToUpper(p[0]) + (p.Length > 1 ? p.Substring(1) : "")));
        }

        private static string ToCamelCase(string name)
        {
            string pascal = ToPascalCase(name);
            if (string.IsNullOrEmpty(pascal)) return pascal;
            return char.ToLower(pascal[0]) + (pascal.Length > 1 ? pascal.Substring(1) : "");
        }
    }
}

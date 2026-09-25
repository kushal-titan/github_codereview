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
        public string RuleName => "Architecture & Clean Standards Suite";

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

            // 6. ARCH006: Obsolete API Usage
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

            // 9. ARCH009: Console I/O in Domain Logic (Console.WriteLine in business logic classes)
            foreach (var invocation in root.DescendantNodes().OfType<InvocationExpressionSyntax>())
            {
                string expr = invocation.Expression.ToString();
                if (expr.StartsWith("Console.Write") || expr.StartsWith("Console.WriteLine"))
                {
                    var enclosingType = invocation.Ancestors().OfType<ClassDeclarationSyntax>().FirstOrDefault();
                    string tName = enclosingType?.Identifier.Text ?? "";
                    if (tName != "Program" && !tName.EndsWith("Console") && !tName.EndsWith("Cli") && !tName.EndsWith("Demo"))
                    {
                        int line = invocation.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
                        if (changedLines == null || changedLines.Contains(line))
                        {
                            var (rationale, recommendation, steps, example) = RecommendationEngine.GetConsoleInDomainAdvice(tName);
                            yield return new Violation
                            {
                                RuleId = "ARCH009",
                                RuleName = "Console I/O in Business Domain Logic",
                                TargetFile = filePath,
                                MemberName = expr,
                                LineNumber = line,
                                Severity = ViolationSeverity.Warning,
                                Description = $"Direct '{expr}' call inside domain class '{tName}'. Use ILogger<T> structured logging.",
                                Rationale = rationale,
                                RecommendedFix = recommendation,
                                ActionSteps = steps,
                                CodeExample = example
                            };
                        }
                    }
                }
            }

            // 10. ARCH010: Public Field Violation (Public fields instead of properties)
            foreach (var field in root.DescendantNodes().OfType<FieldDeclarationSyntax>())
            {
                bool isPublic = field.Modifiers.Any(SyntaxKind.PublicKeyword);
                bool isConst = field.Modifiers.Any(SyntaxKind.ConstKeyword);
                bool isStaticReadonly = field.Modifiers.Any(SyntaxKind.StaticKeyword) && field.Modifiers.Any(SyntaxKind.ReadOnlyKeyword);

                if (isPublic && !isConst && !isStaticReadonly)
                {
                    foreach (var variable in field.Declaration.Variables)
                    {
                        int line = variable.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
                        if (changedLines == null || changedLines.Contains(line))
                        {
                            string fName = variable.Identifier.Text;
                            var (rationale, recommendation, steps, example) = RecommendationEngine.GetPublicFieldAdvice(fName, field.Declaration.Type.ToString());
                            yield return new Violation
                            {
                                RuleId = "ARCH010",
                                RuleName = "Public Field Encapsulation Violation",
                                TargetFile = filePath,
                                MemberName = fName,
                                LineNumber = line,
                                Severity = ViolationSeverity.Warning,
                                Description = $"Public field '{fName}' violates encapsulation. Use auto-implemented properties ({{ get; set; }}).",
                                Rationale = rationale,
                                RecommendedFix = recommendation,
                                ActionSteps = steps,
                                CodeExample = example
                            };
                        }
                    }
                }
            }

            // 11. ARCH011: Empty Marker Interface Anti-Pattern
            foreach (var iface in root.DescendantNodes().OfType<InterfaceDeclarationSyntax>())
            {
                if (iface.Members.Count == 0 && (iface.BaseList == null || iface.BaseList.Types.Count == 0))
                {
                    int line = iface.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
                    if (changedLines == null || changedLines.Contains(line))
                    {
                        var (rationale, recommendation, steps, example) = RecommendationEngine.GetEmptyInterfaceAdvice(iface.Identifier.Text);
                        yield return new Violation
                        {
                            RuleId = "ARCH011",
                            RuleName = "Empty Marker Interface Anti-Pattern",
                            TargetFile = filePath,
                            MemberName = iface.Identifier.Text,
                            LineNumber = line,
                            Severity = ViolationSeverity.Warning,
                            Description = $"Interface '{iface.Identifier.Text}' is an empty marker interface. Use custom Attributes instead.",
                            Rationale = rationale,
                            RecommendedFix = recommendation,
                            ActionSteps = steps,
                            CodeExample = example
                        };
                    }
                }
            }

            // 12. ARCH012: Layer Boundary Violation (Domain referencing Presentation/UI namespaces)
            var usings = root.DescendantNodes().OfType<UsingDirectiveSyntax>();
            bool isDomainFile = filePath.Replace('\\', '/').Contains("/Domain/") || filePath.Replace('\\', '/').Contains("/Entities/") || filePath.Replace('\\', '/').Contains("/Core/");
            if (isDomainFile)
            {
                foreach (var u in usings)
                {
                    string uName = u.Name?.ToString() ?? "";
                    if (uName.Contains("Microsoft.AspNetCore") || uName.Contains("System.Web") || uName.Contains("Controllers") || uName.Contains("Views"))
                    {
                        int line = u.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
                        if (changedLines == null || changedLines.Contains(line))
                        {
                            var (rationale, recommendation, steps, example) = RecommendationEngine.GetLayerBoundaryAdvice(uName);
                            yield return new Violation
                            {
                                RuleId = "ARCH012",
                                RuleName = "Layer Boundary Violation",
                                TargetFile = filePath,
                                MemberName = uName,
                                LineNumber = line,
                                Severity = ViolationSeverity.Warning,
                                Description = $"Domain layer file imports presentation namespace '{uName}', violating clean architecture inversion of control.",
                                Rationale = rationale,
                                RecommendedFix = recommendation,
                                ActionSteps = steps,
                                CodeExample = example
                            };
                        }
                    }
                }
            }

            // 13. ARCH013: Direct DbContext in Controller
            foreach (var creation in root.DescendantNodes().OfType<ObjectCreationExpressionSyntax>())
            {
                string tName = creation.Type.ToString();
                if (tName.EndsWith("DbContext") || tName.EndsWith("Context"))
                {
                    var enclosingClass = creation.Ancestors().OfType<ClassDeclarationSyntax>().FirstOrDefault();
                    if (enclosingClass != null && enclosingClass.Identifier.Text.EndsWith("Controller"))
                    {
                        int line = creation.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
                        if (changedLines == null || changedLines.Contains(line))
                        {
                            var (rationale, recommendation, steps, example) = RecommendationEngine.GetDirectDbContextInControllerAdvice(enclosingClass.Identifier.Text, tName);
                            yield return new Violation
                            {
                                RuleId = "ARCH013",
                                RuleName = "Direct DbContext Instantiation in Controller",
                                TargetFile = filePath,
                                MemberName = $"{enclosingClass.Identifier.Text}->new {tName}",
                                LineNumber = line,
                                Severity = ViolationSeverity.Warning,
                                Description = $"Controller '{enclosingClass.Identifier.Text}' directly instantiates '{tName}' instead of using dependency injection.",
                                Rationale = rationale,
                                RecommendedFix = recommendation,
                                ActionSteps = steps,
                                CodeExample = example
                            };
                        }
                    }
                }
            }

            // 14. ARCH014: Magic Literal Values in Conditions
            foreach (var binary in root.DescendantNodes().OfType<BinaryExpressionSyntax>())
            {
                if (binary.IsKind(SyntaxKind.EqualsExpression) || binary.IsKind(SyntaxKind.NotEqualsExpression) ||
                    binary.IsKind(SyntaxKind.GreaterThanExpression) || binary.IsKind(SyntaxKind.LessThanExpression))
                {
                    if (binary.Right is LiteralExpressionSyntax lit && lit.IsKind(SyntaxKind.NumericLiteralExpression))
                    {
                        int val = 0;
                        if (int.TryParse(lit.Token.ValueText, out val) && val > 1 && val != 100 && val != 0)
                        {
                            var enclosingStatement = binary.Ancestors().OfType<IfStatementSyntax>().FirstOrDefault();
                            if (enclosingStatement != null)
                            {
                                int line = binary.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
                                if (changedLines == null || changedLines.Contains(line))
                                {
                                    var (rationale, recommendation, steps, example) = RecommendationEngine.GetMagicLiteralAdvice(lit.Token.ValueText);
                                    yield return new Violation
                                    {
                                        RuleId = "ARCH014",
                                        RuleName = "Magic Literal in Conditional Logic",
                                        TargetFile = filePath,
                                        MemberName = binary.ToString(),
                                        LineNumber = line,
                                        Severity = ViolationSeverity.Warning,
                                        Description = $"Magic literal number '{lit.Token.ValueText}' used in conditional logic without named constant or enum.",
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

            // 15. ARCH015: Multiple Types Declared in Single File
            var declaredTypes = root.DescendantNodes().OfType<TypeDeclarationSyntax>()
                .Where(t => t.Parent is BaseNamespaceDeclarationSyntax || t.Parent is CompilationUnitSyntax)
                .ToList();

            if (declaredTypes.Count > 1)
            {
                for (int i = 1; i < declaredTypes.Count; i++)
                {
                    var extraType = declaredTypes[i];
                    int line = extraType.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
                    if (changedLines == null || changedLines.Contains(line))
                    {
                        var (rationale, recommendation, steps, example) = RecommendationEngine.GetMultipleTypesPerFileAdvice(extraType.Identifier.Text);
                        yield return new Violation
                        {
                            RuleId = "ARCH015",
                            RuleName = "Multiple Top-Level Types in Single File",
                            TargetFile = filePath,
                            MemberName = extraType.Identifier.Text,
                            LineNumber = line,
                            Severity = ViolationSeverity.Warning,
                            Description = $"Type '{extraType.Identifier.Text}' is declared alongside '{declaredTypes[0].Identifier.Text}'. Each top-level type should reside in its own .cs file.",
                            Rationale = rationale,
                            RecommendedFix = recommendation,
                            ActionSteps = steps,
                            CodeExample = example
                        };
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

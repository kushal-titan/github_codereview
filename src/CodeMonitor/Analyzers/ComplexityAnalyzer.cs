using System.Collections.Generic;
using System.Linq;
using CodeMonitor.Knowledge;
using CodeMonitor.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CodeMonitor.Analyzers
{
    public class ComplexityAnalyzer : ICodeAnalyzer
    {
        public string RuleId => "CQ002";
        public string RuleName => "Structural & Complexity Suite";

        public IEnumerable<Violation> Analyze(SyntaxTree tree, string filePath, QualityConfig config, ISet<int>? changedLines = null)
        {
            var root = tree.GetRoot();

            // 1. CQ002: Cyclomatic Complexity & CQ007: Cognitive Complexity on Methods/Constructors
            var methodNodes = root.DescendantNodes().OfType<BaseMethodDeclarationSyntax>();
            foreach (var method in methodNodes)
            {
                var lineSpan = method.GetLocation().GetLineSpan();
                int startLine = lineSpan.StartLinePosition.Line + 1;
                int endLine = lineSpan.EndLinePosition.Line + 1;
                int lineCount = (endLine - startLine) + 1;

                if (changedLines != null && changedLines.Count > 0)
                {
                    bool isModified = Enumerable.Range(startLine, lineCount).Any(line => changedLines.Contains(line));
                    if (!isModified)
                    {
                        continue;
                    }
                }

                string methodName = method is MethodDeclarationSyntax m
                    ? m.Identifier.Text
                    : (method is ConstructorDeclarationSyntax c ? c.Identifier.Text : "AnonymousMethod");

                // CQ002: Cyclomatic Complexity
                int complexity = CalculateComplexity(method);
                if (complexity > config.MaxCyclomaticComplexity)
                {
                    var (rationale, recommendation, steps, example) = RecommendationEngine.GetComplexityAdvice(methodName, complexity, config.MaxCyclomaticComplexity);

                    yield return new Violation
                    {
                        RuleId = "CQ002",
                        RuleName = "High Cyclomatic Complexity",
                        TargetFile = filePath,
                        MemberName = methodName,
                        LineNumber = startLine,
                        EndLineNumber = endLine,
                        ActualValue = complexity,
                        ThresholdValue = config.MaxCyclomaticComplexity,
                        Severity = complexity > (config.MaxCyclomaticComplexity * 1.5) ? ViolationSeverity.Error : ViolationSeverity.Warning,
                        Description = $"Method '{methodName}' has a cyclomatic complexity of {complexity} (maximum allowed: {config.MaxCyclomaticComplexity}).",
                        Rationale = rationale,
                        RecommendedFix = recommendation,
                        ActionSteps = steps,
                        CodeExample = example
                    };
                }

                // CQ007: Cognitive Complexity (Weighted mental overhead)
                int cognitive = CalculateCognitiveComplexity(method);
                int cognitiveLimit = 15;
                if (cognitive > cognitiveLimit)
                {
                    var (rationale, recommendation, steps, example) = RecommendationEngine.GetCognitiveComplexityAdvice(methodName, cognitive, cognitiveLimit);
                    yield return new Violation
                    {
                        RuleId = "CQ007",
                        RuleName = "High Cognitive Complexity",
                        TargetFile = filePath,
                        MemberName = methodName,
                        LineNumber = startLine,
                        EndLineNumber = endLine,
                        ActualValue = cognitive,
                        ThresholdValue = cognitiveLimit,
                        Severity = cognitive > 25 ? ViolationSeverity.Error : ViolationSeverity.Warning,
                        Description = $"Method '{methodName}' has high cognitive complexity of {cognitive} (threshold: {cognitiveLimit}), indicating high cognitive burden for maintainers.",
                        Rationale = rationale,
                        RecommendedFix = recommendation,
                        ActionSteps = steps,
                        CodeExample = example
                    };
                }

                // CQ006: Constructor Parameter Clump (> 5 dependencies)
                if (method is ConstructorDeclarationSyntax ctor && ctor.ParameterList != null && ctor.ParameterList.Parameters.Count > 5)
                {
                    int paramCount = ctor.ParameterList.Parameters.Count;
                    var (rationale, recommendation, steps, example) = RecommendationEngine.GetConstructorClumpAdvice(methodName, paramCount, 5);
                    yield return new Violation
                    {
                        RuleId = "CQ006",
                        RuleName = "Constructor Parameter Clump",
                        TargetFile = filePath,
                        MemberName = methodName,
                        LineNumber = startLine,
                        EndLineNumber = endLine,
                        ActualValue = paramCount,
                        ThresholdValue = 5,
                        Severity = ViolationSeverity.Warning,
                        Description = $"Constructor '{methodName}' has {paramCount} parameters (> 5 allowed), indicating excessive dependency injection coupling.",
                        Rationale = rationale,
                        RecommendedFix = recommendation,
                        ActionSteps = steps,
                        CodeExample = example
                    };
                }
            }

            // 2. Class Level Checks: CQ005 (God Class), CQ008 (Excessive Inheritance), CQ009 (Member Bloat)
            var classNodes = root.DescendantNodes().OfType<ClassDeclarationSyntax>();
            foreach (var cls in classNodes)
            {
                var lineSpan = cls.GetLocation().GetLineSpan();
                int startLine = lineSpan.StartLinePosition.Line + 1;
                int endLine = lineSpan.EndLinePosition.Line + 1;
                int lineCount = (endLine - startLine) + 1;

                if (changedLines != null && changedLines.Count > 0)
                {
                    bool isModified = Enumerable.Range(startLine, lineCount).Any(line => changedLines.Contains(line));
                    if (!isModified)
                    {
                        continue;
                    }
                }

                string className = cls.Identifier.Text;

                // CQ005: God Class Length (> 300 lines)
                int classLineLimit = 300;
                if (lineCount > classLineLimit)
                {
                    var (rationale, recommendation, steps, example) = RecommendationEngine.GetGodClassAdvice(className, lineCount, classLineLimit);
                    yield return new Violation
                    {
                        RuleId = "CQ005",
                        RuleName = "God Class Length Exceeded",
                        TargetFile = filePath,
                        MemberName = className,
                        LineNumber = startLine,
                        EndLineNumber = endLine,
                        ActualValue = lineCount,
                        ThresholdValue = classLineLimit,
                        Severity = ViolationSeverity.Warning,
                        Description = $"Class '{className}' has {lineCount} lines (maximum recommended: {classLineLimit}), indicating single responsibility principle violation.",
                        Rationale = rationale,
                        RecommendedFix = recommendation,
                        ActionSteps = steps,
                        CodeExample = example
                    };
                }

                // CQ008: Excessive Inheritance Depth (> 3 base types)
                if (cls.BaseList != null && cls.BaseList.Types.Count > 3)
                {
                    int inheritanceCount = cls.BaseList.Types.Count;
                    var (rationale, recommendation, steps, example) = RecommendationEngine.GetExcessiveInheritanceAdvice(className, inheritanceCount, 3);
                    yield return new Violation
                    {
                        RuleId = "CQ008",
                        RuleName = "Excessive Inheritance Hierarchy",
                        TargetFile = filePath,
                        MemberName = className,
                        LineNumber = startLine,
                        ActualValue = inheritanceCount,
                        ThresholdValue = 3,
                        Severity = ViolationSeverity.Warning,
                        Description = $"Class '{className}' implements/inherits {inheritanceCount} types (> 3 threshold), risking tight coupling.",
                        Rationale = rationale,
                        RecommendedFix = recommendation,
                        ActionSteps = steps,
                        CodeExample = example
                    };
                }

                // CQ009: Member / Method Bloat (> 20 methods)
                int methodCount = cls.Members.OfType<MethodDeclarationSyntax>().Count();
                if (methodCount > 20)
                {
                    var (rationale, recommendation, steps, example) = RecommendationEngine.GetMethodBloatAdvice(className, methodCount, 20);
                    yield return new Violation
                    {
                        RuleId = "CQ009",
                        RuleName = "Method Bloat in Class",
                        TargetFile = filePath,
                        MemberName = className,
                        LineNumber = startLine,
                        ActualValue = methodCount,
                        ThresholdValue = 20,
                        Severity = ViolationSeverity.Warning,
                        Description = $"Class '{className}' defines {methodCount} methods (> 20 threshold), indicating lack of cohesion.",
                        Rationale = rationale,
                        RecommendedFix = recommendation,
                        ActionSteps = steps,
                        CodeExample = example
                    };
                }
            }

            // 3. CQ010: Non-Exhaustive Switch Statements (Missing default case)
            var switches = root.DescendantNodes().OfType<SwitchStatementSyntax>();
            foreach (var sw in switches)
            {
                int line = sw.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
                if (changedLines != null && !changedLines.Contains(line))
                    continue;

                bool hasDefault = sw.Sections.Any(sec => sec.Labels.Any(lbl => lbl is DefaultSwitchLabelSyntax));
                if (!hasDefault && sw.Sections.Count > 0)
                {
                    var (rationale, recommendation, steps, example) = RecommendationEngine.GetNonExhaustiveSwitchAdvice();
                    yield return new Violation
                    {
                        RuleId = "CQ010",
                        RuleName = "Non-Exhaustive Switch Statement",
                        TargetFile = filePath,
                        MemberName = "switch",
                        LineNumber = line,
                        Severity = ViolationSeverity.Warning,
                        Description = "Switch statement is missing a 'default:' branch to handle unpredicted values or new enum members.",
                        Rationale = rationale,
                        RecommendedFix = recommendation,
                        ActionSteps = steps,
                        CodeExample = example
                    };
                }
            }
        }

        private static int CalculateComplexity(SyntaxNode methodNode)
        {
            int complexity = 1;

            foreach (var node in methodNode.DescendantNodes())
            {
                switch (node.Kind())
                {
                    case SyntaxKind.IfStatement:
                    case SyntaxKind.WhileStatement:
                    case SyntaxKind.ForStatement:
                    case SyntaxKind.ForEachStatement:
                    case SyntaxKind.DoStatement:
                    case SyntaxKind.CaseSwitchLabel:
                    case SyntaxKind.CasePatternSwitchLabel:
                    case SyntaxKind.CatchClause:
                    case SyntaxKind.ConditionalExpression:
                    case SyntaxKind.CoalesceExpression:
                    case SyntaxKind.CoalesceAssignmentExpression:
                        complexity++;
                        break;

                    case SyntaxKind.LogicalAndExpression:
                    case SyntaxKind.LogicalOrExpression:
                        complexity++;
                        break;
                }
            }

            return complexity;
        }

        private static int CalculateCognitiveComplexity(SyntaxNode methodNode)
        {
            int cognitive = 0;
            foreach (var node in methodNode.DescendantNodes())
            {
                int nesting = 0;
                var curr = node.Parent;
                while (curr != null && curr != methodNode)
                {
                    if (curr is IfStatementSyntax || curr is WhileStatementSyntax ||
                        curr is ForStatementSyntax || curr is ForEachStatementSyntax ||
                        curr is CatchClauseSyntax)
                    {
                        nesting++;
                    }
                    curr = curr.Parent;
                }

                switch (node.Kind())
                {
                    case SyntaxKind.IfStatement:
                    case SyntaxKind.WhileStatement:
                    case SyntaxKind.ForStatement:
                    case SyntaxKind.ForEachStatement:
                    case SyntaxKind.CatchClause:
                    case SyntaxKind.ConditionalExpression:
                        cognitive += 1 + nesting;
                        break;
                    case SyntaxKind.LogicalAndExpression:
                    case SyntaxKind.LogicalOrExpression:
                        cognitive += 1;
                        break;
                }
            }
            return cognitive;
        }
    }
}

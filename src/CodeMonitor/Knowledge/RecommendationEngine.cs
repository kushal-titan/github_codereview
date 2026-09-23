namespace CodeMonitor.Knowledge
{
    public static class RecommendationEngine
    {
        public static (string Rationale, string Recommendation) GetMethodLengthAdvice(string methodName, int actualLines, int limit)
        {
            var rationale = $"Method '{methodName}' spans {actualLines} lines (limit: {limit}). Long methods violate the Single Responsibility Principle (SRP), accumulate high cognitive load, and make unit testing and maintenance significantly harder.";
            var recommendation = "Apply the 'Extract Method' refactoring technique. Break down the method into smaller, cohesive private helper functions that each perform one well-defined task with descriptive naming.";
            return (rationale, recommendation);
        }

        public static (string Rationale, string Recommendation) GetComplexityAdvice(string methodName, int complexity, int limit)
        {
            var rationale = $"Method '{methodName}' has a Cyclomatic Complexity of {complexity} (limit: {limit}). High complexity indicates excessive branching paths, which dramatically increases bug probability and makes complete branch testing impractical.";
            var recommendation = "Simplify control flow: (1) Replace nested conditionals with guard clauses and early returns. (2) Replace complex switch/if-else ladders with Strategy or Polymorphic patterns. (3) Extract validation and sub-rules into specialized helper classes.";
            return (rationale, recommendation);
        }

        public static (string Rationale, string Recommendation) GetParameterCountAdvice(string methodName, int count, int limit)
        {
            var rationale = $"Method '{methodName}' accepts {count} parameters (limit: {limit}). Long parameter lists create fragile signatures, reduce readability, and indicate that the method may be doing too much.";
            var recommendation = "Introduce a Parameter Object (DTO or Command record) to bundle related parameters together, or leverage Builder / Options patterns.";
            return (rationale, recommendation);
        }

        public static (string Rationale, string Recommendation) GetNestingDepthAdvice(string methodName, int depth, int limit)
        {
            var rationale = $"Method '{methodName}' has a control flow nesting depth of {depth} levels (limit: {limit}). Deep nesting ('arrow anti-pattern') makes code flow hard to trace and obscures core business logic.";
            var recommendation = "Invert conditional logic using Guard Clauses ('fail fast') to return early, or extract innermost loops/conditions into private methods.";
            return (rationale, recommendation);
        }
    }
}

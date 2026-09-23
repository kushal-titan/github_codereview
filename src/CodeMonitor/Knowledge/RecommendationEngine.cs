using System.Collections.Generic;

namespace CodeMonitor.Knowledge
{
    public static class RecommendationEngine
    {
        public static (string Rationale, string Recommendation, List<string> ActionSteps, string CodeExample) GetMethodLengthAdvice(string methodName, int actualLines, int limit)
        {
            var rationale = $"Method '{methodName}' is {actualLines} lines long (allowed limit is {limit} lines). Methods of this size usually perform multiple tasks, making them error-prone, hard to read, and difficult to write unit tests for.";
            var recommendation = "Break down this long function into smaller, private helper methods using the 'Extract Method' pattern.";
            
            var steps = new List<string>
            {
                $"Identify distinct phases inside '{methodName}' (e.g. Validation, Calculation, Persistence, Formatting).",
                "Highlight each phase and extract it into a separate private method with a clear name (e.g., CalculateDiscount(), ApplyTaxes()).",
                $"Ensure '{methodName}' acts as a high-level orchestrator under {limit} lines."
            };

            var example = 
@"// ❌ BEFORE (One 80+ line method doing everything):
public decimal ProcessOrder(Order o) {
    // 30 lines of discount calculation
    // 25 lines of tax calculation
    // 25 lines of shipping logic
}

// ✅ AFTER (Orchestrator calling cohesive helper methods):
public decimal ProcessOrder(Order o) {
    var discount = CalculateDiscount(o);
    var tax = CalculateTax(o, discount);
    var shipping = CalculateShipping(o);
    return (o.Total - discount) + tax + shipping;
}";

            return (rationale, recommendation, steps, example);
        }

        public static (string Rationale, string Recommendation, List<string> ActionSteps, string CodeExample) GetComplexityAdvice(string methodName, int complexity, int limit)
        {
            var rationale = $"Method '{methodName}' has {complexity} independent execution branches (allowed limit is {limit}). Code with high cyclomatic complexity requires dozens of unit tests to cover every edge case and frequently conceals hidden production bugs.";
            var recommendation = "Simplify control flow by using Guard Clauses (early returns), dictionary mappings, or the Strategy pattern.";

            var steps = new List<string>
            {
                "Invert nested if-conditions to return early (Guard Clauses / Fail-Fast).",
                "Replace long 'if-else-if' ladders or switch blocks with Dictionary lookups or polymorphic strategy classes.",
                "Extract inner conditional blocks into separate helper methods."
            };

            var example = 
@"// ❌ BEFORE (Complex nested branching, Complexity > 15):
if (user != null) {
    if (user.IsActive) {
        if (user.Tier == ""VIP"") {
            // ...
        }
    }
}

// ✅ AFTER (Early returns with Guard Clauses, Complexity < 5):
if (user == null || !user.IsActive) return 0;
if (user.Tier == ""VIP"") return GetVipRate();
return GetStandardRate();";

            return (rationale, recommendation, steps, example);
        }

        public static (string Rationale, string Recommendation, List<string> ActionSteps, string CodeExample) GetParameterCountAdvice(string methodName, int count, int limit)
        {
            var rationale = $"Method '{methodName}' takes {count} parameters (allowed limit is {limit}). Long parameter lists make method calls hard to read, prone to argument-order bugs, and indicate the method is doing too much.";
            var recommendation = "Group related parameters into a single Parameter Object (DTO or C# record).";

            var steps = new List<string>
            {
                "Create a dedicated class or record for the parameters (e.g., `OrderCalculationRequest`).",
                $"Update '{methodName}' to accept this single parameter object.",
                "Pass properties through the object instead of individual method arguments."
            };

            var example = 
@"// ❌ BEFORE (6 parameters):
public void Calculate(User u, decimal cart, string promo, string region, bool holiday, bool gift)

// ✅ AFTER (Single Parameter DTO / Record):
public record DiscountRequest(User Customer, decimal CartTotal, string PromoCode, string Region, bool IsHoliday);
public void Calculate(DiscountRequest request)";

            return (rationale, recommendation, steps, example);
        }

        public static (string Rationale, string Recommendation, List<string> ActionSteps, string CodeExample) GetNestingDepthAdvice(string methodName, int depth, int limit)
        {
            var rationale = $"Method '{methodName}' has code nested {depth} levels deep (allowed limit is {limit} levels). This 'arrow anti-pattern' makes it extremely hard to trace the execution path and understand business logic.";
            var recommendation = "Flatten the logic by returning early (Guard Clauses) and extracting deeply nested loops into private methods.";

            var steps = new List<string>
            {
                "Inspect the outermost `if` conditions. If a condition fails, return or continue immediately.",
                "Avoid wrapping entire method bodies inside huge `if (isValid)` blocks.",
                "Extract innermost loop bodies into isolated helper functions."
            };

            var example = 
@"// ❌ BEFORE (4 levels of nesting):
if (order != null) {
    if (order.Items != null) {
        foreach (var item in order.Items) {
            if (item.Price > 0) {
                // deep logic
            }
        }
    }
}

// ✅ AFTER (Flattened with early exits):
if (order?.Items == null) return;
foreach (var item in order.Items.Where(i => i.Price > 0)) {
    ProcessItem(item);
}";

            return (rationale, recommendation, steps, example);
        }
    }
}

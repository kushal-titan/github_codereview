using System.Collections.Generic;

namespace CodeMonitor.Knowledge
{
    public static class RecommendationEngine
    {
        // ==========================================
        // 1. STRUCTURAL & COMPLEXITY METRICS (CQ)
        // ==========================================

        public static (string Rationale, string Recommendation, List<string> ActionSteps, string CodeExample) GetMethodLengthAdvice(string methodName, int actualLines, int limit)
        {
            var rationale = $"Method '{methodName}' is {actualLines} lines long (allowed limit is {limit} lines). Methods of this size usually perform multiple tasks, making them error-prone, hard to read, and difficult to test.";
            var recommendation = "Break down this long function into smaller, private helper methods using the 'Extract Method' pattern.";
            var steps = new List<string>
            {
                $"Identify distinct phases inside '{methodName}' (e.g., Validation, Calculation, Persistence, Formatting).",
                "Extract each phase into a separate private method with a descriptive name.",
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
            var rationale = $"Method '{methodName}' has {complexity} independent execution branches (allowed limit is {limit}). Code with high cyclomatic complexity requires dozens of unit tests and conceals bugs.";
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
            var rationale = $"Method '{methodName}' takes {count} parameters (allowed limit is {limit}). Long parameter lists make method calls hard to read and indicate the method is doing too much.";
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
            var rationale = $"Method '{methodName}' has code nested {depth} levels deep (allowed limit is {limit} levels). This 'arrow anti-pattern' makes it extremely hard to trace execution paths.";
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
            if (item.IsAvailable) { Process(item); }
        }
    }
}

// ✅ AFTER (Flattened 1 level of nesting):
if (order?.Items == null) return;
foreach (var item in order.Items.Where(i => i.IsAvailable)) {
    Process(item);
}";
            return (rationale, recommendation, steps, example);
        }

        public static (string Rationale, string Recommendation, List<string> ActionSteps, string CodeExample) GetGodClassAdvice(string className, int lines, int limit)
        {
            var rationale = $"Class '{className}' has {lines} lines (maximum recommended is {limit}). Monolithic classes violate the Single Responsibility Principle and are hard to test and maintain.";
            var recommendation = "Decompose this class into smaller, specialized services or components.";
            var steps = new List<string>
            {
                "Group related methods and fields into cohesive domain services.",
                "Extract data-access, reporting, and business rules into separate classes.",
                "Use Dependency Injection to compose the smaller classes together."
            };
            var example = 
@"// ❌ BEFORE (God Class managing DB, Email, Calculation, and PDF export):
public class OrderManager { /* 400 lines */ }

// ✅ AFTER (Cohesive Single Responsibility services):
public class OrderCalculator { }
public class OrderRepository { }
public class InvoicePdfGenerator { }";
            return (rationale, recommendation, steps, example);
        }

        public static (string Rationale, string Recommendation, List<string> ActionSteps, string CodeExample) GetConstructorClumpAdvice(string ctorName, int count, int limit)
        {
            var rationale = $"Constructor '{ctorName}' accepts {count} injected dependencies (threshold is {limit}). Excessive constructor parameters indicate class bloat.";
            var recommendation = "Group related services into facade services or evaluate if the class has too many responsibilities.";
            var steps = new List<string>
            {
                "Combine related fine-grained services into a Facade pattern.",
                "Split the class into smaller, more focused consumers."
            };
            var example = 
@"// ❌ BEFORE (8 injected dependencies):
public OrderService(IUserRepo u, IOrderRepo o, IEmailService e, ISmsService s, IPaymentService p, ILog l, ITax t, IShip sh)

// ✅ AFTER (Grouped into Facade / Context):
public OrderService(IOrderRepository orderRepo, INotificationFacade notifications, IPaymentProcessor payments)";
            return (rationale, recommendation, steps, example);
        }

        public static (string Rationale, string Recommendation, List<string> ActionSteps, string CodeExample) GetCognitiveComplexityAdvice(string methodName, int cognitive, int limit)
        {
            var rationale = $"Method '{methodName}' has a cognitive complexity score of {cognitive} (threshold is {limit}). High cognitive complexity causes mental fatigue and defect leakage.";
            var recommendation = "Refactor deeply nested conditionals into well-named boolean predicates and helper methods.";
            var steps = new List<string>
            {
                "Extract complex boolean expressions into descriptive private methods (e.g., `CanApplyDiscount()`).",
                "Replace nested loops and conditionals with LINQ pipeline transformations."
            };
            var example = 
@"// ❌ BEFORE (High Cognitive Load):
if (isMember && (points > 100 || (hasCoupon && !isExpired))) { ... }

// ✅ AFTER (Descriptive Domain Predicate):
if (IsEligibleForPromotion(order)) { ... }";
            return (rationale, recommendation, steps, example);
        }

        public static (string Rationale, string Recommendation, List<string> ActionSteps, string CodeExample) GetExcessiveInheritanceAdvice(string className, int count, int limit)
        {
            var rationale = $"Class '{className}' inherits/implements {count} types (threshold is {limit}). Deep inheritance hierarchies create brittle dependencies.";
            var recommendation = "Favor object composition over class inheritance ('Composition over Inheritance').";
            var steps = new List<string>
            {
                "Replace deep base class hierarchies with injected strategy interfaces.",
                "Limit class inheritance depth to 2 levels maximum."
            };
            var example = 
@"// ❌ BEFORE:
public class VipOrderProcessor : OrderProcessorBase, IProcessor, IValidator, ILogger, IAuditor

// ✅ AFTER (Composition):
public class VipOrderProcessor {
    private readonly IOrderValidator _validator;
    private readonly IAuditor _auditor;
}";
            return (rationale, recommendation, steps, example);
        }

        public static (string Rationale, string Recommendation, List<string> ActionSteps, string CodeExample) GetMethodBloatAdvice(string className, int methodCount, int limit)
        {
            var rationale = $"Class '{className}' defines {methodCount} methods (threshold is {limit}). Classes with too many methods often bundle unrelated domains.";
            var recommendation = "Partition the class into domain-specific modules or partial interfaces.";
            var steps = new List<string>
            {
                "Group methods by domain operation and extract new specialized classes.",
                "Ensure each class encapsulates one cohesive feature."
            };
            var example = 
@"// ❌ BEFORE:
public class UserManager { /* 30 methods handling Auth, Profile, Password, Tokens, Audit, Billing */ }

// ✅ AFTER:
public class UserAuthService { }
public class UserProfileService { }
public class UserBillingService { }";
            return (rationale, recommendation, steps, example);
        }

        public static (string Rationale, string Recommendation, List<string> ActionSteps, string CodeExample) GetNonExhaustiveSwitchAdvice()
        {
            var rationale = "Switch statement does not include a 'default:' case. If an unexpected value or new enum member is passed, execution falls through silently.";
            var recommendation = "Add a 'default:' branch that throws an ArgumentOutOfRangeException or logs an unexpected branch.";
            var steps = new List<string>
            {
                "Add `default: throw new ArgumentOutOfRangeException(...);` to catch unhandled enum values.",
                "Or use C# pattern matching switch expressions (`_ => throw new InvalidOperationException()`)."
            };
            var example = 
@"// ❌ BEFORE:
switch (status) {
    case Status.Active: Process(); break;
    case Status.Inactive: Disable(); break;
}

// ✅ AFTER:
switch (status) {
    case Status.Active: Process(); break;
    case Status.Inactive: Disable(); break;
    default: throw new ArgumentOutOfRangeException(nameof(status), $""Unhandled status: {status}"");
}";
            return (rationale, recommendation, steps, example);
        }

        // ==========================================
        // 2. RUNTIME SAFETY & BUG DETECTION (SAF)
        // ==========================================

        public static (string Rationale, string Recommendation, List<string> ActionSteps, string CodeExample) GetNullDereferenceAdvice(string memberAccess)
        {
            var rationale = $"Deep chained member access '{memberAccess}' will throw NullReferenceException at runtime if any intermediate reference is null.";
            var recommendation = "Use null-conditional operators ('?.') or pattern matching.";
            var steps = new List<string>
            {
                "Replace '.' with '?.' on intermediate object accesses.",
                "Provide a default fallback with null-coalescing ('??')."
            };
            var example = 
@"// ❌ BEFORE:
var name = order.Customer.Address.City.ToUpper();

// ✅ AFTER:
var name = order?.Customer?.Address?.City?.ToUpper() ?? ""Unknown"";";
            return (rationale, recommendation, steps, example);
        }

        public static (string Rationale, string Recommendation, List<string> ActionSteps, string CodeExample) GetDivisionByZeroAdvice(string expression)
        {
            var rationale = $"Division or modulo by zero in '{expression}' throws DivideByZeroException at runtime and crashes the execution flow.";
            var recommendation = "Add a divisor guard before performing division or modulo.";
            var steps = new List<string>
            {
                "Check that the divisor is not equal to zero before the arithmetic operation.",
                "Return a fallback value or throw a descriptive ArgumentException."
            };
            var example = 
@"// ❌ BEFORE:
int b = 0;
int result = 100 / b;

// ✅ AFTER:
int b = 0;
int result = b != 0 ? 100 / b : 0;";
            return (rationale, recommendation, steps, example);
        }

        public static (string Rationale, string Recommendation, List<string> ActionSteps, string CodeExample) GetResourceLeakAdvice(string typeName)
        {
            var rationale = $"Disposable object '{typeName}' implements IDisposable but is created without a 'using' statement, causing unmanaged handle leaks.";
            var recommendation = "Wrap the disposable object creation in a 'using' statement or declaration.";
            var steps = new List<string>
            {
                $"Use `using var obj = new {typeName}();` to guarantee automatic disposal."
            };
            var example = 
@"// ❌ BEFORE:
var client = new HttpClient();
var data = client.GetStringAsync(url).Result;

// ✅ AFTER:
using var client = new HttpClient();
var data = await client.GetStringAsync(url);";
            return (rationale, recommendation, steps, example);
        }

        public static (string Rationale, string Recommendation, List<string> ActionSteps, string CodeExample) GetArrayBoundsAdvice(string expr, string issue)
        {
            var rationale = $"Array bounds violation detected ({issue}) in '{expr}', which will throw IndexOutOfRangeException at runtime.";
            var recommendation = "Ensure array indexing is within valid range [0, Length - 1].";
            var steps = new List<string>
            {
                "Change '<=' to '<' in loop boundary conditions.",
                "Verify negative literals are not used as raw array indexers."
            };
            var example = 
@"// ❌ BEFORE:
for (int i = 0; i <= arr.Length; i++) { var x = arr[i]; }

// ✅ AFTER:
for (int i = 0; i < arr.Length; i++) { var x = arr[i]; }";
            return (rationale, recommendation, steps, example);
        }

        public static (string Rationale, string Recommendation, List<string> ActionSteps, string CodeExample) GetEmptyCatchAdvice(string excType)
        {
            var rationale = $"Empty catch block swallowing '{excType}' hides critical production errors and leaves the application in an unpredictable state.";
            var recommendation = "Log the exception using ILogger or rethrow it using 'throw;'.";
            var steps = new List<string>
            {
                "Log the exception with structured parameters: `_logger.LogError(ex, \"Error processing request\");`",
                "Rethrow if the error cannot be safely recovered from."
            };
            var example = 
@"// ❌ BEFORE:
catch (Exception) { }

// ✅ AFTER:
catch (Exception ex) {
    _logger.LogError(ex, ""Failed to execute operation"");
    throw;
}";
            return (rationale, recommendation, steps, example);
        }

        public static (string Rationale, string Recommendation, List<string> ActionSteps, string CodeExample) GetUnreachableCodeAdvice(string statement)
        {
            var rationale = $"Statement '{statement}' will never be executed because it appears after an unconditional jump (return/throw/break).";
            var recommendation = "Remove the unreachable dead code or correct the preceding control flow.";
            var steps = new List<string>
            {
                "Delete the unreachable statement to clean up technical debt."
            };
            var example = 
@"// ❌ BEFORE:
return true;
Console.WriteLine(""Done""); // Dead code

// ✅ AFTER:
return true;";
            return (rationale, recommendation, steps, example);
        }

        public static (string Rationale, string Recommendation, List<string> ActionSteps, string CodeExample) GetGenericExceptionAdvice(string statement)
        {
            var rationale = "Throwing generic 'System.Exception' makes it impossible for calling code to catch specific operational errors.";
            var recommendation = "Throw semantic exceptions like InvalidOperationException, ArgumentNullException, or domain-specific exceptions.";
            var steps = new List<string>
            {
                "Replace `new Exception(...)` with appropriate specific exception classes."
            };
            var example = 
@"// ❌ BEFORE:
throw new Exception(""User not found"");

// ✅ AFTER:
throw new KeyNotFoundException(""User not found"");";
            return (rationale, recommendation, steps, example);
        }

        public static (string Rationale, string Recommendation, List<string> ActionSteps, string CodeExample) GetMissingNullGuardAdvice(string paramName, string methodName)
        {
            var rationale = $"Public method '{methodName}' accesses reference parameter '{paramName}' without validating if it is null.";
            var recommendation = "Use `ArgumentNullException.ThrowIfNull(paramName);` at the top of the method.";
            var steps = new List<string>
            {
                $"Add `ArgumentNullException.ThrowIfNull({paramName});` as the first line of the method."
            };
            var example = 
@"// ❌ BEFORE:
public void Process(Order order) {
    Console.WriteLine(order.Id);
}

// ✅ AFTER (.NET 6+):
public void Process(Order order) {
    ArgumentNullException.ThrowIfNull(order);
    Console.WriteLine(order.Id);
}";
            return (rationale, recommendation, steps, example);
        }

        public static (string Rationale, string Recommendation, List<string> ActionSteps, string CodeExample) GetInfiniteLoopAdvice()
        {
            var rationale = "Infinite loop 'while(true)' has no break, return, or throw statements, causing CPU thread starvation.";
            var recommendation = "Add an exit condition or cancellation token check inside the loop body.";
            var steps = new List<string>
            {
                "Add an exit condition with `break;` or check `cancellationToken.IsCancellationRequested`."
            };
            var example = 
@"// ❌ BEFORE:
while (true) { Process(); }

// ✅ AFTER:
while (!cancellationToken.IsCancellationRequested) {
    if (Queue.IsEmpty) break;
    Process();
}";
            return (rationale, recommendation, steps, example);
        }

        public static (string Rationale, string Recommendation, List<string> ActionSteps, string CodeExample) GetFloatEqualityAdvice(string expr)
        {
            var rationale = $"Direct comparison '{expr}' on floating-point numbers is inaccurate due to binary IEEE 754 precision rounding.";
            var recommendation = "Compare absolute difference against an epsilon tolerance.";
            var steps = new List<string>
            {
                "Use `Math.Abs(a - b) < 0.0001f` instead of `a == b`."
            };
            var example = 
@"// ❌ BEFORE:
if (rate == 0.05f) { ... }

// ✅ AFTER:
if (Math.Abs(rate - 0.05f) < 0.0001f) { ... }";
            return (rationale, recommendation, steps, example);
        }

        public static (string Rationale, string Recommendation, List<string> ActionSteps, string CodeExample) GetRedundantNullCoalescingAdvice(string expr)
        {
            var rationale = $"Expression '{expr}' has identical left and right operands, making the null coalescing operation redundant.";
            var recommendation = "Remove the redundant right operand or provide a fallback value.";
            var steps = new List<string>
            {
                "Replace `x ?? x` with `x ?? defaultValue` or simply `x`."
            };
            var example = 
@"// ❌ BEFORE:
var name = username ?? username;

// ✅ AFTER:
var name = username ?? ""Default"";";
            return (rationale, recommendation, steps, example);
        }

        public static (string Rationale, string Recommendation, List<string> ActionSteps, string CodeExample) GetCollectionMutationAdvice(string colName, string modCall)
        {
            var rationale = $"Mutating collection '{colName}' via '{modCall}' inside a foreach loop throws InvalidOperationException.";
            var recommendation = "Iterate over a copy using `.ToList()` or use a traditional for-loop backwards.";
            var steps = new List<string>
            {
                $"Change `foreach (var item in {colName})` to `foreach (var item in {colName}.ToList())`."
            };
            var example = 
@"// ❌ BEFORE:
foreach (var item in items) {
    if (item.IsExpired) items.Remove(item); // Throws InvalidOperationException
}

// ✅ AFTER:
foreach (var item in items.ToList()) {
    if (item.IsExpired) items.Remove(item);
}";
            return (rationale, recommendation, steps, example);
        }

        public static (string Rationale, string Recommendation, List<string> ActionSteps, string CodeExample) GetStackTraceTruncationAdvice(string exIdentifier)
        {
            var rationale = $"Using 'throw {exIdentifier};' resets the stack trace to the catch block, destroying the original source line of the error.";
            var recommendation = "Use plain 'throw;' to preserve the original exception call stack.";
            var steps = new List<string>
            {
                $"Replace `throw {exIdentifier};` with `throw;`."
            };
            var example = 
@"// ❌ BEFORE (Loses original stack trace):
catch (Exception ex) {
    _logger.LogError(ex, ""Error"");
    throw ex;
}

// ✅ AFTER (Preserves original stack trace):
catch (Exception ex) {
    _logger.LogError(ex, ""Error"");
    throw;
}";
            return (rationale, recommendation, steps, example);
        }

        public static (string Rationale, string Recommendation, List<string> ActionSteps, string CodeExample) GetEqualsHashCodeAdvice(string className, string missingMethod)
        {
            var rationale = $"Class '{className}' overrides one equality method but is missing '{missingMethod}'. This corrupts HashSet and Dictionary behavior.";
            var recommendation = $"Implement both 'Equals(object)' and 'GetHashCode()' together.";
            var steps = new List<string>
            {
                $"Implement the missing '{missingMethod}' method using HashCode.Combine()."
            };
            var example = 
@"// ❌ BEFORE:
public override bool Equals(object obj) => ...; // Missing GetHashCode()

// ✅ AFTER:
public override bool Equals(object obj) => obj is User u && u.Id == Id;
public override int GetHashCode() => HashCode.Combine(Id);";
            return (rationale, recommendation, steps, example);
        }

        public static (string Rationale, string Recommendation, List<string> ActionSteps, string CodeExample) GetDangerousCastAdvice(string castExpr, string targetType)
        {
            var rationale = $"Explicit cast '{castExpr}' will throw InvalidCastException if the object is not of type '{targetType}'.";
            var recommendation = "Use safe type casting with 'as' operator or pattern matching 'is'.";
            var steps = new List<string>
            {
                $"Replace `({targetType})obj` with `obj as {targetType}` or `if (obj is {targetType} t)`."
            };
            var example = 
@"// ❌ BEFORE:
var emp = (Manager)user;

// ✅ AFTER:
if (user is Manager mgr) {
    mgr.Approve();
}";
            return (rationale, recommendation, steps, example);
        }

        // ==========================================
        // 3. CONCURRENCY & ASYNC SAFETY (CON)
        // ==========================================

        public static (string Rationale, string Recommendation, List<string> ActionSteps, string CodeExample) GetDeadlockAdvice(string m1, string m2, string p1, string p2)
        {
            var rationale = $"Lock order inversion detected between '{m1}' ({p1}) and '{m2}' ({p2}). When executed concurrently across threads, this guarantees a deadlock.";
            var recommendation = "Enforce a strict, uniform global lock acquisition hierarchy across all methods.";
            var steps = new List<string>
            {
                "Standardize lock acquisition sequence across all methods.",
                "Or replace multiple locks with a single ReaderWriterLockSlim or SemaphoreSlim."
            };
            var example = 
@"// ❌ BEFORE (Deadlock):
void M1() { lock(A) { lock(B) {} } }
void M2() { lock(B) { lock(A) {} } }

// ✅ AFTER (Consistent order):
void M1() { lock(A) { lock(B) {} } }
void M2() { lock(A) { lock(B) {} } }";
            return (rationale, recommendation, steps, example);
        }

        public static (string Rationale, string Recommendation, List<string> ActionSteps, string CodeExample) GetAsyncVoidAdvice(string methodName)
        {
            var rationale = $"Async void method '{methodName}' cannot be awaited, and unhandled exceptions bypass normal catch handlers to crash the process.";
            var recommendation = "Change return type from 'void' to 'Task' or 'ValueTask'.";
            var steps = new List<string>
            {
                $"Change `public async void {methodName}()` to `public async Task {methodName}()`."
            };
            var example = 
@"// ❌ BEFORE:
public async void ProcessData() { await Task.Delay(100); }

// ✅ AFTER:
public async Task ProcessDataAsync() { await Task.Delay(100); }";
            return (rationale, recommendation, steps, example);
        }

        public static (string Rationale, string Recommendation, List<string> ActionSteps, string CodeExample) GetSyncOverAsyncAdvice(string call)
        {
            var rationale = $"Synchronous blocking call '{call}' blocks thread pool threads while waiting on async tasks, causing thread pool starvation and deadlocks.";
            var recommendation = "Use 'await' asynchronously throughout the entire call chain.";
            var steps = new List<string>
            {
                "Make the enclosing method 'async Task' and replace '.Result' / '.Wait()' with 'await'."
            };
            var example = 
@"// ❌ BEFORE (Sync-over-Async):
var user = GetUserAsync(id).Result;

// ✅ AFTER (Async-all-the-way):
var user = await GetUserAsync(id);";
            return (rationale, recommendation, steps, example);
        }

        public static (string Rationale, string Recommendation, List<string> ActionSteps, string CodeExample) GetUnsafeLockAdvice(string expr)
        {
            var rationale = $"Locking on '{expr}' exposes synchronization handles to external callers, causing unintended deadlocks.";
            var recommendation = "Lock on a private dedicated `readonly object _lock = new();`.";
            var steps = new List<string>
            {
                "Declare `private readonly object _lock = new();` and lock on `_lock`."
            };
            var example = 
@"// ❌ BEFORE:
lock(this) { ... }

// ✅ AFTER:
private readonly object _syncLock = new();
lock(_syncLock) { ... }";
            return (rationale, recommendation, steps, example);
        }

        public static (string Rationale, string Recommendation, List<string> ActionSteps, string CodeExample) GetUnawaitedTaskAdvice(string invocation)
        {
            var rationale = $"Async task '{invocation}' is invoked without 'await', causing fire-and-forget behavior where exceptions are lost.";
            var recommendation = "Add 'await' before calling async methods.";
            var steps = new List<string>
            {
                $"Add `await` keyword before `{invocation}`."
            };
            var example = 
@"// ❌ BEFORE:
SaveAuditLogAsync(); // Fire and forget

// ✅ AFTER:
await SaveAuditLogAsync();";
            return (rationale, recommendation, steps, example);
        }

        public static (string Rationale, string Recommendation, List<string> ActionSteps, string CodeExample) GetThreadSleepInAsyncAdvice(string methodName)
        {
            var rationale = $"Calling 'Thread.Sleep' in async method '{methodName}' blocks OS thread pool worker threads instead of yielding execution.";
            var recommendation = "Replace 'Thread.Sleep' with 'await Task.Delay(...)'.";
            var steps = new List<string>
            {
                "Replace `Thread.Sleep(ms)` with `await Task.Delay(ms)`."
            };
            var example = 
@"// ❌ BEFORE:
public async Task PollAsync() {
    Thread.Sleep(1000);
}

// ✅ AFTER:
public async Task PollAsync() {
    await Task.Delay(1000);
}";
            return (rationale, recommendation, steps, example);
        }

        public static (string Rationale, string Recommendation, List<string> ActionSteps, string CodeExample) GetSharedStaticMutationAdvice(string fieldName, string methodName)
        {
            var rationale = $"Mutating shared static field '{fieldName}' from instance method '{methodName}' without locks causes multi-threaded data corruption.";
            var recommendation = "Protect shared state with a lock or use Interlocked / Concurrent collections.";
            var steps = new List<string>
            {
                "Wrap field mutations in `lock(_syncLock)` or use `Interlocked.Increment`."
            };
            var example = 
@"// ❌ BEFORE:
private static int _counter = 0;
public void Increment() { _counter++; }

// ✅ AFTER:
public void Increment() { Interlocked.Increment(ref _counter); }";
            return (rationale, recommendation, steps, example);
        }

        public static (string Rationale, string Recommendation, List<string> ActionSteps, string CodeExample) GetMissingConfigureAwaitAdvice(string expr)
        {
            var rationale = $"Awaiting '{expr}' without 'ConfigureAwait(false)' forces task continuation back onto synchronization context, hurting library performance.";
            var recommendation = "Append '.ConfigureAwait(false)' to awaited tasks in non-UI / class library code.";
            var steps = new List<string>
            {
                $"Change `await {expr};` to `await {expr}.ConfigureAwait(false);`."
            };
            var example = 
@"// ❌ BEFORE:
var res = await httpClient.GetStringAsync(url);

// ✅ AFTER:
var res = await httpClient.GetStringAsync(url).ConfigureAwait(false);";
            return (rationale, recommendation, steps, example);
        }

        public static (string Rationale, string Recommendation, List<string> ActionSteps, string CodeExample) GetLoopClosureAdvice(string varName)
        {
            var rationale = $"Loop variable '{varName}' is captured inside a background task closure, causing race conditions where multiple tasks see the same mutated value.";
            var recommendation = "Create a local copy of the loop variable inside the loop body before creating the task.";
            var steps = new List<string>
            {
                $"Add `var localCopy = {varName};` inside the loop body and pass `localCopy` to the lambda."
            };
            var example = 
@"// ❌ BEFORE (Race Condition):
for (int i = 0; i < 10; i++) {
    Task.Run(() => Process(i));
}

// ✅ AFTER (Isolated Local Copy):
for (int i = 0; i < 10; i++) {
    var item = i;
    Task.Run(() => Process(item));
}";
            return (rationale, recommendation, steps, example);
        }

        public static (string Rationale, string Recommendation, List<string> ActionSteps, string CodeExample) GetDiscardedCancellationTokenAdvice(string call, string ctName)
        {
            var rationale = $"Method receives CancellationToken '{ctName}' but omits passing it to downstream async call '{call}'.";
            var recommendation = $"Forward '{ctName}' to all underlying async invocations.";
            var steps = new List<string>
            {
                $"Pass `{ctName}` as argument into `{call}`."
            };
            var example = 
@"// ❌ BEFORE:
public async Task DownloadAsync(CancellationToken ct) {
    await httpClient.GetAsync(url); // Omits ct
}

// ✅ AFTER:
public async Task DownloadAsync(CancellationToken ct) {
    await httpClient.GetAsync(url, ct);
}";
            return (rationale, recommendation, steps, example);
        }

        public static (string Rationale, string Recommendation, List<string> ActionSteps, string CodeExample) GetParallelCollectionMutationAdvice(string modCall)
        {
            var rationale = $"Calling '{modCall}' inside a Parallel loop concurrently mutates a non-thread-safe collection, causing index corruption.";
            var recommendation = "Use ConcurrentBag<T>, ConcurrentQueue<T>, or lock synchronization.";
            var steps = new List<string>
            {
                "Replace standard collection with `ConcurrentBag<T>` or `ConcurrentDictionary<K, V>`."
            };
            var example = 
@"// ❌ BEFORE:
var list = new List<int>();
Parallel.ForEach(data, item => list.Add(item));

// ✅ AFTER:
var bag = new ConcurrentBag<int>();
Parallel.ForEach(data, item => bag.Add(item));";
            return (rationale, recommendation, steps, example);
        }

        // ==========================================
        // 4. SECURITY & VULNERABILITIES (SEC)
        // ==========================================

        public static (string Rationale, string Recommendation, List<string> ActionSteps, string CodeExample) GetSqlInjectionAdvice(string invocation)
        {
            var rationale = "Dynamic SQL query constructed with string interpolation/concatenation exposes database to SQL Injection (OWASP A03).";
            var recommendation = "Use parameterized SQL queries with SqlParameter or Dapper parameters.";
            var steps = new List<string>
            {
                "Replace inline concatenated variables with SQL parameters (`@param`)."
            };
            var example = 
@"// ❌ BEFORE:
string query = $""SELECT * FROM Users WHERE Email = '{email}'"";

// ✅ AFTER:
var query = ""SELECT * FROM Users WHERE Email = @Email"";
cmd.Parameters.AddWithValue(""@Email"", email);";
            return (rationale, recommendation, steps, example);
        }

        public static (string Rationale, string Recommendation, List<string> ActionSteps, string CodeExample) GetHardcodedSecretAdvice(string literal)
        {
            var rationale = "Hardcoded credentials in source code will leak into source control repositories.";
            var recommendation = "Store secrets in Environment Variables, Azure KeyVault, or AWS Secrets Manager.";
            var steps = new List<string>
            {
                "Remove raw string credentials from source code.",
                "Read from `Environment.GetEnvironmentVariable(\"API_KEY\")`."
            };
            var example = 
@"// ❌ BEFORE:
string apiKey = ""AKIAIOSFODNN7EXAMPLE"";

// ✅ AFTER:
string apiKey = Environment.GetEnvironmentVariable(""AWS_ACCESS_KEY_ID"");";
            return (rationale, recommendation, steps, example);
        }

        public static (string Rationale, string Recommendation, List<string> ActionSteps, string CodeExample) GetWeakCryptoAdvice(string algorithm)
        {
            var rationale = $"Cryptographic algorithm '{algorithm}' is cryptographically broken and vulnerable to collision/decryption attacks.";
            var recommendation = "Upgrade to SHA-256 / SHA-512 for hashing or AES-GCM / ChaCha20 for encryption.";
            var steps = new List<string>
            {
                "Replace MD5/SHA1 with `SHA256.Create()`.",
                "Replace DES/TripleDES with `Aes.Create()`."
            };
            var example = 
@"// ❌ BEFORE:
using var md5 = MD5.Create();

// ✅ AFTER:
using var sha256 = SHA256.Create();";
            return (rationale, recommendation, steps, example);
        }

        public static (string Rationale, string Recommendation, List<string> ActionSteps, string CodeExample) GetXssAdvice(string sink)
        {
            var rationale = "Writing unencoded dynamic user input to HTML output allows Cross-Site Scripting (XSS) execution.";
            var recommendation = "Encode dynamic content with `HtmlEncoder.Default.Encode()` before rendering.";
            var steps = new List<string>
            {
                "Sanitize and HTML-encode dynamic variables before writing to response."
            };
            var example = 
@"// ❌ BEFORE:
Response.Write($""<h1>Welcome {userName}</h1>"");

// ✅ AFTER:
Response.Write($""<h1>Welcome {HtmlEncoder.Default.Encode(userName)}</h1>"");";
            return (rationale, recommendation, steps, example);
        }

        public static (string Rationale, string Recommendation, List<string> ActionSteps, string CodeExample) GetInsecureRandomAdvice(string context)
        {
            var rationale = $"'System.Random' is pseudo-random and predictable, making it insecure for security tokens/passwords in '{context}'.";
            var recommendation = "Use 'RandomNumberGenerator.Create()' or 'RandomNumberGenerator.GetBytes()' for cryptographic randomness.";
            var steps = new List<string>
            {
                "Use `RandomNumberGenerator.GetInt32(min, max)` or `RandomNumberGenerator.GetBytes(buffer)`."
            };
            var example = 
@"// ❌ BEFORE:
var random = new Random();
int token = random.Next(100000, 999999);

// ✅ AFTER:
int token = RandomNumberGenerator.GetInt32(100000, 999999);";
            return (rationale, recommendation, steps, example);
        }

        public static (string Rationale, string Recommendation, List<string> ActionSteps, string CodeExample) GetPathTraversalAdvice(string call)
        {
            var rationale = $"File I/O method '{call}' opens files without verifying against path traversal attacks (e.g. '../../etc/passwd').";
            var recommendation = "Validate path using `Path.GetFileName()` or verify that `Path.GetFullPath()` starts with the allowed directory.";
            var steps = new List<string>
            {
                "Sanitize filename using `Path.GetFileName(userInput)`.",
                "Verify resolved full path stays within the base allowed directory."
            };
            var example = 
@"// ❌ BEFORE:
File.ReadAllText(userInputPath);

// ✅ AFTER:
string safeFileName = Path.GetFileName(userInputPath);
string fullPath = Path.Combine(baseDir, safeFileName);
if (!fullPath.StartsWith(baseDir)) throw new SecurityException(""Invalid path"");";
            return (rationale, recommendation, steps, example);
        }

        public static (string Rationale, string Recommendation, List<string> ActionSteps, string CodeExample) GetInsecureDeserializationAdvice(string typeName)
        {
            var rationale = $"Deserializer '{typeName}' is unsafe and allows remote code execution when parsing untrusted payloads.";
            var recommendation = "Migrate to System.Text.Json or Google Protocol Buffers.";
            var steps = new List<string>
            {
                "Replace BinaryFormatter with `System.Text.Json.JsonSerializer`."
            };
            var example = 
@"// ❌ BEFORE:
var formatter = new BinaryFormatter();
var obj = formatter.Deserialize(stream);

// ✅ AFTER:
var obj = JsonSerializer.Deserialize<UserDto>(stream);";
            return (rationale, recommendation, steps, example);
        }

        public static (string Rationale, string Recommendation, List<string> ActionSteps, string CodeExample) GetCommandInjectionAdvice(string call)
        {
            var rationale = "Calling 'Process.Start' with dynamic concatenated arguments exposes server to OS command injection.";
            var recommendation = "Use explicit ProcessStartInfo ArgumentList instead of concatenated command line strings.";
            var steps = new List<string>
            {
                "Populate `psi.ArgumentList.Add(...)` instead of raw Arguments string."
            };
            var example = 
@"// ❌ BEFORE:
Process.Start(""cmd.exe"", $""/c {userInput}"");

// ✅ AFTER:
var psi = new ProcessStartInfo(""git"");
psi.ArgumentList.Add(""status"");
Process.Start(psi);";
            return (rationale, recommendation, steps, example);
        }

        public static (string Rationale, string Recommendation, List<string> ActionSteps, string CodeExample) GetXxeAdvice(string typeName)
        {
            var rationale = $"XML parser '{typeName}' without safe DTD settings is vulnerable to XML External Entity (XXE) data exfiltration.";
            var recommendation = "Set 'DtdProcessing = DtdProcessing.Prohibit' and 'XmlResolver = null'.";
            var steps = new List<string>
            {
                "Configure `settings.DtdProcessing = DtdProcessing.Prohibit;`."
            };
            var example = 
@"// ❌ BEFORE:
var settings = new XmlReaderSettings();

// ✅ AFTER:
var settings = new XmlReaderSettings {
    DtdProcessing = DtdProcessing.Prohibit,
    XmlResolver = null
};";
            return (rationale, recommendation, steps, example);
        }

        public static (string Rationale, string Recommendation, List<string> ActionSteps, string CodeExample) GetOpenRedirectAdvice(string urlVar)
        {
            var rationale = $"Redirecting to dynamic URL '{urlVar}' without validation allows attackers to redirect users to malicious phishing websites.";
            var recommendation = "Validate URL using `Url.IsLocalUrl(url)` before redirecting.";
            var steps = new List<string>
            {
                $"Add `if (!Url.IsLocalUrl({urlVar})) return Redirect(\"/\");`."
            };
            var example = 
@"// ❌ BEFORE:
return Redirect(returnUrl);

// ✅ AFTER:
if (Url.IsLocalUrl(returnUrl)) return Redirect(returnUrl);
return Redirect(""/"");";
            return (rationale, recommendation, steps, example);
        }

        public static (string Rationale, string Recommendation, List<string> ActionSteps, string CodeExample) GetInsecureCookieAdvice()
        {
            var rationale = "Cookies created without HttpOnly and Secure flags are readable by JavaScript (XSS theft) and sent over plaintext HTTP.";
            var recommendation = "Set 'HttpOnly = true', 'Secure = true', and 'SameSite = SameSiteMode.Strict'.";
            var steps = new List<string>
            {
                "Configure cookie flags: `HttpOnly = true; Secure = true; SameSite = SameSiteMode.Strict;`."
            };
            var example = 
@"// ❌ BEFORE:
Response.Cookies.Append(""auth"", token);

// ✅ AFTER:
Response.Cookies.Append(""auth"", token, new CookieOptions {
    HttpOnly = true,
    Secure = true,
    SameSite = SameSiteMode.Strict
});";
            return (rationale, recommendation, steps, example);
        }

        // ==========================================
        // 5. PERFORMANCE & MEMORY OPTIMIZATION (PERF)
        // ==========================================

        public static (string Rationale, string Recommendation, List<string> ActionSteps, string CodeExample) GetStringInLoopAdvice(string assignment)
        {
            var rationale = "String concatenation ('+=') inside loops repeatedly allocates new string instances in heap memory, triggering heavy garbage collection.";
            var recommendation = "Use StringBuilder to construct strings across multiple iterations.";
            var steps = new List<string>
            {
                "Instantiate `var sb = new StringBuilder();` before the loop and append inside."
            };
            var example = 
@"// ❌ BEFORE:
string s = """";
foreach(var item in items) s += item;

// ✅ AFTER:
var sb = new StringBuilder();
foreach(var item in items) sb.Append(item);
string s = sb.ToString();";
            return (rationale, recommendation, steps, example);
        }

        public static (string Rationale, string Recommendation, List<string> ActionSteps, string CodeExample) GetBoxingAdvice(string collectionType)
        {
            var rationale = $"Legacy collection '{collectionType}' stores elements as 'object', forcing heap boxing for value types.";
            var recommendation = "Replace non-generic collection with generic equivalent (e.g. List<T>, Dictionary<K,V>).";
            var steps = new List<string>
            {
                "Replace `ArrayList` with `List<T>` and `Hashtable` with `Dictionary<TKey, TValue>`."
            };
            var example = 
@"// ❌ BEFORE:
var list = new ArrayList();
list.Add(100); // Boxes int to object

// ✅ AFTER:
var list = new List<int>();
list.Add(100); // Zero allocations";
            return (rationale, recommendation, steps, example);
        }

        public static (string Rationale, string Recommendation, List<string> ActionSteps, string CodeExample) GetLinqCountAdvice(string expr)
        {
            var rationale = $"Using '{expr}' forces LINQ to enumerate the entire collection to determine count, when only existence needs checking.";
            var recommendation = "Use '.Any()' instead of '.Count() > 0'.";
            var steps = new List<string>
            {
                "Replace `.Count() > 0` with `.Any()` and `.Count() == 0` with `!list.Any()`."
            };
            var example = 
@"// ❌ BEFORE:
if (users.Count() > 0) { ... }

// ✅ AFTER:
if (users.Any()) { ... }";
            return (rationale, recommendation, steps, example);
        }

        public static (string Rationale, string Recommendation, List<string> ActionSteps, string CodeExample) GetMultipleEnumerationAdvice(string paramName)
        {
            var rationale = $"IEnumerable '{paramName}' is enumerated multiple times, re-executing query pipelines and database calls.";
            var recommendation = "Materialize the sequence into an array or list with `.ToList()` before iterating.";
            var steps = new List<string>
            {
                $"Add `var list = {paramName}.ToList();` at method entry and iterate `list`."
            };
            var example = 
@"// ❌ BEFORE (Enumerates query twice):
public void Process(IEnumerable<User> users) {
    if (users.Any()) {
        foreach (var u in users) { ... }
    }
}

// ✅ AFTER (Cached once):
public void Process(IEnumerable<User> users) {
    var list = users.ToList();
    if (list.Count > 0) {
        foreach (var u in list) { ... }
    }
}";
            return (rationale, recommendation, steps, example);
        }

        public static (string Rationale, string Recommendation, List<string> ActionSteps, string CodeExample) GetInefficientStringComparisonAdvice(string expr)
        {
            var rationale = $"Comparing strings via '{expr}' allocates temporary lowercased/uppercased string copies in memory.";
            var recommendation = "Use 'string.Equals(a, b, StringComparison.OrdinalIgnoreCase)'.";
            var steps = new List<string>
            {
                "Replace `.ToLower() == \"abc\"` with `string.Equals(str, \"abc\", StringComparison.OrdinalIgnoreCase)`."
            };
            var example = 
@"// ❌ BEFORE:
if (status.ToLower() == ""active"") { ... }

// ✅ AFTER:
if (string.Equals(status, ""active"", StringComparison.OrdinalIgnoreCase)) { ... }";
            return (rationale, recommendation, steps, example);
        }

        public static (string Rationale, string Recommendation, List<string> ActionSteps, string CodeExample) GetSpanOptimizationAdvice(string call)
        {
            var rationale = $"Calling '{call}' in loop bodies allocates new string objects on every slice.";
            var recommendation = "Use 'ReadOnlySpan<char>' or 'AsSpan()' to perform zero-allocation slicing.";
            var steps = new List<string>
            {
                "Use `str.AsSpan(start, length)` instead of `str.Substring(start, length)`."
            };
            var example = 
@"// ❌ BEFORE:
string prefix = text.Substring(0, 4);

// ✅ AFTER:
ReadOnlySpan<char> prefix = text.AsSpan(0, 4);";
            return (rationale, recommendation, steps, example);
        }

        public static (string Rationale, string Recommendation, List<string> ActionSteps, string CodeExample) GetClosureInLoopAdvice()
        {
            var rationale = "Lambda expression inside a loop captures external variables, allocating delegate closures on every iteration.";
            var recommendation = "Extract lambda into a static method or pass state directly.";
            var steps = new List<string>
            {
                "Avoid creating lambdas inside tight loop bodies."
            };
            var example = 
@"// ❌ BEFORE:
foreach (var item in items) {
    list.Find(x => x.Id == item.Id); // Allocates closure
}

// ✅ AFTER:
var ids = items.Select(i => i.Id).ToHashSet();
var matches = list.Where(x => ids.Contains(x.Id));";
            return (rationale, recommendation, steps, example);
        }

        public static (string Rationale, string Recommendation, List<string> ActionSteps, string CodeExample) GetRegexCompilationAdvice()
        {
            var rationale = "Instantiating 'new Regex(...)' inside methods recompiles the regular expression DFA on every call.";
            var recommendation = "Use static readonly Regex with RegexOptions.Compiled, or use C# [GeneratedRegex].";
            var steps = new List<string>
            {
                "Declare `private static readonly Regex _regex = new(\"pattern\", RegexOptions.Compiled);`."
            };
            var example = 
@"// ❌ BEFORE:
public bool IsValid(string s) => new Regex(@""^\d+$"").IsMatch(s);

// ✅ AFTER (.NET 7+):
[GeneratedRegex(@""^\d+$"")]
private static partial Regex NumberRegex();";
            return (rationale, recommendation, steps, example);
        }

        public static (string Rationale, string Recommendation, List<string> ActionSteps, string CodeExample) GetFinalizerAdvice(string className)
        {
            var rationale = $"Class '{className}' implements a finalizer destructor, forcing instances to survive Generation 0 GC and delay memory reclamation.";
            var recommendation = "Remove finalizer and implement IDisposable if managing unmanaged handles.";
            var steps = new List<string>
            {
                "Remove `~ClassName()` destructor. Only SafeHandle derivatives require finalizers."
            };
            var example = 
@"// ❌ BEFORE:
~OrderService() { /* Cleanup */ }

// ✅ AFTER:
public void Dispose() { /* Cleanup */ }";
            return (rationale, recommendation, steps, example);
        }

        public static (string Rationale, string Recommendation, List<string> ActionSteps, string CodeExample) GetLargeStructAdvice(string paramName, string structType)
        {
            var rationale = $"Struct '{structType}' passed by value copies entire memory layout onto the stack.";
            var recommendation = $"Pass struct by readonly reference using 'in {structType} {paramName}'.";
            var steps = new List<string>
            {
                $"Change parameter signature to `in {structType} {paramName}`."
            };
            var example = 
@"// ❌ BEFORE:
public void Process(Matrix4x4 matrix)

// ✅ AFTER:
public void Process(in Matrix4x4 matrix)";
            return (rationale, recommendation, steps, example);
        }

        // ==========================================
        // 6. ARCHITECTURE & STANDARDS (ARCH)
        // ==========================================

        public static (string Rationale, string Recommendation, List<string> ActionSteps, string CodeExample) GetInterfaceNamingAdvice(string current, string expected)
        {
            var rationale = $"Interface '{current}' does not start with the standard 'I' prefix. Standard .NET naming conventions require all interfaces to begin with 'I'.";
            var recommendation = $"Rename interface to '{expected}'.";
            var steps = new List<string>
            {
                $"Rename `{current}` to `{expected}` across all implementing classes."
            };
            var example = 
@"// ❌ BEFORE:
public interface OrderService { }

// ✅ AFTER:
public interface IOrderService { }";
            return (rationale, recommendation, steps, example);
        }

        public static (string Rationale, string Recommendation, List<string> ActionSteps, string CodeExample) GetAsyncNamingAdvice(string current, string expected)
        {
            var rationale = $"Async method '{current}' is missing the 'Async' suffix, making it non-obvious to consumers that the method is awaitable.";
            var recommendation = $"Rename method to '{expected}'.";
            var steps = new List<string>
            {
                $"Rename `{current}` to `{expected}`."
            };
            var example = 
@"// ❌ BEFORE:
public async Task GetUser() { }

// ✅ AFTER:
public async Task GetUserAsync() { }";
            return (rationale, recommendation, steps, example);
        }

        public static (string Rationale, string Recommendation, List<string> ActionSteps, string CodeExample) GetTypeNamingAdvice(string current, string expected)
        {
            var rationale = $"Type '{current}' violates .NET PascalCase naming standards.";
            var recommendation = $"Rename type to '{expected}'.";
            var steps = new List<string>
            {
                $"Rename `{current}` to `{expected}`."
            };
            var example = 
@"// ❌ BEFORE:
public class employeeManager { }

// ✅ AFTER:
public class EmployeeManager { }";
            return (rationale, recommendation, steps, example);
        }

        public static (string Rationale, string Recommendation, List<string> ActionSteps, string CodeExample) GetPropertyNamingAdvice(string current, string expected)
        {
            var rationale = $"Property '{current}' must follow PascalCase naming standards.";
            var recommendation = $"Rename property to '{expected}'.";
            var steps = new List<string>
            {
                $"Rename `{current}` to `{expected}`."
            };
            var example = 
@"// ❌ BEFORE:
public string first_name { get; set; }

// ✅ AFTER:
public string FirstName { get; set; }";
            return (rationale, recommendation, steps, example);
        }

        public static (string Rationale, string Recommendation, List<string> ActionSteps, string CodeExample) GetVariableNamingAdvice(string current, string expected, string issue)
        {
            var rationale = $"Variable '{current}' {issue}. Local variables must follow camelCase conventions.";
            var recommendation = $"Rename variable to '{expected}'.";
            var steps = new List<string>
            {
                $"Rename `{current}` to `{expected}`."
            };
            var example = 
@"// ❌ BEFORE:
int Total_Count = 10;

// ✅ AFTER:
int totalCount = 10;";
            return (rationale, recommendation, steps, example);
        }

        public static (string Rationale, string Recommendation, List<string> ActionSteps, string CodeExample) GetObsoleteApiAdvice(string apiName)
        {
            var rationale = $"API '{apiName}' is marked [Obsolete] and scheduled for removal in future .NET runtime versions.";
            var recommendation = "Replace with modern non-deprecated runtime alternatives.";
            var steps = new List<string>
            {
                $"Replace `{apiName}` with supported replacement APIs."
            };
            var example = 
@"// ❌ BEFORE:
var req = WebRequest.Create(url);

// ✅ AFTER:
using var client = new HttpClient();
var res = await client.GetAsync(url);";
            return (rationale, recommendation, steps, example);
        }

        public static (string Rationale, string Recommendation, List<string> ActionSteps, string CodeExample) GetMethodNamingAdvice(string current, string expected)
        {
            var rationale = $"Method '{current}' must start with an uppercase letter and use PascalCase conventions.";
            var recommendation = $"Rename method to '{expected}'.";
            var steps = new List<string>
            {
                $"Rename method `{current}` to `{expected}`."
            };
            var example = 
@"// ❌ BEFORE:
public void printInfo() { }

// ✅ AFTER:
public void PrintInfo() { }";
            return (rationale, recommendation, steps, example);
        }

        public static (string Rationale, string Recommendation, List<string> ActionSteps, string CodeExample) GetFieldNamingAdvice(string current, string expected)
        {
            var rationale = $"Private field '{current}' is declared in PascalCase. Private/internal fields must use camelCase or an underscore prefix.";
            var recommendation = $"Rename field to '{expected}'.";
            var steps = new List<string>
            {
                $"Rename field `{current}` to `{expected}`."
            };
            var example = 
@"// ❌ BEFORE:
private List<Employee> EmployeeList;

// ✅ AFTER:
private List<Employee> _employeeList;";
            return (rationale, recommendation, steps, example);
        }

        public static (string Rationale, string Recommendation, List<string> ActionSteps, string CodeExample) GetConsoleInDomainAdvice(string className)
        {
            var rationale = $"Calling 'Console.WriteLine' in domain class '{className}' bypasses structured telemetry, centralized logging, and breaks unit test execution.";
            var recommendation = "Inject ILogger<T> and log structured event messages.";
            var steps = new List<string>
            {
                $"Inject `ILogger<{className}> _logger` and use `_logger.LogInformation(...)`."
            };
            var example = 
@"// ❌ BEFORE:
Console.WriteLine($""Processed user {id}"");

// ✅ AFTER:
_logger.LogInformation(""Processed user {UserId}"", id);";
            return (rationale, recommendation, steps, example);
        }

        public static (string Rationale, string Recommendation, List<string> ActionSteps, string CodeExample) GetPublicFieldAdvice(string fieldName, string typeName)
        {
            var rationale = $"Public field '{fieldName}' exposes internal state directly, preventing encapsulation, validation, and data binding.";
            var recommendation = "Convert public field to an auto-implemented property `{ get; set; }`.";
            var steps = new List<string>
            {
                $"Change `public {typeName} {fieldName};` to `public {typeName} {fieldName} {{ get; set; }}`."
            };
            var example = 
@"// ❌ BEFORE:
public string Name;

// ✅ AFTER:
public string Name { get; set; }";
            return (rationale, recommendation, steps, example);
        }

        public static (string Rationale, string Recommendation, List<string> ActionSteps, string CodeExample) GetEmptyInterfaceAdvice(string ifaceName)
        {
            var rationale = $"Interface '{ifaceName}' contains no members and acts as an empty marker interface.";
            var recommendation = "Replace marker interfaces with custom Attributes.";
            var steps = new List<string>
            {
                $"Define `[AttributeUsage(AttributeTargets.Class)] public class {ifaceName}Attribute : Attribute {{ }}`."
            };
            var example = 
@"// ❌ BEFORE:
public interface IAuditable { }

// ✅ AFTER:
[AttributeUsage(AttributeTargets.Class)]
public class AuditableAttribute : Attribute { }";
            return (rationale, recommendation, steps, example);
        }

        public static (string Rationale, string Recommendation, List<string> ActionSteps, string CodeExample) GetLayerBoundaryAdvice(string namespaceName)
        {
            var rationale = $"Domain/Core layer imports UI/Presentation namespace '{namespaceName}', violating Clean Architecture dependency inversion.";
            var recommendation = "Remove presentation references from core domain entity classes.";
            var steps = new List<string>
            {
                "Move presentation-specific models into Application or Web layers."
            };
            var example = 
@"// ❌ BEFORE (Domain entity referencing ASP.NET):
using Microsoft.AspNetCore.Mvc;
public class Order { }

// ✅ AFTER (Clean Domain):
public class Order { }";
            return (rationale, recommendation, steps, example);
        }

        public static (string Rationale, string Recommendation, List<string> ActionSteps, string CodeExample) GetDirectDbContextInControllerAdvice(string controllerName, string dbContextName)
        {
            var rationale = $"Controller '{controllerName}' directly instantiates '{dbContextName}', violating dependency injection and unit testability.";
            var recommendation = "Inject DbContext or Repository via constructor dependency injection.";
            var steps = new List<string>
            {
                $"Inject `{dbContextName}` into the constructor of `{controllerName}`."
            };
            var example = 
@"// ❌ BEFORE:
public IActionResult Get() {
    using var db = new AppDbContext();
}

// ✅ AFTER:
private readonly AppDbContext _db;
public OrderController(AppDbContext db) => _db = db;";
            return (rationale, recommendation, steps, example);
        }

        public static (string Rationale, string Recommendation, List<string> ActionSteps, string CodeExample) GetMagicLiteralAdvice(string literalValue)
        {
            var rationale = $"Magic literal '{literalValue}' in conditional logic obscures business meaning and makes updates error-prone.";
            var recommendation = "Declare a named constant or enum member.";
            var steps = new List<string>
            {
                $"Replace `{literalValue}` with `const int MaxRetries = {literalValue};` or an enum."
            };
            var example = 
@"// ❌ BEFORE:
if (status == 4) { ... }

// ✅ AFTER:
public const int StatusApproved = 4;
if (status == StatusApproved) { ... }";
            return (rationale, recommendation, steps, example);
        }

        public static (string Rationale, string Recommendation, List<string> ActionSteps, string CodeExample) GetMultipleTypesPerFileAdvice(string extraTypeName)
        {
            var rationale = $"Type '{extraTypeName}' is declared in a file containing multiple top-level types, violating the one-type-per-file convention.";
            var recommendation = $"Move '{extraTypeName}' into its own dedicated '{extraTypeName}.cs' file.";
            var steps = new List<string>
            {
                $"Extract `{extraTypeName}` into a separate file named `{extraTypeName}.cs`."
            };
            var example = 
@"// ❌ BEFORE (Order.cs containing multiple classes):
public class Order { }
public class OrderItem { }

// ✅ AFTER:
// Order.cs -> public class Order { }
// OrderItem.cs -> public class OrderItem { }";
            return (rationale, recommendation, steps, example);
        }
    }
}

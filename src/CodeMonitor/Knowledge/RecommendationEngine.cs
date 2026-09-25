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
            var rationale = $"Method '{methodName}' is {actualLines} lines long (allowed limit is {limit} lines). Methods of this size usually perform multiple tasks, making them error-prone, hard to read, and difficult to write unit tests for.";
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

        // ==========================================
        // 2. RUNTIME SAFETY & BUG DETECTION (SAF)
        // ==========================================

        public static (string Rationale, string Recommendation, List<string> ActionSteps, string CodeExample) GetNullDereferenceAdvice(string expression)
        {
            var rationale = $"Deep property access '{expression}' does not verify that intermediate objects are initialized, risking a runtime NullReferenceException.";
            var recommendation = "Use null-conditional safe navigation (?.) or null check guards before dereferencing.";
            var steps = new List<string>
            {
                "Replace standard dot ('.') access with safe navigation ('?.') across navigation chains.",
                "Provide a null coalescing ('??') fallback value if appropriate.",
                "Add defensive guard clauses (e.g. `ArgumentNullException.ThrowIfNull(param)`) at method start."
            };
            var example = 
@"// ❌ BEFORE (Throws NullReferenceException if Customer or Address is null):
string city = order.Customer.Address.City.ToUpper();

// ✅ AFTER (Safe Navigation with null coalescing fallback):
string city = order?.Customer?.Address?.City?.ToUpper() ?? ""Unknown"";";
            return (rationale, recommendation, steps, example);
        }

        public static (string Rationale, string Recommendation, List<string> ActionSteps, string CodeExample) GetDivisionByZeroAdvice(string expression)
        {
            var rationale = $"Expression '{expression}' attempts division or modulo by literal zero, which causes a DivideByZeroException at runtime.";
            var recommendation = "Ensure divisor is strictly greater than zero using a guard clause or conditional operator.";
            var steps = new List<string>
            {
                "Inspect the denominator/divisor before the arithmetic operation.",
                "Return a fallback value (such as 0) or throw an ArgumentException if divisor is zero.",
                "Use ternary checks: `divisor != 0 ? total / divisor : 0`."
            };
            var example = 
@"// ❌ BEFORE:
int average = total / 0;

// ✅ AFTER:
int average = count > 0 ? total / count : 0;";
            return (rationale, recommendation, steps, example);
        }

        public static (string Rationale, string Recommendation, List<string> ActionSteps, string CodeExample) GetResourceLeakAdvice(string typeName)
        {
            var rationale = $"Type '{typeName}' implements IDisposable and holds unmanaged resources (file handles, network sockets, DB connections). Failing to dispose leaks memory and OS handles.";
            var recommendation = "Wrap the disposable object in a 'using' statement or declaration to ensure deterministic disposal.";
            var steps = new List<string>
            {
                $"Declare '{typeName}' with `using var` for scoped lifecycle.",
                "Or wrap the usage in a `using (var resource = new ...) { }` block."
            };
            var example = 
@"// ❌ BEFORE (Resource leak):
var connection = new SqlConnection(connString);
connection.Open();

// ✅ AFTER (Automatic disposal upon scope exit):
using var connection = new SqlConnection(connString);
await connection.OpenAsync();";
            return (rationale, recommendation, steps, example);
        }

        public static (string Rationale, string Recommendation, List<string> ActionSteps, string CodeExample) GetArrayBoundsAdvice(string expression, string issueDetail)
        {
            var rationale = $"Expression '{expression}' triggers an index boundary defect ({issueDetail}), leading to an IndexOutOfRangeException at runtime.";
            var recommendation = "Ensure loop boundary uses strict '<' (less than) with Length/Count, and indices are non-negative.";
            var steps = new List<string>
            {
                "Verify loop termination condition: use `i < collection.Length` (not `<=`).",
                "Ensure array indices are strictly non-negative (>= 0).",
                "Prefer 'foreach' loops or LINQ expressions over manual index calculations."
            };
            var example = 
@"// ❌ BEFORE (Off-by-one error: accesses arr[arr.Length]):
for (int i = 0; i <= items.Length; i++) {
    Process(items[i]);
}

// ✅ AFTER (Safe boundary):
for (int i = 0; i < items.Length; i++) {
    Process(items[i]);
}";
            return (rationale, recommendation, steps, example);
        }

        public static (string Rationale, string Recommendation, List<string> ActionSteps, string CodeExample) GetEmptyCatchAdvice(string exceptionType)
        {
            var rationale = $"Empty catch block swallowing '{exceptionType}' conceals unexpected errors, corrupts application state, and complicates debugging.";
            var recommendation = "Log the exception details with an ILogger or rethrow after necessary cleanup.";
            var steps = new List<string>
            {
                "Log the exception with `_logger.LogError(ex, \"Operation failed\");`.",
                "If handling is not possible, rethrow with `throw;` to preserve stack trace.",
                "Never leave catch blocks completely empty."
            };
            var example = 
@"// ❌ BEFORE (Swallows exception silently):
try {
    DoWork();
} catch (Exception) { }

// ✅ AFTER (Logs with contextual error details):
try {
    DoWork();
} catch (Exception ex) {
    _logger.LogError(ex, ""Failed to execute DoWork"");
    throw;
}";
            return (rationale, recommendation, steps, example);
        }

        public static (string Rationale, string Recommendation, List<string> ActionSteps, string CodeExample) GetUnreachableCodeAdvice(string statement)
        {
            var rationale = $"Statement '{statement}' is located after an unconditional jump (return, throw, break, continue) and will never be executed.";
            var recommendation = "Remove the unreachable dead code or relocate it before the exit statement.";
            var steps = new List<string>
            {
                "Review the control flow in the enclosing block.",
                "Remove redundant code that comes after `return` or `throw`.",
                "If the code was meant to execute, adjust the condition or move it prior to the exit point."
            };
            var example = 
@"// ❌ BEFORE (Unreachable code):
return result;
Console.WriteLine(""Done""); // Dead code

// ✅ AFTER:
Console.WriteLine(""Done"");
return result;";
            return (rationale, recommendation, steps, example);
        }

        public static (string Rationale, string Recommendation, List<string> ActionSteps, string CodeExample) GetGenericExceptionAdvice(string throwExpr)
        {
            var rationale = $"Throwing generic 'System.Exception' via '{throwExpr}' prevents callers from catching and handling specific domain errors.";
            var recommendation = "Throw specific semantic exceptions such as ArgumentNullException, ArgumentException, or InvalidOperationException.";
            var steps = new List<string>
            {
                "Replace `throw new Exception(...)` with appropriate specific exception.",
                "Use `ArgumentNullException.ThrowIfNull()` or custom domain exceptions."
            };
            var example = 
@"// ❌ BEFORE (Generic Exception):
throw new Exception(""User not found"");

// ✅ AFTER (Specific Semantic Exception):
throw new InvalidOperationException(""User not found"");";
            return (rationale, recommendation, steps, example);
        }

        // ==========================================
        // 3. CONCURRENCY & ASYNC SAFETY (CON)
        // ==========================================

        public static (string Rationale, string Recommendation, List<string> ActionSteps, string CodeExample) GetDeadlockAdvice(string method1, string method2, string lock1, string lock2)
        {
            var rationale = $"Inconsistent lock acquisition order between '{method1}' and '{method2}' ({lock1} vs {lock2}) creates a classic multithreading deadlock.";
            var recommendation = "Establish a uniform global lock acquisition hierarchy across all methods.";
            var steps = new List<string>
            {
                "Ensure all methods acquire nested locks in the identical order.",
                "Consider using a single coarse-grained lock or ReaderWriterLockSlim.",
                "Use async-compatible primitives like SemaphoreSlim for asynchronous code."
            };
            var example = 
@"// ❌ BEFORE (Method A locks 1->2; Method B locks 2->1 = DEADLOCK):
void MethodA() { lock(lock1) { lock(lock2) { ... } } }
void MethodB() { lock(lock2) { lock(lock1) { ... } } }

// ✅ AFTER (Consistent lock ordering):
void MethodA() { lock(lock1) { lock(lock2) { ... } } }
void MethodB() { lock(lock1) { lock(lock2) { ... } } }";
            return (rationale, recommendation, steps, example);
        }

        public static (string Rationale, string Recommendation, List<string> ActionSteps, string CodeExample) GetAsyncVoidAdvice(string methodName)
        {
            var rationale = $"Method '{methodName}' is declared 'async void'. Unhandled exceptions in async void methods cannot be caught by callers and will terminate the runtime process.";
            var recommendation = "Change the return type from 'void' to 'Task' (or 'ValueTask').";
            var steps = new List<string>
            {
                $"Change `public async void {methodName}()` to `public async Task {methodName}()`.",
                "Await callers of this method."
            };
            var example = 
@"// ❌ BEFORE (Process crash on exception):
public async void ProcessPaymentAsync() {
    await _gateway.ChargeAsync();
}

// ✅ AFTER (Safe exception propagation):
public async Task ProcessPaymentAsync() {
    await _gateway.ChargeAsync();
}";
            return (rationale, recommendation, steps, example);
        }

        public static (string Rationale, string Recommendation, List<string> ActionSteps, string CodeExample) GetSyncOverAsyncAdvice(string call)
        {
            var rationale = $"Calling synchronous blocking property/method '{call}' on a Task can cause thread starvation and SynchronizationContext deadlocks.";
            var recommendation = "Use the 'await' keyword to asynchronously wait for task completion.";
            var steps = new List<string>
            {
                "Make the enclosing method `async Task`.",
                $"Replace `{call}` with `await task`."
            };
            var example = 
@"// ❌ BEFORE (Sync-over-async blocking):
var user = _userService.GetUserAsync(id).Result;

// ✅ AFTER (Asynchronous non-blocking await):
var user = await _userService.GetUserAsync(id);";
            return (rationale, recommendation, steps, example);
        }

        public static (string Rationale, string Recommendation, List<string> ActionSteps, string CodeExample) GetUnsafeLockAdvice(string lockTarget)
        {
            var rationale = $"Locking on '{lockTarget}' exposes the synchronization object to external code, causing lock contention or unexpected deadlocks.";
            var recommendation = "Lock on a private, dedicated object instance: `private readonly object _lock = new object();`.";
            var steps = new List<string>
            {
                "Declare `private readonly object _syncRoot = new();` inside your class.",
                "Lock exclusively on `_syncRoot`."
            };
            var example = 
@"// ❌ BEFORE:
lock (this) { ... }
lock (typeof(MyService)) { ... }

// ✅ AFTER:
private readonly object _syncRoot = new();
lock (_syncRoot) { ... }";
            return (rationale, recommendation, steps, example);
        }

        public static (string Rationale, string Recommendation, List<string> ActionSteps, string CodeExample) GetUnawaitedTaskAdvice(string invocation)
        {
            var rationale = $"Async invocation '{invocation}' is not awaited or captured. Any exceptions thrown by the task will be swallowed unhandled.";
            var recommendation = "Await the asynchronous task call or capture the Task reference.";
            var steps = new List<string>
            {
                $"Add `await` before `{invocation}`.",
                "Ensure the enclosing method is marked `async Task`."
            };
            var example = 
@"// ❌ BEFORE (Unawaited fire-and-forget task):
SaveAuditLogAsync(log);

// ✅ AFTER (Safely awaited):
await SaveAuditLogAsync(log);";
            return (rationale, recommendation, steps, example);
        }

        // ==========================================
        // 4. SECURITY & VULNERABILITIES (SEC)
        // ==========================================

        public static (string Rationale, string Recommendation, List<string> ActionSteps, string CodeExample) GetSqlInjectionAdvice(string query)
        {
            var rationale = $"Dynamic SQL query '{query}' concatenates raw strings, allowing malicious SQL injection attacks.";
            var recommendation = "Use parameterized queries or ORM parameter binding (e.g. SqlParameter, Dapper parameters, EF Core FromSqlInterpolated).";
            var steps = new List<string>
            {
                "Replace string interpolation with query parameters (@paramName).",
                "Pass parameters using `SqlParameter` or anonymous objects in Dapper."
            };
            var example = 
@"// ❌ BEFORE (SQL Injection Risk):
string query = $""SELECT * FROM Users WHERE Email = '{email}'"";
cmd.CommandText = query;

// ✅ AFTER (Parameterized SQL):
string query = ""SELECT * FROM Users WHERE Email = @Email"";
cmd.CommandText = query;
cmd.Parameters.AddWithValue(""@Email"", email);";
            return (rationale, recommendation, steps, example);
        }

        public static (string Rationale, string Recommendation, List<string> ActionSteps, string CodeExample) GetHardcodedSecretAdvice(string secret)
        {
            var rationale = $"Hardcoded credential or secret '{secret}' is embedded in source code, risking exposure in version control and deployment artifacts.";
            var recommendation = "Extract credentials to environment variables, Azure Key Vault, AWS Secrets Manager, or user-secrets.";
            var steps = new List<string>
            {
                "Remove the secret literal from source code.",
                "Retrieve the secret at runtime via `IConfiguration[\"ApiKey\"]` or `Environment.GetEnvironmentVariable()`. "
            };
            var example = 
@"// ❌ BEFORE:
string apiKey = ""AKIAIOSFODNN7EXAMPLE1234"";

// ✅ AFTER:
string apiKey = configuration[""Aws:ApiKey""] ?? throw new InvalidOperationException(""ApiKey missing"");";
            return (rationale, recommendation, steps, example);
        }

        public static (string Rationale, string Recommendation, List<string> ActionSteps, string CodeExample) GetWeakCryptoAdvice(string algorithm)
        {
            var rationale = $"Cryptographic algorithm '{algorithm}' is cryptographically broken and vulnerable to collision and preimage attacks.";
            var recommendation = "Upgrade to SHA-256 / SHA-512 for hashing or AES-256-GCM for symmetric encryption.";
            var steps = new List<string>
            {
                $"Replace `{algorithm}` with `SHA256.Create()` or `Aes.Create()`.",
                "Use standard PBKDF2/Argon2 for password hashing."
            };
            var example = 
@"// ❌ BEFORE:
using var md5 = MD5.Create();

// ✅ AFTER:
using var sha256 = SHA256.Create();";
            return (rationale, recommendation, steps, example);
        }

        public static (string Rationale, string Recommendation, List<string> ActionSteps, string CodeExample) GetXssAdvice(string target)
        {
            var rationale = $"Unsanitized output written directly to HTML response via '{target}' can lead to Cross-Site Scripting (XSS) attacks.";
            var recommendation = "Encode user input with `HtmlEncoder.Default.Encode()` or use framework templating engines with auto-escaping.";
            var steps = new List<string>
            {
                "Wrap untrusted inputs in `HtmlEncoder.Default.Encode(input)`.",
                "Avoid `Html.Raw` or `Response.Write` with raw user parameters."
            };
            var example = 
@"// ❌ BEFORE:
Response.Write(""<div>Hello "" + Request.QueryString[""name""] + ""</div>"");

// ✅ AFTER:
string safeName = System.Text.Encodings.Web.HtmlEncoder.Default.Encode(Request.QueryString[""name""] ?? """");
Response.Write($""<div>Hello {safeName}</div>"");";
            return (rationale, recommendation, steps, example);
        }

        // ==========================================
        // 5. PERFORMANCE & MEMORY (PERF)
        // ==========================================

        public static (string Rationale, string Recommendation, List<string> ActionSteps, string CodeExample) GetStringInLoopAdvice(string expression)
        {
            var rationale = $"Repeated string concatenation ('+=') inside a loop creates new string allocations on every iteration, triggering frequent Garbage Collection (GC) pauses.";
            var recommendation = "Use `StringBuilder` to append strings within loops efficiently.";
            var steps = new List<string>
            {
                "Instantiate `var sb = new StringBuilder();` before entering the loop.",
                "Replace `str += val;` with `sb.Append(val);` inside the loop.",
                "Retrieve final string with `sb.ToString()`."
            };
            var example = 
@"// ❌ BEFORE (O(N^2) memory allocations):
string result = """";
foreach (var item in items) {
    result += item.Name + "","";
}

// ✅ AFTER (O(N) with StringBuilder):
var sb = new StringBuilder();
foreach (var item in items) {
    sb.Append(item.Name).Append("","");
}
string result = sb.ToString();";
            return (rationale, recommendation, steps, example);
        }

        public static (string Rationale, string Recommendation, List<string> ActionSteps, string CodeExample) GetBoxingAdvice(string expression)
        {
            var rationale = $"Expression '{expression}' boxes value types into heap-allocated objects, causing GC pressure in performance-sensitive paths.";
            var recommendation = "Use generic collections `List<T>`, generic methods, or string interpolation over legacy non-generic APIs.";
            var steps = new List<string>
            {
                "Replace `ArrayList` or `Hashtable` with `List<T>` or `Dictionary<TKey, TValue>`.",
                "Use strongly typed generic interfaces instead of `object`."
            };
            var example = 
@"// ❌ BEFORE (Boxes integers to object):
var list = new System.Collections.ArrayList();
list.Add(100);

// ✅ AFTER (Zero boxing with generic List<T>):
var list = new List<int>();
list.Add(100);";
            return (rationale, recommendation, steps, example);
        }

        public static (string Rationale, string Recommendation, List<string> ActionSteps, string CodeExample) GetLinqCountAdvice(string expression)
        {
            var rationale = $"Using '{expression}' iterates the entire enumerable sequence to compute the full count just to check for presence.";
            var recommendation = "Replace `.Count() > 0` with `.Any()` to exit immediately on the first matching element.";
            var steps = new List<string>
            {
                "Replace `items.Count() > 0` or `items.Count() != 0` with `items.Any()`.",
                "Replace `items.Count() == 0` with `!items.Any()`."
            };
            var example = 
@"// ❌ BEFORE (Iterates all 10,000 items):
if (items.Count() > 0) { ... }
if (items.Count() == 0) { ... }

// ✅ AFTER (Exits immediately after first item):
if (items.Any()) { ... }
if (!items.Any()) { ... }";
            return (rationale, recommendation, steps, example);
        }

        // ==========================================
        // 6. ARCHITECTURE & STANDARDS (ARCH)
        // ==========================================

        public static (string Rationale, string Recommendation, List<string> ActionSteps, string CodeExample) GetInterfaceNamingAdvice(string interfaceName, string expectedName)
        {
            var rationale = $"Interface '{interfaceName}' violates .NET conventions. Interfaces must begin with a capital 'I' followed by PascalCase.";
            var recommendation = $"Rename interface to '{expectedName}'.";
            var steps = new List<string>
            {
                $"Rename interface `{interfaceName}` to `{expectedName}`.",
                "Update all implementing classes and dependency injection registrations."
            };
            var example = 
@"// ❌ BEFORE:
public interface OrderProcessor { }

// ✅ AFTER:
public interface IOrderProcessor { }";
            return (rationale, recommendation, steps, example);
        }

        public static (string Rationale, string Recommendation, List<string> ActionSteps, string CodeExample) GetAsyncNamingAdvice(string methodName, string expectedName)
        {
            var rationale = $"Async method '{methodName}' is missing the standard 'Async' suffix.";
            var recommendation = $"Rename method to '{expectedName}'.";
            var steps = new List<string>
            {
                $"Rename `{methodName}` to `{expectedName}`.",
                "Update all call sites."
            };
            var example = 
@"// ❌ BEFORE:
public async Task<User> GetUser(int id)

// ✅ AFTER:
public async Task<User> GetUserAsync(int id)";
            return (rationale, recommendation, steps, example);
        }

        public static (string Rationale, string Recommendation, List<string> ActionSteps, string CodeExample) GetTypeNamingAdvice(string typeName, string expectedName)
        {
            var rationale = $"Type '{typeName}' violates .NET naming standards. Classes, structs, and records must be PascalCase without underscores.";
            var recommendation = $"Rename type to '{expectedName}'.";
            var steps = new List<string>
            {
                $"Rename type `{typeName}` to `{expectedName}`.",
                "Ensure file name matches the new type name."
            };
            var example = 
@"// ❌ BEFORE:
public class employee_manager { }

// ✅ AFTER:
public class EmployeeManager { }";
            return (rationale, recommendation, steps, example);
        }

        public static (string Rationale, string Recommendation, List<string> ActionSteps, string CodeExample) GetPropertyNamingAdvice(string propName, string expectedName)
        {
            var rationale = $"Property '{propName}' violates .NET naming standards. Properties must start with an uppercase letter and use PascalCase.";
            var recommendation = $"Rename property to '{expectedName}'.";
            var steps = new List<string>
            {
                $"Rename `{propName}` to `{expectedName}`."
            };
            var example = 
@"// ❌ BEFORE:
public string departmentName { get; set; }

// ✅ AFTER:
public string DepartmentName { get; set; }";
            return (rationale, recommendation, steps, example);
        }

        public static (string Rationale, string Recommendation, List<string> ActionSteps, string CodeExample) GetVariableNamingAdvice(string varName, string expectedName, string reason)
        {
            var rationale = $"Variable '{varName}' {reason}. In C#, local variables and parameters must follow camelCase conventions without underscores.";
            var recommendation = $"Rename variable to '{expectedName}'.";
            var steps = new List<string>
            {
                $"Rename `{varName}` to `{expectedName}`."
            };
            var example = 
@"// ❌ BEFORE:
int Total_Count = 0;
int user_id = 10;

// ✅ AFTER:
int totalCount = 0;
int userId = 10;";
            return (rationale, recommendation, steps, example);
        }

        public static (string Rationale, string Recommendation, List<string> ActionSteps, string CodeExample) GetMethodNamingAdvice(string methodName, string expectedName)
        {
            var rationale = $"Method '{methodName}' violates C# naming standards. Methods must begin with an uppercase letter and follow PascalCase conventions without underscores.";
            var recommendation = $"Rename method to '{expectedName}'.";
            var steps = new List<string>
            {
                $"Rename method `{methodName}` to `{expectedName}`.",
                "Update all call sites across the project."
            };
            var example = 
@"// ❌ BEFORE:
public void printEmployeeInfo() { }

// ✅ AFTER:
public void PrintEmployeeInfo() { }";
            return (rationale, recommendation, steps, example);
        }

        public static (string Rationale, string Recommendation, List<string> ActionSteps, string CodeExample) GetFieldNamingAdvice(string fieldName, string expectedName)
        {
            var rationale = $"Private or internal field '{fieldName}' is declared in PascalCase. In C#, fields should use camelCase or an underscore prefix (_camelCase).";
            var recommendation = $"Rename field to '{expectedName}'.";
            var steps = new List<string>
            {
                $"Rename field `{fieldName}` to `{expectedName}`.",
                "Update all references within the class."
            };
            var example = 
@"// ❌ BEFORE:
private List<Employee> EmployeeList;

// ✅ AFTER:
private readonly List<Employee> _employeeList;";
            return (rationale, recommendation, steps, example);
        }

        public static (string Rationale, string Recommendation, List<string> ActionSteps, string CodeExample) GetObsoleteApiAdvice(string apiName)
        {
            var rationale = $"Usage of deprecated API '{apiName}' is flagged as obsolete and may be removed or poses security/compatibility risks in future .NET versions.";
            var recommendation = $"Replace obsolete '{apiName}' with supported modern .NET alternatives.";
            var steps = new List<string>
            {
                $"Check the Obsolete message on `{apiName}` for recommended replacements.",
                "Upgrade to recommended modern standard APIs."
            };
            var example = 
@"// ❌ BEFORE (Deprecated BinaryFormatter / WebClient):
var client = new WebClient();

// ✅ AFTER (Modern HttpClient):
using var client = new HttpClient();";
            return (rationale, recommendation, steps, example);
        }
    }
}

namespace OneSwiss.V8.Platform.Services;

// AutoContext<T> reflects properties/methods declared on the exact closed type T only, not on
// further subclasses - so RagentService/RasService/CrServer can't share an AutoContext<V8Service>
// base and still expose their own [ContextProperty] members. This stays a plain interface so each
// concrete service can be its own AutoContext<T> while still sharing Name/IsActive polymorphically
// for the internal service-discovery code in V8Services.cs.
public interface IV8Service
{
    string Name { get; set; }
    bool IsActive { get; set; }
}

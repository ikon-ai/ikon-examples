# Function Registry

## Function Registry

Register callable functions that can be invoked by LLMs, clients, and pipelines.

### Direct Registration

```csharp
FunctionRegistry.Instance.AddFunction(
    Function.Register(MyMethod, "my_function",
        new FunctionAttribute { Description = "Description of what it does" }),
    FunctionVisibility.Shared);
```

### Attribute-Based Registration (Static Methods)

```csharp
public class MathFunctions
{
    [Function(Name = "Add", Description = "Adds two numbers", Visibility = FunctionVisibility.Shared)]
    public static int Add(int a, int b) => a + b;
}

FunctionRegistry.Instance.RegisterFromType(typeof(MathFunctions));
```

### Attribute-Based Registration (Instance Methods)

```csharp
[RegisterAll(Visibility = FunctionVisibility.Shared)]
public class GreetingFunctions(string greeting)
{
    [Function(Name = "Greet", Description = "Greets a person")]
    public string Greet(string name) => $"{greeting}, {name}!";
}

FunctionRegistry.Instance.RegisterFromInstance(new GreetingFunctions("Hello"));
```

### Pipeline Registration

```csharp
FunctionRegistry.Instance.RegisterPipeline<MyPipeline>("run_my_pipeline");
```

### Function Visibility

- `FunctionVisibility.Shared` - Distributed to connected clients and available to LLMs
- `FunctionVisibility.Local` - Only callable within the server process

### Calling Functions

```csharp
var result = FunctionRegistry.Instance.Call<int>("Add", [2, 3]);
var result = await FunctionRegistry.Instance.CallAsync<string>("Greet", args: ["World"]);
```

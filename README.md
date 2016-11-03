# CSharpScriptSerializer
A library that generates C# scripts that can be used to construct the given object:

```C#
    var input = new Point {X = 1, Y = 1};
    var script = CSScriptSerializer.Serialize(input);
    var output = CSScriptSerializer.Deserialize<Point>(script);
```

Here `script` is equal to `"new Point {X = 1, Y = 1}"`

See [RoundTrippingTest.cs](https://github.com/AndriySvyryd/CSharpScriptSerializer/blob/dev/test/CSharpScriptSerializer.Tests/RoundTrippingTest.cs) for more examples.

Get it from [NuGet](https://www.nuget.org/packages/CSharpScriptSerializer/).
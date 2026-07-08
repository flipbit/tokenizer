```

BenchmarkDotNet v0.15.8, macOS Sequoia 15.7.8 (24G812) [Darwin 24.6.0]
Apple M3, 1 CPU, 8 logical and 8 physical cores
.NET SDK 10.0.301
  [Host]     : .NET 10.0.9 (10.0.9, 10.0.926.27113), Arm64 RyuJIT armv8.0-a
  DefaultJob : .NET 10.0.9 (10.0.9, 10.0.926.27113), Arm64 RyuJIT armv8.0-a


```
| Method                                             | Mean       | Error     | StdDev    | P95        | Gen0    | Completed Work Items | Lock Contentions | Gen1   | Allocated |
|--------------------------------------------------- |-----------:|----------:|----------:|-----------:|--------:|---------------------:|-----------------:|-------:|----------:|
| &#39;Compile small template (3 tokens)&#39;                |   9.397 μs | 0.0938 μs | 0.0877 μs |   9.506 μs |  4.5776 |                    - |                - | 0.0763 |  37.41 KB |
| &#39;Compile medium template (12 tokens)&#39;              |  44.828 μs | 0.5371 μs | 0.5024 μs |  45.509 μs | 20.0195 |                    - |                - | 1.1597 | 163.85 KB |
| &#39;Compile large template (39 tokens, front matter)&#39; | 150.509 μs | 1.1683 μs | 0.9756 μs | 152.112 μs | 66.4063 |                    - |                - | 9.2773 | 543.71 KB |

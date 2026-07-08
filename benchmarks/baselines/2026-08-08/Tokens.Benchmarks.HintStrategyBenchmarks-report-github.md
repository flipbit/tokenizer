```

BenchmarkDotNet v0.15.8, macOS Sequoia 15.7.8 (24G812) [Darwin 24.6.0]
Apple M3, 1 CPU, 8 logical and 8 physical cores
.NET SDK 10.0.301
  [Host]     : .NET 10.0.9 (10.0.9, 10.0.926.27113), Arm64 RyuJIT armv8.0-a
  DefaultJob : .NET 10.0.9 (10.0.9, 10.0.926.27113), Arm64 RyuJIT armv8.0-a


```
| Method                              | Mean         | Error       | StdDev      | P95          | Ratio | RatioSD | Gen0   | Completed Work Items | Lock Contentions | Gen1   | Allocated | Alloc Ratio |
|------------------------------------ |-------------:|------------:|------------:|-------------:|------:|--------:|-------:|---------------------:|-----------------:|-------:|----------:|------------:|
| &#39;Hints present — full tokenization&#39; | 215,030.5 ns | 3,271.66 ns | 3,359.75 ns | 220,471.9 ns | 1.000 |    0.02 | 3.9063 |                    - |                - |      - |  46.17 KB |        1.00 |
| &#39;Hints missing — early rejection&#39;   |     797.1 ns |     5.28 ns |     4.94 ns |     801.9 ns | 0.004 |    0.00 | 0.9003 |                    - |                - | 0.0038 |   7.36 KB |        0.16 |

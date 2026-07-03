```

BenchmarkDotNet v0.15.8, macOS Sequoia 15.7.8 (24G809) [Darwin 24.6.0]
Apple M3, 1 CPU, 8 logical and 8 physical cores
.NET SDK 10.0.301
  [Host]     : .NET 10.0.9 (10.0.9, 10.0.926.27113), Arm64 RyuJIT armv8.0-a
  DefaultJob : .NET 10.0.9 (10.0.9, 10.0.926.27113), Arm64 RyuJIT armv8.0-a


```
| Method                              | Mean       | Error     | StdDev    | P95        | Ratio | RatioSD | Completed Work Items | Lock Contentions | Gen0    | Gen1   | Allocated | Alloc Ratio |
|------------------------------------ |-----------:|----------:|----------:|-----------:|------:|--------:|---------------------:|-----------------:|--------:|-------:|----------:|------------:|
| &#39;Hints present — full tokenization&#39; | 253.145 μs | 3.3567 μs | 2.8030 μs | 257.094 μs | 1.000 |    0.02 |                    - |                - | 25.3906 |      - |  207.8 KB |        1.00 |
| &#39;Hints missing — early rejection&#39;   |   2.385 μs | 0.0420 μs | 0.0351 μs |   2.442 μs | 0.009 |    0.00 |                    - |                - |  1.3275 | 0.0076 |  10.86 KB |        0.05 |

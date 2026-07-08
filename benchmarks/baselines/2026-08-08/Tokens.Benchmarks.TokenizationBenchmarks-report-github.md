```

BenchmarkDotNet v0.15.8, macOS Sequoia 15.7.8 (24G812) [Darwin 24.6.0]
Apple M3, 1 CPU, 8 logical and 8 physical cores
.NET SDK 10.0.301
  [Host]     : .NET 10.0.9 (10.0.9, 10.0.926.27113), Arm64 RyuJIT armv8.0-a
  DefaultJob : .NET 10.0.9 (10.0.9, 10.0.926.27113), Arm64 RyuJIT armv8.0-a


```
| Method                                     | Mean       | Error     | StdDev    | P95        | Gen0   | Completed Work Items | Lock Contentions | Gen1   | Allocated |
|------------------------------------------- |-----------:|----------:|----------:|-----------:|-------:|---------------------:|-----------------:|-------:|----------:|
| &#39;Tokenize small (3 tokens)&#39;                |   2.530 μs | 0.0177 μs | 0.0165 μs |   2.555 μs | 1.2398 |                    - |                - | 0.0153 |  10.16 KB |
| &#39;Tokenize medium (12 tokens)&#39;              |  22.780 μs | 0.0879 μs | 0.0734 μs |  22.867 μs | 2.9297 |                    - |                - |      - |  25.92 KB |
| &#39;Tokenize large (39 tokens, front matter)&#39; | 210.550 μs | 0.6815 μs | 0.5691 μs | 211.319 μs | 5.8594 |                    - |                - |      - |  48.86 KB |

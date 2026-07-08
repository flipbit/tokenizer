```

BenchmarkDotNet v0.15.8, macOS Sequoia 15.7.8 (24G812) [Darwin 24.6.0]
Apple M3, 1 CPU, 8 logical and 8 physical cores
.NET SDK 10.0.301
  [Host]     : .NET 10.0.9 (10.0.9, 10.0.926.27113), Arm64 RyuJIT armv8.0-a
  DefaultJob : .NET 10.0.9 (10.0.9, 10.0.926.27113), Arm64 RyuJIT armv8.0-a


```
| Method                                  | Mean       | Error      | StdDev     | P95        | Ratio  | RatioSD | Gen0    | Completed Work Items | Lock Contentions | Gen1   | Allocated | Alloc Ratio |
|---------------------------------------- |-----------:|-----------:|-----------:|-----------:|-------:|--------:|--------:|---------------------:|-----------------:|-------:|----------:|------------:|
| &#39;Pre-compiled: small (3 tokens)&#39;        |   2.583 μs |  0.0385 μs |  0.0412 μs |   2.660 μs |   1.00 |    0.02 |  1.2398 |                    - |                - | 0.0153 |  10.16 KB |        1.00 |
| &#39;Pre-compiled: medium (12 tokens)&#39;      |  23.682 μs |  0.3604 μs |  0.3010 μs |  24.141 μs |   9.17 |    0.18 |  3.1433 |                    - |                - | 0.0610 |  25.92 KB |        2.55 |
| &#39;Pre-compiled: large (39 tokens)&#39;       | 217.219 μs |  1.8123 μs |  1.6952 μs | 219.895 μs |  84.12 |    1.44 |  5.8594 |                    - |                - |      - |  48.86 KB |        4.81 |
| &#39;Concurrent tokenize: 8 threads, large&#39; | 597.062 μs | 11.9028 μs | 28.7465 μs | 647.173 μs | 231.22 |   11.61 | 46.8750 |               8.3711 |                - | 7.8125 | 401.92 KB |       39.57 |

```

BenchmarkDotNet v0.15.8, macOS Sequoia 15.7.8 (24G812) [Darwin 24.6.0]
Apple M3, 1 CPU, 8 logical and 8 physical cores
.NET SDK 10.0.301
  [Host]     : .NET 10.0.9 (10.0.9, 10.0.926.27113), Arm64 RyuJIT armv8.0-a
  DefaultJob : .NET 10.0.9 (10.0.9, 10.0.926.27113), Arm64 RyuJIT armv8.0-a


```
| Method                                                     | TemplateCount | Mean     | Error    | StdDev   | Median   | P95      | Gen0    | Completed Work Items | Lock Contentions | Gen1   | Allocated |
|----------------------------------------------------------- |-------------- |---------:|---------:|---------:|---------:|---------:|--------:|---------------------:|-----------------:|-------:|----------:|
| **&#39;Tokenize best-first (matching template registered first)&#39;** | **5**             | **26.83 μs** | **0.202 μs** | **0.179 μs** | **26.85 μs** | **27.05 μs** |  **6.1035** |                    **-** |                **-** |      **-** |  **51.35 KB** |
| &#39;Tokenize best-last (matching template registered last)&#39;   | 5             | 27.14 μs | 0.239 μs | 0.212 μs | 27.14 μs | 27.43 μs |  6.3477 |                    - |                - |      - |  52.17 KB |
| **&#39;Tokenize best-first (matching template registered first)&#39;** | **15**            | **36.55 μs** | **0.730 μs** | **1.370 μs** | **35.96 μs** | **39.10 μs** | **14.1602** |                    **-** |                **-** | **0.4883** | **115.69 KB** |
| &#39;Tokenize best-last (matching template registered last)&#39;   | 15            | 35.58 μs | 0.332 μs | 0.310 μs | 35.55 μs | 35.98 μs | 14.1602 |                    - |                - | 0.4883 | 115.69 KB |
| **&#39;Tokenize best-first (matching template registered first)&#39;** | **50**            | **65.81 μs** | **0.988 μs** | **0.825 μs** | **65.80 μs** | **66.91 μs** | **41.0156** |                    **-** |                **-** | **3.4180** | **338.31 KB** |
| &#39;Tokenize best-last (matching template registered last)&#39;   | 50            | 65.17 μs | 1.293 μs | 1.437 μs | 65.72 μs | 66.46 μs | 41.0156 |                    - |                - | 3.4180 | 338.31 KB |

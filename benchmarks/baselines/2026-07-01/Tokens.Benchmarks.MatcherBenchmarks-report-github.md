```

BenchmarkDotNet v0.15.8, macOS Sequoia 15.7.8 (24G809) [Darwin 24.6.0]
Apple M3, 1 CPU, 8 logical and 8 physical cores
.NET SDK 10.0.301
  [Host]   : .NET 10.0.9 (10.0.9, 10.0.926.27113), Arm64 RyuJIT armv8.0-a
  ShortRun : .NET 10.0.9 (10.0.9, 10.0.926.27113), Arm64 RyuJIT armv8.0-a

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                                  | TemplateCount | Mean       | Error     | StdDev   | P95        | Completed Work Items | Lock Contentions | Gen0     | Gen1    | Allocated  |
|-------------------------------------------------------- |-------------- |-----------:|----------:|---------:|-----------:|---------------------:|-----------------:|---------:|--------:|-----------:|
| **&#39;Match best-first (matching template registered first)&#39;** | **5**             |   **179.7 μs** |  **87.26 μs** |  **4.78 μs** |   **184.4 μs** |                    **-** |                **-** |  **61.0352** |  **1.4648** |  **502.26 KB** |
| &#39;Match best-last (matching template registered last)&#39;   | 5             |   171.2 μs |  29.31 μs |  1.61 μs |   172.7 μs |                    - |                - |  61.0352 |  1.4648 |  502.26 KB |
| **&#39;Match best-first (matching template registered first)&#39;** | **15**            |   **378.9 μs** |  **36.99 μs** |  **2.03 μs** |   **380.9 μs** |                    **-** |                **-** | **147.4609** |  **6.8359** |  **1210.6 KB** |
| &#39;Match best-last (matching template registered last)&#39;   | 15            |   375.5 μs |  58.99 μs |  3.23 μs |   377.9 μs |                    - |                - | 147.4609 |  5.8594 |  1210.4 KB |
| **&#39;Match best-first (matching template registered first)&#39;** | **50**            | **1,106.3 μs** | **571.13 μs** | **31.31 μs** | **1,127.3 μs** |                    **-** |                **-** | **449.2188** | **46.8750** | **3689.25 KB** |
| &#39;Match best-last (matching template registered last)&#39;   | 50            | 1,072.3 μs | 118.05 μs |  6.47 μs | 1,077.8 μs |                    - |                - | 449.2188 | 46.8750 | 3688.08 KB |

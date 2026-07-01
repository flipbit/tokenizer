```

BenchmarkDotNet v0.15.8, macOS Sequoia 15.7.8 (24G809) [Darwin 24.6.0]
Apple M3, 1 CPU, 8 logical and 8 physical cores
.NET SDK 10.0.301
  [Host]   : .NET 10.0.9 (10.0.9, 10.0.926.27113), Arm64 RyuJIT armv8.0-a
  ShortRun : .NET 10.0.9 (10.0.9, 10.0.926.27113), Arm64 RyuJIT armv8.0-a

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                          | ThreadCount | Mean       | Error       | StdDev    | P95        | Completed Work Items | Lock Contentions | Gen0       | Gen1       | Gen2     | Allocated |
|------------------------------------------------ |------------ |-----------:|------------:|----------:|-----------:|---------------------:|-----------------:|-----------:|-----------:|---------:|----------:|
| **&#39;Parallel tokenize - shared Tokenizer instance&#39;** | **2**           |   **4.675 ms** |   **0.2447 ms** | **0.0134 ms** |   **4.688 ms** |               **1.0000** |                **-** |  **2664.0625** |    **78.1250** |        **-** |  **21.25 MB** |
| &#39;Parallel tokenize - instance per thread&#39;       | 2           |   5.334 ms |   0.3100 ms | 0.0170 ms |   5.349 ms |               1.0000 |                - |  2695.3125 |    93.7500 |        - |  21.45 MB |
| &#39;Parallel match - shared TokenMatcher instance&#39; | 2           |  17.156 ms |   4.4471 ms | 0.2438 ms |  17.394 ms |               1.0000 |                - | 11343.7500 |   750.0000 |        - |  90.55 MB |
| &#39;Parallel match - instance per thread&#39;          | 2           |  27.164 ms |   2.8313 ms | 0.1552 ms |  27.283 ms |               1.0000 |                - | 18687.5000 |  3031.2500 |        - |  148.8 MB |
| **&#39;Parallel tokenize - shared Tokenizer instance&#39;** | **4**           |   **5.927 ms** |   **3.1947 ms** | **0.1751 ms** |   **6.100 ms** |               **3.0000** |                **-** |  **5343.7500** |   **304.6875** |        **-** |  **42.49 MB** |
| &#39;Parallel tokenize - instance per thread&#39;       | 4           |   6.277 ms |   2.1738 ms | 0.1192 ms |   6.389 ms |               3.0000 |                - |  5398.4375 |   351.5625 |        - |  42.91 MB |
| &#39;Parallel match - shared TokenMatcher instance&#39; | 4           |  23.427 ms |  11.0249 ms | 0.6043 ms |  23.851 ms |               3.0000 |           0.0313 | 22750.0000 |  2625.0000 |        - |  181.1 MB |
| &#39;Parallel match - instance per thread&#39;          | 4           |  42.712 ms |  55.0943 ms | 3.0199 ms |  45.452 ms |               3.0000 |           0.0769 | 37384.6154 |  9461.5385 |        - | 297.58 MB |
| **&#39;Parallel tokenize - shared Tokenizer instance&#39;** | **8**           |  **12.116 ms** |   **2.3124 ms** | **0.1268 ms** |  **12.197 ms** |               **7.0000** |                **-** | **10687.5000** |  **1031.2500** |        **-** |  **84.98 MB** |
| &#39;Parallel tokenize - instance per thread&#39;       | 8           |  12.313 ms |   3.7262 ms | 0.2042 ms |  12.515 ms |               7.0000 |                - | 10796.8750 |  1218.7500 |  15.6250 |  85.79 MB |
| &#39;Parallel match - shared TokenMatcher instance&#39; | 8           |  45.387 ms |  35.0681 ms | 1.9222 ms |  47.280 ms |               7.0000 |           0.5000 | 45500.0000 |  7666.6667 |        - | 362.18 MB |
| &#39;Parallel match - instance per thread&#39;          | 8           | 149.558 ms | 176.9817 ms | 9.7010 ms | 158.276 ms |               7.0000 |           1.0000 | 75000.0000 | 24500.0000 | 500.0000 | 597.32 MB |

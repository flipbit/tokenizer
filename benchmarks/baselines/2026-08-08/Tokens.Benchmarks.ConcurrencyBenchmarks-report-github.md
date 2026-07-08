```

BenchmarkDotNet v0.15.8, macOS Sequoia 15.7.8 (24G812) [Darwin 24.6.0]
Apple M3, 1 CPU, 8 logical and 8 physical cores
.NET SDK 10.0.301
  [Host]     : .NET 10.0.9 (10.0.9, 10.0.926.27113), Arm64 RyuJIT armv8.0-a
  DefaultJob : .NET 10.0.9 (10.0.9, 10.0.926.27113), Arm64 RyuJIT armv8.0-a


```
| Method                                                      | ThreadCount | Mean      | Error     | StdDev    | Median    | P95       | Gen0       | Completed Work Items | Lock Contentions | Gen1      | Allocated |
|------------------------------------------------------------ |------------ |----------:|----------:|----------:|----------:|----------:|-----------:|---------------------:|-----------------:|----------:|----------:|
| **&#39;Parallel tokenize - shared Tokenizer instance&#39;**             | **2**           |  **1.420 ms** | **0.0244 ms** | **0.0228 ms** |  **1.411 ms** |  **1.454 ms** |   **318.3594** |               **1.0000** |                **-** |   **17.5781** |   **2.53 MB** |
| &#39;Parallel tokenize - instance per thread&#39;                   | 2           |  1.453 ms | 0.0286 ms | 0.0340 ms |  1.464 ms |  1.516 ms |   341.7969 |               1.0000 |                - |   21.4844 |   2.73 MB |
| &#39;Parallel tokenize - shared TemplateMatcher instance&#39;       | 2           |  1.941 ms | 0.0266 ms | 0.0235 ms |  1.942 ms |  1.977 ms |  1117.1875 |               1.0000 |                - |   82.0313 |   8.82 MB |
| &#39;Parallel tokenize - TemplateMatcher per thread&#39;            | 2           |  7.199 ms | 0.1248 ms | 0.1168 ms |  7.170 ms |  7.371 ms |  5468.7500 |               1.0000 |                - |  859.3750 |  42.93 MB |
| &#39;Parallel tokenize async - shared Tokenizer instance&#39;       | 2           |  2.450 ms | 0.0447 ms | 0.0460 ms |  2.452 ms |  2.523 ms |   316.4063 |                    - |                - |   58.5938 |   2.53 MB |
| &#39;Parallel tokenize async - shared TemplateMatcher instance&#39; | 2           | 10.783 ms | 0.1499 ms | 0.1329 ms | 10.766 ms | 11.010 ms |  2750.0000 |                    - |                - |  531.2500 |  22.02 MB |
| **&#39;Parallel tokenize - shared Tokenizer instance&#39;**             | **4**           |  **2.047 ms** | **0.0407 ms** | **0.1099 ms** |  **2.068 ms** |  **2.213 ms** |   **638.6719** |               **3.0000** |                **-** |   **76.1719** |   **5.08 MB** |
| &#39;Parallel tokenize - instance per thread&#39;                   | 4           |  2.095 ms | 0.0415 ms | 0.0694 ms |  2.075 ms |  2.221 ms |   691.4063 |               3.0000 |                - |   97.6563 |   5.48 MB |
| &#39;Parallel tokenize - shared TemplateMatcher instance&#39;       | 4           |  3.314 ms | 0.0709 ms | 0.2089 ms |  3.312 ms |  3.649 ms |  2406.2500 |               3.0000 |           0.0078 |  320.3125 |  17.65 MB |
| &#39;Parallel tokenize - TemplateMatcher per thread&#39;            | 4           | 12.191 ms | 0.2430 ms | 0.5727 ms | 12.049 ms | 13.341 ms | 11140.6250 |               3.0000 |                - | 2750.0000 |  85.88 MB |
| &#39;Parallel tokenize async - shared Tokenizer instance&#39;       | 4           |  4.994 ms | 0.0761 ms | 0.0636 ms |  4.995 ms |  5.078 ms |   632.8125 |                    - |                - |  171.8750 |   5.07 MB |
| &#39;Parallel tokenize async - shared TemplateMatcher instance&#39; | 4           | 21.911 ms | 0.4249 ms | 0.3766 ms | 21.839 ms | 22.554 ms |  5333.3333 |                    - |                - |  333.3333 |  44.05 MB |
| **&#39;Parallel tokenize - shared Tokenizer instance&#39;**             | **8**           |  **3.639 ms** | **0.0727 ms** | **0.1927 ms** |  **3.598 ms** |  **3.989 ms** |  **1386.7188** |               **6.9688** |                **-** |  **277.3438** |  **10.92 MB** |
| &#39;Parallel tokenize - instance per thread&#39;                   | 8           |  3.602 ms | 0.0701 ms | 0.0911 ms |  3.559 ms |  3.740 ms |  1507.8125 |               7.0000 |                - |  351.5625 |  11.87 MB |
| &#39;Parallel tokenize - shared TemplateMatcher instance&#39;       | 8           |  5.781 ms | 0.1148 ms | 0.1718 ms |  5.691 ms |  6.042 ms |  5109.3750 |               7.0000 |           0.0859 | 1234.3750 |  35.92 MB |
| &#39;Parallel tokenize - TemplateMatcher per thread&#39;            | 8           | 23.813 ms | 0.4393 ms | 0.3669 ms | 23.784 ms | 24.407 ms | 22285.7143 |               7.0000 |                - | 7285.7143 | 171.87 MB |
| &#39;Parallel tokenize async - shared Tokenizer instance&#39;       | 8           | 10.387 ms | 0.1584 ms | 0.1482 ms | 10.386 ms | 10.590 ms |  1265.6250 |                    - |                - |  453.1250 |  10.14 MB |
| &#39;Parallel tokenize async - shared TemplateMatcher instance&#39; | 8           | 41.842 ms | 0.7947 ms | 0.8161 ms | 41.712 ms | 42.895 ms | 10583.3333 |                    - |                - |  583.3333 |  84.95 MB |

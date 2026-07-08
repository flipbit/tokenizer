```

BenchmarkDotNet v0.15.8, macOS Sequoia 15.7.8 (24G812) [Darwin 24.6.0]
Apple M3, 1 CPU, 8 logical and 8 physical cores
.NET SDK 10.0.301
  [Host]     : .NET 10.0.9 (10.0.9, 10.0.926.27113), Arm64 RyuJIT armv8.0-a
  DefaultJob : .NET 10.0.9 (10.0.9, 10.0.926.27113), Arm64 RyuJIT armv8.0-a


```
| Method                         | Mean     | Error    | StdDev   | P95      | Ratio | RatioSD | Gen0    | Completed Work Items | Lock Contentions | Gen1   | Allocated | Alloc Ratio |
|------------------------------- |---------:|---------:|---------:|---------:|------:|--------:|--------:|---------------------:|-----------------:|-------:|----------:|------------:|
| &#39;Tokenize sync (string)&#39;       | 24.86 μs | 0.348 μs | 0.326 μs | 25.33 μs |  1.00 |    0.02 |  3.1433 |                    - |                - | 0.0610 |  25.92 KB |        1.00 |
| &#39;TokenizeAsync (StringReader)&#39; | 24.74 μs | 0.486 μs | 0.560 μs | 25.67 μs |  1.00 |    0.03 |  3.1738 |                    - |                - |      - |  25.99 KB |        1.00 |
| &#39;Compile sync (string)&#39;        | 45.20 μs | 0.895 μs | 1.312 μs | 47.95 μs |  1.82 |    0.06 | 20.2026 |                    - |                - | 1.2207 | 165.44 KB |        6.38 |
| &#39;CompileAsync (StringReader)&#39;  | 44.25 μs | 0.867 μs | 0.890 μs | 45.79 μs |  1.78 |    0.04 | 21.3623 |                    - |                - | 1.2207 | 174.83 KB |        6.74 |

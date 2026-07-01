```

BenchmarkDotNet v0.15.8, macOS Sequoia 15.7.8 (24G809) [Darwin 24.6.0]
Apple M3, 1 CPU, 8 logical and 8 physical cores
.NET SDK 10.0.301
  [Host]   : .NET 10.0.9 (10.0.9, 10.0.926.27113), Arm64 RyuJIT armv8.0-a
  ShortRun : .NET 10.0.9 (10.0.9, 10.0.926.27113), Arm64 RyuJIT armv8.0-a

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                     | Mean      | Error      | StdDev    | P95       | Completed Work Items | Lock Contentions | Gen0     | Gen1   | Allocated |
|------------------------------------------- |----------:|-----------:|----------:|----------:|---------------------:|-----------------:|---------:|-------:|----------:|
| &#39;Tokenize small (3 tokens)&#39;                |  10.62 μs |   1.620 μs |  0.089 μs |  10.71 μs |                    - |                - |   5.0507 | 0.0305 |  41.37 KB |
| &#39;Tokenize medium (12 tokens)&#39;              |  89.92 μs |  31.264 μs |  1.714 μs |  91.60 μs |                    - |                - |  26.6113 | 0.3662 | 217.55 KB |
| &#39;Tokenize large (39 tokens, front matter)&#39; | 651.16 μs | 528.458 μs | 28.967 μs | 679.47 μs |                    - |                - | 115.2344 | 1.9531 | 944.76 KB |

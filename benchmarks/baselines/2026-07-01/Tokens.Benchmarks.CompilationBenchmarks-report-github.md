```

BenchmarkDotNet v0.15.8, macOS Sequoia 15.7.8 (24G809) [Darwin 24.6.0]
Apple M3, 1 CPU, 8 logical and 8 physical cores
.NET SDK 10.0.301
  [Host]   : .NET 10.0.9 (10.0.9, 10.0.926.27113), Arm64 RyuJIT armv8.0-a
  ShortRun : .NET 10.0.9 (10.0.9, 10.0.926.27113), Arm64 RyuJIT armv8.0-a

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                             | Mean      | Error     | StdDev   | P95       | Completed Work Items | Lock Contentions | Gen0    | Gen1    | Allocated |
|--------------------------------------------------- |----------:|----------:|---------:|----------:|---------------------:|-----------------:|--------:|--------:|----------:|
| &#39;Compile small template (3 tokens)&#39;                |  12.40 μs |  4.225 μs | 0.232 μs |  12.58 μs |                    - |                - |  6.0577 |  0.1373 |  49.55 KB |
| &#39;Compile medium template (12 tokens)&#39;              |  58.40 μs |  3.529 μs | 0.193 μs |  58.58 μs |                    - |                - | 28.3203 |  1.8921 | 231.36 KB |
| &#39;Compile large template (39 tokens, front matter)&#39; | 204.51 μs | 19.547 μs | 1.071 μs | 205.49 μs |                    - |                - | 95.7031 | 16.6016 | 782.31 KB |

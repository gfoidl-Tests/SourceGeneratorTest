// (c) gfoidl, all rights reserved

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using SmartFormat;

namespace SourceGeneratorTest.NamedFormatGenerator;

[MemoryDiagnoser]
[CategoriesColumn]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]

//[ShortRunJob]
//[DisassemblyDiagnoser]
public partial class NamedFormatBenchmarks
{
    public static void Run()
    {
        NamedFormatBenchmarks bench = new();
        Console.WriteLine(bench.SmartFormatPostId());
        Console.WriteLine(bench.GeneratedPostId());
        Console.WriteLine();
        Console.WriteLine(bench.SmartFormatFormumBoardWithName());
        Console.WriteLine(bench.GeneratedForumBoardWithName());

#if !DEBUG
        BenchmarkDotNet.Running.BenchmarkRunner.Run<NamedFormatBenchmarks>();
#endif
    }
    //-------------------------------------------------------------------------
    private int    _postId = 42;
    private int    _boardId = 3;
    private string _boardSlug = "boardId-3";

    [Benchmark(Baseline = true, Description = "SmartFormat")]
    [BenchmarkCategory("PostId")]
    public string SmartFormatPostId() => Smart.Format(RouteTemplates.ForumPost, new { postId = _postId });

    [Benchmark(Description = "Generated")]
    [BenchmarkCategory("PostId")]
    public string GeneratedPostId() => GeneratedPostId(_postId);

    [NamedFormatTemplate(RouteTemplates.ForumPost)]
    private static partial string GeneratedPostId(int postId);
    //-------------------------------------------------------------------------
    [Benchmark(Baseline = true, Description = "SmartFormat")]
    [BenchmarkCategory("ForumBoardWithName")]
    public string SmartFormatFormumBoardWithName() => Smart.Format(RouteTemplates.ForumBoardWithName, new { boardId = _boardId, boardNameSlug = _boardSlug });

    [Benchmark(Description = "Generated")]
    [BenchmarkCategory("ForumBoardWithName")]
    public string GeneratedForumBoardWithName() => GeneratedForumBoardWithName(_boardId, _boardSlug);

    [NamedFormatTemplate(RouteTemplates.ForumBoardWithName)]
    private static partial string GeneratedForumBoardWithName(int boardId, string boardSlug);
}

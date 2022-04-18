// (c) gfoidl, all rights reserved

namespace SourceGeneratorTest.NamedFormatGenerator;

public static class RouteTemplates
{
    public const string Root = "/";

    public const string ForumPosts = Forum + "/posts";
    public const string ForumPost  = ForumPosts + "/{postId}";

    public const string Forum              = Root + "forum";
    public const string ForumBoards        = Forum + "/boards";
    public const string ForumBoard         = ForumBoards + "/{boardId}";
    public const string ForumBoardWithName = ForumBoard + "/{boardNameSlug}";
}

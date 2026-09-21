namespace ListIt.Core.Services;

/// <summary>
/// Static implementation supplying initial greeting and title content.
/// </summary>
public class StaticContentProvider : IContentProvider
{
    public string GetHeader() => "LIST-IT";

    public string GetBody() => "hello there";
}

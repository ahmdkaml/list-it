namespace ListIt.Core.Services;

/// <summary>
/// Service supplying content to be presented in the widget.
/// </summary>
public interface IContentProvider
{
    string GetHeader();
    string GetBody();
}

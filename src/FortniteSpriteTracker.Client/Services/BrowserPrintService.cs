using Microsoft.JSInterop;

namespace FortniteSpriteTracker.Services;

public sealed class BrowserPrintService(IJSRuntime js)
{
    public ValueTask PrintAsync() => js.InvokeVoidAsync("print");
}

using Xunit;

namespace Notification.Wpf.Tests.Lifecycle
{
    /// <summary>
    /// Serializes every test that shows a real window: the overlay window and the area
    /// registry are process-wide statics, so GUI tests must never run in parallel.
    /// </summary>
    [CollectionDefinition("Gui", DisableParallelization = true)]
    public class GuiCollection
    {
    }
}

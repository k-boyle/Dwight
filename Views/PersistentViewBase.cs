using System;
using Disqord;
using Disqord.Extensions.Interactivity.Menus;

namespace Dwight;

/// <summary>
/// A view that <see cref="PersistedViewService"/> rebuilds and reattaches to its message after a restart.
/// Disqord's default custom IDs are random GUIDs, so a rebuilt view would never match the buttons on the
/// already-sent message and clicks would go unanswered; derive them from the button instead.
/// </summary>
public abstract class PersistentViewBase(Action<LocalMessageBase>? messageTemplate) : ViewBase(messageTemplate)
{
    protected override string GetCustomId(InteractableViewComponent component)
    {
        if (component is not ButtonViewComponent { Label: { } label })
            throw new NotSupportedException($"{GetType().Name} can only contain labelled buttons to keep its custom IDs stable");

        return $"{GetType().Name}:{label}";
    }
}

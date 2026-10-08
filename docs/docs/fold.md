---
title: Reading the fold
description: Read the posture, hinge angle and fold bounds from XAML and code.
---

# Reading the fold

[Fold](xref:Nova.Avalonia.Foldable.Fold) publishes the fold of each top level as inherited attached properties. Every control in the tree can read them, bind to them, and use them in style selectors.

## Start tracking

`TwoPaneView` tracks the fold on its own. Anywhere else, set `Fold.IsEnabled` once, near the root of your view:

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:fold="using:Nova.Avalonia.Foldable"
             fold:Fold.IsEnabled="True">
  ...
</UserControl>
```

The provider for the top level is created the first time it is needed and disposed when the top level closes.

## Attached properties

All of these are read-only from your side and inherited by every control below the top level.

| Property | Type | Description |
| --- | --- | --- |
| `Fold.State` | `FoldState` | The whole snapshot. |
| `Fold.Posture` | `DevicePosture` | `Unknown`, `Closed`, `HalfOpened` or `Flat`. |
| `Fold.HingeAngle` | `double?` | Degrees, 0 closed and 180 flat; `null` when the platform doesn't report it. |
| `Fold.IsSeparating` | `bool` | The fold currently divides the content. |
| `Fold.FoldOrientation` | `FoldOrientation` | `None`, `Vertical` (panes left and right) or `Horizontal` (panes top and bottom). |
| `Fold.IsFoldable` | `bool` | The device has a hinge. |

[FoldState](xref:Nova.Avalonia.Foldable.FoldState) also offers `FoldBounds`, the fold as a `Rect?` in top-level coordinates, and two shortcuts: `IsBook` (half open with a vertical fold) and `IsTabletop` (half open with a horizontal fold).

## In XAML

Bind to the properties with the attached property syntax, and use them in style selectors. Selectors separate the namespace with `|`, bindings with `:`.

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:fold="using:Nova.Avalonia.Foldable"
             fold:Fold.IsEnabled="True">
  <UserControl.Styles>
    <!-- Dim the controls while the device is half open. -->
    <Style Selector="Border.controls[(fold|Fold.Posture)=HalfOpened]">
      <Setter Property="Opacity" Value="0.85" />
    </Style>
  </UserControl.Styles>

  <TextBlock Text="{Binding $self.(fold:Fold.Posture)}" />
</UserControl>
```

## In code

Watch for property changes on any control in the tree:

```csharp
protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
{
    base.OnPropertyChanged(change);

    if (change.Property == Fold.StateProperty && Fold.GetState(this) is { IsTabletop: true } state)
        ShowControlsBelow(state.FoldBounds);
}
```

`Fold.State` changes first, and `Fold.GetPosture` and the other `Get` methods read from it, so a handler on any of these properties sees the same snapshot.

Or subscribe to the provider directly:

```csharp
var provider = Fold.GetProvider(TopLevel.GetTopLevel(this)!);
provider.Changed += (_, state) => Debug.WriteLine($"{state.Posture} {state.HingeAngle}°");
```

Providers raise `Changed` on the UI thread.

## Coordinates

`FoldBounds` is in the coordinates of the top level, not of your control. Map it before you position anything against it:

```csharp
if (Fold.GetState(this).FoldBounds is { } fold
    && TopLevel.GetTopLevel(this) is { } topLevel
    && topLevel.TransformToVisual(this) is { } toLocal)
{
    var localFold = fold.TransformToAABB(toLocal);
}
```

Keep in mind that Avalonia 12 pads the content for safe areas (`TopLevel.AutoSafeAreaPadding`), such as the iPhone Duo's sensor edge. `TwoPaneView` maps the fold for you and accounts for it.

## Hinge angle

Use the hinge angle for effects only, never for layout. The iPhone Duo reports it, but many Android foldables don't expose a hinge sensor to apps. Base layout on `Posture`, `IsSeparating` and `FoldBounds`.

```csharp
if (change.Property == Fold.HingeAngleProperty && change.NewValue is double angle)
    Parallax.Depth = angle / 180;
```

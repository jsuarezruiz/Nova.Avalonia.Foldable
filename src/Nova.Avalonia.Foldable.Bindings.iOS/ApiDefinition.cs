using System;
using CoreGraphics;
using Foundation;
using ObjCRuntime;
using UIKit;

namespace Nova.Avalonia.Foldable.Bindings.iOS
{
	// Mirrors https://github.com/dotnet/macios/pull/26705 (src/uikit.cs); keep names identical
	// so switching to the official bindings is a namespace change.

	[NoTV, NoMacCatalyst, iOS (27, 1)]
	[BaseType (typeof (NSObject))]
	[DisableDefaultCtor]
	interface UIHinge : INSCopying
	{
		[Export ("status")]
		UIHingeStatus Status { get; }

		// Radians: 0 is closed, PI is flat.
		[Export ("angle")]
		nfloat Angle { get; }
	}

	[NoTV, NoMacCatalyst, iOS (27, 1)]
	[BaseType (typeof (NSObject))]
	[DisableDefaultCtor]
	interface UIHingeInteractionUpdate : INSCopying
	{
		[NullAllowed, Export ("hinge", ArgumentSemantic.Copy)]
		UIHinge Hinge { get; }
	}

	[NoTV, NoMacCatalyst, iOS (27, 1)]
	delegate void UIHingeInteractionUpdateHandler (UIHingeInteraction interaction, UIHingeInteractionUpdate update);

	[NoTV, NoMacCatalyst, iOS (27, 1)]
	[BaseType (typeof (NSObject))]
	[DisableDefaultCtor]
	interface UIHingeInteraction : IUIInteraction
	{
		[DesignatedInitializer]
		[Export ("initWithUpdateHandler:")]
		NativeHandle Constructor (UIHingeInteractionUpdateHandler updateHandler);

		[Export ("enabled")]
		bool Enabled { [Bind ("isEnabled")] get; set; }

		// UIInteraction protocol members.
		[Export ("view", ArgumentSemantic.Weak)]
		new UIView View { get; }

		[Export ("willMoveToView:")]
		new void WillMoveToView ([NullAllowed] UIView view);

		[Export ("didMoveToView:")]
		new void DidMoveToView ([NullAllowed] UIView view);
	}

	[NoTV, NoMacCatalyst, iOS (27, 1)]
	[BaseType (typeof (NSObject))]
	[DisableDefaultCtor]
	[Sealed]
	interface UIViewReservedRegion : INSCopying
	{
		[Export ("identifier")]
		UIViewReservedRegionIdentifier Identifier { get; }

		[Export ("kind")]
		UIViewReservedRegionKind Kind { get; }

		[Export ("frame")]
		CGRect Frame { get; }

		[Export ("margins")]
		UIEdgeInsets Margins { get; }

		[Export ("active")]
		bool Active { [Bind ("isActive")] get; }
	}

	[NoTV, NoMacCatalyst, iOS (27, 1)]
	[BaseType (typeof (NSObject))]
	[DisableDefaultCtor]
	[Sealed]
	interface UIViewReservedRegionIdentifier : INSCopying
	{
	}

	[NoTV, NoMacCatalyst, iOS (27, 1)]
	[BaseType (typeof (NSObject))]
	[DisableDefaultCtor]
	[Sealed]
	interface UIViewReservedRegionKind : INSCopying
	{
		// Areas the app should not draw important content into, e.g. the camera housing.
		[Static]
		[Export ("occlusionRegionKind")]
		UIViewReservedRegionKind CreateOcclusion ();

		// The fold: the line where the display is divided.
		[Static]
		[Export ("divisionRegionKind")]
		UIViewReservedRegionKind CreateDivision ();
	}

	// A category, so it surfaces as extension methods on UIView.
	[NoTV, NoMacCatalyst, iOS (27, 1)]
	[Category]
	[BaseType (typeof (UIView))]
	interface UIView_ReservedRegions
	{
		[Export ("reservedRegionsOfKind:")]
		UIViewReservedRegion [] GetReservedRegions (UIViewReservedRegionKind kind);

		[Export ("reservedRegionsOfKind:options:")]
		UIViewReservedRegion [] GetReservedRegions (UIViewReservedRegionKind kind, UIViewReservedRegionQueryOptions options);
	}
}

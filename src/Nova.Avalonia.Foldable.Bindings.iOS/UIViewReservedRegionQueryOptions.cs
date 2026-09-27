using System;
using System.Runtime.Versioning;
using ObjCRuntime;

// Mirrors https://github.com/dotnet/macios/pull/26705 (src/UIKit/UIEnums.cs).
namespace Nova.Avalonia.Foldable.Bindings.iOS
{
	[Flags]
	[SupportedOSPlatform ("ios27.1")]
	[Native]
	public enum UIViewReservedRegionQueryOptions : ulong
	{
		None = 0,
		IncludeInactive = 1uL << 0,
	}
}

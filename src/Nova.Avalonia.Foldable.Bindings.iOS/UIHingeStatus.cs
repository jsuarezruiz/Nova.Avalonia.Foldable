using System.Runtime.Versioning;
using ObjCRuntime;

// Mirrors https://github.com/dotnet/macios/pull/26705 (src/UIKit/UIEnums.cs).
namespace Nova.Avalonia.Foldable.Bindings.iOS
{
	[SupportedOSPlatform ("ios27.1")]
	[Native]
	public enum UIHingeStatus : long
	{
		Unknown = 0,
		Closed = 1,
		PartiallyOpen = 2,
		FullyOpen = 3,
	}
}

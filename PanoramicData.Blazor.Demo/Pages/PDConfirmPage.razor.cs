using System.Diagnostics.CodeAnalysis;

namespace PanoramicData.Blazor.Demo.Pages;

public partial class PDConfirmPage
{
	[AllowNull]
	private PDConfirm _confirmModal1 = null!;
	private string _result1 = string.Empty;

	[AllowNull]
	private PDConfirm _confirmModal2 = null!;
	private string _result2 = string.Empty;
	private CancellationTokenSource _cancellationToken2 = new CancellationTokenSource();

	[AllowNull]
	private PDConfirm _confirmModal3 = null!;
	private string _result3 = string.Empty;

	[AllowNull]
	private PDConfirm _confirmModal4 = null!;
	private string _result4 = string.Empty;
}

namespace PanoramicData.Blazor;

/// <summary>
/// PDFormFooter: the Save, Cancel and Delete buttons, and the Yes and No buttons that confirm a delete or cancel.
/// </summary>
public partial class PDFormFooter<TItem> where TItem : class
{
	private async Task OnCancelAsync()
	{
		if (Form is { Item: TItem item } form)
		{
			if (form.ConfirmCancel && form.Delta.Count > 0)
			{
				await form.EditItemAsync(item, FormModes.Cancel).ConfigureAwait(true);
			}
			else
			{
				await form.ResetChanges();
				await Click.InvokeAsync("Cancel").ConfigureAwait(true);
			}
		}
	}

	private async Task OnDeleteAsync()
	{
		if (Form is { Item: TItem item } form)
		{
			await form.EditItemAsync(item, FormModes.Delete).ConfigureAwait(true);
		}
	}

	private async Task OnNoAsync()
	{
		if (Form is { Item: TItem item } form)
		{
			await form.EditItemAsync(item, form.PreviousMode, false).ConfigureAwait(true);
			await Click.InvokeAsync("No").ConfigureAwait(true);
		}
	}

	private async Task OnSaveAsync()
	{
		if (Form is { Item: not null, DataProvider: not null } form)
		{
			var success = await form.SaveAsync().ConfigureAwait(true);
			if (!success)
			{
				return;
			}

			await form.ResetChanges();
		}

		await Click.InvokeAsync("Save").ConfigureAwait(true);
	}

	private async Task OnYesAsync()
	{
		if (Form is not { Item: not null } form)
		{
			return;
		}

		if (form.Mode == FormModes.Delete)
		{
			if (form.DataProvider != null)
			{
				var success = await form.DeleteAsync().ConfigureAwait(true);
				if (!success)
				{
					return;
				}

				await form.ResetChanges();
			}

			// DO NOT change this to "Delete" as it means that there is a non-closable modal after deletion (or cancellation of a delete) !!!
			await Click.InvokeAsync("Yes").ConfigureAwait(true);
		}
		else if (form.Mode == FormModes.Cancel)
		{
			await form.ResetChanges();
			await Click.InvokeAsync("Cancel").ConfigureAwait(true);
		}
	}
}

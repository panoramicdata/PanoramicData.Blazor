namespace PanoramicData.Blazor.Demo.Data;

/// <summary>
/// Creating, updating and deleting demo people.
/// </summary>
public partial class PersonDataProvider
{
	/// <summary>
	/// Requests that the item is deleted.
	/// </summary>
	/// <param name="item">The item to be deleted.</param>
	/// <param name="cancellationToken">A cancellation token for the async operation.</param>
	/// <returns>A new OperationResponse instance that contains the results of the operation.</returns>
	public override async Task<OperationResponse> DeleteAsync(Person item, CancellationToken cancellationToken)
	{
		if (AddDelay)
		{
			await Task.Delay(1000, cancellationToken);
		}

		var existingPerson = _people.Find(x => x.Id == item.Id);
		if (existingPerson == null)
		{
			return new OperationResponse { ErrorMessage = $"Person not found (id {item.Id})" };
		}

		_people.Remove(existingPerson);
		return new OperationResponse { Success = true };
	}

	/// <summary>
	/// Requests the given item is updated by applying the given delta.
	/// </summary>
	/// <param name="item">The original item to be updated.</param>
	/// <param name="delta">A dictionary with new property values.</param>
	/// <param name="cancellationToken">A cancellation token for the async operation.</param>
	/// <returns>A new OperationResponse instance that contains the results of the operation.</returns>
	public override async Task<OperationResponse> UpdateAsync(Person item, IDictionary<string, object?> delta, CancellationToken cancellationToken)
	{
		if (AddDelay)
		{
			await Task.Delay(1000, cancellationToken);
		}


		var existingPerson = _people.Find(x => x.Id == item.Id);
		if (existingPerson == null)
		{
			return new OperationResponse { ErrorMessage = $"Person not found (id {item.Id})" };
		}

		foreach (var kvp in delta)
		{
			var prop = item.GetType().GetProperty(kvp.Key);
			if (prop == null)
			{
				return new OperationResponse { ErrorMessage = $"Person does not contain a property named {kvp.Key}" };
			}
			else
			{
				try
				{
					var value = kvp.Value.Cast(prop.PropertyType);
					prop.SetValue(existingPerson, value);
				}
				catch (Exception ex)
				{
					return new OperationResponse { ErrorMessage = $"Failed to update property {kvp.Key} to {kvp.Value}: {ex.Message}" };
				}
			}
		}

		existingPerson.DateModified = DateTime.Now;
		return new OperationResponse { Success = true };
	}

	/// <summary>
	/// Requests the given item is created.
	/// </summary>
	/// <param name="item">New item details.</param>
	/// <param name="cancellationToken">A cancellation token for the async operation.</param>
	/// <returns>A new OperationResponse instance that contains the results of the operation.</returns>
	public override async Task<OperationResponse> CreateAsync(Person item, CancellationToken cancellationToken)
	{
		if (AddDelay)
		{
			await Task.Delay(1000, cancellationToken);
		}

		item.Id = _people.Max(x => x.Id) + 1;
		item.DateModified = item.DateCreated = DateTime.Now;
		_people.Add(item);
		return new OperationResponse { Success = true };
	}
}

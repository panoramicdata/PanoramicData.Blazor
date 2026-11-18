using Microsoft.JSInterop;

namespace PanoramicData.Blazor.Test;

/// <summary>Turns what a fake resolved into the completed result its caller awaits.</summary>
internal interface IJsResolver
{
	object? Resolve(string identifier, object?[] args);
}

internal static class JsFakeResult
{
	public static ValueTask<TValue> Of<TValue>(IJsResolver resolver, string identifier, object?[] args)
		=> ValueTask.FromResult(resolver.Resolve(identifier, args) is TValue value ? value : default!);
}

/// <summary>A hand-written <see cref="IJSRuntime"/> that implements the cancellable overload once.</summary>
internal abstract class FakeJsRuntime : IJSRuntime, IJsResolver
{
	public abstract object? Resolve(string identifier, object?[] args);

	public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
		=> JsFakeResult.Of<TValue>(this, identifier, args ?? []);

	public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
	{
		cancellationToken.ThrowIfCancellationRequested();
		return JsFakeResult.Of<TValue>(this, identifier, args ?? []);
	}
}

/// <summary>A hand-written <see cref="IJSObjectReference"/> that implements the cancellable overload once.</summary>
internal abstract class FakeJsObject : IJSObjectReference, IJsResolver
{
	public abstract ValueTask DisposeAsync();

	public abstract object? Resolve(string identifier, object?[] args);

	public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
		=> JsFakeResult.Of<TValue>(this, identifier, args ?? []);

	public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
	{
		cancellationToken.ThrowIfCancellationRequested();
		return JsFakeResult.Of<TValue>(this, identifier, args ?? []);
	}
}

/// <summary>A runtime whose only job is to hand out one module on import.</summary>
internal sealed class ModuleOnlyRuntime(IJSObjectReference module) : FakeJsRuntime
{
	public override object? Resolve(string identifier, object?[] args)
		=> identifier == "import"
			? module
			: throw new InvalidOperationException($"Unexpected JS call '{identifier}' with {args.Length} args.");
}

/// <summary>A runtime that cannot import anything.</summary>
internal sealed class OfflineJsRuntime : FakeJsRuntime
{
	public override object? Resolve(string identifier, object?[] args)
		=> throw new JSException($"{identifier} ({args.Length} args) failed: offline");
}

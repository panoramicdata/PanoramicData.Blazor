using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using AwesomeAssertions;
using Bunit;
using PanoramicData.Blazor.Attributes;
using PanoramicData.Blazor.Extensions;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Tests for <see cref="PDColumn{TItem}"/>: value rendering and assignment, titles, filter metadata, sort icons,
/// column group registration and the runtime state setters.
/// </summary>
public class PDColumnTests : BunitContext
{
	private readonly RowProvider _provider = new();

	/// <summary>Sets up the rendering context.</summary>
	public PDColumnTests()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
		Services.AddPanoramicDataBlazor();
	}

	private IRenderedComponent<PDTable<Row>> RenderTable(params Action<ComponentParameterCollectionBuilder<PDColumn<Row>>>[] columns)
	{
		var table = Render<PDTable<Row>>(p =>
		{
			p.Add(x => x.DataProvider, _provider);
			p.Add(x => x.KeyField, r => r.Id);
			p.Add(x => x.ShowPager, false);
			foreach (var column in columns)
			{
				p.AddChildContent<PDColumn<Row>>(column);
			}
		});

		table.WaitForAssertion(() => table.Instance.Columns.Should().HaveCount(columns.Length));
		return table;
	}

	private PDColumn<Row> RenderColumn(Action<ComponentParameterCollectionBuilder<PDColumn<Row>>> column)
		=> RenderTable(column).Instance.Columns.Single();

	private PDColumn<Row> Column(Expression<Func<Row, object>>? field, Action<ComponentParameterCollectionBuilder<PDColumn<Row>>>? configure = null)
		=> RenderColumn(c =>
		{
			if (field is not null)
			{
				c.Add(x => x.Field, field);
			}

			configure?.Invoke(c);
		});

	/// <summary>A column rendered outside a table cannot register and says why.</summary>
	[Fact]
	public void OutsideATable_Throws()
	{
		var act = () => Render<PDColumn<Row>>(p => p.Add(x => x.Id, "orphan"));

		act.Should().Throw<InvalidOperationException>().WithMessage("*orphan*Table reference is null*");
	}

	/// <summary>Registering copies the visibility and ordinal parameters into the column state.</summary>
	[Fact]
	public void Registration_InitialisesStateFromParameters()
	{
		var column = RenderColumn(c => c.Add(x => x.Field, r => r.Name).Add(x => x.IsVisible, false).Add(x => x.Ordinal, 7));

		column.State.Visible.Should().BeFalse();
		column.State.Ordinal.Should().Be(7);
		column.Table.Should().NotBeNull();
	}

	/// <summary>Parameters set derive the data type and property info from the field.</summary>
	[Fact]
	public void ParametersSet_DerivesTypeAndPropertyInfo()
	{
		var column = RenderColumn(c => c.Add(x => x.Field, r => r.Count));

		column.Type.Should().Be<int>();
		column.PropertyInfo!.Name.Should().Be(nameof(Row.Count));
	}

	/// <summary>An explicit type is kept rather than derived.</summary>
	[Fact]
	public void ParametersSet_KeepsAnExplicitType()
	{
		var column = RenderColumn(c => c.Add(x => x.Field, r => r.Count).Add(x => x.Type, typeof(long)));

		column.Type.Should().Be<long>();
	}

	/// <summary>A column wrapped in a column group inherits its name and registers the group with the table.</summary>
	[Fact]
	public void ColumnGroupWrapper_RegistersTheGroup()
	{
		var table = Render<PDTable<Row>>(p => p
			.Add(x => x.DataProvider, _provider)
			.Add(x => x.KeyField, r => r.Id)
			.AddChildContent<PDColumnGroup>(g => g
				.Add(x => x.Name, "Metrics")
				.Add(x => x.Icon, "fas fa-chart-bar")
				.AddChildContent<PDColumn<Row>>(c => c.Add(x => x.Field, r => r.Count))));

		table.WaitForAssertion(() => table.Instance.Columns.Should().ContainSingle());
		table.Instance.Columns[0].GroupName.Should().Be("Metrics");
		table.Instance.ColumnGroups.Should().ContainSingle().Which.Icon.Should().Be("fas fa-chart-bar");
	}

	/// <summary>Inline group metadata on a column registers a group with that metadata.</summary>
	[Fact]
	public void InlineGroupMetadata_RegistersTheGroup()
	{
		var table = RenderTable(c => c
			.Add(x => x.Field, r => r.Count)
			.Add(x => x.Group, "Stats")
			.Add(x => x.GroupIcon, "icon")
			.Add(x => x.GroupOrdinal, 3)
			.Add(x => x.GroupDescription, "desc"));

		var group = table.Instance.ColumnGroups.Should().ContainSingle().Subject;
		group.Name.Should().Be("Stats");
		group.Icon.Should().Be("icon");
		group.Ordinal.Should().Be(3);
		group.Description.Should().Be("desc");
		table.Instance.Columns[0].GroupName.Should().Be("Stats");
	}

	/// <summary>A group name alone, with no metadata, registers no group but still names the column's group.</summary>
	[Fact]
	public void GroupNameWithoutMetadata_RegistersNoGroup()
	{
		var table = RenderTable(c => c.Add(x => x.Field, r => r.Count).Add(x => x.Group, "Stats"));

		table.Instance.ColumnGroups.Should().BeEmpty();
		table.Instance.Columns[0].GroupName.Should().Be("Stats");
	}

	/// <summary>Only a description among the inline metadata is enough to register the group, with the default ordinal.</summary>
	[Fact]
	public void InlineGroupDescriptionOnly_RegistersWithDefaultOrdinal()
	{
		var table = RenderTable(c => c.Add(x => x.Field, r => r.Count).Add(x => x.Group, "Stats").Add(x => x.GroupDescription, "d"));

		table.Instance.ColumnGroups.Should().ContainSingle().Which.Ordinal.Should().Be(1000);
	}

	/// <summary>The state setters change the column's runtime state.</summary>
	[Fact]
	public async Task StateSetters_UpdateRuntimeState()
	{
		var table = RenderTable(c => c.Add(x => x.Field, r => r.Name));
		var column = table.Instance.Columns[0];

		await table.InvokeAsync(() => column.SetOrdinal(4));
		await table.InvokeAsync(() => column.SetVisible(false));
		await table.InvokeAsync(() => column.SetShowInList(false));
		await table.InvokeAsync(() => column.SetTitle("ignored"));
		column.SetId("new-id");

		column.State.Ordinal.Should().Be(4);
		column.State.Visible.Should().BeFalse();
		column.ShowInList.Should().BeFalse();
		column.Id.Should().Be("new-id");
	}

	/// <summary>Rendering a null item gives an empty string.</summary>
	[Fact]
	public void GetRenderValue_NullItem_IsEmpty()
		=> Column(r => r.Name).GetRenderValue(null!).Should().BeEmpty();

	/// <summary>A null field value renders empty; a column with no field renders empty.</summary>
	[Fact]
	public void GetRenderValue_NullValueOrNoField_IsEmpty()
	{
		Column(r => r.MaybeCount!).GetRenderValue(new Row()).Should().BeEmpty();
		Column(null).GetRenderValue(new Row()).Should().BeEmpty();
	}

	/// <summary>A plain value is rendered with ToString, a formatted value with the format string.</summary>
	[Fact]
	public void GetRenderValue_AppliesFormat()
	{
		var row = new Row { Count = 1234 };

		Column(r => r.Count).GetRenderValue(row).Should().Be("1234");
		Column(r => r.Count, c => c.Add(x => x.Format, "D6")).GetRenderValue(row).Should().Be("001234");
	}

	/// <summary>Password columns and sensitive items are masked with one star per character.</summary>
	[Fact]
	public void GetRenderValue_MasksPasswordsAndSensitiveValues()
	{
		var row = new Row { Name = "secret" };
		var password = Column(r => r.Name, c => c.Add(x => x.IsPassword, true));
		var sensitive = Column(r => r.Name, c => c.Add(x => x.IsSensitive, (item, _) => item?.Name == "secret"));

		password.GetRenderValue(row).Should().Be("******");
		sensitive.GetRenderValue(row).Should().Be("******");
	}

	/// <summary>Enum values render their Display name when they have one, otherwise their member name.</summary>
	[Fact]
	public void GetRenderValue_UsesEnumDisplayNames()
	{
		Column(r => r.Colour).GetRenderValue(new Row { Colour = Colour.DarkRed }).Should().Be("Dark red");
		Column(r => r.Colour).GetRenderValue(new Row { Colour = Colour.Blue }).Should().Be("Blue");
	}

	/// <summary>A nested field whose parent is null renders empty rather than throwing.</summary>
	[Fact]
	public void GetRenderValue_NestedNullParent_IsEmpty()
		=> Column(r => r.Child!.Name).GetRenderValue(new Row()).Should().BeEmpty();

	/// <summary>GetValue returns the raw field value, or null when there is no field.</summary>
	[Fact]
	public void GetValue_ReturnsRawValue()
	{
		Column(r => r.Count).GetValue(new Row { Count = 5 }).Should().Be(5);
		Column(null).GetValue(new Row()).Should().BeNull();
	}

	/// <summary>The property name comes from the field, or is empty with no field.</summary>
	[Fact]
	public void GetPropertyName_ComesFromTheField()
	{
		Column(r => r.Name).GetPropertyName().Should().Be(nameof(Row.Name));
		Column(null).GetPropertyName().Should().BeEmpty();
	}

	/// <summary>The sort icon reflects the sort direction.</summary>
	[Fact]
	public void SortIcon_ReflectsDirection()
	{
		var column = Column(r => r.Name);
		column.SortIcon.Should().Contain("fa-sort").And.NotContain("fa-sort-up").And.NotContain("fa-sort-down");

		column.SortDirection = SortDirection.Ascending;
		column.SortIcon.Should().Contain("fa-sort-up");

		column.SortDirection = SortDirection.Descending;
		column.SortIcon.Should().Contain("fa-sort-down");
	}

	/// <summary>The title comes from TitleFunc, then Title, then Display name, then property name.</summary>
	[Fact]
	public void GetTitle_FollowsItsPrecedence()
	{
		var withFunc = Column(r => r.Name, c => c.Add(x => x.TitleFunc, item => $"T:{item?.Name}").Add(x => x.Title, "ignored"));
		withFunc.GetTitle(new Row { Name = "x" }).Should().Be("T:x");

		Column(r => r.Name, c => c.Add(x => x.Title, "Explicit")).GetTitle().Should().Be("Explicit");

		Column(r => r.Shorted).GetTitle().Should().Be("Pretty");
		Column(r => r.Name).GetTitle().Should().Be(nameof(Row.Name));
		Column(null).GetTitle().Should().BeEmpty();
	}

	/// <summary>The filter data type is inferred from the field's property type.</summary>
	/// <param name="property">The Row property to bind.</param>
	/// <param name="expected">The expected filter data type.</param>
	[Theory]
	[InlineData(nameof(Row.Colour), FilterDataTypes.Enum)]
	[InlineData(nameof(Row.MaybeColour), FilterDataTypes.Enum)]
	[InlineData(nameof(Row.Flag), FilterDataTypes.Bool)]
	[InlineData(nameof(Row.MaybeFlag), FilterDataTypes.Bool)]
	[InlineData(nameof(Row.Name), FilterDataTypes.Text)]
	[InlineData(nameof(Row.When), FilterDataTypes.Date)]
	[InlineData(nameof(Row.MaybeWhen), FilterDataTypes.Date)]
	[InlineData(nameof(Row.Stamp), FilterDataTypes.Date)]
	[InlineData(nameof(Row.MaybeStamp), FilterDataTypes.Date)]
	[InlineData(nameof(Row.Count), FilterDataTypes.Numeric)]
	public void GetFilterDataType_IsInferredFromTheProperty(string property, FilterDataTypes expected)
		=> Column(FieldFor(property)).GetFilterDataType().Should().Be(expected);

	/// <summary>A column with no field is treated as numeric.</summary>
	[Fact]
	public void GetFilterDataType_NoField_IsNumeric()
		=> Column(null).GetFilterDataType().Should().Be(FilterDataTypes.Numeric);

	/// <summary>Strings and nullable value types are nullable for filtering; plain value types and no field are not.</summary>
	[Fact]
	public void GetFilterIsNullable_ReflectsThePropertyType()
	{
		Column(r => r.Name).GetFilterIsNullable().Should().BeTrue();
		Column(r => r.MaybeCount!).GetFilterIsNullable().Should().BeTrue();
		Column(r => r.Count).GetFilterIsNullable().Should().BeFalse();
		Column(null).GetFilterIsNullable().Should().BeFalse();
	}

	/// <summary>The filter key honours FilterKey and Display short names, lower-cases the first character otherwise, and chains nested members.</summary>
	[Fact]
	public void GetFilterKey_UsesAttributesAndChains()
	{
		Column(r => r.Keyed).GetFilterKey().Should().Be("custom");
		Column(r => r.Shorted).GetFilterKey().Should().Be("sn");
		Column(r => r.Name).GetFilterKey().Should().Be("name");
		Column(r => r.Child!.Name).GetFilterKey().Should().Be("child.name");
	}

	/// <summary>With no field the filter key is the column id.</summary>
	[Fact]
	public void GetFilterKey_NoField_IsTheId()
	{
		Column(null, c => c.Add(x => x.Id, "custom-id")).GetFilterKey().Should().Be("custom-id");
	}

	/// <summary>SetValue assigns a value of the property's type directly.</summary>
	[Fact]
	public void SetValue_AssignsMatchingType()
	{
		var row = new Row();

		Column(r => r.Name).SetValue(row, "new");

		row.Name.Should().Be("new");
	}

	/// <summary>SetValue converts from a string for a boxed value-type field.</summary>
	[Fact]
	public void SetValue_ConvertsFromString_ForBoxedValueTypes()
	{
		var row = new Row();

		Column(r => r.Count).SetValue(row, "42");

		row.Count.Should().Be(42);
	}

	/// <summary>SetValue with a null value on a nullable field clears it.</summary>
	[Fact]
	public void SetValue_Null_ClearsANullableField()
	{
		var row = new Row { MaybeCount = 3 };

		Column(r => r.MaybeCount!).SetValue(row, null);

		row.MaybeCount.Should().BeNull();
	}

	/// <summary>SetValue on a calculated column (no field) changes nothing.</summary>
	[Fact]
	public void SetValue_NoField_DoesNothing()
	{
		var row = new Row { Name = "same" };

		Column(null).SetValue(row, "other");

		row.Name.Should().Be("same");
	}

	/// <summary>SetValue rejects a null item.</summary>
	[Fact]
	public void SetValue_NullItem_Throws()
	{
		var act = () => Column(r => r.Name).SetValue(null, "x");

		act.Should().Throw<ArgumentNullException>();
	}

	private static Expression<Func<Row, object>> FieldFor(string property)
	{
		var parameter = Expression.Parameter(typeof(Row), "r");
		var body = Expression.Convert(Expression.Property(parameter, property), typeof(object));
		return Expression.Lambda<Func<Row, object>>(body, parameter);
	}

	/// <summary>A colour, one member of which has a display name.</summary>
	public enum Colour
	{
		/// <summary>Blue.</summary>
		Blue,

		/// <summary>Dark red.</summary>
		[Display(Name = "Dark red")]
		DarkRed
	}

	/// <summary>A nested child object.</summary>
	public sealed class Child
	{
		/// <summary>Gets or sets the child's name.</summary>
		public string Name { get; set; } = string.Empty;
	}

	/// <summary>A row exercising each kind of property the column handles.</summary>
	public sealed class Row
	{
		/// <summary>Gets or sets the key.</summary>
		public int Id { get; set; }

		/// <summary>Gets or sets a text value.</summary>
		public string Name { get; set; } = string.Empty;

		/// <summary>Gets or sets an enum value.</summary>
		public Colour Colour { get; set; }

		/// <summary>Gets or sets a nullable enum value.</summary>
		public Colour? MaybeColour { get; set; }

		/// <summary>Gets or sets a boolean value.</summary>
		public bool Flag { get; set; }

		/// <summary>Gets or sets a nullable boolean value.</summary>
		public bool? MaybeFlag { get; set; }

		/// <summary>Gets or sets a date.</summary>
		public DateTime When { get; set; }

		/// <summary>Gets or sets a nullable date.</summary>
		public DateTime? MaybeWhen { get; set; }

		/// <summary>Gets or sets a date with offset.</summary>
		public DateTimeOffset Stamp { get; set; }

		/// <summary>Gets or sets a nullable date with offset.</summary>
		public DateTimeOffset? MaybeStamp { get; set; }

		/// <summary>Gets or sets a number.</summary>
		public int Count { get; set; }

		/// <summary>Gets or sets a nullable number.</summary>
		public int? MaybeCount { get; set; }

		/// <summary>Gets or sets a nested object.</summary>
		public Child? Child { get; set; }

		/// <summary>Gets or sets a property with an explicit filter key.</summary>
		[FilterKey("custom")]
		public string Keyed { get; set; } = string.Empty;

		/// <summary>Gets or sets a property with a display name and short name.</summary>
		[Display(Name = "Pretty", ShortName = "sn")]
		public string Shorted { get; set; } = string.Empty;
	}

	/// <summary>A provider serving a single row.</summary>
	private sealed class RowProvider : DataProviderBase<Row>
	{
		public override Task<DataResponse<Row>> GetDataAsync(DataRequest<Row> request, CancellationToken cancellationToken)
		{
			ArgumentNullException.ThrowIfNull(request);
			cancellationToken.ThrowIfCancellationRequested();
			List<Row> rows = [new Row { Id = 1, Name = "Alpha" }];
			return Task.FromResult(new DataResponse<Row>(rows, rows.Count));
		}
	}
}

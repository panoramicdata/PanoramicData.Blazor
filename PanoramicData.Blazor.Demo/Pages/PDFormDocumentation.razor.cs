namespace PanoramicData.Blazor.Demo.Pages;

public partial class PDFormDocumentation
{
	private readonly SampleModel _sampleItem = new()
	{
		Name = "John Doe",
		Email = "john@example.com",
		Age = 30
	};

	private const string _example1Code = """
		<PDForm TItem="Person"
		        Item="_person"
		        CssClass="w-100">
		    <PDFormBody TItem="Person">
		        <PDField TItem="Person" Field="x => x.Name" />
		        <PDField TItem="Person" Field="x => x.Email" />
		        <PDField TItem="Person" Field="x => x.Age" />
		    </PDFormBody>
		</PDForm>

		@code {
		    private Person _person = new();
		}
		""";

	private const string _example2Code = """
		<PDForm TItem="Person"
		        Item="_person"
		        CssClass="w-100">
		    <PDFormHeader TItem="Person">
		        <h4>Edit Profile</h4>
		    </PDFormHeader>
		    <PDFormBody TItem="Person">
		        <PDField TItem="Person" 
		                 Field="x => x.Name" 
		                 Title="Full Name" />
		        <PDField TItem="Person" 
		                 Field="x => x.Email" 
		                 Title="Email Address" />
		    </PDFormBody>
		    <PDFormFooter TItem="Person">
		        <button class="btn btn-primary">Save</button>
		        <button class="btn btn-secondary">Cancel</button>
		    </PDFormFooter>
		</PDForm>
		""";

	// Simple model for documentation examples
	public class SampleModel
	{
		public string Name { get; set; } = "";
		public string Email { get; set; } = "";
		public int Age { get; set; }
	}
}

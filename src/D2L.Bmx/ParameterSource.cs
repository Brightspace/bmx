namespace D2L.Bmx;

internal record ParameterSource {
	public string Description { get; init; }

	private ParameterSource( string description ) {
		Description = description;
	}

	public static ParameterSource CliArg => new( "command line argument" );
	public static ParameterSource Config => new( "config file" );

	public override string ToString() => Description;
}

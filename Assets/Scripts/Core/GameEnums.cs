/// <summary>
/// The five prey species. Each one drops exactly one ability once the hunter
/// has eaten enough of them — the mapping lives in an AbilityDefinition asset,
/// never in code, so designers can re-wire progression without a recompile.
/// </summary>
public enum Species
{
	Velociraptor,
	Pterosaur,
	Crocodile,
	Camelsaur,
	Vulturesaur
}

public enum AbilityId
{
	Dash,
	OpponentSearch,
	Swim,
	WaterStorage,
	Wings
}

public enum ZoneType
{
	Forest,
	River,
	Desert,
	Rocks
}

/// <summary>How the hunter is currently getting around. Drives its speed factor.</summary>
public enum LocomotionMode
{
	Ground,
	Swimming,
	Flying
}

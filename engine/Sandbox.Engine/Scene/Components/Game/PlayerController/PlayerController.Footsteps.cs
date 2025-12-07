namespace Sandbox;

public sealed partial class PlayerController : Component
{
	/// <summary>
	/// Draw debug overlay on footsteps
	/// </summary>
	public bool DebugFootsteps;

	TimeSince _timeSinceStep;

	private void OnFootstepEvent( SceneModel.FootstepEvent e )
	{
		if ( !IsOnGround ) return;
		if ( !EnableFootstepSounds ) return;
		if ( _timeSinceStep < 0.2f ) return;

		_timeSinceStep = 0;

		float volume = e.Volume * WishVelocity.Length.Remap( 0, 400, 0, 1 );
		if ( volume <= 0.1f ) return;


	}

}

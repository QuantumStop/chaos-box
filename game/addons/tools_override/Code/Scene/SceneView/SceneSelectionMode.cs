namespace Editor;

class SceneSelectionMode
{
	public Scene Scene { get; }
	public SelectionSystem Selection { get; }

	public Ray FirstRay { get; private set; }
	public Ray Ray { get; private set; }

	int calls = 0;

	Ray previousRay;

	public SceneSelectionMode( Scene scene, SelectionSystem selection )
	{
		Scene = scene;
		Selection = selection;
	}

	internal void Finish( Ray currentRay )
	{
		Ray = currentRay;
		OnDisabled();
	}

	internal void Think( Ray currentRay )
	{
		Ray = currentRay;

		calls++;

		if ( calls == 1 )
		{
			FirstRay = Ray;
			OnEnabled();
		}

		if ( previousRay != currentRay )
		{
			OnMouseMoved();
		}

		OnUpdate();

		previousRay = Ray;
	}

	/// <summary>
	/// Called at start
	/// </summary>
	public virtual void OnEnabled()
	{

	}

	/// <summary>
	/// The mouse has moved, update the selection
	/// </summary>
	public virtual void OnMouseMoved()
	{

	}

	/// <summary>
	///  Called every frame
	/// </summary>
	public virtual void OnUpdate()
	{

	}

	/// <summary>
	/// Called when disabled
	/// </summary>
	public virtual void OnDisabled()
	{

	}
}
